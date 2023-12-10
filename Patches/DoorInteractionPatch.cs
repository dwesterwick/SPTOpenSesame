using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using HarmonyLib;

namespace SPTOpenSesame.Patches
{
    public class DoorInteractionPatch : ModulePatch
    {
        public static Type TargetType { get; set; } = null;
        public static Type ResultType { get; set; } = null;
        public static Type ActionType { get; set; } = null;

        protected override MethodBase GetTargetMethod()
        {
            return TargetType.GetMethod("smethod_9", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref object __result, GamePlayerOwner owner, Door door)
        {
            // Ignore interactions from bots
            if (owner?.Player?.Id != Singleton<GameWorld>.Instance?.MainPlayer?.Id)
            {
                return;
            }

            if (OpenSesamePlugin.WriteMessagesForAllDoors.Value)
            {
                LoggingController.LogInfo("Checking available actions for door: " + door.Id + "...");
            }

            // Don't do anything else unless the door is locked and requires a key
            if ((door.DoorState != EDoorState.Locked) || (door.KeyId == ""))
            {
                return;
            }

            // Create a new action to unlock the door
            var newAction = Activator.CreateInstance(ActionType);

            AccessTools.Field(ActionType, "Name").SetValue(newAction, "Open Sesame");

            UnlockActionWrapper unlockActionWrapper = new UnlockActionWrapper(owner, door);
            AccessTools.Field(ActionType, "Action").SetValue(newAction, new Action(unlockActionWrapper.unlockAction));

            AccessTools.Field(ActionType, "Disabled").SetValue(newAction, !door.Operatable);

            // Add the new action to the context menu for the door
            IList actionList = (IList)AccessTools.Field(ResultType, "Actions").GetValue(__result);
            actionList.Add(newAction);
        }
    }

    internal sealed class UnlockActionWrapper
    {
        public GamePlayerOwner owner;
        public Door door;

        public UnlockActionWrapper(GamePlayerOwner _owner, Door _door)
        {
            owner = _owner;
            door = _door;
        }

        internal void unlockAction()
        {
            if (door == null)
            {
                LoggingController.LogError("Cannot unlock and open a null door");
                return;
            }

            if (OpenSesamePlugin.WriteMessagesWhenUnlockingDoors.Value)
            {
                LoggingController.LogInfo("Unlocking and opening door " + door.Id + " which requires key " + door.KeyId + "...");
            }

            // Unlock the door
            door.DoorState = EDoorState.Shut;
            door.OnEnable();

            owner.Player.MovementContext.ResetCanUsePropState();

            // Open the door
            var gstruct = Door.Interact(this.owner.Player, EInteractionType.Open);
            if (!gstruct.Succeeded)
            {
                return;
            }

            owner.Player.CurrentManagedState.ExecuteDoorInteraction(door, gstruct.Value, null, owner.Player);
        }
    }
}
