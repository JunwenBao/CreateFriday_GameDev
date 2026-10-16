using UnityEngine;

namespace Game.Story
{
    /// <summary>Writes a message to the console. Useful during production.</summary>
    [CreateAssetMenu(fileName = "Action_Log", menuName = "Story/Actions/Debug Log")]
    public class DebugLogStoryAction : StoryAction
    {
        [SerializeField, TextArea] private string message;

        public override void Execute(StoryManager manager, ChapterNodeData sourceNode)
        {
            Debug.Log($"[Story] {message}", manager);
        }
    }
}
