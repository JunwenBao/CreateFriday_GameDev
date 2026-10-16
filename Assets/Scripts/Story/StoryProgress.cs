using System.Collections.Generic;

namespace Game.Story
{
    /// <summary>
    /// Plain serializable snapshot of player progression. Persisted as JSON.
    /// Chapter completion and story completion are derived from the unlocked nodes.
    /// </summary>
    [System.Serializable]
    public class StoryProgress
    {
        public List<string> unlockedNodeIDs = new List<string>();
    }
}
