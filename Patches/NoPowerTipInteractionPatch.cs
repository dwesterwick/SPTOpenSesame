using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT.Interactive;
using SPTOpenSesame.Helpers;

namespace SPTOpenSesame.Patches
{
    public class NoPowerTipInteractionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return OpenSesamePlugin.TargetType.GetMethod("smethod_13", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref object __result, NoPowerTip noPowerTip)
        {
            if (!OpenSesamePlugin.AddNewActions.Value)
            {
                return;
            }

            // Try to add the "Turn On Power" action to the doors's context menu
            OpenSesamePlugin.PowerSwitch.addTurnOnPowerToActionList(__result);
        }
    }
}
