﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public static void addOpenSesameToActionList(this WorldInteractiveObject interactiveObject, object actionListObject, GamePlayerOwner owner)
        {
            // Don't do anything else unless the door is locked and requires a key
            if ((interactiveObject.DoorState != EDoorState.Locked) || (interactiveObject.KeyId == ""))
            {
                return;
            }

            // Create a new action to unlock the door
            var newAction = Activator.CreateInstance(OpenSesamePlugin.ActionType);

            AccessTools.Field(OpenSesamePlugin.ActionType, "Name").SetValue(newAction, "Open Sesame");

            InteractiveObjectInteractionWrapper unlockActionWrapper = new InteractiveObjectInteractionWrapper(interactiveObject, owner);
            AccessTools.Field(OpenSesamePlugin.ActionType, "Action").SetValue(newAction, new Action(unlockActionWrapper.unlockAndOpenAction));

            AccessTools.Field(OpenSesamePlugin.ActionType, "Disabled").SetValue(newAction, !interactiveObject.Operatable);

            // Add the new action to the context menu for the door
            IList actionList = (IList)AccessTools.Field(OpenSesamePlugin.ResultType, "Actions").GetValue(actionListObject);
            actionList.Add(newAction);
        }

        public static void addTurnOnPowerToActionList(this WorldInteractiveObject interactiveObject, object actionListObject)
        {
            // Create a new action to turn on the power switch
            var newAction = Activator.CreateInstance(OpenSesamePlugin.ActionType);

            AccessTools.Field(OpenSesamePlugin.ActionType, "Name").SetValue(newAction, "Turn On Power");

            InteractiveObjectInteractionWrapper turnOnPowerActionWrapper = new InteractiveObjectInteractionWrapper(OpenSesamePlugin.PowerSwitch);
            AccessTools.Field(OpenSesamePlugin.ActionType, "Action").SetValue(newAction, new Action(turnOnPowerActionWrapper.turnOnAction));

            AccessTools.Field(OpenSesamePlugin.ActionType, "Disabled").SetValue(newAction, !OpenSesamePlugin.PowerSwitch.canToggle());

            // Add the new action to the context menu for the door
            IList actionList = (IList)AccessTools.Field(OpenSesamePlugin.ResultType, "Actions").GetValue(actionListObject);
            actionList.Add(newAction);
        }

        internal sealed class InteractiveObjectInteractionWrapper
        {
            public GamePlayerOwner owner;
            public WorldInteractiveObject interactiveObject;

            public InteractiveObjectInteractionWrapper(WorldInteractiveObject _interactiveObject)
            {
                interactiveObject = _interactiveObject;
            }

            public InteractiveObjectInteractionWrapper(WorldInteractiveObject _interactiveObject, GamePlayerOwner _owner) : this(_interactiveObject)
            {
                owner = _owner;
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
                    LoggingUtil.LogError("A GamePlayerOwner must be defined to unlock and open object " + interactiveObject.Id);
                    return;
                }

                if (OpenSesamePlugin.WriteMessagesWhenUnlockingDoors.Value)
                {
                    LoggingUtil.LogInfo("Unlocking and opening interactive object " + interactiveObject.Id + " which requires key " + interactiveObject.KeyId + "...");
                }

                // Unlock the door
                interactiveObject.DoorState = EDoorState.Shut;
                interactiveObject.OnEnable();

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
                you.CurrentManagedState.ExecuteDoorInteraction(interactiveObject, new InteractionResult(EInteractionType.Open), null, you);
            }
        }
    }
}
