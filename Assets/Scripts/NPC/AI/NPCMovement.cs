using UnityEngine;
using UnityEngine.AI;

// NPC 移动马达：封装 NavMeshAgent，负责寻路移动、到达判定和驱动动画速度参数。
// 上层 AI 行为（巡逻、跟随等）只调用 MoveTo / Stop，不直接操作 NavMeshAgent。
// 移动速度、转向速度、停止距离等在 NavMeshAgent 组件上配置。
[RequireComponent(typeof(NavMeshAgent))]
public class NPCMovement : MonoBehaviour
{
    [Header("Arrival")]
    [Tooltip("NavMeshAgent 的 Stopping Distance 小于该值时，用该值作为到达判定距离")]
    [SerializeField] private float minArriveDistance = 0.1f;

    [Header("Animation")]
    [Tooltip("Animator 中用于混合 Idle/Walk 的 Float 参数名；参数不存在时不驱动动画")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private float speedDampTime = 0.1f;
    [Tooltip("由 NavMeshAgent 控制位移，关闭 Animator 的 Root Motion 以免两者冲突")]
    [SerializeField] private bool disableRootMotion = true;

    private NavMeshAgent agent;
    private Animator animator;
    private NPC npc;
    private int speedHash;
    private bool hasSpeedParam;
    private bool hasDestination;

    // 对话等情况下暂停移动，暂停期间上层行为也应停止计时
    public bool IsPaused { get; private set; }

    public bool HasArrived
    {
        get
        {
            if (!hasDestination || !agent.isOnNavMesh) return false;
            if (agent.pathPending) return false;
            return agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, minArriveDistance);
        }
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        npc = GetComponent<NPC>();

        if (animator != null)
        {
            if (disableRootMotion) animator.applyRootMotion = false;

            speedHash = Animator.StringToHash(speedParam);
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == speedHash && p.type == AnimatorControllerParameterType.Float)
                {
                    hasSpeedParam = true;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        SetPaused(npc != null && npc.npcIsTalking);

        if (hasSpeedParam)
        {
            float speed = IsPaused ? 0f : agent.velocity.magnitude;
            animator.SetFloat(speedHash, speed, speedDampTime, Time.deltaTime);
        }
    }

    public bool MoveTo(Vector3 destination)
    {
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning($"{name} is not on a NavMesh, cannot move. Bake a NavMesh and place the NPC on it.", this);
            return false;
        }

        hasDestination = agent.SetDestination(destination);
        return hasDestination;
    }

    public void Stop()
    {
        hasDestination = false;
        if (agent.isOnNavMesh) agent.ResetPath();
    }

    // 原地按 NavMeshAgent 的 Angular Speed 转向 direction（忽略高度），朝向误差在容差内时返回 true
    public bool TurnTowards(Vector3 direction, float toleranceDegrees = 2f)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return true;
        if (IsPaused) return false;

        Quaternion target = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, agent.angularSpeed * Time.deltaTime);
        return Quaternion.Angle(transform.rotation, target) <= toleranceDegrees;
    }

    private void SetPaused(bool paused)
    {
        if (IsPaused == paused) return;
        IsPaused = paused;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = paused;
            if (paused) agent.velocity = Vector3.zero;
        }
        // 对话时由 NPC.TurnToPlayer 控制朝向，避免与 Agent 的转向互相抢夺
        agent.updateRotation = !paused;
    }
}
