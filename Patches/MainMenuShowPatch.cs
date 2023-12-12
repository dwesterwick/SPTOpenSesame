using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using SPTOpenSesame.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SPTOpenSesame.Patches
{
    public class MainMenuShowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(MainMenuController).GetMethod("ShowScreen", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            Dictionary<string, string> currentLanguageDictionary = getCurrentLanguageDictionary();
            if (currentLanguageDictionary == null)
            {
                LoggingUtil.LogError("Cannot load language dictionary");
                return;
            }

            injectNewTranslations(currentLanguageDictionary);
        }

        private static Dictionary<string, string> getCurrentLanguageDictionary()
        {
            GClass1722 localeClass = (GClass1722)AccessTools.Property(typeof(GClass1722), "GClass1722_0").GetValue(null);
            if (localeClass == null)
            {
                LoggingUtil.LogError("Cannot get instance of GClass1722_0");
                return null;
            }
            LoggingUtil.LogInfo("Loaded instance of GClass1722");

            Dictionary<string, GClass1719> languages = (Dictionary<string, GClass1719>)AccessTools.Field(typeof(GClass1722), "dictionary_4").GetValue(localeClass);
            if ((languages == null) || (languages.Count == 0))
            {
                LoggingUtil.LogError("Cannot get instance of dictionary_4");
                return null;
            }

            LoggingUtil.LogInfo("Languages loaded: " + string.Join(", ", languages.Keys));

             return languages[languages.Keys.First()];
        }

        private static void injectNewTranslations(Dictionary<string, string> dictionary)
        {
            dictionary.Add("TestString", "TRANSLATED TEXT");

            LoggingUtil.LogInfo("Injected new translations");
        }
    }
}
