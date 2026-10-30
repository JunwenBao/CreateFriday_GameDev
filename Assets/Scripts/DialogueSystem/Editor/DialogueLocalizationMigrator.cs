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

                if (line.HasChoices)
                {
                    for (int c = 0; c < line.choices.Count; c++)
                    {
                        DialogueChoice choice = line.choices[c];

                        if (choice.replies != null)
                        {
                            for (int r = 0; r < choice.replies.Count; r++)
                            {
                                DialogueReply reply = choice.replies[r];
                                if (!string.IsNullOrEmpty(reply.key) || string.IsNullOrEmpty(reply.sourceText))
                                    continue;

                                reply.key = AddEntry(collection, $"{keyPrefix}.{i:D2}.choice{c}.reply{r}", reply.sourceText, untranslated);
                                reply.sourceText = string.Empty;
                                choice.replies[r] = reply;
                                changed = true;
                                migratedCount++;
                            }
                        }

                        if (!string.IsNullOrEmpty(choice.key) || string.IsNullOrEmpty(choice.sourceText))
                            continue;

                        choice.key = AddEntry(collection, $"{keyPrefix}.{i:D2}.choice{c}", choice.sourceText, untranslated);
                        choice.sourceText = string.Empty;
                        line.choices[c] = choice;
                        changed = true;
                        migratedCount++;
                    }
                }

                if (!string.IsNullOrEmpty(line.key) || string.IsNullOrEmpty(line.sourceText))
                    continue;

                line.key = AddEntry(collection, $"{keyPrefix}.{i:D2}", line.sourceText, untranslated);
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

        string message = $"已迁移 {migratedCount} 条文本（台词 / 选项）。";
        if (untranslated.Count > 0)
            message += "\n以下条目目前用英文原文占位，请在 Localization Tables 窗口中翻译：\n" + string.Join("\n", untranslated);
        Debug.Log("[DialogueLocalizationMigrator] " + message);
        EditorUtility.DisplayDialog("Dialogue Localization", message, "OK");
    }

    private static string AddEntry(StringTableCollection collection, string baseKey, string sourceText, List<string> untranslated)
    {
        string key = MakeUniqueKey(collection.SharedData, baseKey);
        collection.SharedData.AddKey(key);

        foreach (StringTable table in collection.StringTables)
        {
            // 原文写入英文表；其他语言先用原文占位，避免游戏中出现 "No translation found"
            table.AddEntry(key, sourceText);
            EditorUtility.SetDirty(table);
            if (table.LocaleIdentifier.Code != SourceLocaleCode)
                untranslated.Add($"{table.LocaleIdentifier.Code}: {key}");
        }

        return key;
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
