using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT.Interactive;
using EFT;
using SPTOpenSesame.Helpers;

namespace SPTOpenSesame.Patches
{
    public class KeycardDoorInteractionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return InteractionHelpers.TargetType.GetMethod("smethod_9", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref object __result, GamePlayerOwner owner, KeycardDoor door, bool isProxy)
        {
            // Ignore interactions from bots
            if (InteractionHelpers.IsInteractorABot(owner))
            {
                return;
            }

            if (OpenSesamePlugin.WriteMessagesForAllDoors.Value)
            {
                LoggingUtil.LogInfo("Checking available actions for door: " + door.Id + "...");
            }

            if (!OpenSesamePlugin.AddNewActions.Value)
            {
                return;
            }

            // Try to add the "Open Sesame" action to the door's context menu
            door.AddOpenSesameToActionList(__result, owner);
        }
    }
}
