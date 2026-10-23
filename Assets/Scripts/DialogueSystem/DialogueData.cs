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

// 玩家选择某个选项后对话的走向
public enum DialogueChoiceOutcome
{
    Continue,     // 继续播放下一句
    EndDialogue,  // 立即正常结束对话（会广播 DialogueFinished）
    GameOver      // 结束对话并触发游戏结束（不广播 DialogueFinished）
}

// 对话中的一个选项：选项文本 Key + 选择后的结果
[Serializable]
public struct DialogueChoice
{
    [Tooltip("String Table 中的 Key，运行时根据当前语言显示对应文本")]
    public string key;

    [Tooltip("尚未本地化的选项原文，迁移工具会自动生成 Key 并清空此处")]
    [TextArea(1, 3)]
    public string sourceText;

    public DialogueChoiceOutcome outcome;
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

    [Tooltip("这句台词显示完后让玩家选择；留空则按 E 进入下一句")]
    public List<DialogueChoice> choices;

    public bool HasChoices => choices != null && choices.Count > 0;
}

[CreateAssetMenu(fileName = "Game Data", menuName = "Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public const string DefaultTableName = "Dialogue";

    [Tooltip("台词所在的 String Table Collection 名称")]
    public string tableName = DefaultTableName;

    public List<DialogueLine> conversationBlock;

    // 按当前语言获取台词；未配置 Key 时退回原文，方便迁移前继续使用
    public string GetLineText(DialogueLine line) => GetText(line.key, line.sourceText);

    public string GetChoiceText(DialogueChoice choice) => GetText(choice.key, choice.sourceText);

    private string GetText(string key, string sourceText)
    {
        if (string.IsNullOrEmpty(key))
            return sourceText;

        string table = string.IsNullOrEmpty(tableName) ? DefaultTableName : tableName;
        return LocalizationSettings.StringDatabase.GetLocalizedString(table, key);
    }
}
