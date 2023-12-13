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

        public static void FindTypes()
        {
            // Find the class responsible for managing locales and translations
            string methodName = "AddLocaleUpdateListener";
            Type[] localeManagerTypeOptions = Aki.Reflection.Utils.PatchConstants.EftTypes.Where(t => t.GetMethods().Any(m => m.Name.Contains(methodName))).ToArray();
            if (localeManagerTypeOptions.Length != 1)
            {
                throw new TypeLoadException("Cannot find type containing method " + methodName);
            }

            localeManagerType = localeManagerTypeOptions[0];
            LoggingUtil.LogInfo("Locale manager type: " + localeManagerType.FullName);

            // Find a method that we know has a parameter of the type that stores translations in a dictionary
            methodName = "UpdateMainMenuLocales";
            MethodInfo updateMainMenuLocalesMethod = AccessTools.FirstMethod(localeManagerType, m => m.Name.Contains(methodName));
            if (updateMainMenuLocalesMethod == null)
            {
                throw new MissingMethodException(localeManagerType.FullName, methodName);
            }

            // Find the matching parameter in the method above
            string paramName = "newLocale";
            ParameterInfo[] updateMainMenuLocalesMethodParamOptions = updateMainMenuLocalesMethod.GetParameters().Where(p => p.Name == paramName).ToArray();
            if (localeManagerTypeOptions.Length != 1)
            {
                throw new TypeLoadException("Cannot find parameter " + paramName + " in method " + methodName);
            }

            translationsType = updateMainMenuLocalesMethodParamOptions[0].ParameterType;
            LoggingUtil.LogInfo("Translations type: " + translationsType.FullName);

            // Find the field that stores translations for all locales
            FieldInfo[] translationFieldOptions = AccessTools.GetDeclaredFields(localeManagerType)
                .Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                .Where(f => f.FieldType.GetGenericArguments()[1] == translationsType)
                .ToArray();
            if (translationFieldOptions.Length != 1)
            {
                throw new MissingFieldException("Cannot find translations dictionary field in class " + localeManagerType.FullName);
            }

            translationsFieldName = translationFieldOptions[0].Name;
            LoggingUtil.LogInfo("Translations field name: " + translationsFieldName);
        }

        public static bool HaveTypesBeenFound()
        {
            if ((localeManagerType == null) || (translationsType == null) || (translationsFieldName == null))
            {
                return false;
            }

            return true;
        }

        public static object AddLocaleUpdateListener(Action<object> updateAction)
        {
            if (!HaveTypesBeenFound())
            {
                throw new TypeLoadException("Types have not been loaded");
            }

            // Get the property that returns the static instance of the class
            string instanceName = localeManagerType.FullName + "_0";
            object localeManagerObj = AccessTools.Property(localeManagerType, instanceName).GetValue(null);
            if (localeManagerObj == null)
            {
                LoggingUtil.LogError("Cannot get instance of " + instanceName);
                return null;
            }

            // Find the method used to add a listener for when locales are updated
            string methodName = "AddLocaleUpdateListener";
            MethodInfo addLocaleUpdateListenerMethod = AccessTools.FirstMethod(localeManagerType, m => m.Name.Contains(methodName));
            if (addLocaleUpdateListenerMethod == null)
            {
                LoggingUtil.LogError("Cannot find method " + methodName + " in type " + localeManagerType.FullName);
                return null;
            }

            LoggingUtil.LogInfo("Adding locale update listener...");

            // Add a new listener action
            Action localeUpdateAction = () => { updateAction(localeManagerObj); };
            return addLocaleUpdateListenerMethod.Invoke(localeManagerObj, new object[] { localeUpdateAction });
        }

        public static void AddNewTranslations(object localeManager)
        {
            if (!HaveTypesBeenFound())
            {
                throw new TypeLoadException("Types have not been loaded");
            }

            // Get the translations for all locales
            object loadedLocalesObj = AccessTools.Field(localeManagerType, translationsFieldName).GetValue(localeManager);
            IDictionary loadedLocales = loadedLocalesObj as IDictionary;
            if (loadedLocales == null)
            {
                LoggingUtil.LogError("Cannot load translations");
                return;
            }

            // Loop through all locales that EFT has loaded
            foreach (string locale in loadedLocales.Keys)
            {
                // Skip locales for which translations have already been added
                if (updatedLocales.Contains(locale))
                {
                    continue;
                }

                // Get the translations for the locale
                IDictionary<string, string> translations = loadedLocales[locale] as IDictionary<string, string>;
                if (translations == null)
                {
                    LoggingUtil.LogError("Cannot load translations for locale \"" + locale + "\"");
                    continue;
                }

                // Get the translations that need to be added;
                Dictionary<string, string> newTranslations = GetNewTranslationsForLocale(locale);
                if (newTranslations.Count == 0)
                {
                    LoggingUtil.LogWarning("No new translations to add for locale \"" + locale + "\"");
                    continue;
                }

                // Make sure translations don't already exist for the keys that will be added
                if (newTranslations.Any(x => translations.ContainsKey(x.Key)))
                {
                    LoggingUtil.LogError("Duplicate translations found for locale \"" + locale + "\". New translations will not be added.");
                    continue;
                }

                // Add the new translations and track that the locale has been updated
                translations.AddRange(newTranslations);
                updatedLocales.Add(locale);

                LoggingUtil.LogInfo("Added new translations for locale \"" + locale + "\"");
            }
        }

        public static Dictionary<string, string> GetNewTranslationsForLocale(string locale)
        {
            Dictionary<string, string> translations = new Dictionary<string, string>();

            // Load the matching resource type for the selected locale
            Type resType = GetTranslationResourceType(locale);

            // Find all new translations in the resource 
            PropertyInfo[] resEntries = resType.GetProperties(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetProperty);
            foreach (PropertyInfo resEntry in resEntries)
            {
                // Make sure the property type is a string
                if (resEntry.PropertyType != typeof(string))
                {
                    continue;
                }

                // Make sure the property value converts into a string that isn't empty
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
            if (resType != null)
            {
                return resType;
            }

            LoggingUtil.LogError("Cannot get new translations for locale \"" + locale + "\", falling back to locale \"en\"");

            resName = "SPTOpenSesame.Resources.en";
            resType = Type.GetType(resName);
            if (resType != null)
            {
                return resType;
            }

            throw new TypeLoadException("Cannot load new translations from default locale (\"en\")");
        }
    }
}
