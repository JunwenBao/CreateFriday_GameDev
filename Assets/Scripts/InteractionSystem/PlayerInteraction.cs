using UnityEngine;

// 挂在玩家上：检测附近的可互动物品，高亮显示，并在按 E 时执行互动
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private DialogueTrigger dialogueTrigger;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Inventory inventory;

    private Interactable currentInteractable;

    private void Awake()
    {
        if (dialogueTrigger == null)
        {
            dialogueTrigger = GetComponent<DialogueTrigger>();
        }

        if (playerAnimator == null)
        {
            playerAnimator = GetComponentInChildren<Animator>();
        }

        if (inventory == null)
        {
            inventory = GetComponent<Inventory>();
        }
    }

    private void Update()
    {
        // 对话进行中时，E键交给对话系统处理，避免重复触发
        if (dialogueTrigger != null && dialogueTrigger.IsDialogueActive)
        {
            return;
        }

        if (currentInteractable != null && Input.GetKeyDown(interactKey))
        {
            currentInteractable.Interact(dialogueTrigger, playerAnimator, inventory);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Interactable interactable = other.GetComponent<Interactable>();
        if (interactable == null) return;

        if (currentInteractable != null)
        {
            currentInteractable.SetHighlight(false);
        }

        currentInteractable = interactable;
        currentInteractable.SetHighlight(true);
    }

    private void OnTriggerExit(Collider other)
    {
        Interactable interactable = other.GetComponent<Interactable>();
        if (interactable != null && interactable == currentInteractable)
        {
            currentInteractable.SetHighlight(false);
            currentInteractable = null;
        }
    }
}
