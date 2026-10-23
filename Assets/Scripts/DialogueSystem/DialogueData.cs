using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Serialization;

public enum Speaker
{
    Player,
    NPC
}

// 一句对话：说话人 + 台词 Key（文本存放在 Localization 的 String Table 中）
[Serializable]
public struct DialogueLine
{
    public Speaker speaker;
    public string animTrigger;

    [Tooltip("String Table 中的 Key，运行时根据当前语言显示对应文本")]
    public string key;

    [Tooltip("尚未本地化的原文。运行菜单 Tools/Localization/Migrate Dialogue Text To Keys 后会自动生成 Key 并清空此处")]
    [FormerlySerializedAs("text")]
    [TextArea(2, 4)]
    public string sourceText;
}

[CreateAssetMenu(fileName = "Game Data", menuName = "Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public const string DefaultTableName = "Dialogue";

    [Tooltip("台词所在的 String Table Collection 名称")]
    public string tableName = DefaultTableName;

    public List<DialogueLine> conversationBlock;

    // 按当前语言获取台词；未配置 Key 时退回原文，方便迁移前继续使用
    public string GetLineText(DialogueLine line)
    {
        if (string.IsNullOrEmpty(line.key))
            return line.sourceText;

        string table = string.IsNullOrEmpty(tableName) ? DefaultTableName : tableName;
        return LocalizationSettings.StringDatabase.GetLocalizedString(table, line.key);
    }
}
