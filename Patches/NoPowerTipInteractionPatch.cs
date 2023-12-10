using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT.Interactive;
using EFT;
using HarmonyLib;

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
            // Create a new action to turn on the power switch
            var newAction = Activator.CreateInstance(OpenSesamePlugin.ActionType);

            AccessTools.Field(OpenSesamePlugin.ActionType, "Name").SetValue(newAction, "Turn On Power");

            TurnOnPowerActionWrapper turnOnPowerActionWrapper = new TurnOnPowerActionWrapper( OpenSesamePlugin.PowerSwitch);
            AccessTools.Field(OpenSesamePlugin.ActionType, "Action").SetValue(newAction, new Action(turnOnPowerActionWrapper.turnOnPowerAction));

            AccessTools.Field(OpenSesamePlugin.ActionType, "Disabled").SetValue(newAction, !canToggleSwitch(OpenSesamePlugin.PowerSwitch));

            // Add the new action to the context menu for the door
            IList actionList = (IList)AccessTools.Field(OpenSesamePlugin.ResultType, "Actions").GetValue(__result);
            actionList.Add(newAction);
        }

        private static bool canToggleSwitch(Switch sw)
        {
            if (!sw.Operatable)
            {
                return false;
            }

            if (sw.DoorState != EDoorState.Shut)
            {
                return false;
            }

            return true;
        }

        internal sealed class TurnOnPowerActionWrapper
        {
            public Switch sw;

            public TurnOnPowerActionWrapper(Switch _sw)
            {
                sw = _sw;
            }

            internal void turnOnPowerAction()
            {
                if (sw == null)
                {
                    LoggingController.LogError("Cannot toggle a null switch");
                    return;
                }

                if (sw.DoorState == EDoorState.Open)
                {
                    LoggingController.LogWarning("Switch" + sw.Id + " is already turned on");
                    return;
                }

                if (sw.DoorState == EDoorState.Interacting)
                {
                    LoggingController.LogWarning("Somebody is already interacting with " + sw.Id);
                    return;
                }

                if (OpenSesamePlugin.WriteMessagesWhenTogglingSwitches.Value)
                {
                    LoggingController.LogInfo("Toggling switch " + sw.Id + "...");
                }

                Player you = Singleton<GameWorld>.Instance.MainPlayer;
                you.CurrentManagedState.ExecuteDoorInteraction(sw, new InteractionResult(EInteractionType.Open), null, you);
            }
        }
    }
}
