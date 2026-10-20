using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 窗口开关动画基类。挂在窗口根节点或子节点上，可叠加多个同时播放；
    /// 使用真实时间（暂停时照常播放），中途打断会从当前进度平滑反向。
    /// </summary>
    public abstract class UIWindowTransition : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float duration = 0.2f;
        [SerializeField] private UIEase ease = UIEase.OutCubic;

        // 0 = 隐藏，1 = 显示
        private float _progress;

        protected virtual UIEase DefaultEase => UIEase.OutCubic;

        internal void Initialize()
        {
            CaptureShownState();
            _progress = 0f;
            Apply(ease.Evaluate(_progress));
        }

        /// <summary>向显示或隐藏推进一步，到达终点返回 true。</summary>
        internal bool Tick(bool show, float deltaTime)
        {
            float goal = show ? 1f : 0f;
            _progress = duration > 0f ? Mathf.MoveTowards(_progress, goal, deltaTime / duration) : goal;
            Apply(ease.Evaluate(_progress));
            return _progress == goal;
        }

        /// <summary>记录完全显示时的状态，窗口创建时调用一次。</summary>
        protected virtual void CaptureShownState() { }

        /// <summary>按进度设置目标：0 隐藏，1 显示（OutBack 缓动时会略超出 1）。</summary>
        protected abstract void Apply(float t);

        protected virtual void Reset() => ease = DefaultEase;
    }
}
