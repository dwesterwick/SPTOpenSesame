using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace SPTOpenSesame.Helpers
{
    public static class LocalizationUtil
    {
        private static List<string> updatedLocales = new List<string>();

        public static void AddLocaleUpdateListener(Action<object> updateAction)
        {
            GClass1722 localeObj = (GClass1722)AccessTools.Property(typeof(GClass1722), "GClass1722_0").GetValue(null);
            if (localeObj == null)
            {
                LoggingUtil.LogError("Cannot get instance of GClass1722_0");
                return;
            }

            LoggingUtil.LogInfo("Adding locale update listener...");

            localeObj.AddLocaleUpdateListener(() => { updateAction(localeObj); });
        }

        public static void AddNewTranslations(object localeObj)
        {
            Dictionary<string, GClass1719> loadedLocales = (Dictionary<string, GClass1719>)AccessTools.Field(typeof(GClass1722), "dictionary_4").GetValue(localeObj);

            foreach (var (locale, translations) in loadedLocales)
            {
                if (updatedLocales.Contains(locale))
                {
                    continue;
                }

                LoggingUtil.LogInfo("Adding new translations for locale \"" + locale + "\"...");

                Dictionary<string, string> newTranslations = GetNewTranslationsForLocale(locale)
                    .Where(x => !translations.ContainsKey(x.Key))
                    .ToDictionary(x => x.Key, x => x.Value);

                if (newTranslations.Count == 0)
                {
                    LoggingUtil.LogWarning("No new translations to add for locale \"" + locale + "\"");
                    continue;
                }

                translations.AddRange(newTranslations);
                updatedLocales.Add(locale);
            }
        }

        public static Dictionary<string, string> GetNewTranslationsForLocale(string locale)
        {
            Dictionary<string, string> translations = new Dictionary<string, string>();

            Type resType = GetTranslationResourceType(locale);
            PropertyInfo[] resEntries = resType.GetProperties(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetProperty);
            foreach (PropertyInfo resEntry in resEntries)
            {
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

                LoggingUtil.LogInfo("Adding translation for \"" + resEntry.Name + "\" for locale \"" + locale + "\": " + translation + "...");
                translations.Add(resEntry.Name, translation);
            }

            return translations;
        }

        public static Type GetTranslationResourceType(string locale)
        {
            string resName = "SPTOpenSesame.Resources." + locale;

            Type resType = Type.GetType(resName);
            if (resType == null)
            {
                LoggingUtil.LogError("Cannot get translations for locale \"" + locale + "\", falling back to locale \"en\"");

                resName = "SPTOpenSesame.Resources.en";
                resType = Type.GetType(resName);
            }

            return resType;
        }
    }
}
