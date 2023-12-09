using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;

namespace SPTOpenSesame
{
    [BepInPlugin("com.DanW.OpenSesame", "DanW-OpenSesame", "1.0.0")]
    public class OpenSesamePlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Loading OpenSesame...");

            LoggingController.Logger = Logger;

            new Patches.DoorInteractionPatch().Enable();

            // Add options to the F12 menu
            SPTOpenSesamePluginConfig.BuildConfigOptions(Config);

            Logger.LogInfo("Loading OpenSesame...done.");
        }
    }
}
