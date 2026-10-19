using UnityEngine;

public class NPC : InteractableBase
{
    public NPCData data;
    public DialogueData dialogue;
    public bool npcIsTalking;

    private Animator animator;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
    }

    public override void Interact(PlayerInteraction player)
    {
        if (player.DialogueTrigger != null)
        {
            SetHighlight(false);
            player.DialogueTrigger.StartNPCDialogue(this);
        }
    }

    public void TurnToPlayer(Vector3 playerPosition)
    {
        Vector3 direction = (playerPosition - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}