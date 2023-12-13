using System;
using System.Collections;
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
        private static Type localeManagerType = null;
        private static Type translationsType = null;
        private static string translationsFieldName = null;

        private static List<string> updatedLocales = new List<string>();

        public static void LoadTypes()
        {
            Type[] localeManagerTypeOptions = Aki.Reflection.Utils.PatchConstants.EftTypes.Where(t => t.GetMethods().Any(m => m.Name.Contains("AddLocaleUpdateListener"))).ToArray();
            if (localeManagerTypeOptions.Length != 1)
            {
                throw new TypeLoadException("Cannot find target method");
            }

            localeManagerType = localeManagerTypeOptions[0];
            LoggingUtil.LogInfo("Locale manager type: " + localeManagerType.FullName);

            string methodName = "UpdateMainMenuLocales";
            MethodInfo updateMainMenuLocalesMethod = AccessTools.FirstMethod(localeManagerType, m => m.Name.Contains(methodName));
            if (updateMainMenuLocalesMethod == null)
            {
                throw new MissingMethodException(localeManagerType.FullName, methodName);
            }

            string paramName = "newLocale";
            ParameterInfo[] updateMainMenuLocalesMethodParamOptions = updateMainMenuLocalesMethod.GetParameters().Where(p => p.Name == paramName).ToArray();
            if (localeManagerTypeOptions.Length != 1)
            {
                throw new TypeLoadException("Cannot find parameter " + paramName + " in method " + methodName);
            }

            translationsType = updateMainMenuLocalesMethodParamOptions[0].ParameterType;
            LoggingUtil.LogInfo("Translations type: " + translationsType.FullName);

            FieldInfo[] translationFieldOptions = AccessTools.GetDeclaredFields(localeManagerType)
                .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                .Where(f => f.FieldType.GetGenericArguments()[1] == translationsType)
                .ToArray();
            if (translationFieldOptions.Length != 1)
            {
                LoggingUtil.LogInfo("Found possible matches for translations dictionary field: " + string.Join(", ", translationFieldOptions.Select(f => f.Name)));

                throw new MissingFieldException("Cannot find translations dictionary field in class " + localeManagerType.FullName);
            }

            translationsFieldName = translationFieldOptions[0].Name;
            LoggingUtil.LogInfo("Translations field name: " + translationsFieldName);
        }

        public static bool HaveTypesBeenLoaded()
        {
            if ((localeManagerType == null) || (translationsType == null) || (translationsFieldName == null))
            {
                return false;
            }

            return true;
        }

        public static object AddLocaleUpdateListener(Action<object> updateAction)
        {
            if (!HaveTypesBeenLoaded())
            {
                throw new TypeLoadException("Types have not been loaded");
            }

            string instanceName = localeManagerType.FullName + "_0";
            object localeManagerObj = AccessTools.Property(localeManagerType, instanceName).GetValue(null);
            if (localeManagerObj == null)
            {
                LoggingUtil.LogError("Cannot get instance of " + instanceName);
                return null;
            }

            LoggingUtil.LogInfo("Adding locale update listener...");

            string methodName = "AddLocaleUpdateListener";
            MethodInfo addLocaleUpdateListenerMethod = AccessTools.FirstMethod(localeManagerType, m => m.Name.Contains(methodName));
            if (addLocaleUpdateListenerMethod == null)
            {
                LoggingUtil.LogError("Cannot find method " + methodName + " in type " + localeManagerType.FullName);
                return null;
            }

            Action localeUpdateAction = () => { updateAction(localeManagerObj); };
            return addLocaleUpdateListenerMethod.Invoke(localeManagerObj, new object[] { localeUpdateAction });
        }

        public static void AddNewTranslations(object localeManager)
        {
            if (!HaveTypesBeenLoaded())
            {
                throw new TypeLoadException("Types have not been loaded");
            }

            object loadedLocalesObj = AccessTools.Field(localeManagerType, translationsFieldName).GetValue(localeManager);
            IDictionary loadedLocales = loadedLocalesObj as IDictionary;
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

                IDictionary<string, string> translations = loadedLocales[locale] as IDictionary<string, string>;
                if (translations == null)
                {
                    LoggingUtil.LogError("Cannot load translations for locale \"" + locale + "\"");
                    continue;
                }

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
