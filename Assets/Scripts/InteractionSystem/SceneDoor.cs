using UnityEngine;
using UnityEngine.SceneManagement;

// 挂在建筑门口：玩家靠近按 F 后加载目标场景，并在目标场景的同名出生点生成
public class SceneDoor : InteractableBase
{
    [SerializeField] private string targetSceneName;
    [Tooltip("目标场景中 SceneSpawnPoint 的 spawnId，留空则使用场景默认位置")]
    [SerializeField] private string targetSpawnId;

    private bool isLoading;

    public override bool CanInteract => !isLoading;

    public override void Interact(PlayerInteraction player)
    {
        if (isLoading || string.IsNullOrEmpty(targetSceneName)) return;

        isLoading = true;
        SceneSpawnPoint.PendingSpawnId = targetSpawnId;
        SceneManager.LoadSceneAsync(targetSceneName);
    }
}
