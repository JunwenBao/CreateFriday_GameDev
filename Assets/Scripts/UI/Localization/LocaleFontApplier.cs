using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 挂在运行时实例化（且不是 UIWindow）的预制体根节点上，启用时把子物体文本切换为当前语言字体。
    /// </summary>
    public class LocaleFontApplier : MonoBehaviour
    {
        private void OnEnable()
        {
            LocaleFontSwitcher.ApplyTo(gameObject);
        }
    }
}
