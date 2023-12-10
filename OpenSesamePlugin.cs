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
    [BepInPlugin("com.DanW.OpenSesame", "DanW-OpenSesame", "1.0.0")]
    public class OpenSesamePlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> WriteMessagesForAllDoors;
        public static ConfigEntry<bool> WriteMessagesWhenUnlockingDoors;

        private void Awake()
        {
            Logger.LogInfo("Loading OpenSesame...");

            LoggingController.Logger = Logger;

            findTypes();

            new Patches.DoorInteractionPatch().Enable();

            addConfigOptions();

            Logger.LogInfo("Loading OpenSesame...done.");
        }

        private void addConfigOptions()
        {
            WriteMessagesForAllDoors = Config.Bind("Main", "Write messages for all doors",
                false, "Write a debug message to the game console when the context menu for doors is displayed");

            WriteMessagesWhenUnlockingDoors = Config.Bind("Main", "Write messages when unlocking doors",
                false, "Write a debug message to the game console when you use this mod to unlock a door");
        }

        private void findTypes()
        {
            Type[] targetTypeOptions = Aki.Reflection.Utils.PatchConstants.EftTypes.Where(t => t.GetMethods().Any(m => m.Name.Contains("GetAvailableActions"))).ToArray();
            if (targetTypeOptions.Length != 1)
            {
                throw new TypeLoadException("Cannot find target method");
            }

            Patches.DoorInteractionPatch.TargetType = targetTypeOptions[0];
            LoggingController.LogInfo("Target type: " + Patches.DoorInteractionPatch.TargetType);
            
            Patches.DoorInteractionPatch.ResultType = AccessTools.FirstMethod(Patches.DoorInteractionPatch.TargetType, m => m.Name.Contains("GetAvailableActions")).ReturnType;
            LoggingController.LogInfo("Return type: " + Patches.DoorInteractionPatch.ResultType.FullName);

            Patches.DoorInteractionPatch.ActionType = AccessTools.Field(Patches.DoorInteractionPatch.ResultType, "SelectedAction").FieldType;
            LoggingController.LogInfo("Action type: " + Patches.DoorInteractionPatch.ActionType.FullName);
        }
    }
}
