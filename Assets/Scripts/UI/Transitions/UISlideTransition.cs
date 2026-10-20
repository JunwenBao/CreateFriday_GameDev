using UnityEngine;

namespace Game.UI
{
    [AddComponentMenu("UI/Window Transition/Slide")]
    public class UISlideTransition : UIWindowTransition
    {
        [Tooltip("留空则移动本物体")]
        [SerializeField] private RectTransform target;
        [Tooltip("隐藏时相对正常位置的偏移（画布单位），例如 (0, -80) 表示从下方滑入")]
        [SerializeField] private Vector2 hiddenOffset = new Vector2(0f, -80f);

        private Vector2 _shownPosition;

        protected override void CaptureShownState()
        {
            if (target == null)
                target = (RectTransform)transform;
            _shownPosition = target.anchoredPosition;
        }

        protected override void Apply(float t) =>
            target.anchoredPosition = Vector2.LerpUnclamped(_shownPosition + hiddenOffset, _shownPosition, t);
    }
}
