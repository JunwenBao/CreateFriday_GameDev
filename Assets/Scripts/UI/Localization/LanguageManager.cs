using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Game.UI
{
    /// <summary>
    /// 对 Unity Localization 的轻量封装：切换语言、查询当前语言、在代码中获取本地化文本。
    /// 语言选择的持久化由 Localization Settings 中的 "Player Pref Locale Selector" 负责。
    /// </summary>
    public static class LanguageManager
    {
        public const string English = "en";
        public const string ChineseSimplified = "zh-Hans";
        public const string DefaultTable = "UI_Text";

        public static string CurrentCode
        {
            get
            {
                EnsureInitialized();
                var locale = LocalizationSettings.SelectedLocale;
                return locale != null ? locale.Identifier.Code : null;
            }
        }

        public static bool IsChinese => CurrentCode == ChineseSimplified;

        public static void SetLanguage(string localeCode)
        {
            EnsureInitialized();
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
            if (locale == null)
            {
                Debug.LogWarning($"[LanguageManager] 未找到 Locale: {localeCode}，请检查 Localization Settings 中的 Available Locales。");
                return;
            }

            LocalizationSettings.SelectedLocale = locale;
        }

        public static void ToggleChineseEnglish()
        {
            SetLanguage(IsChinese ? English : ChineseSimplified);
        }

        /// <summary>
        /// 在代码中同步获取本地化文本，支持 Smart String 参数（如 "{0}"）。
        /// </summary>
        public static string Get(string key, string table = DefaultTable, params object[] args)
        {
            EnsureInitialized();
            return LocalizationSettings.StringDatabase.GetLocalizedString(table, key, args);
        }

        private static void EnsureInitialized()
        {
            var op = LocalizationSettings.InitializationOperation;
            if (!op.IsDone)
                op.WaitForCompletion();
        }
    }
}
