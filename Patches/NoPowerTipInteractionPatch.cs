using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using EFT.Interactive;
using SPTOpenSesame.Helpers;

namespace SPTOpenSesame.Patches
{
    public class NoPowerTipInteractionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return InteractionHelpers.TargetType.GetMethod("smethod_18", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPostfix]
        protected static void PatchPostfix(ref object __result, NoPowerTip noPowerTip)
        {
            if (!OpenSesamePlugin.FeaturesEnabled.Value.HasFlag(OpenSesamePlugin.EFeaturesEnabled.TurnOnPower))
            {
                return;
            }

            // Try to add the "Turn On Power" action to the doors's context menu
            OpenSesamePlugin.PowerSwitch.AddTurnOnPowerToActionList(__result);
        }
    }
}
