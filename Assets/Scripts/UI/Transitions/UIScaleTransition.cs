using UnityEngine;

namespace Game.UI
{
    [AddComponentMenu("UI/Window Transition/Scale")]
    public class UIScaleTransition : UIWindowTransition
    {
        [Tooltip("留空则缩放本物体；通常指定为窗口里的面板，而不是铺满全屏的根节点")]
        [SerializeField] private RectTransform target;
        [Tooltip("隐藏时相对正常大小的缩放")]
        [SerializeField] private float hiddenScale = 0.85f;

        private Vector3 _shownScale;

        protected override UIEase DefaultEase => UIEase.OutBack;

        protected override void CaptureShownState()
        {
            if (target == null)
                target = (RectTransform)transform;
            _shownScale = target.localScale;
        }

        protected override void Apply(float t) =>
            target.localScale = Vector3.LerpUnclamped(_shownScale * hiddenScale, _shownScale, t);
    }
}
