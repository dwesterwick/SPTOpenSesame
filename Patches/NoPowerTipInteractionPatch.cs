using EFT;
using EFT.Interactive;
using EFT.UI;
using SPT.Reflection.Patching;
using SPTOpenSesame.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SPTOpenSesame.Patches
{
    public class NoPowerTipInteractionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InteractionContextHelper).GetMethod(
                nameof(InteractionContextHelper.GetAvailableActions),
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[] { typeof(NoPowerTip) },
                null);
        }

        [PatchPostfix]
        protected static void PatchPostfix(ref AvailableInteractionState __result, NoPowerTip noPowerTip)
        {
            if (!OpenSesamePlugin.FeaturesEnabled.Value.HasFlag(OpenSesamePlugin.EFeaturesEnabled.TurnOnPower))
            {
                return;
            }

            // Try to add the "Turn On Power" action to the doors's context menu
            InteractionHelpers.AddTurnOnPowerToActionList(__result);
        }
    }
}
