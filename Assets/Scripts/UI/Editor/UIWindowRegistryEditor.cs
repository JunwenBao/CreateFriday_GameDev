using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Game.UI
{
    [CustomEditor(typeof(UIWindowRegistry))]
    public class UIWindowRegistryEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var registry = (UIWindowRegistry)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("收集窗口预制体"))
                Collect(registry);

            var duplicates = registry.Windows
                .Where(window => window != null)
                .GroupBy(window => window.GetType())
                .Where(group => group.Count() > 1)
                .Select(group => group.Key.Name)
                .ToArray();
            if (duplicates.Length > 0)
                EditorGUILayout.HelpBox($"同一窗口类型只能注册一次：{string.Join(", ", duplicates)}", MessageType.Error);
            if (registry.Windows.Any(window => window == null))
                EditorGUILayout.HelpBox("列表中有空条目。", MessageType.Warning);
        }

        private void Collect(UIWindowRegistry registry)
        {
            var folders = registry.SearchFolders.Where(AssetDatabase.IsValidFolder).ToArray();
            if (folders.Length == 0)
            {
                Debug.LogWarning("[UI] None of the registry's search folders exist.", registry);
                return;
            }

            var windows = AssetDatabase.FindAssets("t:Prefab", folders)
                .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
                .Select(prefab => prefab != null ? prefab.GetComponent<UIWindow>() : null)
                .Where(window => window != null)
                .OrderBy(window => window.name)
                .ToList();

            serializedObject.Update();
            var list = serializedObject.FindProperty("windows");
            list.arraySize = windows.Count;
            for (int i = 0; i < windows.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = windows[i];
            serializedObject.ApplyModifiedProperties();

            Debug.Log($"[UI] Registered {windows.Count} window prefab(s).", registry);
        }
    }
}
