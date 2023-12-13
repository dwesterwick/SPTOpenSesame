using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;

namespace SPTOpenSesame
{
    [BepInPlugin("com.DanW.OpenSesame", "DanW-OpenSesame", "2.1.0")]
    public class OpenSesamePlugin : BaseUnityPlugin
    {
        public static string[] PowerSwitchIds = new string[]
        {
            "custom_DesignStuff_00034",
            "Shopping_Mall_DesignStuff_00055"
        };

        public static EFT.Interactive.Switch PowerSwitch { get; set; } = null;

        public static ConfigEntry<bool> AddNewActions;
        public static ConfigEntry<bool> AddDoNothingAction;
        public static ConfigEntry<bool> WriteMessagesForAllDoors;
        public static ConfigEntry<bool> WriteMessagesWhenUnlockingDoors;
        public static ConfigEntry<bool> WriteMessagesWhenTogglingSwitches;

        private void Awake()
        {
            Logger.LogInfo("Loading OpenSesame...");

            Helpers.LoggingUtil.Logger = Logger;

            Helpers.InteractionHelpers.LoadTypes();
            Helpers.LocalizationUtil.LoadTypes();

            new Patches.OnGameStartedPatch().Enable();
            new Patches.GameWorldOnDestroyPatch().Enable();
            new Patches.InteractiveObjectInteractionPatch().Enable();
            new Patches.KeycardDoorInteractionPatch().Enable();
            new Patches.NoPowerTipInteractionPatch().Enable();

            addConfigOptions();

            Helpers.LocalizationUtil.AddLocaleUpdateListener(Helpers.LocalizationUtil.AddNewTranslations);

            Logger.LogInfo("Loading OpenSesame...done.");
        }

        private void addConfigOptions()
        {
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
    }
}
