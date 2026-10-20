namespace Game.UI
{
    /// <summary>窗口所在的渲染层，自下而上；同层内后打开的在上。枚举值用作数组下标，须从 0 连续。</summary>
    public enum UILayer
    {
        HUD,     // 常驻界面（血条、小地图），不参与返回键与手柄焦点
        Window,  // 普通窗口（背包、地图、设置）
        Popup,   // 弹窗（确认框、奖励）
        Toast,   // 飘字提示，不参与返回键与手柄焦点
        Loading, // 加载界面
        System,  // 最高层（断线、错误提示）
    }

    public enum UIWindowState
    {
        Closed,
        Opening,
        Opened,
        Closing,
    }

    /// <summary>窗口打开期间对鼠标和玩家输入的要求；多个窗口同时打开时取最严格的。</summary>
    public enum UIInputMode
    {
        Game,      // 不影响玩家输入，不显示鼠标（HUD、提示）
        GameAndUI, // 显示鼠标，玩家仍可操作
        UIOnly,    // 显示鼠标并屏蔽玩家输入（菜单、对话框）
    }

    /// <summary>窗口成为焦点窗口时对返回键（Esc / 手柄 B）的处理。</summary>
    public enum UIBackBehavior
    {
        Close,  // 关闭本窗口
        Block,  // 吞掉返回键（必须做出选择的弹窗、加载界面）
        Ignore, // 不接收焦点，返回键交给下方窗口
    }

    /// <summary>窗口关闭后实例的去留。</summary>
    public enum UICachePolicy
    {
        Cache,      // 隐藏缓存，再次打开免实例化；超出缓存上限或 ClearCache 时销毁
        Destroy,    // 关闭后立即销毁（体积大且不常用的窗口）
        Persistent, // 常驻，不会被淘汰
    }
}
