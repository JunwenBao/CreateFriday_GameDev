using System.Collections.Generic;
using UnityEngine;

namespace Game.Story
{
    /// <summary>
    /// The single source of truth for all authored story content. The order of
    /// <see cref="chapters"/> and of each chapter's nodes defines the linear
    /// story order.
    /// </summary>
    [CreateAssetMenu(fileName = "StoryDatabase", menuName = "Story/Story Database", order = -10)]
    public class StoryDatabase : ScriptableObject
    {
        [Tooltip("Played strictly top to bottom.")]
        [SerializeField] private List<ChapterData> chapters = new List<ChapterData>();

        private Dictionary<string, ChapterNodeData> _nodeLookup;
        private Dictionary<string, ChapterData> _chapterLookup;
        private Dictionary<ChapterNodeData, ChapterData> _nodeToChapter;
        private List<ChapterNodeData> _allNodes;

        public IReadOnlyList<ChapterData> Chapters => chapters;

        /// <summary>Every node across all chapters, flattened in story order.</summary>
        public IReadOnlyList<ChapterNodeData> AllNodes
        {
            get
            {
                EnsureBuilt();
                return _allNodes;
            }
        }

        public ChapterNodeData GetNode(string nodeID)
        {
            EnsureBuilt();
            return nodeID != null && _nodeLookup.TryGetValue(nodeID, out var n) ? n : null;
        }

        public ChapterData GetChapter(string chapterID)
        {
            EnsureBuilt();
            return chapterID != null && _chapterLookup.TryGetValue(chapterID, out var ch) ? ch : null;
        }

        public ChapterData GetChapterOf(ChapterNodeData node)
        {
            EnsureBuilt();
            return node != null && _nodeToChapter.TryGetValue(node, out var ch) ? ch : null;
        }

        /// <summary>Call after live-editing content; forces lookups to rebuild.</summary>
        public void Invalidate() => _nodeLookup = null;

        private void EnsureBuilt()
        {
            if (_nodeLookup != null)
                return;

            _nodeLookup = new Dictionary<string, ChapterNodeData>();
            _chapterLookup = new Dictionary<string, ChapterData>();
            _nodeToChapter = new Dictionary<ChapterNodeData, ChapterData>();
            _allNodes = new List<ChapterNodeData>();

            for (int c = 0; c < chapters.Count; c++)
            {
                var chapter = chapters[c];
                if (chapter == null)
                    continue;

                Register(_chapterLookup, chapter.ChapterID, chapter);

                for (int n = 0; n < chapter.Nodes.Count; n++)
                {
                    var node = chapter.Nodes[n];
                    if (node == null)
                        continue;

                    if (Register(_nodeLookup, node.NodeID, node))
                    {
                        _allNodes.Add(node);
                        _nodeToChapter[node] = chapter;
                    }
                }
            }
        }

        private static bool Register<T>(Dictionary<string, T> map, string id, T value) where T : Object
        {
            if (value == null || string.IsNullOrEmpty(id))
                return false;

            if (map.ContainsKey(id))
            {
                Debug.LogError($"[Story] Duplicate id '{id}' on '{value.name}'. Ids must be unique.", value);
                return false;
            }

            map.Add(id, value);
            return true;
        }
    }
}
