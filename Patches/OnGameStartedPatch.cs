using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;
using EFT.Interactive;

namespace SPTOpenSesame.Patches
{
    public class OnGameStartedPatch: ModulePatch
    {
        private static string[] powerSwitchIds = new string[]
        {
            "custom_DesignStuff_00034",
            "Shopping_Mall_DesignStuff_00055"
        };

        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(GameWorld __instance)
        {
            Switch[] powerSwitches = UnityEngine.Object.FindObjectsOfType<Switch>().Where(s => powerSwitchIds.Contains(s.Id)).ToArray();
            foreach (Switch powerSwitch in powerSwitches)
            {
                LoggingController.LogInfo("Found power switch " + powerSwitch.Id);
                OpenSesamePlugin.PowerSwitch = powerSwitch;
            }
        }
    }
}
