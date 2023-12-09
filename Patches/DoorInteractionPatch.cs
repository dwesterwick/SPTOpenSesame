using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Interactive;

namespace SPTOpenSesame.Patches
{
    public class DoorInteractionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1726).GetMethod("smethod_9", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref GClass2805 __result, GamePlayerOwner owner, Door door)
        {
            // Ignore interactions from bots
            if ((owner.Player == null) || (owner.Player.Id != Singleton<GameWorld>.Instance.MainPlayer.Id))
            {
                return;
            }

            LoggingController.LogInfo("Checking interaction options for door: " + door.Id + "...");

            if (door.DoorState != EDoorState.Locked)
            {
                return;
            }

            UnlockActionWrapper unlockActionWrapper = new UnlockActionWrapper(owner, door);

            __result.Actions.Add(new GClass2804
            {
                Name = "Open Sesame",
                Action = new Action(unlockActionWrapper.unlockAction),
                Disabled = !door.Operatable
            });
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
                return;
            }

            LoggingController.LogInfo("Unlocking and opening door " + door.Id + " which requires key " + door.KeyId + "...");

            door.DoorState = EDoorState.Shut;
            door.OnEnable();

            owner.Player.MovementContext.ResetCanUsePropState();
            GStruct376<InteractionResult> gstruct = Door.Interact(this.owner.Player, EInteractionType.Open);
            if (!gstruct.Succeeded)
            {
                return;
            }

            owner.Player.CurrentManagedState.ExecuteDoorInteraction(door, gstruct.Value, null, owner.Player);
        }
    }
}
