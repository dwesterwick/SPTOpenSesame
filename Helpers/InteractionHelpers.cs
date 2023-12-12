using System;
using EFT.Interactive;
using EFT;
using HarmonyLib;
using System.Collections;
using Comfort.Common;

namespace SPTOpenSesame.Helpers
{
    internal static class InteractionHelpers
    {
        public static bool isInteractorABot(GamePlayerOwner owner)
        {
            if (owner?.Player?.Id != Singleton<GameWorld>.Instance?.MainPlayer?.Id)
            {
                return true;
            }

            return false;
        }

        public static bool canToggle(this WorldInteractiveObject interactiveObject)
        {
            if (!interactiveObject.Operatable)
            {
                return false;
            }

            if (interactiveObject.DoorState != EDoorState.Shut)
            {
                return false;
            }

            return true;
        }

        public static void addDoNothingToActionList(object actionListObject)
        {
            if (!OpenSesamePlugin.AddDoNothingAction.Value)
            {
                return;
            }

            // Create a new action to do nothing
            var newAction = Activator.CreateInstance(OpenSesamePlugin.ActionType);

            AccessTools.Field(OpenSesamePlugin.ActionType, "Name").SetValue(newAction, "DoNothing");

            InteractiveObjectInteractionWrapper unlockActionWrapper = new InteractiveObjectInteractionWrapper();
            AccessTools.Field(OpenSesamePlugin.ActionType, "Action")
                .SetValue(newAction, new Action(unlockActionWrapper.doNothingAction));

            AccessTools.Field(OpenSesamePlugin.ActionType, "Disabled").SetValue(newAction, false);

            // Add the new action to the context menu for the door
            IList actionList =
                (IList)AccessTools.Field(OpenSesamePlugin.ResultType, "Actions").GetValue(actionListObject);
            actionList.Add(newAction);
        }

        public static void addOpenSesameToActionList(this WorldInteractiveObject interactiveObject,
            object actionListObject, GamePlayerOwner owner)
        {
            // Don't do anything else unless the door is locked and requires a key
            if ((interactiveObject.DoorState != EDoorState.Locked) || (interactiveObject.KeyId == ""))
            {
                return;
            }

            // Add "Do Nothing" to the action list as the default selection
            addDoNothingToActionList(actionListObject);

            // Create a new action to unlock the door
            var newAction = Activator.CreateInstance(OpenSesamePlugin.ActionType);

            AccessTools.Field(OpenSesamePlugin.ActionType, "Name").SetValue(newAction, "OpenSesame");

            InteractiveObjectInteractionWrapper unlockActionWrapper =
                new InteractiveObjectInteractionWrapper(interactiveObject, owner);
            AccessTools.Field(OpenSesamePlugin.ActionType, "Action")
                .SetValue(newAction, new Action(unlockActionWrapper.unlockAndOpenAction));

            AccessTools.Field(OpenSesamePlugin.ActionType, "Disabled")
                .SetValue(newAction, !interactiveObject.Operatable);

            // Add the new action to the context menu for the door
            IList actionList =
                (IList)AccessTools.Field(OpenSesamePlugin.ResultType, "Actions").GetValue(actionListObject);
            actionList.Add(newAction);
        }

        public static void addTurnOnPowerToActionList(this WorldInteractiveObject interactiveObject,
            object actionListObject)
        {
            // Add "Do Nothing" to the action list as the default selection
            addDoNothingToActionList(actionListObject);

            // Create a new action to turn on the power switch
            var newAction = Activator.CreateInstance(OpenSesamePlugin.ActionType);

            AccessTools.Field(OpenSesamePlugin.ActionType, "Name").SetValue(newAction, "TurnOnPower");

            InteractiveObjectInteractionWrapper turnOnPowerActionWrapper =
                new InteractiveObjectInteractionWrapper(OpenSesamePlugin.PowerSwitch);
            AccessTools.Field(OpenSesamePlugin.ActionType, "Action")
                .SetValue(newAction, new Action(turnOnPowerActionWrapper.turnOnAction));

            AccessTools.Field(OpenSesamePlugin.ActionType, "Disabled")
                .SetValue(newAction, !OpenSesamePlugin.PowerSwitch.canToggle());

            // Add the new action to the context menu for the door
            IList actionList =
                (IList)AccessTools.Field(OpenSesamePlugin.ResultType, "Actions").GetValue(actionListObject);
            actionList.Add(newAction);
        }

        internal sealed class InteractiveObjectInteractionWrapper
        {
            public GamePlayerOwner owner;
            public WorldInteractiveObject interactiveObject;

            public InteractiveObjectInteractionWrapper()
            {
            }

            public InteractiveObjectInteractionWrapper(WorldInteractiveObject _interactiveObject) : this()
            {
                interactiveObject = _interactiveObject;
            }

            public InteractiveObjectInteractionWrapper(WorldInteractiveObject _interactiveObject,
                GamePlayerOwner _owner) : this(_interactiveObject)
            {
                owner = _owner;
            }

            internal void doNothingAction()
            {
                LoggingUtil.LogInfo("Nothing happened. What did you expect...?");
            }

            internal void unlockAndOpenAction()
            {
                if (interactiveObject == null)
                {
                    LoggingUtil.LogError("Cannot unlock and open a null object");
                    return;
                }

                if (owner == null)
                {
                    LoggingUtil.LogError("A GamePlayerOwner must be defined to unlock and open object " +
                                         interactiveObject.Id);
                    return;
                }

                if (OpenSesamePlugin.WriteMessagesWhenUnlockingDoors.Value)
                {
                    LoggingUtil.LogInfo("Unlocking interactive object " + interactiveObject.Id +
                                        " which requires key " + interactiveObject.KeyId + "...");
                }

                // Unlock the door
                interactiveObject.DoorState = EDoorState.Shut;
                interactiveObject.OnEnable();

                // Do not open lootable containers like safes, cash registers, etc.
                if ((interactiveObject as LootableContainer) != null)
                {
                    return;
                }

                if (OpenSesamePlugin.WriteMessagesWhenUnlockingDoors.Value)
                {
                    LoggingUtil.LogInfo("Opening interactive object " + interactiveObject.Id + "...");
                }

                owner.Player.MovementContext.ResetCanUsePropState();

                // Open the door
                var gstruct = Door.Interact(this.owner.Player, EInteractionType.Open);
                if (!gstruct.Succeeded)
                {
                    return;
                }

                owner.Player.CurrentManagedState.ExecuteDoorInteraction(interactiveObject, gstruct.Value, null,
                    owner.Player);
            }

            internal void turnOnAction()
            {
                if (interactiveObject == null)
                {
                    LoggingUtil.LogError("Cannot toggle a null switch");
                    return;
                }

                if (!interactiveObject.canToggle())
                {
                    LoggingUtil.LogWarning("Cannot interact with object " + interactiveObject.Id + " right now");
                    return;
                }

                if (OpenSesamePlugin.WriteMessagesWhenTogglingSwitches.Value)
                {
                    LoggingUtil.LogInfo("Toggling object " + interactiveObject.Id + "...");
                }

                Player you = Singleton<GameWorld>.Instance.MainPlayer;
                you.CurrentManagedState.ExecuteDoorInteraction(interactiveObject,
                    new InteractionResult(EInteractionType.Open), null, you);
            }
        }
    }
}