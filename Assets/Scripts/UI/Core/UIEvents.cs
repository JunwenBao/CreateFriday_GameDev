using System;
using UnityEngine;

namespace Game.UI
{
    /// <summary>UI 事件总线：音效、新手引导、玩家输入等系统借此响应窗口变化，无需引用具体窗口。</summary>
    public static class UIEvents
    {
        /// <summary>窗口开始打开（OnOpen 之后）。</summary>
        public static event Action<UIWindow> WindowOpened;
        /// <summary>窗口开始关闭（OnClose 之后）。</summary>
        public static event Action<UIWindow> WindowClosed;
        /// <summary>所有打开窗口合并后的输入模式发生变化。</summary>
        public static event Action<UIInputMode> InputModeChanged;
        /// <summary>因窗口的 Pause Game 设置而暂停 / 恢复游戏。</summary>
        public static event Action<bool> PauseChanged;
        /// <summary>按下返回键但没有窗口可处理，例如可用来打开暂停菜单。</summary>
        public static event Action BackUnhandled;

        internal static void RaiseWindowOpened(UIWindow window) => WindowOpened?.Invoke(window);
        internal static void RaiseWindowClosed(UIWindow window) => WindowClosed?.Invoke(window);
        internal static void RaiseInputModeChanged(UIInputMode mode) => InputModeChanged?.Invoke(mode);
        internal static void RaisePauseChanged(bool paused) => PauseChanged?.Invoke(paused);
        internal static void RaiseBackUnhandled() => BackUnhandled?.Invoke();

        // 关闭 Domain Reload 进入 Play 模式时，清掉上一次运行残留的订阅
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            WindowOpened = null;
            WindowClosed = null;
            InputModeChanged = null;
            PauseChanged = null;
            BackUnhandled = null;
        }
    }
}
