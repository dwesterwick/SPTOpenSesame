using System;
using System.ComponentModel;
using EFT;

namespace SPTOpenSesame.Helpers
{
    public enum Language
    {
        [Description("简体中文")] [LanguageFile("zh_CN")]
        SimplifiedChinese,

        [Description("English")] [LanguageFile("en_US")]
        English
    }

    /// <summary>
    /// The language file's name in Resources folder without suffix name.
    /// </summary>
    internal class LanguageFile : Attribute
    {
        public string FileName { get; }

        public LanguageFile(string fileName)
        {
            if (fileName.EndsWith(".resx"))
            {
                fileName = fileName.Replace(".resx", "");
            }

            FileName = fileName;
        }
    }

    /// <summary>
    /// Language enum's util
    /// </summary>
    public static class LanguageUtil
    {
        /// <summary>
        /// Get file name of language
        /// </summary>
        /// <param name="language">language</param>
        /// <returns>language file name in Resources folder without suffix name</returns>
        public static string GetLanguageFile(Language language)
        {
            var field = language.GetType().GetField(language.ToString());
            var customAttribute = Attribute.GetCustomAttribute(field, typeof(LanguageFile));
            return customAttribute == null ? language.ToString() : ((LanguageFile)customAttribute).FileName;
        }
    }
}