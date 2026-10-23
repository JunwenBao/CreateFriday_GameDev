using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 各语言对应的 TMP 字体。必须放在 Resources 文件夹下，并命名为 LocaleFontSettings。
    /// </summary>
    [CreateAssetMenu(fileName = "LocaleFontSettings", menuName = "Localization/Locale Font Settings")]
    public class LocaleFontSettings : ScriptableObject
    {
        public const string ResourcePath = "LocaleFontSettings";

        [Serializable]
        public struct Entry
        {
            [Tooltip("Locale Code，例如 en、zh-Hans")]
            public string localeCode;
            public TMP_FontAsset font;
        }

        [Tooltip("只有正在使用这里列出字体的文本才会被自动切换，其余文本保持不变")]
        [SerializeField] private List<Entry> entries = new List<Entry>();

        public TMP_FontAsset GetFont(string localeCode)
        {
            foreach (var entry in entries)
            {
                if (entry.localeCode == localeCode)
                    return entry.font;
            }
            return null;
        }

        public bool IsManaged(TMP_FontAsset font)
        {
            if (font == null)
                return false;

            foreach (var entry in entries)
            {
                if (entry.font == font)
                    return true;
            }
            return false;
        }
    }
}
