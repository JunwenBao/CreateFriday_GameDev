using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 可被子弹打碎的物体（如瓶子）。需与 BulletTarget 一起使用：
/// 被击中达到次数后，将自身网格切成若干碎片向外飞散，可选替换为破碎后的模型，然后移除原物体
/// </summary>
[RequireComponent(typeof(BulletTarget))]
public class BreakableTarget : MonoBehaviour
{
    [Header("破碎条件")]
    [SerializeField] private int hitsToBreak = 1;                   // 被击中多少次后破碎

    [Header("破碎后替换")]
    [Tooltip("破碎后在原位置生成的模型（如 SM_Prop_Bottle_Broken_01），可为空")]
    [SerializeField] private GameObject brokenPrefab;
    [Tooltip("破碎瞬间播放的特效，可为空")]
    [SerializeField] private Transform breakVfx;
    [SerializeField] private float breakVfxLifetime = 2f;

    [Header("碎片")]
    [SerializeField] private bool spawnShards = true;
    [SerializeField] private int shardCount = 12;                   // 碎片数量
    [SerializeField] private float explosionSpeed = 3f;             // 碎片从命中点向外飞散的速度
    [SerializeField] private float upwardSpeed = 1.5f;              // 碎片额外的向上速度
    [SerializeField] private float shardLifetime = 3f;              // 碎片存在时间
    [SerializeField] private float shardShrinkTime = 0.5f;          // 存在时间结束后缩小消失所需时间
    [Tooltip("碎片材质，为空时使用原物体的材质。原网格未开启 Read/Write 时会生成通用碎片，建议设置此项（如玻璃材质）")]
    [SerializeField] private Material shardMaterialOverride;

    [Header("事件")]
    [SerializeField] private UnityEvent onBreak = new UnityEvent();

    private const int IgnoreRaycastLayer = 2;

    private static Mesh fallbackShardMesh;

    private BulletTarget bulletTarget;
    private int hitCount;
    private bool isBroken;

    private void Awake()
    {
        bulletTarget = GetComponent<BulletTarget>();
    }

    private void OnEnable()
    {
        bulletTarget.Hit += OnBulletHit;
    }

    private void OnDisable()
    {
        bulletTarget.Hit -= OnBulletHit;
    }

    private void OnBulletHit(Vector3 hitPoint, Vector3 hitNormal, Vector3 shotDirection)
    {
        if (isBroken)
        {
            return;
        }

        hitCount++;
        if (hitCount >= hitsToBreak)
        {
            Break(hitPoint, shotDirection);
        }
    }

    /// <summary>
    /// 立即打碎该物体
    /// </summary>
    public void Break(Vector3 hitPoint, Vector3 shotDirection)
    {
        if (isBroken)
        {
            return;
        }
        isBroken = true;

        // 先关闭碰撞体，避免碎片和原物体发生碰撞，也避免同一帧被再次命中
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        if (spawnShards)
        {
            SpawnShards(hitPoint, shotDirection);
        }

        if (brokenPrefab != null)
        {
            GameObject broken = Instantiate(brokenPrefab, transform.position, transform.rotation, transform.parent);
            broken.transform.localScale = transform.localScale;
        }

        if (breakVfx != null)
        {
            Transform vfx = Instantiate(breakVfx, transform.position, Quaternion.identity);
            Destroy(vfx.gameObject, breakVfxLifetime);
        }

        onBreak.Invoke();
        Destroy(gameObject);
    }

    private void SpawnShards(Vector3 hitPoint, Vector3 shotDirection)
    {
        MeshFilter meshFilter = GetComponentInChildren<MeshFilter>();
        MeshRenderer meshRenderer = meshFilter != null ? meshFilter.GetComponent<MeshRenderer>() : null;
        if (meshFilter == null || meshRenderer == null || meshFilter.sharedMesh == null)
        {
            return;
        }

        Material[] materials = meshRenderer.sharedMaterials;
        if (shardMaterialOverride != null)
        {
            materials = new Material[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = shardMaterialOverride;
            }
        }

        Mesh sourceMesh = meshFilter.sharedMesh;
        if (sourceMesh.isReadable)
        {
            SpawnMeshShards(sourceMesh, meshFilter.transform, materials, hitPoint, shotDirection);
        }
        else
        {
            Debug.LogWarning($"[BreakableTarget] 网格 {sourceMesh.name} 未开启 Read/Write，使用通用碎片代替。" +
                             "可在模型导入设置中勾选 Read/Write 以使用原网格切分碎片。", this);
            SpawnFallbackShards(meshRenderer.bounds, meshFilter.transform, materials, hitPoint, shotDirection);
        }
    }

    /// <summary>
    /// 按空间位置把原网格的三角面分组，每组生成一个碎片
    /// </summary>
    private void SpawnMeshShards(Mesh source, Transform sourceTransform, Material[] materials, Vector3 hitPoint, Vector3 shotDirection)
    {
        Vector3[] vertices = source.vertices;
        Vector3[] normals = source.normals;
        Vector2[] uvs = source.uv;
        bool hasNormals = normals.Length == vertices.Length;
        bool hasUVs = uvs.Length == vertices.Length;
        int subMeshCount = source.subMeshCount;

        // 收集所有三角面（子网格索引 + 三个顶点索引）及其中心
        List<int> triSub = new List<int>();
        List<int> triIndices = new List<int>();
        List<Vector3> triCenters = new List<Vector3>();
        for (int s = 0; s < subMeshCount; s++)
        {
            int[] tris = source.GetTriangles(s);
            for (int t = 0; t + 2 < tris.Length; t += 3)
            {
                triSub.Add(s);
                triIndices.Add(tris[t]);
                triIndices.Add(tris[t + 1]);
                triIndices.Add(tris[t + 2]);
                triCenters.Add((vertices[tris[t]] + vertices[tris[t + 1]] + vertices[tris[t + 2]]) / 3f);
            }
        }

        int triCount = triCenters.Count;
        if (triCount == 0)
        {
            return;
        }
        int chunkCount = Mathf.Clamp(shardCount, 1, triCount);

        // 随机选取若干三角面中心作为种子，每个三角面归属到最近的种子
        Vector3[] seeds = new Vector3[chunkCount];
        for (int i = 0; i < chunkCount; i++)
        {
            seeds[i] = triCenters[Random.Range(0, triCount)];
        }

        List<int>[] chunkTris = new List<int>[chunkCount];
        for (int i = 0; i < chunkCount; i++)
        {
            chunkTris[i] = new List<int>();
        }
        for (int t = 0; t < triCount; t++)
        {
            int best = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < chunkCount; i++)
            {
                float d = (triCenters[t] - seeds[i]).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            chunkTris[best].Add(t);
        }

        for (int c = 0; c < chunkCount; c++)
        {
            List<int> tris = chunkTris[c];
            if (tris.Count == 0)
            {
                continue;
            }

            Vector3 center = Vector3.zero;
            foreach (int t in tris)
            {
                center += triCenters[t];
            }
            center /= tris.Count;

            List<Vector3> chunkVerts = new List<Vector3>(tris.Count * 6);
            List<Vector3> chunkNormals = new List<Vector3>(tris.Count * 6);
            List<Vector2> chunkUVs = new List<Vector2>(tris.Count * 6);
            List<int>[] chunkSubTris = new List<int>[subMeshCount];
            for (int s = 0; s < subMeshCount; s++)
            {
                chunkSubTris[s] = new List<int>();
            }

            foreach (int t in tris)
            {
                int a = triIndices[t * 3];
                int b = triIndices[t * 3 + 1];
                int d = triIndices[t * 3 + 2];
                Vector3 faceNormal = Vector3.Cross(vertices[b] - vertices[a], vertices[d] - vertices[a]).normalized;
                List<int> sub = chunkSubTris[triSub[t]];

                // 正面
                int start = chunkVerts.Count;
                foreach (int v in new[] { a, b, d })
                {
                    chunkVerts.Add(vertices[v] - center);
                    chunkNormals.Add(hasNormals ? normals[v] : faceNormal);
                    chunkUVs.Add(hasUVs ? uvs[v] : Vector2.zero);
                }
                sub.Add(start);
                sub.Add(start + 1);
                sub.Add(start + 2);

                // 背面，使碎片从内侧看也不是空的
                start = chunkVerts.Count;
                foreach (int v in new[] { a, d, b })
                {
                    chunkVerts.Add(vertices[v] - center);
                    chunkNormals.Add(-(hasNormals ? normals[v] : faceNormal));
                    chunkUVs.Add(hasUVs ? uvs[v] : Vector2.zero);
                }
                sub.Add(start);
                sub.Add(start + 1);
                sub.Add(start + 2);
            }

            Mesh chunkMesh = new Mesh { name = source.name + "_Shard" };
            if (chunkVerts.Count > 65535)
            {
                chunkMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }
            chunkMesh.SetVertices(chunkVerts);
            chunkMesh.SetNormals(chunkNormals);
            chunkMesh.SetUVs(0, chunkUVs);
            chunkMesh.subMeshCount = subMeshCount;
            for (int s = 0; s < subMeshCount; s++)
            {
                chunkMesh.SetTriangles(chunkSubTris[s], s);
            }
            chunkMesh.RecalculateBounds();

            Vector3 worldPos = sourceTransform.TransformPoint(center);
            CreateShard(chunkMesh, true, materials, worldPos, sourceTransform.rotation, sourceTransform.lossyScale, hitPoint, shotDirection);
        }
    }

    /// <summary>
    /// 网格不可读时，在物体包围盒内生成通用的四面体碎片
    /// </summary>
    private void SpawnFallbackShards(Bounds bounds, Transform sourceTransform, Material[] materials, Vector3 hitPoint, Vector3 shotDirection)
    {
        Mesh mesh = GetFallbackShardMesh();
        Material[] shardMaterials = { materials.Length > 0 ? materials[0] : null };
        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.25f;

        for (int i = 0; i < Mathf.Max(shardCount, 1); i++)
        {
            Vector3 pos = bounds.center + Vector3.Scale(Random.insideUnitSphere * 0.5f, bounds.size);
            Vector3 scale = new Vector3(Random.Range(0.4f, 1f), Random.Range(0.4f, 1f), Random.Range(0.1f, 0.3f)) * size;
            CreateShard(mesh, false, shardMaterials, pos, Random.rotation, scale, hitPoint, shotDirection);
        }
    }

    private void CreateShard(Mesh mesh, bool ownsMesh, Material[] materials, Vector3 position, Quaternion rotation, Vector3 scale,
        Vector3 hitPoint, Vector3 shotDirection)
    {
        GameObject shard = new GameObject(mesh.name);
        shard.layer = IgnoreRaycastLayer;   // 碎片不阻挡射击/瞄准射线
        shard.transform.SetPositionAndRotation(position, rotation);
        shard.transform.localScale = scale;

        shard.AddComponent<MeshFilter>().sharedMesh = mesh;
        shard.AddComponent<MeshRenderer>().sharedMaterials = materials;
        shard.AddComponent<BoxCollider>();

        Rigidbody rb = shard.AddComponent<Rigidbody>();
        rb.mass = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        Vector3 away = position - hitPoint;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : Random.onUnitSphere;
        rb.velocity = away * explosionSpeed
                      + shotDirection.normalized * (explosionSpeed * 0.5f)
                      + Vector3.up * upwardSpeed
                      + Random.insideUnitSphere * (explosionSpeed * 0.3f);
        rb.angularVelocity = Random.insideUnitSphere * 10f;

        BreakableShard shardComp = shard.AddComponent<BreakableShard>();
        shardComp.Init(shardLifetime, shardShrinkTime, ownsMesh ? mesh : null);
    }

    private static Mesh GetFallbackShardMesh()
    {
        if (fallbackShardMesh == null)
        {
            Vector3 p0 = new Vector3(0f, 0.5f, 0f);
            Vector3 p1 = new Vector3(-0.5f, -0.5f, 0.3f);
            Vector3 p2 = new Vector3(0.5f, -0.5f, 0.3f);
            Vector3 p3 = new Vector3(0f, -0.3f, -0.4f);
            Vector3[] corners = { p0, p1, p2, p0, p2, p3, p0, p3, p1, p1, p3, p2 };
            int[] tris = new int[corners.Length];
            for (int i = 0; i < tris.Length; i++)
            {
                tris[i] = i;
            }

            fallbackShardMesh = new Mesh { name = "FallbackShard" };
            fallbackShardMesh.vertices = corners;
            fallbackShardMesh.triangles = tris;
            fallbackShardMesh.RecalculateNormals();
            fallbackShardMesh.RecalculateBounds();
        }
        return fallbackShardMesh;
    }
}

/// <summary>
/// 碎片：存在一段时间后缩小并销毁
/// </summary>
public class BreakableShard : MonoBehaviour
{
    private float lifetime;
    private float shrinkTime;
    private float elapsed;
    private Vector3 initialScale;
    private Mesh ownedMesh;

    public void Init(float lifetime, float shrinkTime, Mesh ownedMesh)
    {
        this.lifetime = Mathf.Max(lifetime, 0f);
        this.shrinkTime = Mathf.Max(shrinkTime, 0.01f);
        this.ownedMesh = ownedMesh;
        initialScale = transform.localScale;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed < lifetime)
        {
            return;
        }

        float remain = 1f - Mathf.Clamp01((elapsed - lifetime) / shrinkTime);
        transform.localScale = initialScale * remain;
        if (remain <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (ownedMesh != null)
        {
            Destroy(ownedMesh);
        }
    }
}
