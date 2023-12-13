using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using HarmonyLib;
using SPTOpenSesame.Helpers;

namespace SPTOpenSesame.Patches
{
    public class MainMenuShowPatch : ModulePatch
    {
        private static bool translationsUpdated = false;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(MainMenuController).GetMethod("ShowScreen", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            if (translationsUpdated)
            {
                LoggingUtil.LogInfo("Translations have already been updated");
                return;
            }
            
            GClass1722 localeClass = (GClass1722)AccessTools.Property(typeof(GClass1722), "GClass1722_0").GetValue(null);
            if (localeClass == null)
            {
                LoggingUtil.LogError("Cannot get instance of GClass1722_0");
                return;
            }
            
            LoggingUtil.LogInfo("Loaded instance of GClass1722");
            
            localeClass.AddLocaleUpdateListener(() =>
            {
                Dictionary<string, GClass1719> loadedLocales = getLoadedLocales(localeClass);
                foreach (var (locale, translations) in loadedLocales)
                {
                    tryInjectNewTranslations(locale, translations);
                }
                
                LoggingUtil.LogInfo("Languages loaded: " + string.Join(", ", loadedLocales.Keys));
            });
            
            Dictionary<string, GClass1719> locales = getLoadedLocales(localeClass);

            var currentLocale = locales.Keys.First();
            translationsUpdated = tryInjectNewTranslations(currentLocale, locales[currentLocale]);
        }

        private static Dictionary<string, GClass1719> getLoadedLocales(GClass1722 localeClass)
        {
            Dictionary<string, GClass1719> locales = (Dictionary<string, GClass1719>)AccessTools.Field(typeof(GClass1722), "dictionary_4").GetValue(localeClass);
            
            return locales;
        }

        private static bool tryInjectNewTranslations(string locale, Dictionary<string, string> translations)
        {
            string resName = "SPTOpenSesame.Resources." + locale;
            Type resType = Type.GetType(resName);
            if (resType == null)
            {
                LoggingUtil.LogError("Cannot get translations for locale \"" + locale + "\", falling back to locale \"en\"");
                resName = "SPTOpenSesame.Resources.en";
                resType = Type.GetType(resName);
            }

            PropertyInfo[] resEntries = resType.GetProperties(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetProperty);
            foreach (PropertyInfo resEntry in resEntries)
            {
                if (translations.ContainsKey(resEntry.Name))
                {
                    continue;
                }

                if (resEntry.PropertyType != typeof(string))
                {
                    continue;
                }

                string translation = resEntry.GetValue(null, null) as string;
                if ((translation == null) || (translation.Length == 0))
                {
                    LoggingUtil.LogError("Invalid translation for key \"" + resEntry.Name + "\" for locale \"" + locale + "\"");
                    continue;
                }

                LoggingUtil.LogInfo("Injecting translation for \"" + resEntry.Name + "\" for locale \"" + locale + "\": " + translation + "...");
                translations.Add(resEntry.Name, translation);
            }

            return true;
        }
    }
}
