﻿using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SPTOpenSesame.Helpers;

namespace SPTOpenSesame
{
    [BepInPlugin("com.DanW.OpenSesame", "DanW-OpenSesame", "2.0.0")]
    public class OpenSesamePlugin : BaseUnityPlugin
    {
        public static string[] PowerSwitchIds = new string[]
        {
            "custom_DesignStuff_00034",
            "Shopping_Mall_DesignStuff_00055"
        };

        public static ConfigEntry<Language> NewActionsLanguage;
        public static ConfigEntry<bool> AddNewActions;
        public static ConfigEntry<bool> AddDoNothingAction;
        public static ConfigEntry<bool> WriteMessagesForAllDoors;
        public static ConfigEntry<bool> WriteMessagesWhenUnlockingDoors;
        public static ConfigEntry<bool> WriteMessagesWhenTogglingSwitches;

        public static Type TargetType { get; set; } = null;
        public static Type ResultType { get; set; } = null;
        public static Type ActionType { get; set; } = null;

        public static EFT.Interactive.Switch PowerSwitch { get; set; } = null;

        private void Awake()
        {
            Logger.LogInfo("Loading OpenSesame...");

            Helpers.LoggingUtil.Logger = Logger;

            findTypes();

            new Patches.OnGameStartedPatch().Enable();
            new Patches.GameWorldOnDestroyPatch().Enable();
            new Patches.InteractiveObjectInteractionPatch().Enable();
            new Patches.KeycardDoorInteractionPatch().Enable();
            new Patches.NoPowerTipInteractionPatch().Enable();
            
            addConfigOptions();

            Logger.LogInfo("Loading OpenSesame...done.");
        }

        private void addConfigOptions()
        {
            NewActionsLanguage = Config.Bind("Main", "Actions Language", Language.English,
                "The display text of new actions");
            
            AddNewActions = Config.Bind("Main", "Add new actions to menus",
                true, "Adds new actions to context menus where applicable");

            AddDoNothingAction = Config.Bind("Main", "Add Do-Nothing action to menus",
                true, "Adds the \"Do Nothing\" action to context menus where applicable so you don't accidentally unlock things");

            WriteMessagesForAllDoors = Config.Bind("Logging", "Write messages for all doors",
                false, "Write a debug message to the game console when the context menu for doors is displayed");

            WriteMessagesWhenUnlockingDoors = Config.Bind("Logging", "Write messages when unlocking doors",
                false, "Write a debug message to the game console when you use this mod to unlock a door");

            WriteMessagesWhenTogglingSwitches = Config.Bind("Logging", "Write messages when toggling switches",
                false, "Write a debug message to the game console when you toggle a switch");
        }

        private void findTypes()
        {
            Type[] targetTypeOptions = Aki.Reflection.Utils.PatchConstants.EftTypes.Where(t => t.GetMethods().Any(m => m.Name.Contains("GetAvailableActions"))).ToArray();
            if (targetTypeOptions.Length != 1)
            {
                throw new TypeLoadException("Cannot find target method");
            }

            TargetType = targetTypeOptions[0];
            Helpers.LoggingUtil.LogInfo("Target type: " + TargetType);
            
            ResultType = AccessTools.FirstMethod(TargetType, m => m.Name.Contains("GetAvailableActions")).ReturnType;
            Helpers.LoggingUtil.LogInfo("Return type: " + ResultType.FullName);

            ActionType = AccessTools.Field(ResultType, "SelectedAction").FieldType;
            Helpers.LoggingUtil.LogInfo("Action type: " + ActionType.FullName);
        }
    }
}
