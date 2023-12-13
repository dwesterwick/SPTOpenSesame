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
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("OnGameStarted", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(GameWorld __instance)
        {
            IEnumerable<Switch> powerSwitches = UnityEngine.Object
                .FindObjectsOfType<Switch>()
                .Where(s => OpenSesamePlugin.PowerSwitchIds.Contains(s.Id));
            
            foreach (Switch powerSwitch in powerSwitches)
            {
                Helpers.LoggingUtil.LogInfo("Found power switch " + powerSwitch.Id);
                OpenSesamePlugin.PowerSwitch = powerSwitch;
            }
        }
    }
}
