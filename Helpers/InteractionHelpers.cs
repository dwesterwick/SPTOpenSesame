using System;
using System.Collections;
using System.Linq;
using Comfort.Common;
using EFT.Interactive;
using EFT;
using HarmonyLib;

namespace SPTOpenSesame.Helpers
{
    public static class InteractionHelpers
    {
        public static Type TargetType { get; private set; } = null;

        private static Type resultType = null;
        private static Type actionType = null;

        public static void FindTypes()
        {
            // Find the class that generates the context menus for each object type
            string methodName = "GetAvailableActions";
            Type[] targetTypeOptions = SPT.Reflection.Utils.PatchConstants.EftTypes.Where(t => t.GetMethods().Any(m => m.Name.Contains(methodName))).ToArray();
            if (targetTypeOptions.Length != 1)
            {
                throw new TypeLoadException("Cannot find type containing method " + methodName);
            }

            TargetType = targetTypeOptions[0];
            LoggingUtil.LogInfo("Target type: " + TargetType.FullName);

            // Find the class containing the context menu
            resultType = AccessTools.FirstMethod(TargetType, m => m.Name.Contains(methodName)).ReturnType;
            LoggingUtil.LogInfo("Return type: " + resultType.FullName);

            // Find the class representing each action in the context menu
            actionType = AccessTools.Field(resultType, "SelectedAction").FieldType;
            LoggingUtil.LogInfo("Action type: " + actionType.FullName);
        }

        public static bool HaveTypesBeenFound()
        {
            if ((TargetType == null) || (resultType == null) || (actionType == null))
            {
                return false;
            }

            return true;
        }

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

        public static void AddDoNothingToActionList(object actionListObject)
        {
            if (!OpenSesamePlugin.FeaturesEnabled.Value.HasFlag(OpenSesamePlugin.EFeaturesEnabled.DoNothing))
            {
                return;
            }

            if (!HaveTypesBeenFound())
            {
                throw new TypeLoadException("Types have not been loaded");
            }

            // Create a new action to do nothing
            var newAction = Activator.CreateInstance(actionType);

            AccessTools.Field(actionType, "Name").SetValue(newAction, "DoNothing");

            InteractiveObjectInteractionWrapper unlockActionWrapper = new InteractiveObjectInteractionWrapper();
            AccessTools.Field(actionType, "Action").SetValue(newAction, new Action(unlockActionWrapper.doNothingAction));

            AccessTools.Field(actionType, "Disabled").SetValue(newAction, false);

            // Add the new action to the context menu for the door
            IList actionList = (IList)AccessTools.Field(resultType, "Actions").GetValue(actionListObject);
            actionList.Add(newAction);
        }

        public static void AddOpenSesameToActionList(this WorldInteractiveObject interactiveObject, object actionListObject, GamePlayerOwner owner)
        {
            // Don't do anything else unless the door is locked and requires a key
            if ((interactiveObject.DoorState != EDoorState.Locked) || (interactiveObject.KeyId == ""))
            {
                return;
            }

            if (!HaveTypesBeenFound())
            {
                throw new TypeLoadException("Types have not been loaded");
            }

            // Add "Do Nothing" to the action list as the default selection
            AddDoNothingToActionList(actionListObject);

            // Create a new action to unlock the door
            var newAction = Activator.CreateInstance(actionType);

            AccessTools.Field(actionType, "Name").SetValue(newAction, "OpenSesame");

            InteractiveObjectInteractionWrapper unlockActionWrapper = new InteractiveObjectInteractionWrapper(interactiveObject, owner);
            AccessTools.Field(actionType, "Action").SetValue(newAction, new Action(unlockActionWrapper.unlockAndOpenAction));

            AccessTools.Field(actionType, "Disabled").SetValue(newAction, !interactiveObject.Operatable);

            // Add the new action to the context menu for the door
            IList actionList = (IList)AccessTools.Field(resultType, "Actions").GetValue(actionListObject);
            actionList.Add(newAction);
        }

        public static void AddTurnOnPowerToActionList(this WorldInteractiveObject interactiveObject, object actionListObject)
        {
            if (!HaveTypesBeenFound())
            {
                throw new TypeLoadException("Types have not been loaded");
            }

            // Add "Do Nothing" to the action list as the default selection
            AddDoNothingToActionList(actionListObject);

            // Create a new action to turn on the power switch
            var newAction = Activator.CreateInstance(actionType);

            AccessTools.Field(actionType, "Name").SetValue(newAction, "TurnOnPower");

            InteractiveObjectInteractionWrapper turnOnPowerActionWrapper = new InteractiveObjectInteractionWrapper(OpenSesamePlugin.PowerSwitch);
            AccessTools.Field(actionType, "Action").SetValue(newAction, new Action(turnOnPowerActionWrapper.turnOnAction));

            AccessTools.Field(actionType, "Disabled").SetValue(newAction, !OpenSesamePlugin.PowerSwitch.CanToggle());

            // Add the new action to the context menu for the door
            IList actionList = (IList)AccessTools.Field(resultType, "Actions").GetValue(actionListObject);
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

            public InteractiveObjectInteractionWrapper(WorldInteractiveObject _interactiveObject, GamePlayerOwner _owner) : this(_interactiveObject)
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
                    LoggingUtil.LogError("A GamePlayerOwner must be defined to unlock and open object " + interactiveObject.Id);
                    return;
                }

                if (OpenSesamePlugin.DebugMessagesEnabled.Value.HasFlag(OpenSesamePlugin.EDebugMessagesEnabled.UnlockingDoors))
                {
                    LoggingUtil.LogInfo("Unlocking interactive object " + interactiveObject.Id + " which requires key " + interactiveObject.KeyId + "...");
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
                    LoggingUtil.LogInfo("Opening interactive object " + interactiveObject.Id + "...");
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
                    LoggingUtil.LogError("Cannot toggle a null switch");
                    return;
                }

                if (!interactiveObject.CanToggle())
                {
                    LoggingUtil.LogWarning("Cannot interact with object " + interactiveObject.Id + " right now");
                    return;
                }

                if (OpenSesamePlugin.DebugMessagesEnabled.Value.HasFlag(OpenSesamePlugin.EDebugMessagesEnabled.TogglingSwitches))
                {
                    LoggingUtil.LogInfo("Toggling object " + interactiveObject.Id + "...");
                }

                Player you = Singleton<GameWorld>.Instance.MainPlayer;
                you.CurrentManagedState.ExecuteDoorInteraction(interactiveObject, new InteractionResult(EInteractionType.Open), null, you);
            }
        }
    }
}