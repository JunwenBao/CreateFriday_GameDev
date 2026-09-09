using System.Collections.Generic;
using UnityEngine;

namespace Game.Story
{
    /// <summary>
    /// The single source of truth for all authored story content. It owns the
    /// chapter list and provides fast id-based lookups used by the runtime
    /// manager and the save system.
    /// </summary>
    [CreateAssetMenu(fileName = "StoryDatabase", menuName = "Story/Story Database", order = -10)]
    public class StoryDatabase : ScriptableObject
    {
        [SerializeField] private List<ChapterData> chapters = new List<ChapterData>();
        [SerializeField] private List<ClueData> allClues = new List<ClueData>();

        private Dictionary<string, ClueData> _clueLookup;
        private Dictionary<string, StoryNodeData> _nodeLookup;
        private Dictionary<string, ChapterData> _chapterLookup;
        private List<StoryNodeData> _allNodes;

        public IReadOnlyList<ChapterData> Chapters => chapters;
        public IReadOnlyList<ClueData> AllClues => allClues;

        public IReadOnlyList<StoryNodeData> AllNodes
        {
            get
            {
                EnsureBuilt();
                return _allNodes;
            }
        }

        public ClueData GetClue(string clueID)
        {
            EnsureBuilt();
            return clueID != null && _clueLookup.TryGetValue(clueID, out var c) ? c : null;
        }

        public StoryNodeData GetNode(string nodeID)
        {
            EnsureBuilt();
            return nodeID != null && _nodeLookup.TryGetValue(nodeID, out var n) ? n : null;
        }

        public ChapterData GetChapter(string chapterID)
        {
            EnsureBuilt();
            return chapterID != null && _chapterLookup.TryGetValue(chapterID, out var ch) ? ch : null;
        }

        /// <summary>Call after live-editing content; forces lookups to rebuild.</summary>
        public void Invalidate() => _clueLookup = null;

        private void EnsureBuilt()
        {
            if (_clueLookup != null)
                return;

            _clueLookup = new Dictionary<string, ClueData>();
            _nodeLookup = new Dictionary<string, StoryNodeData>();
            _chapterLookup = new Dictionary<string, ChapterData>();
            _allNodes = new List<StoryNodeData>();

            for (int i = 0; i < allClues.Count; i++)
                Register(_clueLookup, allClues[i]?.ClueID, allClues[i]);

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
                        _allNodes.Add(node);
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

#if UNITY_EDITOR
        [ContextMenu("Collect Clues From Nodes")]
        private void CollectCluesFromNodes()
        {
            var set = new HashSet<ClueData>(allClues);
            foreach (var chapter in chapters)
            {
                if (chapter == null) continue;
                foreach (var node in chapter.Nodes)
                {
                    if (node?.UnlockCondition == null) continue;
                    foreach (var clue in node.UnlockCondition.RequiredClues)
                        if (clue != null) set.Add(clue);
                }
            }

            allClues = new List<ClueData>(set);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
