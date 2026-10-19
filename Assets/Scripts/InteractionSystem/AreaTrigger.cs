using UnityEngine;

// 挂在带触发器碰撞体的区域上：玩家进入时广播 GameEvents.AreaEntered(areaID)
[RequireComponent(typeof(Collider))]
public class AreaTrigger : MonoBehaviour
{
    [SerializeField] private string areaID;
    [SerializeField] private string playerTag = "Player";

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!string.IsNullOrEmpty(areaID) && other.CompareTag(playerTag))
            GameEvents.RaiseAreaEntered(areaID);
    }
}
