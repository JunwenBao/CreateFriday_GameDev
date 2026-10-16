using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Story
{
    /// <summary>
    /// A single event within a chapter. Nodes unlock strictly in order:
    /// a node can only be triggered once the node before it is unlocked.
    /// </summary>
    [CreateAssetMenu(fileName = "ChapterNode_", menuName = "Story/Chapter Node", order = 1)]
    public class ChapterNodeData : ScriptableObject
    {
        [Tooltip("Stable unique id used for saving. Auto-generated if left empty.")]
        [SerializeField] private string nodeID;
        [SerializeField] private string title;
        [SerializeField, TextArea(3, 8)] private string narrative;

        [SerializeField] private List<StoryAction> onUnlockActions = new List<StoryAction>();

        public string NodeID => nodeID;
        public string Title => title;
        public string Narrative => narrative;

        public void RunUnlockActions(StoryManager manager)
        {
            if (onUnlockActions == null)
                return;

            for (int i = 0; i < onUnlockActions.Count; i++)
            {
                var action = onUnlockActions[i];
                if (action == null)
                    continue;

                try
                {
                    action.Execute(manager, this);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Story] Action '{action.name}' on node '{nodeID}' threw: {e}", manager);
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(nodeID))
                nodeID = Guid.NewGuid().ToString("N");
        }
#endif
    }
}