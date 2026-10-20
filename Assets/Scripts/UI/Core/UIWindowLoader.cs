using System;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// 为 UIManager 提供窗口预制体。默认实现为 UIWindowRegistry（直接引用）；
    /// 需要按需加载时可派生 Addressables / AssetBundle 版本，接口同时支持同步与异步完成。
    /// </summary>
    public abstract class UIWindowLoader : ScriptableObject
    {
        /// <summary>加载 windowType 对应的预制体，完成时（可以同步）调用且只调用一次 onLoaded，失败传 null。</summary>
        public abstract void Load(Type windowType, Action<UIWindow> onLoaded);

        /// <summary>每次成功的 Load 在预制体不再需要（实例已销毁）时对应调用一次。</summary>
        public virtual void Release(Type windowType) { }
    }
}
