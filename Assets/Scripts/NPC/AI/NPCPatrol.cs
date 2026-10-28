using UnityEngine;

// NPC 巡逻行为：在预设点位之间往返走动，每到一个点随机停留一段时间（逛街效果）。
[RequireComponent(typeof(NPCMovement))]
public class NPCPatrol : MonoBehaviour
{
    public enum PatrolMode
    {
        PingPong,   // A → B → C → B → A ...
        Loop        // A → B → C → A ...
    }

    [Tooltip("巡逻点位，至少 2 个。建议用场景中的空物体作为点位")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private PatrolMode mode = PatrolMode.PingPong;
    [Tooltip("到达点位后的停留时间范围（秒），在 x 和 y 之间随机")]
    [SerializeField] private Vector2 waitTimeRange = new Vector2(2f, 5f);
    [Tooltip("开始时从最近的点位出发，否则从第一个点位出发")]
    [SerializeField] private bool startFromNearest = true;

    private enum State { Moving, Waiting }

    private const float RetryDelay = 1f;

    private NPCMovement movement;
    private State state;
    private int currentIndex;
    private int direction = 1;
    private float waitTimer;

    private void Awake()
    {
        movement = GetComponent<NPCMovement>();
    }

    private void Start()
    {
        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogWarning($"{name}: NPCPatrol needs at least 2 waypoints.", this);
            enabled = false;
            return;
        }

        currentIndex = startFromNearest ? FindNearestIndex() : 0;
        GoToCurrent();
    }

    private void Update()
    {
        if (movement.IsPaused) return;

        switch (state)
        {
            case State.Moving:
                if (movement.HasArrived)
                {
                    state = State.Waiting;
                    waitTimer = Random.Range(waitTimeRange.x, waitTimeRange.y);
                }
                break;

            case State.Waiting:
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    AdvanceIndex();
                    GoToCurrent();
                }
                break;
        }
    }

    private void OnDisable()
    {
        if (movement != null) movement.Stop();
    }

    private void GoToCurrent()
    {
        Transform target = waypoints[currentIndex];
        if (target != null && movement.MoveTo(target.position))
        {
            state = State.Moving;
        }
        else
        {
            // 点位缺失或暂时无法寻路时，稍后重试下一个点位
            state = State.Waiting;
            waitTimer = RetryDelay;
        }
    }

    private void AdvanceIndex()
    {
        if (mode == PatrolMode.Loop)
        {
            currentIndex = (currentIndex + 1) % waypoints.Length;
            return;
        }

        int next = currentIndex + direction;
        if (next < 0 || next >= waypoints.Length)
            direction = -direction;
        currentIndex += direction;
    }

    private int FindNearestIndex()
    {
        int nearest = 0;
        float best = float.MaxValue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            float d = (waypoints[i].position - transform.position).sqrMagnitude;
            if (d < best)
            {
                best = d;
                nearest = i;
            }
        }
        return nearest;
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);

            int next = i + 1;
            if (next >= waypoints.Length)
            {
                if (mode != PatrolMode.Loop) break;
                next = 0;
            }
            if (waypoints[next] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
        }
    }
}
