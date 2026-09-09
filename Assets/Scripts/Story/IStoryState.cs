namespace Game.Story
{
    /// <summary>
    /// Read-only view of the player's current story progression.
    /// Conditions query this instead of the concrete manager so unlock logic
    /// stays decoupled and unit-testable.
    /// </summary>
    public interface IStoryState
    {
        bool HasClue(string clueID);
        bool IsNodeUnlocked(string nodeID);
        bool IsChapterCompleted(string chapterID);
    }
}
