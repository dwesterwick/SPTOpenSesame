using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;

namespace SPTOpenSesame
{
    [BepInPlugin("com.DanW.OpenSesame", "DanW-OpenSesame", "2.5.0")]
    public class OpenSesamePlugin : BaseUnityPlugin
    {
        public static string[] PowerSwitchIds = new string[]
        {
            "custom_DesignStuff_00034",
            "Shopping_Mall_DesignStuff_00055"
        };

        public static EFT.Interactive.Switch PowerSwitch { get; set; } = null;

        [Flags]
        public enum EFeaturesEnabled
        {
            UnlockDoors = 1,
            TurnOnPower = 2,
            DoNothing = 4,

            All = UnlockDoors | TurnOnPower | DoNothing,
        }

        [Flags]
        public enum EDebugMessagesEnabled
        {
            DoorInteractions = 1,
            UnlockingDoors = 2,
            TogglingSwitches = 4,

            All = DoorInteractions | UnlockingDoors | TogglingSwitches,
        }

        public static ConfigEntry<EFeaturesEnabled> FeaturesEnabled;
        public static ConfigEntry<EDebugMessagesEnabled> DebugMessagesEnabled;

        protected void Awake()
        {
            Logger.LogInfo("Loading OpenSesame...");

            Helpers.LoggingUtil.Logger = Logger;

            // Find types so we don't need to use GClasses
            Helpers.InteractionHelpers.FindTypes();
            Helpers.LocalizationUtil.FindTypes();

            new Patches.OnGameStartedPatch().Enable();
            new Patches.GameWorldOnDestroyPatch().Enable();
            new Patches.InteractiveObjectInteractionPatch().Enable();
            new Patches.KeycardDoorInteractionPatch().Enable();
            new Patches.NoPowerTipInteractionPatch().Enable();

            addConfigOptions();

            // Add a listener to automatically add translations when EFT first loads and when the user switches languages
            Helpers.LocalizationUtil.AddLocaleUpdateListener(Helpers.LocalizationUtil.AddNewTranslationsForLoadedLocales);

            Logger.LogInfo("Loading OpenSesame...done.");
        }

        private void addConfigOptions()
        {
            FeaturesEnabled = Config.Bind("Main", "Enabled Features",
                EFeaturesEnabled.All, "Enabled features of this mod");

            DebugMessagesEnabled = Config.Bind("Main", "Enabled Debug Messages",
                (EDebugMessagesEnabled)0, "Enabled debugging messages");
        }
    }
}
