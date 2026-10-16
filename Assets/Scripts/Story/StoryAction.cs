using UnityEngine;

namespace Game.Story
{
    /// <summary>
    /// A composable side-effect that runs when a chapter node unlocks.
    /// Implement subclasses (play a Timeline, start dialogue, post a Wwise
    /// event, open a door, ...) and drop them onto a node's action list.
    /// Each subclass must live in a file matching its class name.
    /// </summary>
    public abstract class StoryAction : ScriptableObject
    {
        public abstract void Execute(StoryManager manager, ChapterNodeData sourceNode);
    }
}
