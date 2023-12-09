using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.Interactive;

namespace SPTOpenSesame.Patches
{
    public class DoorInteractionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass1726).GetMethod("smethod_9", BindingFlags.NonPublic | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(GamePlayerOwner owner, Door door)
        {
            // Ignore interactions from bots
            if ((owner.Player == null) || (owner.Player.Id != Singleton<GameWorld>.Instance.MainPlayer.Id))
            {
                return;
            }

            LoggingController.LogInfo("Checking interaction options for door: " + door.Id + "...");
        }
    }
}
