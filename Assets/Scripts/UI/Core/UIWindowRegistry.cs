using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    /// <summary>直接引用全部窗口预制体的加载器，按窗口脚本类型查找。</summary>
    [CreateAssetMenu(fileName = "UIWindowRegistry", menuName = "UI/Window Registry")]
    public class UIWindowRegistry : UIWindowLoader
    {
        [Tooltip("窗口预制体，每种窗口类型只能出现一次")]
        [SerializeField] private List<UIWindow> windows = new List<UIWindow>();
        [Tooltip("「收集窗口预制体」按钮扫描的文件夹")]
        [SerializeField] private string[] searchFolders = { "Assets/Prefabs/UI" };

        private Dictionary<Type, UIWindow> _lookup;

        public IReadOnlyList<UIWindow> Windows => windows;
        public IReadOnlyList<string> SearchFolders => searchFolders;

        public override void Load(Type windowType, Action<UIWindow> onLoaded)
        {
            if (_lookup == null)
                BuildLookup();

            _lookup.TryGetValue(windowType, out var prefab);
            onLoaded(prefab);
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<Type, UIWindow>();
            foreach (var prefab in windows)
            {
                if (prefab != null && !_lookup.TryAdd(prefab.GetType(), prefab))
                    Debug.LogWarning($"[UI] '{prefab.GetType().Name}' is registered more than once in {name}; using the first entry.", this);
            }
        }

        private void OnValidate() => _lookup = null;
    }
}
