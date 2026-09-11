using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Game Data", menuName = "NPC")]
public class NPCData : ScriptableObject
{
    public string npcName;
    public DialogueData dialogue;
}