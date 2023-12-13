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

        public static object AddLocaleUpdateListener(Action<object> updateAction)
        {
            string instanceName = OpenSesamePlugin.LocaleManagerType.FullName + "_0";
            object localeManagerObj = AccessTools.Property(OpenSesamePlugin.LocaleManagerType, instanceName).GetValue(null);
            if (localeManagerObj == null)
            {
                LoggingUtil.LogError("Cannot get instance of " + instanceName);
                return null;
            }

            LoggingUtil.LogInfo("Adding locale update listener...");

            string methodName = "AddLocaleUpdateListener";
            MethodInfo addLocaleUpdateListenerMethod = AccessTools.FirstMethod(OpenSesamePlugin.LocaleManagerType, m => m.Name.Contains(methodName));
            if (addLocaleUpdateListenerMethod == null)
            {
                LoggingUtil.LogError("Cannot find method " + methodName + " in type " + OpenSesamePlugin.LocaleManagerType.FullName);
                return null;
            }

            Action localeUpdateAction = () => { updateAction(localeManagerObj); };
            return addLocaleUpdateListenerMethod.Invoke(localeManagerObj, new object[] { localeUpdateAction });
        }

        public static void AddNewTranslations(object localeManager)
        {
            object loadedLocalesObj = AccessTools.Field(OpenSesamePlugin.LocaleManagerType, OpenSesamePlugin.TranslationsFieldName).GetValue(localeManager);
            Dictionary<string, GClass1719> loadedLocales = loadedLocalesObj as Dictionary<string, GClass1719>;
            //IDictionary<string, IDictionary<string, string>> loadedLocales = loadedLocalesObj as IDictionary<string, IDictionary<string, string>>;
            if (loadedLocales == null)
            {
                LoggingUtil.LogError("Cannot load translations");
                return;
            }

            foreach (string locale in loadedLocales.Keys)
            {
                if (updatedLocales.Contains(locale))
                {
                    continue;
                }

                Dictionary<string, string> newTranslations = GetNewTranslationsForLocale(locale)
                    .Where(x => !loadedLocales[locale].ContainsKey(x.Key))
                    .ToDictionary(x => x.Key, x => x.Value);

                if (newTranslations.Count == 0)
                {
                    LoggingUtil.LogWarning("No new translations to add for locale \"" + locale + "\"");
                    continue;
                }

                loadedLocales[locale].AddRange(newTranslations);
                updatedLocales.Add(locale);

                LoggingUtil.LogInfo("Added new translations for locale \"" + locale + "\"");
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

                //LoggingUtil.LogInfo("Found translation for \"" + resEntry.Name + "\" for locale \"" + locale + "\": " + translation);
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
