using System.Collections.Generic;
using UnityEngine;

namespace Game.Story
{
    /// <summary>
    /// An ordered group of story nodes forming one chapter of the mystery.
    /// A chapter is complete once all of its nodes are unlocked.
    /// </summary>
    [CreateAssetMenu(fileName = "Chapter_", menuName = "Story/Chapter", order = 2)]
    public class ChapterData : ScriptableObject
    {
        [SerializeField] private string chapterID;
        [SerializeField] private string title;
        [SerializeField, TextArea(2, 5)] private string synopsis;
        [SerializeField] private List<StoryNodeData> nodes = new List<StoryNodeData>();

        public string ChapterID => chapterID;
        public string Title => title;
        public string Synopsis => synopsis;
        public IReadOnlyList<StoryNodeData> Nodes => nodes;

        public bool IsCompleted(IStoryState state)
        {
            if (nodes == null || nodes.Count == 0)
                return false;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node != null && !state.IsNodeUnlocked(node.NodeID))
                    return false;
            }
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(chapterID))
                chapterID = System.Guid.NewGuid().ToString("N");
        }
#endif
    }
}
