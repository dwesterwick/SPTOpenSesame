using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using SPTOpenSesame.Components;
using SPTOpenSesame.Utils;
using System;
using System.Collections;
using System.Linq;

namespace SPTOpenSesame.Helpers
{
    public static class InteractionHelpers
    {
        public static bool IsInteractorABot(GamePlayerOwner owner)
        {
            if (owner?.Player?.Id != Singleton<GameWorld>.Instance?.MainPlayer?.Id)
            {
                return true;
            }

            return false;
        }

        public static bool CanToggle(this WorldInteractiveObject interactiveObject)
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

        public static void AddDoNothingToActionList(AvailableInteractionState actionListObject)
        {
            if (!OpenSesamePlugin.FeaturesEnabled.Value.HasFlag(OpenSesamePlugin.EFeaturesEnabled.DoNothing))
            {
                return;
            }

            // Create a new action to do nothing
            InteractiveObjectInteractionWrapper interactiveObjectInteractionWrapper = new InteractiveObjectInteractionWrapper();
            InteractionAction newAction = new InteractionAction
            {
                Name = "DoNothing",
                Action = new Action(interactiveObjectInteractionWrapper.doNothingAction),
                Disabled = false
            };

            // Add the new action to the context menu for the door
            actionListObject.Actions.Add(newAction);
        }

        public static void AddOpenSesameToActionList(this WorldInteractiveObject interactiveObject, AvailableInteractionState actionListObject, GamePlayerOwner owner)
        {
            // Don't do anything else unless the door is locked and requires a key
            if ((interactiveObject.DoorState != EDoorState.Locked) || (interactiveObject.KeyId == ""))
            {
                return;
            }

            // Add "Do Nothing" to the action list as the default selection
            AddDoNothingToActionList(actionListObject);

            // Create a new action to unlock the door
            InteractiveObjectInteractionWrapper interactiveObjectInteractionWrapper = new InteractiveObjectInteractionWrapper(interactiveObject, owner);
            InteractionAction newAction = new InteractionAction
            {
                Name = "OpenSesame",
                Action = new Action(interactiveObjectInteractionWrapper.unlockAndOpenAction),
                Disabled = !interactiveObject.Operatable
            };

            // Add the new action to the context menu for the door
            actionListObject.Actions.Add(newAction);
        }

        public static void AddTurnOnPowerToActionList(AvailableInteractionState actionListObject)
        {
            // Add "Do Nothing" to the action list as the default selection
            AddDoNothingToActionList(actionListObject);

            // Find the power switch
            Switch powerSwitch = Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<PowerSwitchIdentificationComponent>().PowerSwitch;
            InteractiveObjectInteractionWrapper turnOnPowerActionWrapper = new InteractiveObjectInteractionWrapper(powerSwitch);

            // Create a new action to turn on the power switch
            InteractionAction newAction = new InteractionAction
            {
                Name = "TurnOnPower",
                Action = new Action(turnOnPowerActionWrapper.turnOnAction),
                Disabled = !powerSwitch.Operatable
            };

            // Add the new action to the context menu for the door
            actionListObject.Actions.Add(newAction);
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

            public InteractiveObjectInteractionWrapper(WorldInteractiveObject _interactiveObject, GamePlayerOwner _owner) : this(_interactiveObject)
            {
                owner = _owner;
            }

            internal void doNothingAction()
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Nothing happened. What did you expect...?");
            }

            internal void unlockAndOpenAction()
            {
                if (interactiveObject == null)
                {
                    Singleton<LoggingUtil>.Instance.LogError("Cannot unlock and open a null object");
                    return;
                }

                if (owner == null)
                {
                    Singleton<LoggingUtil>.Instance.LogError("A GamePlayerOwner must be defined to unlock and open object " + interactiveObject.Id);
                    return;
                }

                if (OpenSesamePlugin.DebugMessagesEnabled.Value.HasFlag(OpenSesamePlugin.EDebugMessagesEnabled.UnlockingDoors))
                {
                    Singleton<LoggingUtil>.Instance.LogInfo("Unlocking interactive object " + interactiveObject.Id + " which requires key " + interactiveObject.KeyId + "...");
                }

                // Unlock the door
                interactiveObject.DoorState = EDoorState.Shut;
                interactiveObject.OnEnable();

                // Do not open lootable containers like safes, cash registers, etc.
                if ((interactiveObject as LootableContainer) != null)
                {
                    return;
                }

                if (OpenSesamePlugin.DebugMessagesEnabled.Value.HasFlag(OpenSesamePlugin.EDebugMessagesEnabled.UnlockingDoors))
                {
                    Singleton<LoggingUtil>.Instance.LogInfo("Opening interactive object " + interactiveObject.Id + "...");
                }

                owner.Player.MovementContext.ResetCanUsePropState();

                // Open the door
                var gstruct = Door.Interact(this.owner.Player, EInteractionType.Open);
                if (!gstruct.Succeeded)
                {
                    return;
                }

                owner.Player.CurrentManagedState.ExecuteDoorInteraction(interactiveObject, gstruct.Value, null, owner.Player);
            }

            internal void turnOnAction()
            {
                if (interactiveObject == null)
                {
                    Singleton<LoggingUtil>.Instance.LogError("Cannot toggle a null switch");
                    return;
                }

                if (!interactiveObject.CanToggle())
                {
                    Singleton<LoggingUtil>.Instance.LogWarning("Cannot interact with object " + interactiveObject.Id + " right now");
                    return;
                }

                if (OpenSesamePlugin.DebugMessagesEnabled.Value.HasFlag(OpenSesamePlugin.EDebugMessagesEnabled.TogglingSwitches))
                {
                    Singleton<LoggingUtil>.Instance.LogInfo("Toggling object " + interactiveObject.Id + "...");
                }

                Player you = Singleton<GameWorld>.Instance.MainPlayer;
                you.CurrentManagedState.ExecuteDoorInteraction(interactiveObject, new InteractionResult(EInteractionType.Open), null, you);
            }
        }
    }
}