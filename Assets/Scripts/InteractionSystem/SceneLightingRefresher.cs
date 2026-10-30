using UnityEngine;
using UnityEngine.SceneManagement;

// 运行时切换场景后，未烘焙光照数据的场景（如 SheriffScene）的天空盒环境光/反射不会自动重新计算，导致画面漆黑。
// 每次加载场景后强制按当前激活场景的 RenderSettings 刷新环境光照。
public static class SceneLightingRefresher
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        DynamicGI.UpdateEnvironment();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive && scene != SceneManager.GetActiveScene()) return;
        DynamicGI.UpdateEnvironment();
    }
}
