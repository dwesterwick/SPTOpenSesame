using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPTOpenSesame.Utils;

namespace SPTOpenSesame.Helpers
{
    public static class LocalizationHelpers
    {
        private const string DEFAULT_LOCALE = "en";
        private const string NEW_TRANSLATIONS_NAMESPACE = "SPTOpenSesame.Resources";

        private static List<string> updatedLocales = new List<string>();
        
        public static void AddNewTranslationsForLoadedLocales()
        {
            // Loop through all locales that EFT has loaded
            foreach ((string locale, Locale existingTranslations) in LocalizationManager.Instance._locales)
            {
                // Skip locales for which translations have already been added
                if (updatedLocales.Contains(locale))
                {
                    continue;
                }

                // Get the existing translations for the locale
                if (existingTranslations == null)
                {
                    Singleton<LoggingUtil>.Instance.LogError("Cannot load existing translations for locale \"" + locale + "\"");
                    continue;
                }

                // Check if translations can be added for the locale
                if (!TryAddNewTranslationsForLocale(locale, existingTranslations))
                {
                    Singleton<LoggingUtil>.Instance.LogError("Could not load translations for locale \"" + locale + "\"");
                    continue;
                }

                updatedLocales.Add(locale);
                Singleton<LoggingUtil>.Instance.LogDebug("Added translations for locale \"" + locale + "\"");
            }
        }

        public static bool TryAddNewTranslationsForLocale(string locale, Locale existingTranslations)
        {
            // Load the matching resource type for the selected locale
            Type resType = GetTranslationResourceType(locale);
            if (resType == null)
            {
                // If this is the default locale, there is no fall-back option, so throw an exception
                if (locale == DEFAULT_LOCALE)
                {
                    throw new TypeLoadException("Cannot load translations for default locale (\"" + locale + "\")");
                }

                // If a matching type cannot be found, load the one for English instead
                Singleton<LoggingUtil>.Instance.LogWarning("Cannot find translations for locale \"" + locale + "\". Using translations for default locale (\"" + DEFAULT_LOCALE + "\") instead...");
                return TryAddNewTranslationsForLocale(DEFAULT_LOCALE, existingTranslations);
            }
            
            // Get the translations that need to be added;
            Dictionary<string, string> newTranslations = GetNewTranslationsForLocale(locale, resType);
            if (newTranslations.Count == 0)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("No translations to add for locale \"" + locale + "\"");
                return false;
            }

            // Make sure translations don't already exist for the keys that will be added
            if (newTranslations.Any(x => existingTranslations.ContainsKey(x.Key)))
            {
                Singleton<LoggingUtil>.Instance.LogError("Duplicate translations found for locale \"" + locale + "\". Translations will not be added.");
                return false;
            }

            // Add the new translations and track that the locale has been updated
            existingTranslations.AddRange(newTranslations);

            return true;
        }

        public static Dictionary<string, string> GetNewTranslationsForLocale(string locale, Type resourceType)
        {
            Dictionary<string, string> translations = new Dictionary<string, string>();

            // Find all new translations in the resource 
            PropertyInfo[] resEntries = resourceType.GetProperties(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetProperty);
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
                    Singleton<LoggingUtil>.Instance.LogError("Invalid translation for key \"" + resEntry.Name + "\" for locale \"" + locale + "\"");
                    continue;
                }

                //Singleton<LoggingUtil>.Instance.LogInfo("Found translation for \"" + resEntry.Name + "\" for locale \"" + locale + "\": " + translation);
                translations.Add(resEntry.Name, translation);
            }

            return translations;
        }

        public static Type GetTranslationResourceType(string locale)
        {
            // Dashes are automatically changed to underscores in resource file names
            string adjustedLocaleName = locale.Replace('-', '_');

            string resName = NEW_TRANSLATIONS_NAMESPACE + "." + adjustedLocaleName;
            Type resType = Type.GetType(resName);

            return resType;
        }
    }
}
