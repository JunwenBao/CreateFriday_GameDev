using System;
using UnityEngine;

namespace Game.Story
{
    /// <summary>
    /// The gameplay event a <see cref="ChapterNodeData"/> waits for. Only the
    /// current <see cref="StoryManager.NextNode"/>'s trigger is bound at any time.
    /// Add a new trigger type by subclassing this; it appears in the node's dropdown automatically.
    /// </summary>
    [Serializable]
    public abstract class StoryTrigger
    {
        [NonSerialized] private Action _onFired;

        public void Bind(Action onFired)
        {
            Unbind();
            _onFired = onFired;
            OnBind();
        }

        public void Unbind()
        {
            if (_onFired == null)
                return;
            OnUnbind();
            _onFired = null;
        }

        protected void Fire() => _onFired?.Invoke();

        protected abstract void OnBind();
        protected abstract void OnUnbind();
    }

    /// <summary>Fires when the given dialogue finishes playing (NPC or item dialogue).</summary>
    [Serializable]
    public class DialogueFinishedTrigger : StoryTrigger
    {
        [SerializeField] private DialogueData dialogue;

        protected override void OnBind() => GameEvents.DialogueFinished += Handle;
        protected override void OnUnbind() => GameEvents.DialogueFinished -= Handle;

        private void Handle(DialogueData finished)
        {
            if (dialogue != null && finished == dialogue)
                Fire();
        }
    }

    /// <summary>Fires when the player picks up the given item.</summary>
    [Serializable]
    public class ItemPickedTrigger : StoryTrigger
    {
        [SerializeField] private ItemData item;

        protected override void OnBind() => GameEvents.ItemPicked += Handle;
        protected override void OnUnbind() => GameEvents.ItemPicked -= Handle;

        private void Handle(ItemData picked)
        {
            if (item != null && picked == item)
                Fire();
            Debug.Log($"Item picked: {picked?.name}");
        }
    }

    /// <summary>Fires when the player enters an <see cref="AreaTrigger"/> with the given id.</summary>
    [Serializable]
    public class AreaEnteredTrigger : StoryTrigger
    {
        [SerializeField] private string areaID;

        protected override void OnBind() => GameEvents.AreaEntered += Handle;
        protected override void OnUnbind() => GameEvents.AreaEntered -= Handle;

        private void Handle(string entered)
        {
            if (!string.IsNullOrEmpty(areaID) && entered == areaID)
                Fire();
        }
    }
}
