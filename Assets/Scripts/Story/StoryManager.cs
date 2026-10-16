using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Story
{
    /// <summary>
    /// Runtime heart of the story system. Drives a strictly linear sequence of
    /// chapter nodes, fires chapter/story completion, and persists progress.
    ///
    /// Gameplay calls <see cref="TryUnlockNode"/>, which only succeeds for the
    /// next node in order. Everything else reacts via <see cref="StoryEventManager"/>.
    /// </summary>
    public class StoryManager : MonoBehaviour
    {
        public static StoryManager Instance { get; private set; }

        [Header("Content")]
        [SerializeField] private StoryDatabase database;

        [Header("Persistence")]
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private string saveFileName = "story_progress.json";

        [Header("Lifetime")]
        [SerializeField] private bool persistAcrossScenes = true;

        private readonly HashSet<string> _unlockedNodes = new HashSet<string>();
        // Index into database.AllNodes of the next node that may be unlocked.
        private int _nextIndex;

        public IReadOnlyCollection<string> UnlockedNodeIDs => _unlockedNodes;

        /// <summary>The only node that can be unlocked right now; null once the story is finished.</summary>
        public ChapterNodeData NextNode
        {
            get
            {
                if (database == null)
                    return null;
                var nodes = database.AllNodes;
                return _nextIndex < nodes.Count ? nodes[_nextIndex] : null;
            }
        }

        /// <summary>The chapter the player is currently in; null once the story is finished.</summary>
        public ChapterData CurrentChapter => database != null ? database.GetChapterOf(NextNode) : null;

        public bool StoryCompleted =>
            database != null && database.AllNodes.Count > 0 && _nextIndex >= database.AllNodes.Count;

        private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

        #region Unity lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (persistAcrossScenes)
                DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (loadOnStart)
                LoadProgress();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        #endregion

        #region Queries

        public bool IsNodeUnlocked(string nodeID) => nodeID != null && _unlockedNodes.Contains(nodeID);
        public bool IsNodeUnlocked(ChapterNodeData node) => node != null && IsNodeUnlocked(node.NodeID);

        public bool IsChapterCompleted(ChapterData chapter)
        {
            if (chapter == null || chapter.Nodes.Count == 0)
                return false;

            for (int i = 0; i < chapter.Nodes.Count; i++)
            {
                var node = chapter.Nodes[i];
                if (node != null && !IsNodeUnlocked(node))
                    return false;
            }
            return true;
        }

        public bool IsChapterCompleted(string chapterID) =>
            database != null && IsChapterCompleted(database.GetChapter(chapterID));

        #endregion

        #region Public API

        /// <summary>
        /// Unlocks <paramref name="node"/> if it is the next node in story order.
        /// Returns false and does nothing otherwise.
        /// </summary>
        public bool TryUnlockNode(ChapterNodeData node)
        {
            if (database == null)
            {
                Debug.LogError("[Story] No StoryDatabase assigned.", this);
                return false;
            }

            if (node == null || IsNodeUnlocked(node))
                return false;

            var next = NextNode;
            if (node != next)
            {
                Debug.Log($"[Story] '{node.Title}' is not reachable yet; next node is '{(next != null ? next.Title : "<none>")}'.", this);
                return false;
            }

            UnlockNext();
            return true;
        }

        #endregion

        private void UnlockNext()
        {
            var node = database.AllNodes[_nextIndex];
            _unlockedNodes.Add(node.NodeID);
            _nextIndex++;
            Debug.Log($"[Story] Node unlocked: {node.Title}", this);

            StoryEventManager.RaiseNodeUnlocked(node);

            // Linear story: a chapter ends exactly when the next node belongs to another chapter.
            var chapter = database.GetChapterOf(node);
            if (chapter != database.GetChapterOf(NextNode))
            {
                Debug.Log($"[Story] Chapter completed: {chapter.Title}", this);
                StoryEventManager.RaiseChapterCompleted(chapter);
            }

            if (StoryCompleted)
            {
                Debug.Log("[Story] Story complete.", this);
                StoryEventManager.RaiseStoryCompleted();
            }

            // Run last so an action that unlocks the following node sees a consistent state.
            node.RunUnlockActions(this);
        }

        #region Persistence

        public void SaveProgress()
        {
            var progress = new StoryProgress
            {
                unlockedNodeIDs = new List<string>(_unlockedNodes)
            };

            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(progress, true));
                Debug.Log($"[Story] Progress saved to {SavePath}", this);
            }
            catch (IOException e)
            {
                Debug.LogError($"[Story] Failed to save progress: {e.Message}", this);
            }
        }

        public void LoadProgress()
        {
            if (!File.Exists(SavePath))
                return;

            StoryProgress progress;
            try
            {
                progress = JsonUtility.FromJson<StoryProgress>(File.ReadAllText(SavePath));
            }
            catch (IOException e)
            {
                Debug.LogError($"[Story] Failed to read save file: {e.Message}", this);
                return;
            }

            if (progress != null)
                ApplyProgress(progress);
        }

        /// <summary>
        /// Restores a snapshot without re-running unlock actions (they already
        /// happened when the player first reached each node). Only the unbroken
        /// prefix of unlocked nodes is kept, so reordered content stays linear.
        /// </summary>
        public void ApplyProgress(StoryProgress progress)
        {
            _unlockedNodes.Clear();
            _nextIndex = 0;

            if (database != null && progress.unlockedNodeIDs != null)
            {
                var saved = new HashSet<string>(progress.unlockedNodeIDs);
                var nodes = database.AllNodes;
                while (_nextIndex < nodes.Count && saved.Contains(nodes[_nextIndex].NodeID))
                {
                    _unlockedNodes.Add(nodes[_nextIndex].NodeID);
                    _nextIndex++;
                }
            }

            StoryEventManager.RaiseProgressLoaded(progress);
        }

        /// <summary>Wipes runtime state and deletes the save file. Handy for New Game.</summary>
        public void ResetProgress()
        {
            _unlockedNodes.Clear();
            _nextIndex = 0;

            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }

        #endregion
    }
}