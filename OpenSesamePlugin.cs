using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace SPTOpenSesame
{
    [BepInPlugin("com.DanW.OpenSesame", "DanW-OpenSesame", "1.1.0")]
    public class OpenSesamePlugin : BaseUnityPlugin
    {
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
            new Patches.NoPowerTipInteractionPatch().Enable();
            new Patches.DoorInteractionPatch().Enable();
            new Patches.KeycardDoorInteractionPatch().Enable();

            addConfigOptions();

            Logger.LogInfo("Loading OpenSesame...done.");
        }

        private void addConfigOptions()
        {
            WriteMessagesForAllDoors = Config.Bind("Main", "Write messages for all doors",
                false, "Write a debug message to the game console when the context menu for doors is displayed");

            WriteMessagesWhenUnlockingDoors = Config.Bind("Main", "Write messages when unlocking doors",
                false, "Write a debug message to the game console when you use this mod to unlock a door");

            WriteMessagesWhenTogglingSwitches = Config.Bind("Main", "Write messages when toggling switches",
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
