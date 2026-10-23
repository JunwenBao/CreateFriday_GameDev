using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 挂在任意 Button 上即可切换语言。
    /// targetLocaleCode 留空：在中英之间来回切换；填写（如 en / zh-Hans）：切换到指定语言。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LanguageSwitchButton : MonoBehaviour
    {
        [Tooltip("留空 = 中英互切；否则填写 Locale Code，例如 en 或 zh-Hans")]
        [SerializeField] private string targetLocaleCode;

        private void Awake()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if (string.IsNullOrWhiteSpace(targetLocaleCode))
                LanguageManager.ToggleChineseEnglish();
            else
                LanguageManager.SetLanguage(targetLocaleCode.Trim());
        }
    }
}
