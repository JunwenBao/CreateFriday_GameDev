using UnityEngine;

namespace Game.UI
{
    [AddComponentMenu("UI/Window Transition/Fade")]
    public class UIFadeTransition : UIWindowTransition
    {
        [Tooltip("留空则使用本物体上的 CanvasGroup")]
        [SerializeField] private CanvasGroup target;

        private float _shownAlpha;

        protected override void CaptureShownState()
        {
            if (target == null && !TryGetComponent(out target))
                target = gameObject.AddComponent<CanvasGroup>();
            _shownAlpha = target.alpha;
        }

        protected override void Apply(float t) => target.alpha = _shownAlpha * Mathf.Clamp01(t);
    }
}
