using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;
using SPTOpenSesame.Helpers;

namespace SPTOpenSesame.Patches
{
    public class InteractiveObjectInteractionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return InteractionHelpers.TargetType.GetMethod("smethod_3", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref object __result, GamePlayerOwner owner, WorldInteractiveObject worldInteractiveObject)
        {
            // Ignore interactions from bots
            if (InteractionHelpers.IsInteractorABot(owner))
            {
                return;
            }

            if (OpenSesamePlugin.DebugMessagesEnabled.Value.HasFlag(OpenSesamePlugin.EDebugMessagesEnabled.DoorInteractions))
            {
                LoggingUtil.LogInfo("Checking available actions for object " + worldInteractiveObject.Id + "...");
            }

            if (!OpenSesamePlugin.FeaturesEnabled.Value.HasFlag(OpenSesamePlugin.EFeaturesEnabled.UnlockDoors))
            {
                return;
            }

            // Try to add the "Open Sesame" action to the door's context menu
            worldInteractiveObject.AddOpenSesameToActionList(__result, owner);
        }
    }
}
