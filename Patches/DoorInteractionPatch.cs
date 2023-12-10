﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;
using EFT.Interactive;
using SPTOpenSesame.Helpers;

namespace SPTOpenSesame.Patches
{
    public class DoorInteractionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return OpenSesamePlugin.TargetType.GetMethod("smethod_9", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref object __result, GamePlayerOwner owner, Door door)
        {
            // Ignore interactions from bots
            if (InteractionHelpers.isInteractorABot(owner))
            {
                return;
            }

            if (OpenSesamePlugin.WriteMessagesForAllDoors.Value)
            {
                LoggingUtil.LogInfo("Checking available actions for door: " + door.Id + "...");
            }

            // Try to add the "Open Sesame" action to the door's context menu
            door.addOpenSesameToActionList(__result, owner);
        }
    }
}
