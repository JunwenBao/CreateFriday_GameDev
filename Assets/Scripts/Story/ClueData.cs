using System;
using UnityEngine;

namespace Game.Story
{
    public enum ClueCategory
    {
        Evidence,
        Testimony,
        Document,
        Location,
        Memory
    }

    /// <summary>
    /// Authoring data for a single collectable clue. One asset per clue.
    /// Fields are private-serialized and exposed read-only so gameplay code
    /// can never mutate authored content at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "Clue_", menuName = "Story/Clue", order = 0)]
    public class ClueData : ScriptableObject
    {
        [Tooltip("Stable unique id used for saving. Auto-generated if left empty.")]
        [SerializeField] private string clueID;
        [SerializeField] private string clueName;
        [SerializeField, TextArea(2, 5)] private string description;
        [SerializeField] private ClueCategory category = ClueCategory.Evidence;
        [SerializeField] private Sprite icon;

        [Tooltip("Critical clues are required to reach the story's final reveal.")]
        [SerializeField] private bool isCritical;

        public string ClueID => clueID;
        public string ClueName => clueName;
        public string Description => description;
        public ClueCategory Category => category;
        public Sprite Icon => icon;
        public bool IsCritical => isCritical;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(clueID))
                clueID = Guid.NewGuid().ToString("N");
        }
#endif
    }
}
