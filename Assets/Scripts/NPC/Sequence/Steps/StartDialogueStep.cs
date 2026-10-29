using System;
using System.Collections;
using UnityEngine;

// 让 NPC 主动与玩家开始对话（切对话相机、弹对话框），可等对话结束再继续
[Serializable]
public class StartDialogueStep : SequenceStep
{
    [Tooltip("要播放的对话，留空则播放 NPC 当前的对话")]
    [SerializeField] private DialogueData dialogue;
    [Tooltip("把这段对话设为 NPC 之后的默认对话，玩家再按 F 时播放它而不是旧对话")]
    [SerializeField] private bool setAsNPCDialogue = true;
    [SerializeField] private bool waitUntilFinished = true;

    public override IEnumerator Run(SequenceContext context)
    {
        NPC npc = context.Npc;
        DialogueTrigger player = context.Player;

        DialogueData data = dialogue != null ? dialogue : npc.dialogue;
        if (data == null)
        {
            Debug.LogWarning($"{npc.name}: StartDialogueStep has no dialogue, skipped.", npc);
            yield break;
        }
        if (setAsNPCDialogue) npc.dialogue = data;

        // 玩家正在进行其他对话（如查看物品）时，等它结束
        while (player.IsDialogueActive) yield return null;

        player.StartNPCDialogue(npc, data);
        if (!player.IsDialogueActive)
        {
            Debug.LogWarning($"{npc.name}: failed to start dialogue '{data.name}'.", npc);
            yield break;
        }

        if (waitUntilFinished)
        {
            while (player.IsDialogueActive) yield return null;
        }
    }
}
