using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

/// <summary>
/// 把 DialogueData 中尚未本地化的原文（sourceText）迁移到 String Table：
/// 自动生成 Key、写入各语言表，并清空原文。可重复执行，已有 Key 的台词会被跳过。
/// </summary>
public static class DialogueLocalizationMigrator
{
    private const string SourceLocaleCode = "en";
    private const string TableDirectory = "Assets/Configs/Localization";

    [MenuItem("Tools/Localization/Migrate Dialogue Text To Keys")]
    private static void Migrate()
    {
        var collections = new Dictionary<string, StringTableCollection>();
        var untranslated = new List<string>();
        int migratedCount = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:DialogueData"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (dialogue == null || dialogue.conversationBlock == null)
                continue;

            if (string.IsNullOrEmpty(dialogue.tableName))
                dialogue.tableName = DialogueData.DefaultTableName;

            StringTableCollection collection = GetOrCreateCollection(dialogue.tableName, collections);
            string keyPrefix = dialogue.name.Replace("DialogueData_", string.Empty);
            bool changed = false;

            for (int i = 0; i < dialogue.conversationBlock.Count; i++)
            {
                DialogueLine line = dialogue.conversationBlock[i];
                if (!string.IsNullOrEmpty(line.key) || string.IsNullOrEmpty(line.sourceText))
                    continue;

                string key = MakeUniqueKey(collection.SharedData, $"{keyPrefix}.{i:D2}");
                collection.SharedData.AddKey(key);

                foreach (StringTable table in collection.StringTables)
                {
                    // 原文写入英文表；其他语言先用原文占位，避免游戏中出现 "No translation found"
                    table.AddEntry(key, line.sourceText);
                    EditorUtility.SetDirty(table);
                    if (table.LocaleIdentifier.Code != SourceLocaleCode)
                        untranslated.Add($"{table.LocaleIdentifier.Code}: {key}");
                }

                line.key = key;
                line.sourceText = string.Empty;
                dialogue.conversationBlock[i] = line;
                changed = true;
                migratedCount++;
            }

            if (changed)
            {
                EditorUtility.SetDirty(collection.SharedData);
                EditorUtility.SetDirty(dialogue);
            }
        }

        AssetDatabase.SaveAssets();

        string message = $"已迁移 {migratedCount} 句台词。";
        if (untranslated.Count > 0)
            message += "\n以下条目目前用英文原文占位，请在 Localization Tables 窗口中翻译：\n" + string.Join("\n", untranslated);
        Debug.Log("[DialogueLocalizationMigrator] " + message);
        EditorUtility.DisplayDialog("Dialogue Localization", message, "OK");
    }

    private static StringTableCollection GetOrCreateCollection(string tableName, Dictionary<string, StringTableCollection> cache)
    {
        if (cache.TryGetValue(tableName, out var cached))
            return cached;

        StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(tableName);
        if (collection == null)
        {
            collection = LocalizationEditorSettings.CreateStringTableCollection(tableName, TableDirectory);
            Debug.Log($"[DialogueLocalizationMigrator] 已创建 String Table Collection: {tableName}");
        }

        cache[tableName] = collection;
        return collection;
    }

    private static string MakeUniqueKey(SharedTableData sharedData, string baseKey)
    {
        string key = baseKey;
        int suffix = 1;
        while (sharedData.Contains(key))
            key = $"{baseKey}_{suffix++}";
        return key;
    }
}
