using System.Collections.Generic;

namespace Game.Story
{
    /// <summary>
    /// Plain serializable snapshot of player progression. Persisted as JSON.
    /// Only stores stable ids so authored content can be re-ordered or extended
    /// without invalidating existing saves.
    /// </summary>
    [System.Serializable]
    public class StoryProgress
    {
        public List<string> collectedClueIDs = new List<string>();
        public List<string> unlockedNodeIDs = new List<string>();
        public List<string> completedChapterIDs = new List<string>();
        public bool storyCompleted;
    }
}
