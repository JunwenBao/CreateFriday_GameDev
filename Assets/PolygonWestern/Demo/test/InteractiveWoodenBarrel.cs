using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractiveWoodenBarrel : InteractableBase
{
    [Header("移动位置")]
    [Tooltip("滚动的起始点")]
    public Vector3 startPoint;
    [Tooltip("滚动的目标终点")]
    public Vector3 endPoint;

    [Header("速度控制")]
    [Tooltip("移动速度（米/秒）")]
    public float speed = 2.0f;

    [Header("物理属性（用于计算滚动）")]
    [Tooltip("木桶的半径。半径越小，滚得越快。若为0则自动使用 Collider 的大小。")]
    public float barrelRadius = 0.5f;
    [Tooltip("木桶滚动的旋转轴（通常是物体的 Right 轴或 Forward 轴）")]
    public Vector3 rotationAxis = Vector3.right;


    [Header("贴地与重力设置")]
    [Tooltip("地面层（建议设置一个专门的 Ground Layer，防止射线检测到木桶自己）")]
    public LayerMask groundLayer = ~0; // 默认检测所有层
    [Tooltip("重力加速度（米/秒²）")]
    public float gravity = 9.81f;
    [Tooltip("最大下落速度")]
    public float maxFallSpeed = 20f;

    // 物理变量
    private float verticalVelocity = 0f; // 当前垂直方向的速度（下落速度）
    private bool isGrounded = false;     // 是否接触地面


    private float currentProgress = 0f; // 移动进度 (0 到 1)
    private Vector3 lastPosition;
    private bool StartAnimation = false;

    protected virtual void Start()
    {
        // 自动初始化：如果未手动设置半径，尝试从 Collider 获取
        if (barrelRadius <= 0f)
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                barrelRadius = col.bounds.extents.y;
            }
            else
            {
                barrelRadius = 0.5f; // 默认备用值
            }
        }

        // 初始化位置
        if (startPoint != null)
        {
            startPoint = transform.position;
        }
            lastPosition = transform.position;

        // 获取 MeshRenderer 组件
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            // 获取世界空间下的包围盒大小（已计算缩放 Scale）
            Vector3 worldSize = meshRenderer.bounds.size;
            Debug.Log($"模型世界尺寸 - 长(X): {worldSize.x}, 高(Y): {worldSize.y}, 宽(Z): {worldSize.z}");

            // 获取半径
            barrelRadius = worldSize.x/2*0.7f;
        }

        //简单初始化y轴
        ApplyGroundAlignmentAndGravity();
    }

    protected virtual void Update()
    {
        if (!StartAnimation) return;

        // 1. 计算总距离与单帧移动步长
        float totalDistance = Vector3.Distance(startPoint, endPoint);
        if (totalDistance <= 0f) return;

        // 根据速度计算这一帧增加的进度百分比
        currentProgress += (speed * Time.deltaTime) / totalDistance;
        currentProgress = Mathf.Clamp01(currentProgress); // 限制在 0 ~ 1 之间

        // 2. 位置插值 (Move)
        Vector3 nextPosition = Vector3.Lerp(startPoint, endPoint, currentProgress);
        nextPosition.y = transform.position.y;
        transform.position = nextPosition;

        // 3. 计算旋转 (Roll) - 核心物理公式：滚动弧度 = 移动距离 / 半径
        float movedDistance = Vector3.Distance(transform.position, lastPosition);
        if (movedDistance > 0.001f)
        {
            // 将弧度转换为角度 (1 弧度 = 180 /  Mathf.PI 度)
            float angleToRotate = (movedDistance / barrelRadius) * Mathf.Rad2Deg;

            // 旋转木桶（绕着局部坐标轴旋转）
            transform.Rotate(rotationAxis, angleToRotate, Space.Self);
        }

        // 更新上一帧位置
        lastPosition = transform.position;

        //// 4. 到达终点后自动重置（可选循环测试）
        //if (currentProgress >= 1.0f)
        //{
        //    // 如果你想让它循环滚动，可以取消注释下面这行：
        //     ResetAnimation();
        //}

        ApplyGroundAlignmentAndGravity();
    }
    /// <summary>
    /// 核心功能函数：仅朝世界下方进行射线检测，控制下落高度，不影响模型旋转
    /// </summary>
    private void ApplyGroundAlignmentAndGravity()
    {
        float rayExtraHeight = 0.5f;
        Vector3 rayOrigin = transform.position + Vector3.up * rayExtraHeight;
        Vector3 rayDirection = Vector3.down;
        float rayDistance = barrelRadius + rayExtraHeight + 0.1f;

        // 1. 获取木桶自身的 Collider
        Collider myCollider = GetComponent<Collider>();

        // 【修改处】：给 hit 变量赋一个默认值 (default)，消除编译器报错
        RaycastHit hit = default(RaycastHit);
        bool hasHit = false;

        // 2. 获取下方所有的碰撞目标
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, rayDistance, groundLayer);

        // 遍历所有碰撞点，寻找第一个“不是自己”的碰撞体
        foreach (var candidateHit in hits)
        {
            if (myCollider != null && candidateHit.collider == myCollider)
            {
                continue; // 如果碰到了自己，直接跳过
            }

            // 找到了真正的地面
            hit = candidateHit;
            hasHit = true;
            break;
        }

        // 3. 执行逻辑
        if (hasHit)
        {
            // 【情况 A：在地面上】
            isGrounded = true;
            verticalVelocity = 0f;

            Vector3 alignedPosition = transform.position;
            alignedPosition.y = hit.point.y + barrelRadius;
            transform.position = alignedPosition;

            // 关闭运动
            if (currentProgress >= 1) StartAnimation = false;
        }
        else
        {
            // 【情况 B：悬空状态】
            isGrounded = false;

            verticalVelocity -= gravity * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, -maxFallSpeed);

            transform.Translate(Vector3.up * verticalVelocity * Time.deltaTime, Space.World);
        }
    }

    // 互动后开始动画
    public override void Interact(PlayerInteraction player)
    {
        StartAnimation = true;
    }

    ///// <summary>
    ///// 重置动画到起点
    ///// </summary>
    //public void ResetAnimation()
    //{
    //    currentProgress = 0f;
    //    if (startPoint != null)
    //    {
    //        transform.position = startPoint.position;
    //        lastPosition = startPoint.position;
    //    }
    //}
}
