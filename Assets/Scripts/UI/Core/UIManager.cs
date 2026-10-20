using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace Game.UI
{
    /// <summary>
    /// 窗口管理器，所有窗口的打开 / 关闭入口：UIManager.Instance.Open&lt;T&gt;() / Close&lt;T&gt;() / Back()。
    /// 负责画布层级、分层窗口栈、开关动画、共享模态遮罩、返回键、手柄焦点、窗口缓存，
    /// 并根据当前打开的窗口统一管理鼠标、玩家输入与游戏暂停。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        private const int LayerOrderStep = 1000;
        private const int WindowOrderStep = 10;
        // 实例化大窗口造成卡顿后，开场动画不至于第一帧就跳到结尾
        private const float MaxTransitionDeltaTime = 1f / 20f;
        private const int MaxRefreshPasses = 8;

        private static readonly int LayerCount = Enum.GetValues(typeof(UILayer)).Length;
        private static readonly Predicate<UIWindow> IsDestroyed = window => window == null;

        public static UIManager Instance { get; private set; }

        [Header("窗口资源")]
        [Tooltip("窗口预制体加载器，一般使用 UIWindowRegistry")]
        [SerializeField] private UIWindowLoader loader;
        [Tooltip("关闭后保留的缓存窗口上限，超出时销毁最早关闭的（常驻窗口不计入）")]
        [SerializeField, Min(0)] private int maxCachedWindows = 8;

        [Header("画布")]
        [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
        [SerializeField, Range(0f, 1f)] private float matchWidthOrHeight = 0.5f;

        [Header("模态遮罩")]
        [SerializeField] private Color maskColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField, Min(0f)] private float maskFadeDuration = 0.15f;

        [Header("输入")]
        [Tooltip("由 Esc / 手柄 B 键触发 Back()")]
        [SerializeField] private bool handleBackInput = true;

        [Header("生命周期")]
        [SerializeField] private bool persistAcrossScenes = true;

        private sealed class PendingLoad
        {
            public bool Open;
            public object Data;
            public Action<UIWindow> Callbacks;
        }

        private readonly Dictionary<Type, UIWindow> _instances = new Dictionary<Type, UIWindow>();
        private readonly Dictionary<Type, PendingLoad> _pending = new Dictionary<Type, PendingLoad>();
        // 每层正在显示（含关闭动画中）的窗口，末尾在最上面
        private List<UIWindow>[] _stacks;
        private RectTransform[] _layerRoots;
        // 已关闭、可被淘汰的窗口，最早关闭的在前
        private readonly List<UIWindow> _cached = new List<UIWindow>();
        private readonly List<UIWindow> _animating = new List<UIWindow>();
        private readonly List<UIWindow> _tickBuffer = new List<UIWindow>();
        private readonly List<UIWindow> _ordered = new List<UIWindow>();
        private readonly List<Type> _destroyedTypes = new List<Type>();

        private UIModalMask _mask;
        private UIWindow _maskOwner;
        private bool _refreshing;
        private bool _refreshQueued;

        private CursorLockMode _savedCursorLockState;
        private bool _savedCursorVisible;
        private float _savedTimeScale = 1f;

        /// <summary>所有打开窗口中最严格的输入模式。</summary>
        public UIInputMode InputMode { get; private set; }
        public bool IsGamePaused { get; private set; }
        /// <summary>接收返回键与手柄导航的窗口：最上层、未被遮挡、参与导航的打开窗口。</summary>
        public UIWindow FocusedWindow { get; private set; }
        /// <summary>所有已创建的窗口实例（含已关闭的缓存实例）。</summary>
        public IReadOnlyCollection<UIWindow> AllWindows => _instances.Values;

        /// <summary>该层正在显示的窗口（含关闭动画中），自下而上。</summary>
        public IReadOnlyList<UIWindow> GetWindows(UILayer layer) => _stacks[(int)layer];

        #region Unity 生命周期

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (persistAcrossScenes)
                DontDestroyOnLoad(gameObject);

            if (loader == null)
                Debug.LogError("[UI] No UIWindowLoader assigned.", this);

            BuildHierarchy();
            EnsureEventSystem();
        }

        private void Update()
        {
            if (handleBackInput)
                PollBackInput();

            float deltaTime = Mathf.Min(Time.unscaledDeltaTime, MaxTransitionDeltaTime);
            TickTransitions(deltaTime);
            _mask.Tick(deltaTime);
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            // 管理器被销毁时不能把游戏留在暂停或鼠标解锁的状态
            if (InputMode != UIInputMode.Game)
            {
                Cursor.lockState = _savedCursorLockState;
                Cursor.visible = _savedCursorVisible;
            }
            if (IsGamePaused)
                Time.timeScale = _savedTimeScale;

            if (loader != null)
            {
                foreach (var type in _instances.Keys)
                    loader.Release(type);
            }

            Instance = null;
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 打开窗口；已打开则置顶并调用其 OnRefresh。
        /// onOpened 在 OnOpen 之后回调（开场动画可能仍在播放），打开被取消时不回调。
        /// </summary>
        public void Open<T>(object data = null, Action<T> onOpened = null) where T : UIWindow =>
            Open(typeof(T), data, onOpened == null ? null : window => onOpened((T)window));

        public void Open(Type windowType, object data = null, Action<UIWindow> onOpened = null)
        {
            if (!IsWindowType(windowType))
                return;

            var window = GetLiveInstance(windowType);
            if (window != null)
            {
                if (window.IsOpen)
                    BringToFront(window, data);
                else
                    Show(window, data);
                InvokeOpened(onOpened, window);
                return;
            }

            if (_pending.TryGetValue(windowType, out var pending))
            {
                pending.Open = true;
                pending.Data = data;
                pending.Callbacks += onOpened;
                return;
            }

            Load(windowType, new PendingLoad { Open = true, Data = data, Callbacks = onOpened });
        }

        public void Close<T>() where T : UIWindow => Close(typeof(T));

        public void Close(Type windowType)
        {
            if (windowType == null)
                return;

            // 加载中的窗口：取消打开，加载完成后只放入缓存
            if (_pending.TryGetValue(windowType, out var pending))
                CancelPending(pending);

            if (_instances.TryGetValue(windowType, out var window))
                Close(window);
        }

        public void Close(UIWindow window)
        {
            if (window == null || !window.IsOpen)
                return;

            window.BeginClose();
            if (window.State != UIWindowState.Closing)
                return; // OnClose 中又被重新打开

            RefreshState();
            StartTransition(window);
            UIEvents.RaiseWindowClosed(window);
        }

        public void Toggle<T>(object data = null) where T : UIWindow => Toggle(typeof(T), data);

        public void Toggle(Type windowType, object data = null)
        {
            if (IsOpen(windowType))
                Close(windowType);
            else
                Open(windowType, data);
        }

        /// <summary>交给焦点窗口处理返回；没有可处理的窗口时广播 UIEvents.BackUnhandled 并返回 false。</summary>
        public bool Back()
        {
            var target = FocusedWindow;
            if (target == null)
            {
                UIEvents.RaiseBackUnhandled();
                return false;
            }

            target.HandleBack();
            return true;
        }

        public void CloseAll()
        {
            foreach (var pending in _pending.Values)
                CancelPending(pending);

            for (int layer = LayerCount - 1; layer >= 0; layer--)
                CloseAll((UILayer)layer);
        }

        public void CloseAll(UILayer layer)
        {
            // Close 可能同步修改栈，先拷贝
            var windows = _stacks[(int)layer].ToArray();
            for (int i = windows.Length - 1; i >= 0; i--)
                Close(windows[i]);
        }

        public bool IsOpen<T>() where T : UIWindow => IsOpen(typeof(T));

        /// <summary>已打开，或已请求打开且仍在加载中。</summary>
        public bool IsOpen(Type windowType)
        {
            if (windowType == null)
                return false;
            if (_instances.TryGetValue(windowType, out var window) && window != null && window.IsOpen)
                return true;
            return _pending.TryGetValue(windowType, out var pending) && pending.Open;
        }

        /// <summary>窗口实例（可能是已关闭的缓存实例），不存在时返回 null。</summary>
        public T GetWindow<T>() where T : UIWindow => GetWindow(typeof(T)) as T;

        public UIWindow GetWindow(Type windowType) =>
            windowType != null && _instances.TryGetValue(windowType, out var window) && window != null ? window : null;

        /// <summary>提前加载并创建窗口（不打开），避免首次打开卡顿。</summary>
        public void Preload<T>() where T : UIWindow => Preload(typeof(T));

        public void Preload(Type windowType)
        {
            if (!IsWindowType(windowType) || GetLiveInstance(windowType) != null || _pending.ContainsKey(windowType))
                return;

            Load(windowType, new PendingLoad());
        }

        /// <summary>销毁所有已关闭的非常驻窗口，例如切换场景时释放内存。</summary>
        public void ClearCache()
        {
            for (int i = _cached.Count - 1; i >= 0; i--)
                DestroyWindow(_cached[i]);
        }

        #endregion

        #region 加载

        private void Load(Type windowType, PendingLoad pending)
        {
            if (loader == null)
            {
                Debug.LogError($"[UI] Cannot load '{windowType.Name}': no UIWindowLoader assigned.", this);
                return;
            }

            _pending.Add(windowType, pending);
            try
            {
                loader.Load(windowType, prefab => OnPrefabLoaded(windowType, pending, prefab));
            }
            catch (Exception e)
            {
                _pending.Remove(windowType);
                Debug.LogException(e, this);
            }
        }

        private void OnPrefabLoaded(Type windowType, PendingLoad pending, UIWindow prefab)
        {
            // 异步加载期间管理器已被销毁，或该请求已失效
            if (this == null || !_pending.TryGetValue(windowType, out var current) || current != pending)
            {
                if (prefab != null)
                    loader.Release(windowType);
                return;
            }
            _pending.Remove(windowType);

            if (prefab == null)
            {
                Debug.LogError($"[UI] Failed to load window '{windowType.Name}'. Is it registered in '{loader.name}'?", this);
                return;
            }

            if (prefab.GetType() != windowType)
            {
                Debug.LogError($"[UI] Loader returned '{prefab.GetType().Name}' for '{windowType.Name}'.", prefab);
                loader.Release(windowType);
                return;
            }

            var window = Instantiate(prefab, _layerRoots[(int)prefab.Layer]);
            window.name = prefab.name;
            _instances.Add(windowType, window);
            window.Initialize(this);

            if (pending.Open)
            {
                Show(window, pending.Data);
                InvokeOpened(pending.Callbacks, window);
            }
            else
            {
                window.gameObject.SetActive(false);
                AddToCache(window);
            }
        }

        private static void CancelPending(PendingLoad pending)
        {
            pending.Open = false;
            pending.Data = null;
            pending.Callbacks = null;
        }

        #endregion

        #region 打开 / 关闭流程

        private void Show(UIWindow window, object data)
        {
            _cached.Remove(window);
            MoveToTop(window);

            window.BeginOpen(data);
            if (window.State != UIWindowState.Opening)
                return; // OnOpen 中又被关闭

            RefreshState();
            StartTransition(window);
            UIEvents.RaiseWindowOpened(window);
        }

        private void BringToFront(UIWindow window, object data)
        {
            MoveToTop(window);
            window.Refresh(data);
            RefreshState();
        }

        private void MoveToTop(UIWindow window)
        {
            var stack = _stacks[(int)window.Layer];
            stack.Remove(window);
            stack.Add(window);
        }

        private void StartTransition(UIWindow window)
        {
            if (!window.TickTransitions(0f))
            {
                if (!_animating.Contains(window))
                    _animating.Add(window);
                return;
            }

            _animating.Remove(window);
            CompleteTransition(window);
        }

        private void TickTransitions(float deltaTime)
        {
            if (_animating.Count == 0)
                return;

            // 动画结束的回调可能再开关窗口，遍历副本
            _tickBuffer.Clear();
            _tickBuffer.AddRange(_animating);
            foreach (var window in _tickBuffer)
            {
                if (!_animating.Contains(window))
                    continue;
                if (window == null)
                {
                    _animating.Remove(window);
                    continue;
                }
                if (!window.TickTransitions(deltaTime))
                    continue;

                _animating.Remove(window);
                CompleteTransition(window);
            }
        }

        private void CompleteTransition(UIWindow window)
        {
            if (window.State == UIWindowState.Opening)
            {
                window.CompleteOpen();
                // 全屏窗口完全打开后才开始遮挡下层
                RefreshState();
            }
            else if (window.State == UIWindowState.Closing)
            {
                _stacks[(int)window.Layer].Remove(window);
                window.CompleteClose();
                if (window.CachePolicy == UICachePolicy.Destroy)
                    DestroyWindow(window);
                else
                    AddToCache(window);
                RefreshState();
            }
        }

        private void AddToCache(UIWindow window)
        {
            if (window.CachePolicy == UICachePolicy.Persistent || _cached.Contains(window))
                return;

            _cached.Add(window);
            while (_cached.Count > maxCachedWindows)
                DestroyWindow(_cached[0]);
        }

        private void DestroyWindow(UIWindow window)
        {
            var type = window.GetType();
            _instances.Remove(type);
            _cached.Remove(window);
            _animating.Remove(window);
            if (window != null)
                Destroy(window.gameObject);
            if (loader != null)
                loader.Release(type);
        }

        /// <summary>返回存活的实例；若实例被外部直接销毁，则清理残留引用并返回 null。</summary>
        private UIWindow GetLiveInstance(Type windowType)
        {
            if (!_instances.TryGetValue(windowType, out var window))
                return null;
            if (window != null)
                return window;

            PurgeDestroyedWindows();
            return null;
        }

        private void PurgeDestroyedWindows()
        {
            foreach (var pair in _instances)
            {
                if (pair.Value == null)
                    _destroyedTypes.Add(pair.Key);
            }
            if (_destroyedTypes.Count == 0)
                return;

            foreach (var type in _destroyedTypes)
            {
                _instances.Remove(type);
                if (loader != null)
                    loader.Release(type);
                Debug.LogWarning($"[UI] Window '{type.Name}' was destroyed directly; close windows through UIManager instead.", this);
            }
            _destroyedTypes.Clear();

            foreach (var stack in _stacks)
                stack.RemoveAll(IsDestroyed);
            _cached.RemoveAll(IsDestroyed);
            _animating.RemoveAll(IsDestroyed);
        }

        #endregion

        #region 状态刷新

        /// <summary>根据各层窗口栈重算排序、遮挡、遮罩、焦点、输入模式与暂停。</summary>
        private void RefreshState()
        {
            // 窗口回调中再开关窗口时，排队到本轮结束后重算
            if (_refreshing)
            {
                _refreshQueued = true;
                return;
            }

            _refreshing = true;
            try
            {
                int passes = 0;
                do
                {
                    _refreshQueued = false;
                    ApplyState();
                }
                while (_refreshQueued && ++passes < MaxRefreshPasses);

                if (_refreshQueued)
                    Debug.LogWarning("[UI] Window callbacks keep opening/closing windows; state refresh stopped early.", this);
            }
            finally
            {
                _refreshing = false;
            }
        }

        private void ApplyState()
        {
            PurgeDestroyedWindows();

            // 自上而下收集窗口并分配排序值
            _ordered.Clear();
            for (int layer = LayerCount - 1; layer >= 0; layer--)
            {
                var stack = _stacks[layer];
                for (int i = stack.Count - 1; i >= 0; i--)
                {
                    stack[i].SetSortingOrder((layer + 1) * LayerOrderStep + i * WindowOrderStep);
                    _ordered.Add(stack[i]);
                }
            }

            bool occluded = false;
            UIWindow openModal = null;
            UIWindow closingModal = null;
            UIWindow focus = null;
            var inputMode = UIInputMode.Game;
            bool pause = false;

            foreach (var window in _ordered)
            {
                // 本轮回调中已关闭的窗口留给下一轮处理
                if (window.State == UIWindowState.Closed)
                    continue;

                if (window.IsOpen)
                {
                    if (window.InputMode > inputMode)
                        inputMode = window.InputMode;
                    pause |= window.PauseGame;
                }

                window.SetCovered(occluded);
                if (occluded)
                    continue;

                if (window.Modal)
                {
                    if (window.IsOpen)
                    {
                        if (openModal == null)
                            openModal = window;
                    }
                    else if (closingModal == null)
                    {
                        closingModal = window;
                    }
                }

                if (focus == null && window.IsOpen && IsNavigable(window))
                    focus = window;

                if (window.FullScreen && window.State == UIWindowState.Opened)
                    occluded = true;
            }

            UpdateMask(openModal, closingModal);
            SetFocus(focus);
            SetInputMode(inputMode);
            SetPaused(pause);
        }

        private static bool IsNavigable(UIWindow window) =>
            window.BackBehavior != UIBackBehavior.Ignore &&
            window.Layer != UILayer.HUD &&
            window.Layer != UILayer.Toast;

        private void UpdateMask(UIWindow openModal, UIWindow closingModal)
        {
            _maskOwner = openModal;
            if (openModal != null)
                _mask.Place(openModal.SortingOrder - 1, true);
            else if (closingModal != null)
                _mask.Place(closingModal.SortingOrder - 1, false);
            else
                _mask.Hide();
        }

        private void OnMaskClicked()
        {
            if (_maskOwner != null && _maskOwner.IsOpen && _maskOwner.CloseOnMaskClick)
                Close(_maskOwner);
        }

        private void SetFocus(UIWindow window)
        {
            if (window == FocusedWindow)
                return;

            var eventSystem = EventSystem.current;
            if (FocusedWindow != null)
                FocusedWindow.SaveSelection(eventSystem);

            FocusedWindow = window;
            // 新焦点窗口没有可选控件时也要清空选中，否则手柄确认键会点到下层窗口的按钮
            if (eventSystem != null)
                eventSystem.SetSelectedGameObject(window != null ? window.GetSelectionTarget() : null);
        }

        private void SetInputMode(UIInputMode mode)
        {
            if (mode == InputMode)
                return;

            if (InputMode == UIInputMode.Game)
            {
                _savedCursorLockState = Cursor.lockState;
                _savedCursorVisible = Cursor.visible;
            }

            InputMode = mode;
            if (mode == UIInputMode.Game)
            {
                Cursor.lockState = _savedCursorLockState;
                Cursor.visible = _savedCursorVisible;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            UIEvents.RaiseInputModeChanged(mode);
        }

        private void SetPaused(bool paused)
        {
            if (paused == IsGamePaused)
                return;

            IsGamePaused = paused;
            if (paused)
            {
                _savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = _savedTimeScale;
            }

            UIEvents.RaisePauseChanged(paused);
        }

        #endregion

        #region 输入

        private void PollBackInput()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Back();
                return;
            }

            // 手柄 B 键在游戏中可能另有用途，只在有窗口可返回时响应
            var gamepad = Gamepad.current;
            if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame && FocusedWindow != null)
                Back();
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
                Back();
#endif
        }

        #endregion

        #region 层级搭建

        private void BuildHierarchy()
        {
            var root = new GameObject("UIRoot", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.layer = LayerMask.NameToLayer("UI");
            root.transform.SetParent(transform, false);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1
                                               | AdditionalCanvasShaderChannels.Normal
                                               | AdditionalCanvasShaderChannels.Tangent;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = matchWidthOrHeight;

            _layerRoots = new RectTransform[LayerCount];
            _stacks = new List<UIWindow>[LayerCount];
            for (int i = 0; i < LayerCount; i++)
            {
                _layerRoots[i] = CreateStretchedRect(((UILayer)i).ToString(), root.transform);
                _stacks[i] = new List<UIWindow>();
            }

            _mask = UIModalMask.Create(root.transform, maskColor, maskFadeDuration);
            _mask.Clicked += OnMaskClicked;
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.transform.SetParent(transform, false);
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        internal static RectTransform CreateStretchedRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Stretch(rect);
            return rect;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition3D = Vector3.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        #endregion

        private static bool IsWindowType(Type type)
        {
            if (type != null && !type.IsAbstract && typeof(UIWindow).IsAssignableFrom(type))
                return true;

            Debug.LogError($"[UI] '{type?.Name ?? "null"}' is not a concrete UIWindow type.");
            return false;
        }

        private static void InvokeOpened(Action<UIWindow> callbacks, UIWindow window)
        {
            if (callbacks == null || !window.IsOpen)
                return;

            foreach (Action<UIWindow> callback in callbacks.GetInvocationList())
            {
                try
                {
                    callback(window);
                }
                catch (Exception e)
                {
                    Debug.LogException(e, window);
                }
            }
        }
    }
}
