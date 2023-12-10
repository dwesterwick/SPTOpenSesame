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
            InteractiveSubscriber[] allSubscribers = UnityEngine.Object.FindObjectsOfType<InteractiveSubscriber>();
            foreach (InteractiveSubscriber subscribers in allSubscribers)
            {
                Switch sw = subscribers.Subscribee as Switch;
                if (sw == null)
                {
                    continue;
                }

                if (sw.ContextMenuTip.Localized().ToLower().Contains("restore power supply"))
                {
                    LoggingController.LogInfo("Found power switch " + sw.Id);
                }
            }
        }
    }
}
