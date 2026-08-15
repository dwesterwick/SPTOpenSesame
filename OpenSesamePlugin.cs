using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using SPTOpenSesame.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SPTOpenSesame
{
    [BepInPlugin("com.danw.opensesame", "DanW-OpenSesame", "3.0.0")]
    public class OpenSesamePlugin : BaseUnityPlugin
    {
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
            Singleton<LoggingUtil>.Create(new LoggingUtil(Logger));

            new Patches.InteractiveObjectInteractionPatch().Enable();
            new Patches.KeycardDoorInteractionPatch().Enable();
            new Patches.NoPowerTipInteractionPatch().Enable();

            addConfigOptions();

            // Add a listener to automatically add translations when EFT first loads and when the user switches languages
            Singleton<LoggingUtil>.Instance.LogInfo("Adding locale update listener...");
            LocalizationManager.Instance.AddLocaleUpdateListener(Helpers.LocalizationHelpers.AddNewTranslationsForLoadedLocales);

            Singleton<LoggingUtil>.Instance.LogInfo("Loading OpenSesame...done.");
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
