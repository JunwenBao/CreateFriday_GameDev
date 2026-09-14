using System;
using System.Collections.Generic;
using UnityEngine;

public enum Speaker
{
    Player,
    NPC
}

// 一句对话：说话人 + 台词内容
[Serializable]
public struct DialogueLine
{
    public Speaker speaker;
    [TextArea(2, 4)]
    public string text;
}

[CreateAssetMenu(fileName = "Game Data", menuName = "Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public List<DialogueLine> conversationBlock;
}