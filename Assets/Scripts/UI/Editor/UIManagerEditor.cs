using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.UI
{
    /// <summary>运行时在 Inspector 中显示窗口栈，便于排查遮挡、焦点、输入被锁等问题。</summary>
    [CustomEditor(typeof(UIManager))]
    public class UIManagerEditor : Editor
    {
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var manager = (UIManager)target;
            if (!Application.isPlaying || UIManager.Instance != manager)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("运行时状态", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("输入模式", manager.InputMode.ToString());
            EditorGUILayout.LabelField("暂停游戏", manager.IsGamePaused ? "是" : "否");
            EditorGUILayout.LabelField("焦点窗口", manager.FocusedWindow != null ? manager.FocusedWindow.name : "-");

            EditorGUILayout.LabelField("显示中（自上而下）", EditorStyles.boldLabel);
            var layers = (UILayer[])Enum.GetValues(typeof(UILayer));
            for (int l = layers.Length - 1; l >= 0; l--)
            {
                var windows = manager.GetWindows(layers[l]);
                for (int i = windows.Count - 1; i >= 0; i--)
                {
                    var window = windows[i];
                    if (window == null)
                        continue;
                    string covered = window.IsCovered ? "  已遮挡" : string.Empty;
                    EditorGUILayout.LabelField($"[{layers[l]}] {window.name}", $"{window.State}  #{window.SortingOrder}{covered}");
                }
            }

            var closed = manager.AllWindows
                .Where(window => window != null && window.State == UIWindowState.Closed)
                .Select(window => window.name)
                .ToArray();
            EditorGUILayout.LabelField("已关闭的实例", closed.Length > 0 ? string.Join(", ", closed) : "-");

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("关闭全部"))
                    manager.CloseAll();
                if (GUILayout.Button("清空缓存"))
                    manager.ClearCache();
            }
        }
    }
}
