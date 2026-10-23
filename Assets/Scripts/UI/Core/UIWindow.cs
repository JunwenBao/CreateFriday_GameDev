using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// 所有窗口的基类。窗口是一个预制体，根节点挂本类的子类（运行时被拉伸铺满屏幕，实际面板做成子节点）。
    /// 只能通过 UIManager 打开 / 关闭，不要直接 SetActive 或 Destroy；重写 On* 钩子响应生命周期。
    /// </summary>
    [RequireComponent(typeof(Canvas), typeof(GraphicRaycaster), typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public abstract class UIWindow : MonoBehaviour
    {
        [Header("窗口")]
        [SerializeField] private UILayer layer = UILayer.Window;
        [Tooltip("不透明且铺满全屏：完全打开后隐藏下方所有窗口以节省渲染")]
        [SerializeField] private bool fullScreen;
        [Tooltip("模态：用共享遮罩压暗下方界面并屏蔽其点击")]
        [SerializeField] private bool modal;
        [Tooltip("点击遮罩时关闭（仅模态窗口有效）")]
        [SerializeField] private bool closeOnMaskClick;
        [SerializeField] private UIBackBehavior backBehavior = UIBackBehavior.Close;
        [SerializeField] private UICachePolicy cachePolicy = UICachePolicy.Cache;

        [Header("游戏")]
        [Tooltip("打开期间对鼠标与玩家输入的要求。HUD、提示类窗口请设为 Game")]
        [SerializeField] private UIInputMode inputMode = UIInputMode.UIOnly;
        [Tooltip("打开期间暂停游戏（Time.timeScale = 0）")]
        [SerializeField] private bool pauseGame;

        [Header("导航")]
        [Tooltip("获得焦点时选中的控件；设置后启用键盘 / 手柄导航")]
        [SerializeField] private Selectable defaultSelected;

        private UIManager _manager;
        private Canvas _canvas;
        private GraphicRaycaster _raycaster;
        private CanvasGroup _canvasGroup;
        private UIWindowTransition[] _transitions = Array.Empty<UIWindowTransition>();
        private GameObject _lastSelected;

        public UILayer Layer => layer;
        public bool FullScreen => fullScreen;
        public bool Modal => modal;
        public bool CloseOnMaskClick => closeOnMaskClick;
        public UIBackBehavior BackBehavior => backBehavior;
        public UICachePolicy CachePolicy => cachePolicy;
        public UIInputMode InputMode => inputMode;
        public bool PauseGame => pauseGame;

        public UIWindowState State { get; private set; }
        /// <summary>从请求打开到请求关闭之间（含开场动画）。</summary>
        public bool IsOpen => State == UIWindowState.Opening || State == UIWindowState.Opened;
        /// <summary>被上方完全打开的全屏窗口挡住而隐藏。</summary>
        public bool IsCovered { get; private set; }
        public int SortingOrder { get; private set; }

        /// <summary>关闭本窗口，可直接绑定到按钮的 OnClick。</summary>
        public void Close()
        {
            if (_manager != null)
                _manager.Close(this);
        }

        #region 生命周期钩子

        /// <summary>实例化后调用一次，早于第一次 OnOpen。</summary>
        protected virtual void OnCreate() { }

        /// <summary>每次打开时调用，早于开场动画。data 为 Open 传入的参数。</summary>
        protected virtual void OnOpen(object data) { }

        /// <summary>已打开时再次 Open：窗口被置顶并收到新参数。</summary>
        protected virtual void OnRefresh(object data) { }

        /// <summary>开始关闭时调用，早于关闭动画。</summary>
        protected virtual void OnClose() { }

        /// <summary>被上方的全屏窗口遮挡（已隐藏）。</summary>
        protected virtual void OnCover() { }

        /// <summary>遮挡解除，重新可见。</summary>
        protected virtual void OnReveal() { }

        /// <summary>作为焦点窗口收到返回键；默认按 Back Behavior 处理。</summary>
        protected virtual void OnBackPressed()
        {
            if (backBehavior == UIBackBehavior.Close)
                Close();
        }

        #endregion

        #region 由 UIManager 调用

        internal void Initialize(UIManager manager)
        {
            _manager = manager;
            _canvas = GetOrAddComponent<Canvas>();
            _raycaster = GetOrAddComponent<GraphicRaycaster>();
            _canvasGroup = GetOrAddComponent<CanvasGroup>();
            UIManager.Stretch((RectTransform)transform);

            _transitions = Array.FindAll(GetComponentsInChildren<UIWindowTransition>(true), t => t.enabled);
            foreach (var transition in _transitions)
                transition.Initialize();

            LocaleFontSwitcher.ApplyTo(gameObject);
            RunHook(OnCreate);
        }

        internal void BeginOpen(object data)
        {
            State = UIWindowState.Opening;
            _lastSelected = null;
            _canvasGroup.blocksRaycasts = true;
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            RunHook(OnOpen, data);
        }

        internal void Refresh(object data) => RunHook(OnRefresh, data);

        internal void BeginClose()
        {
            State = UIWindowState.Closing;
            // 关闭动画期间点击穿透到下方，防止重复点击本窗口的按钮
            _canvasGroup.blocksRaycasts = false;
            RunHook(OnClose);
        }

        internal void CompleteOpen() => State = UIWindowState.Opened;

        internal void CompleteClose()
        {
            State = UIWindowState.Closed;
            // 静默复位遮挡状态，下次打开不会收到多余的 OnReveal
            IsCovered = false;
            _canvas.enabled = true;
            _raycaster.enabled = true;
            gameObject.SetActive(false);
        }

        /// <summary>推进开关动画，全部播完返回 true。</summary>
        internal bool TickTransitions(float deltaTime)
        {
            bool show = IsOpen;
            bool finished = true;
            foreach (var transition in _transitions)
            {
                if (!transition.Tick(show, deltaTime))
                    finished = false;
            }
            return finished;
        }

        internal void SetSortingOrder(int order)
        {
            SortingOrder = order;
            if (!_canvas.overrideSorting)
                _canvas.overrideSorting = true;
            if (_canvas.sortingOrder != order)
                _canvas.sortingOrder = order;
        }

        internal void SetCovered(bool covered)
        {
            if (IsCovered == covered)
                return;

            IsCovered = covered;
            // 只关画布不关 GameObject，避免 OnDisable/OnEnable 和布局重建
            _canvas.enabled = !covered;
            _raycaster.enabled = !covered;
            if (covered)
                RunHook(OnCover);
            else
                RunHook(OnReveal);
        }

        internal void HandleBack() => RunHook(OnBackPressed);

        internal void SaveSelection(EventSystem eventSystem)
        {
            if (defaultSelected == null || eventSystem == null)
                return;

            var selected = eventSystem.currentSelectedGameObject;
            _lastSelected = selected != null && selected.transform.IsChildOf(transform) ? selected : null;
        }

        /// <summary>获得焦点时应选中的对象；未设置 defaultSelected 的窗口不参与键盘 / 手柄导航。</summary>
        internal GameObject GetSelectionTarget()
        {
            if (defaultSelected == null)
                return null;
            return _lastSelected != null && _lastSelected.activeInHierarchy ? _lastSelected : defaultSelected.gameObject;
        }

        #endregion

        private T GetOrAddComponent<T>() where T : Component =>
            TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();

        // 窗口代码抛出的异常不能打断管理器的状态流转
        private void RunHook(Action hook)
        {
            try
            {
                hook();
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        private void RunHook(Action<object> hook, object data)
        {
            try
            {
                hook(data);
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }
    }

    /// <summary>带强类型参数的窗口：UIManager.Instance.Open&lt;MyWindow&gt;(myData)。</summary>
    public abstract class UIWindow<TData> : UIWindow
    {
        protected sealed override void OnOpen(object data) => OnOpen(Cast(data));
        protected sealed override void OnRefresh(object data) => OnRefresh(Cast(data));

        protected virtual void OnOpen(TData data) { }
        protected virtual void OnRefresh(TData data) { }

        private TData Cast(object data)
        {
            if (data is TData typed)
                return typed;
            if (data != null)
                Debug.LogError($"[UI] {GetType().Name} expects {typeof(TData).Name} but was opened with {data.GetType().Name}.", this);
            return default;
        }
    }
}
