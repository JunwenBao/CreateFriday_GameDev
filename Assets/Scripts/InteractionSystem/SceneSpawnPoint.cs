using UnityEngine;

// 放在场景中作为出生点：通过 SceneDoor 进入本场景时，把玩家移到 spawnId 匹配的位置
public class SceneSpawnPoint : MonoBehaviour
{
    public static string PendingSpawnId;

    [SerializeField] private string spawnId;

    private void Start()
    {
        if (string.IsNullOrEmpty(PendingSpawnId) || PendingSpawnId != spawnId) return;
        PendingSpawnId = null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // CharacterController 启用时会覆盖直接设置的 position
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        player.transform.SetPositionAndRotation(transform.position, transform.rotation);
        if (cc != null) cc.enabled = true;
    }
}
