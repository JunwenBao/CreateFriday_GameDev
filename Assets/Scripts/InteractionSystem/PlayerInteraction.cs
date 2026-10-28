using UnityEngine;

// 挂在玩家上：检测附近的可互动物品，高亮显示，并在按 E 时执行互动
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private DialogueTrigger dialogueTrigger;
    [SerializeField] private Animator playerAnimator;

    private InteractableBase currentInteractable;

    public DialogueTrigger DialogueTrigger => dialogueTrigger;
    public Animator Animator => playerAnimator;

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
    }

    private void Update()
    {
        // 对话进行中时，E键交给对话系统处理，避免重复触发
        if (dialogueTrigger != null && (dialogueTrigger.IsDialogueActive || dialogueTrigger.DialogueEndedThisFrame))
        {
            return;
        }

        if (currentInteractable == null) return;

        if (!currentInteractable.CanInteract)
        {
            currentInteractable.SetHighlight(false);
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            currentInteractable.Interact(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        InteractableBase interactable = other.GetComponent<InteractableBase>();
        if (interactable == null) return;

        if (currentInteractable != null)
        {
            currentInteractable.SetHighlight(false);
        }

        currentInteractable = interactable;
        currentInteractable.SetHighlight(interactable.CanInteract);
    }

    private void OnTriggerExit(Collider other)
    {
        InteractableBase interactable = other.GetComponent<InteractableBase>();
        if (interactable != null && interactable == currentInteractable)
        {
            currentInteractable.SetHighlight(false);
            currentInteractable = null;
        }
    }
}
