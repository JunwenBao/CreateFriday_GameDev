using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Story
{
    /// <summary>
    /// A single beat of the narrative. It becomes "unlocked" once its
    /// <see cref="StoryCondition"/> is met, runs its actions, and (optionally)
    /// marks the story's final reveal.
    /// </summary>
    [CreateAssetMenu(fileName = "Node_", menuName = "Story/Story Node", order = 1)]
    public class StoryNodeData : ScriptableObject
    {
        [Tooltip("Stable unique id used for saving. Auto-generated if left empty.")]
        [SerializeField] private string nodeID;
        [SerializeField] private string title;
        [SerializeField, TextArea(3, 8)] private string narrative;

        [SerializeField] private StoryCondition unlockCondition = new StoryCondition();
        [SerializeField] private List<StoryAction> onUnlockActions = new List<StoryAction>();

        [Tooltip("When this node unlocks, the mystery is considered solved.")]
        [SerializeField] private bool isTruthReveal;

        public string NodeID => nodeID;
        public string Title => title;
        public string Narrative => narrative;
        public bool IsTruthReveal => isTruthReveal;
        public StoryCondition UnlockCondition => unlockCondition;

        public bool CanUnlock(IStoryState state)
        {
            return unlockCondition != null && unlockCondition.IsSatisfied(state);
        }

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