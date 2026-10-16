using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Story;

public class NPC : MonoBehaviour
{
    public NPCData data;
    public DialogueData dialogue;
    public bool npcIsTalking;

    [Tooltip("对话结束后尝试解锁的剧情节点，只有它是下一个节点时才会成功；可留空")]
    public ChapterNodeData storyNodeOnTalk;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
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