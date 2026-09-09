using System;
using UnityEngine;

namespace Game.Story
{
    /// <summary>
    /// Global, strongly-typed event hub that decouples story producers
    /// (pickups, triggers, cutscenes) from consumers (UI, audio, quest log).
    /// Statics are reset on every play session so no stale subscriptions
    /// survive domain-reload-disabled entering of Play mode.
    /// </summary>
    public static class StoryEventManager
    {
        public static event Action<ClueData> ClueCollected;
        public static event Action<StoryNodeData> StoryNodeUnlocked;
        public static event Action<ChapterData> ChapterCompleted;
        public static event Action StoryCompleted;
        public static event Action<StoryProgress> ProgressLoaded;

        public static void RaiseClueCollected(ClueData clue) => ClueCollected?.Invoke(clue);
        public static void RaiseStoryNodeUnlocked(StoryNodeData node) => StoryNodeUnlocked?.Invoke(node);
        public static void RaiseChapterCompleted(ChapterData chapter) => ChapterCompleted?.Invoke(chapter);
        public static void RaiseStoryCompleted() => StoryCompleted?.Invoke();
        public static void RaiseProgressLoaded(StoryProgress progress) => ProgressLoaded?.Invoke(progress);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ClueCollected = null;
            StoryNodeUnlocked = null;
            ChapterCompleted = null;
            StoryCompleted = null;
            ProgressLoaded = null;
        }
    }
}