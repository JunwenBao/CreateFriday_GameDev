using System;
using UnityEngine;

// 玩法事件总线：玩法系统只广播“发生了什么”，不关心谁在监听（剧情、成就、音效等）
public static class GameEvents
{
    public static event Action<DialogueData> DialogueFinished;
    public static event Action<ItemData> ItemPicked;
    public static event Action<string> AreaEntered;

    public static void RaiseDialogueFinished(DialogueData dialogue) => DialogueFinished?.Invoke(dialogue);
    public static void RaiseItemPicked(ItemData item) => ItemPicked?.Invoke(item);
    public static void RaiseAreaEntered(string areaID) => AreaEntered?.Invoke(areaID);

    // 关闭 Domain Reload 进入 Play 模式时，清掉上一次运行残留的订阅
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        DialogueFinished = null;
        ItemPicked = null;
        AreaEntered = null;
    }
}
