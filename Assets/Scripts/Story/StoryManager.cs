using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Story
{
    /// <summary>
    /// Runtime heart of the story system. Tracks collected clues, evaluates
    /// unlock conditions (including chained unlocks), advances chapters, fires
    /// the final reveal, and persists progress to disk.
    ///
    /// Gameplay talks to it through a tiny surface: <see cref="CollectClue(ClueData)"/>.
    /// Everything else reacts via <see cref="StoryEventManager"/>.
    /// </summary>
    public class StoryManager : MonoBehaviour, IStoryState
    {
        public static StoryManager Instance { get; private set; }

        [Header("Content")]
        [SerializeField] private StoryDatabase database;

        [Header("Persistence")]
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private string saveFileName = "story_progress.json";

        [Header("Lifetime")]
        [SerializeField] private bool persistAcrossScenes = true;

        private readonly HashSet<string> _collectedClues = new HashSet<string>();
        private readonly HashSet<string> _unlockedNodes = new HashSet<string>();
        private readonly HashSet<string> _completedChapters = new HashSet<string>();
        private bool _storyCompleted;
        private bool _evaluating;

        public bool StoryCompleted => _storyCompleted;
        public IReadOnlyCollection<string> CollectedClueIDs => _collectedClues;
        public IReadOnlyCollection<string> UnlockedNodeIDs => _unlockedNodes;

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

        #region IStoryState

        public bool HasClue(string clueID) => clueID != null && _collectedClues.Contains(clueID);
        public bool IsNodeUnlocked(string nodeID) => nodeID != null && _unlockedNodes.Contains(nodeID);
        public bool IsChapterCompleted(string chapterID) => chapterID != null && _completedChapters.Contains(chapterID);

        #endregion

        #region Public API

        /// <summary>Grant a clue by asset reference (e.g. from a world pickup).</summary>
        public void CollectClue(ClueData clue)
        {
            if (clue == null)
            {
                Debug.LogWarning("[Story] CollectClue called with null clue.", this);
                return;
            }
            CollectClueInternal(clue);
        }

        /// <summary>Grant a clue by id (e.g. from a save or scripted event).</summary>
        public void CollectClue(string clueID)
        {
            var clue = database != null ? database.GetClue(clueID) : null;
            if (clue == null)
            {
                Debug.LogWarning($"[Story] No clue with id '{clueID}' in database.", this);
                return;
            }
            CollectClueInternal(clue);
        }

        public bool HasClue(ClueData clue) => clue != null && HasClue(clue.ClueID);

        #endregion

        private void CollectClueInternal(ClueData clue)
        {
            if (!_collectedClues.Add(clue.ClueID))
                return; // already owned

            Debug.Log($"[Story] Clue collected: {clue.ClueName}", this);
            StoryEventManager.RaiseClueCollected(clue);

            EvaluateProgression();
        }

        /// <summary>
        /// Re-evaluates every locked node until the state is stable. Looping is
        /// required because unlocking a node can grant new clues, which may in
        /// turn satisfy further nodes in the same frame.
        /// </summary>
        private void EvaluateProgression()
        {
            if (database == null)
            {
                Debug.LogError("[Story] No StoryDatabase assigned.", this);
                return;
            }

            // Re-entrancy guard: a GrantClueAction calls back into CollectClue.
            if (_evaluating)
                return;

            _evaluating = true;
            try
            {
                var nodes = database.AllNodes;
                bool changed;
                do
                {
                    changed = false;
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        var node = nodes[i];
                        if (node == null || _unlockedNodes.Contains(node.NodeID))
                            continue;

                        if (node.CanUnlock(this))
                        {
                            UnlockNode(node);
                            changed = true;
                        }
                    }
                }
                while (changed);
            }
            finally
            {
                _evaluating = false;
            }

            EvaluateChapters();
        }

        private void UnlockNode(StoryNodeData node)
        {
            _unlockedNodes.Add(node.NodeID);
            Debug.Log($"[Story] Node unlocked: {node.Title}", this);

            node.RunUnlockActions(this);
            StoryEventManager.RaiseStoryNodeUnlocked(node);

            if (node.IsTruthReveal && !_storyCompleted)
            {
                _storyCompleted = true;
                Debug.Log("[Story] The truth has been revealed. Story complete.", this);
                StoryEventManager.RaiseStoryCompleted();
            }
        }

        private void EvaluateChapters()
        {
            var chapters = database.Chapters;
            for (int i = 0; i < chapters.Count; i++)
            {
                var chapter = chapters[i];
                if (chapter == null || _completedChapters.Contains(chapter.ChapterID))
                    continue;

                if (chapter.IsCompleted(this))
                {
                    _completedChapters.Add(chapter.ChapterID);
                    Debug.Log($"[Story] Chapter completed: {chapter.Title}", this);
                    StoryEventManager.RaiseChapterCompleted(chapter);
                }
            }
        }

        #region Persistence

        public void SaveProgress()
        {
            var progress = new StoryProgress
            {
                collectedClueIDs = new List<string>(_collectedClues),
                unlockedNodeIDs = new List<string>(_unlockedNodes),
                completedChapterIDs = new List<string>(_completedChapters),
                storyCompleted = _storyCompleted
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
        /// happened when the player first reached each beat).
        /// </summary>
        public void ApplyProgress(StoryProgress progress)
        {
            _collectedClues.Clear();
            _unlockedNodes.Clear();
            _completedChapters.Clear();

            if (progress.collectedClueIDs != null)
                _collectedClues.UnionWith(progress.collectedClueIDs);
            if (progress.unlockedNodeIDs != null)
                _unlockedNodes.UnionWith(progress.unlockedNodeIDs);
            if (progress.completedChapterIDs != null)
                _completedChapters.UnionWith(progress.completedChapterIDs);

            _storyCompleted = progress.storyCompleted;

            StoryEventManager.RaiseProgressLoaded(progress);
        }

        /// <summary>Wipes runtime state and deletes the save file. Handy for New Game.</summary>
        public void ResetProgress()
        {
            _collectedClues.Clear();
            _unlockedNodes.Clear();
            _completedChapters.Clear();
            _storyCompleted = false;

            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }

        #endregion
    }
}