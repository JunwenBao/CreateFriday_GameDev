using System.Collections.Generic;
using UnityEngine;

namespace Game.Story
{
    /// <summary>
    /// One chapter of the story: a linear sequence of <see cref="ChapterNodeData"/>s.
    /// A chapter is complete once its last node is unlocked.
    /// </summary>
    [CreateAssetMenu(fileName = "Chapter_", menuName = "Story/Chapter", order = 0)]
    public class ChapterData : ScriptableObject
    {
        [SerializeField] private string chapterID;
        [SerializeField] private string title;
        [SerializeField, TextArea(2, 5)] private string synopsis;
        [Tooltip("Unlocked strictly top to bottom.")]
        [SerializeField] private List<ChapterNodeData> nodes = new List<ChapterNodeData>();

        public string ChapterID => chapterID;
        public string Title => title;
        public string Synopsis => synopsis;
        public IReadOnlyList<ChapterNodeData> Nodes => nodes;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(chapterID))
                chapterID = System.Guid.NewGuid().ToString("N");
        }
#endif
    }
}
