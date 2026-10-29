using UnityEngine;

public enum InteractableType
{
    None,
    Pickup,
    Door,
    Message
}

// 挂在可互动物品上：拾取 / 消息等物品类互动
public class Interactable : InteractableBase
{
    [SerializeField] private InteractableType interactableType = InteractableType.None;
    [SerializeField] private string interactionMessage = "你与物品发生了互动！";
    [SerializeField] private DialogueData dialogue;   // Message 类型持有的对话数据

    [Header("Pickup")]
    [SerializeField] private ItemData pickupItem;
    private bool isPickedUp;   // 防止销毁延迟期间重复拾取

    // 拾取后立即隐藏高亮和互动提示，不必等物体销毁
    public override bool CanInteract => !isPickedUp;

    public override void Interact(PlayerInteraction player)
    {
        if (interactableType == InteractableType.Message)
        {
            if (dialogue != null && player.DialogueTrigger != null)
            {
                player.DialogueTrigger.StartItemDialogue(dialogue);
            }
            else
            {
                Debug.Log(interactionMessage);
            }
        }
        else if (interactableType == InteractableType.Pickup)
        {
            if (isPickedUp) return;

            isPickedUp = true;
            if (pickupItem != null) GameEvents.RaiseItemPicked(pickupItem);

            if (player.Animator != null)
            {
                player.Animator.SetTrigger("Pickup");
            }

            Destroy(this.gameObject, 0.8f);
        }
    }
}