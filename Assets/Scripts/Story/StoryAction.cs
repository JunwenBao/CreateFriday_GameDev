using UnityEngine;

namespace Game.Story
{
    /// <summary>
    /// A composable side-effect that runs when a story node unlocks.
    /// Implement subclasses (play a Timeline, start dialogue, post a Wwise
    /// event, open a door, ...) and drop them onto a node's action list.
    /// Being ScriptableObjects, actions are reusable across nodes and
    /// authored entirely in the inspector.
    /// </summary>
    public abstract class StoryAction : ScriptableObject
    {
        public abstract void Execute(StoryManager manager, StoryNodeData sourceNode);
    }

    /// <summary>Grants another clue when a node unlocks, enabling clue chains.</summary>
    [CreateAssetMenu(fileName = "Action_GrantClue", menuName = "Story/Actions/Grant Clue")]
    public class GrantClueAction : StoryAction
    {
        [SerializeField] private ClueData clue;

        public override void Execute(StoryManager manager, StoryNodeData sourceNode)
        {
            if (manager != null && clue != null)
                manager.CollectClue(clue);
        }
    }

    /// <summary>Writes a message to the console. Useful during production.</summary>
    [CreateAssetMenu(fileName = "Action_Log", menuName = "Story/Actions/Debug Log")]
    public class DebugLogStoryAction : StoryAction
    {
        [SerializeField, TextArea] private string message;

        public override void Execute(StoryManager manager, StoryNodeData sourceNode)
        {
            Debug.Log($"[Story] {message}", manager);
        }
    }
}
