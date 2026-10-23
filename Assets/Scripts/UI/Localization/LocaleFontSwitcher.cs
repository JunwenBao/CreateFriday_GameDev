using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

namespace Game.UI
{
    /// <summary>
    /// 切换语言时，自动把场景中使用"受管字体"的 TMP 文本替换为当前语言对应的字体。
    /// 覆盖范围：已加载场景中的所有文本（含未激活物体）、之后加载的场景、通过 UIManager 创建的窗口。
    /// 其他运行时实例化的预制体可挂 LocaleFontApplier，或手动调用 ApplyTo。
    /// </summary>
    public static class LocaleFontSwitcher
    {
        private static LocaleFontSettings _settings;
        private static TMP_FontAsset _currentFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            _currentFont = null;
            _settings = Resources.Load<LocaleFontSettings>(LocaleFontSettings.ResourcePath);
            if (_settings == null)
            {
                Debug.LogWarning($"[LocaleFontSwitcher] 未找到 Resources/{LocaleFontSettings.ResourcePath}，语言切换时不会更换字体。");
                return;
            }

            // 关闭 Domain Reload 时静态事件不会被清理，先移除再注册避免重复
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            LocalizationSettings.InitializationOperation.Completed += _ => OnLocaleChanged(LocalizationSettings.SelectedLocale);
        }

        public static void ApplyTo(GameObject root)
        {
            if (root == null || _currentFont == null)
                return;

            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                Apply(text);
        }

        public static void ApplyToAllLoaded()
        {
            if (_currentFont == null)
                return;

            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Apply(text);
        }

        private static void Apply(TMP_Text text)
        {
            if (text.font == _currentFont || !_settings.IsManaged(text.font))
                return;

            text.font = _currentFont;
        }

        private static void OnLocaleChanged(Locale locale)
        {
            if (_settings == null || locale == null)
                return;

            _currentFont = _settings.GetFont(locale.Identifier.Code);
            ApplyToAllLoaded();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyToAllLoaded();
    }
}
