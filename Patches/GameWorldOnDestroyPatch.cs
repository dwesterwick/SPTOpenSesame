using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using EFT;

namespace SPTOpenSesame.Patches
{
    public class GameWorldOnDestroyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("OnDestroy", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(GameWorld __instance)
        {
            OpenSesamePlugin.PowerSwitch = null;
        }
    }
}
