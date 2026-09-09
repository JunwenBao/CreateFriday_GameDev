using System.Collections.Generic;
using UnityEngine;

namespace Game.Story
{
    public enum ClueRequirementMode
    {
        /// <summary>Every listed clue must be owned.</summary>
        All,
        /// <summary>At least one listed clue must be owned.</summary>
        Any,
        /// <summary>At least <see cref="StoryCondition"/>.requiredCount clues must be owned.</summary>
        AtLeast
    }

    /// <summary>
    /// Declarative unlock rule for a story node. Combines clue requirements
    /// (AND / OR / N-of-M) with node prerequisites so writers can model
    /// branching investigation gates without any code.
    /// </summary>
    [System.Serializable]
    public class StoryCondition
    {
        [SerializeField] private List<ClueData> requiredClues = new List<ClueData>();
        [SerializeField] private ClueRequirementMode mode = ClueRequirementMode.All;
        [SerializeField, Min(1)] private int requiredCount = 1;

        [Tooltip("These nodes must already be unlocked before this condition can pass.")]
        [SerializeField] private List<StoryNodeData> prerequisiteNodes = new List<StoryNodeData>();

        public IReadOnlyList<ClueData> RequiredClues => requiredClues;
        public IReadOnlyList<StoryNodeData> PrerequisiteNodes => prerequisiteNodes;

        public bool IsSatisfied(IStoryState state)
        {
            if (state == null)
                return false;

            for (int i = 0; i < prerequisiteNodes.Count; i++)
            {
                var node = prerequisiteNodes[i];
                if (node != null && !state.IsNodeUnlocked(node.NodeID))
                    return false;
            }

            if (requiredClues == null || requiredClues.Count == 0)
                return true;

            int owned = 0;
            for (int i = 0; i < requiredClues.Count; i++)
            {
                var clue = requiredClues[i];
                if (clue != null && state.HasClue(clue.ClueID))
                    owned++;
            }

            switch (mode)
            {
                case ClueRequirementMode.All:
                    return owned >= requiredClues.Count;
                case ClueRequirementMode.Any:
                    return owned >= 1;
                case ClueRequirementMode.AtLeast:
                    return owned >= requiredCount;
                default:
                    return false;
            }
        }
    }
}
