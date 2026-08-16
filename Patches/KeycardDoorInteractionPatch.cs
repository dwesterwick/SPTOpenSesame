using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using SPT.Reflection.Patching;
using SPTOpenSesame.Helpers;
using SPTOpenSesame.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SPTOpenSesame.Patches
{
    public class KeycardDoorInteractionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(InteractionContextHelper).GetMethod(
                nameof(InteractionContextHelper.GetAvailableActions),
                BindingFlags.Public | BindingFlags.Static,
                null,
                new Type[] { typeof(GamePlayerOwner), typeof(KeycardDoor), typeof(bool) },
                null);
        }

        [PatchPostfix]
        protected static void PatchPostfix(ref AvailableInteractionState __result, GamePlayerOwner owner, KeycardDoor door, bool isProxy)
        {
            // Ignore interactions from bots
            if (InteractionHelpers.IsInteractorABot(owner))
            {
                return;
            }

            if (OpenSesamePlugin.DebugMessagesEnabled.Value.HasFlag(OpenSesamePlugin.EDebugMessagesEnabled.DoorInteractions))
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Checking available actions for door: " + door.Id + "...");
            }

            if (!OpenSesamePlugin.FeaturesEnabled.Value.HasFlag(OpenSesamePlugin.EFeaturesEnabled.UnlockDoors))
            {
                return;
            }

            // Try to add the "Open Sesame" action to the door's context menu
            door.AddOpenSesameToActionList(ref __result, owner);
        }
    }
}
