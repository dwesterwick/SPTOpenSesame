using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using SPT.Reflection.Patching;
using SPTOpenSesame.Helpers;
using SPTOpenSesame.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SPTOpenSesame.Patches
{
    public class SwitchInteractionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InteractionContextHelper).GetMethod(
                nameof(InteractionContextHelper.GetAvailableActions),
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[] { typeof(GamePlayerOwner), typeof(Switch) },
                null);
        }

        [PatchPostfix]
        protected static void PatchPostfix(ref AvailableInteractionState __result, GamePlayerOwner owner, Switch interactiveSwitch)
        {
            // Ignore interactions from bots
            if (InteractionHelpers.IsInteractorABot(owner))
            {
                return;
            }

            if (OpenSesamePlugin.DebugMessagesEnabled.Value.HasFlag(OpenSesamePlugin.EDebugMessagesEnabled.DoorInteractions))
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Checking available actions for switch " + interactiveSwitch.Id + "...");
            }

            if (!OpenSesamePlugin.FeaturesEnabled.Value.HasFlag(OpenSesamePlugin.EFeaturesEnabled.UnlockDoors))
            {
                return;
            }

            // Try to add the "Open Sesame" action to the switch's context menu
            interactiveSwitch.AddOpenSesameToSwitchActionList(ref __result, owner);

            // If no actions are available, check if a power switch needs to be turned on to use it
            addTurnOnPowerAction(ref __result, interactiveSwitch);
        }

        private static void addTurnOnPowerAction(ref AvailableInteractionState __result, Switch interactiveSwitch)
        {
            // Check if the switch position has changed during the raid
            if (interactiveSwitch.DoorState != interactiveSwitch.InitialDoorState)
            {
                return;
            }

            // Check if there are any other actions available in the context menu
            if (__result?.Actions?.Count > 0)
            {
                return;
            }

            // Check if a power switch must be toggled for this switch to be toggled
            if (interactiveSwitch.PreviousSwitch?.IsPowerSwitch() != true)
            {
                return;
            }

            InteractionHelpers.AddTurnOnPowerToActionList(ref __result);
        }
    }
}
