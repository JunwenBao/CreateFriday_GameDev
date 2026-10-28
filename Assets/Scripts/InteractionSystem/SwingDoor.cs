using UnityEngine;

// 绕铰链旋转开关的门：由 NPC 演出（DoorStep）或其他脚本调用 Open / Close。
// 旋转的是门板 Leaf 的本地 Y 轴，所以 Leaf 的轴心必须在铰链处；
// 模型轴心不在铰链时，在铰链位置建一个空物体作为父节点，把它设为 Leaf。
// 门需要让 NPC 穿过时：门板不要设为 Navigation Static，改挂 NavMeshObstacle 并勾选 Carve。
public class SwingDoor : MonoBehaviour
{
    [Tooltip("要旋转的门板，留空则旋转本物体")]
    [SerializeField] private Transform leaf;
    [Tooltip("打开角度（绕门板本地 Y 轴，正负决定往哪边开）")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField, Min(0.01f)] private float duration = 1f;
    [SerializeField] private AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private bool startOpen;

    [Header("Audio")]
    [SerializeField] private AK.Wwise.Event openSound;
    [SerializeField] private AK.Wwise.Event closeSound;

    private Quaternion closedRotation;
    private float progress;   // 0 = 关闭，1 = 完全打开
    private float target;

    public bool IsOpen => target >= 1f;
    public bool IsMoving => !Mathf.Approximately(progress, target);

    private void Awake()
    {
        if (leaf == null) leaf = transform;
        closedRotation = leaf.localRotation;

        progress = target = startOpen ? 1f : 0f;
        ApplyRotation();
    }

    private void Update()
    {
        if (!IsMoving) return;

        progress = Mathf.MoveTowards(progress, target, Time.deltaTime / duration);
        ApplyRotation();
    }

    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);
    public void Toggle() => SetOpen(!IsOpen);

    public void SetOpen(bool open)
    {
        float newTarget = open ? 1f : 0f;
        if (Mathf.Approximately(target, newTarget)) return;

        target = newTarget;
        AK.Wwise.Event sound = open ? openSound : closeSound;
        if (sound != null && sound.IsValid()) sound.Post(leaf.gameObject);
    }

    private void ApplyRotation()
    {
        leaf.localRotation = closedRotation * Quaternion.Euler(0f, openAngle * curve.Evaluate(progress), 0f);
    }

#if UNITY_EDITOR
    // 选中时画出开门方向：黄线为关闭位置，绿线为打开位置（从铰链指向门板中心）
    private void OnDrawGizmosSelected()
    {
        Transform t = leaf != null ? leaf : transform;
        Renderer r = t.GetComponentInChildren<Renderer>();
        Vector3 local = r != null ? t.InverseTransformPoint(r.bounds.center) : Vector3.right;
        local.y = 0f;
        if (local.sqrMagnitude < 0.0001f) local = Vector3.right;

        Quaternion closed = Application.isPlaying ? closedRotation : t.localRotation;
        Quaternion parent = t.parent != null ? t.parent.rotation : Quaternion.identity;
        // 运行时 t 已在旋转，所以用关闭朝向还原门板在世界中的偏移
        Vector3 closedDir = parent * closed * Vector3.Scale(local, t.lossyScale);
        Vector3 openDir = parent * closed * Quaternion.Euler(0f, openAngle, 0f) * Vector3.Scale(local, t.lossyScale);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(t.position, t.position + closedDir);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(t.position, t.position + openDir);
    }
#endif
}
