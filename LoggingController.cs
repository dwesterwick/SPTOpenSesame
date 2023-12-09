using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTOpenSesame
{
    public static class LoggingController
    {
        public static BepInEx.Logging.ManualLogSource Logger { get; set; } = null;

        public static void LogInfo(string message)
        {
            Logger.LogInfo(message);
        }

        public static void LogWarning(string message)
        {
            Logger.LogWarning(message);
        }

        public static void LogError(string message)
        {
            Logger.LogError(message);
        }
    }
}
