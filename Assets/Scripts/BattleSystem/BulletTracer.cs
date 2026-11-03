using UnityEngine;

/// <summary>
/// 射击枪线：从枪口画一条线到子弹落点，先完整保留 holdTime 秒，再在 fadeTime 秒内变细、变透明后自动销毁
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class BulletTracer : MonoBehaviour
{
    private static Material defaultMaterial;

    private LineRenderer lineRenderer;
    private Color startColor;
    private Color endColor;
    private float startWidth;
    private float endWidth;
    private float holdTime;
    private float fadeTime;
    private float elapsed;

    /// <summary>
    /// 在场景中生成一条枪线
    /// </summary>
    /// <param name="start">起点（枪口）</param>
    /// <param name="end">终点（命中点或射线最远点）</param>
    /// <param name="color">枪线颜色（枪口端），尾端会自动渐隐</param>
    /// <param name="width">枪线宽度</param>
    /// <param name="holdTime">枪线完整显示的保留时间（秒）</param>
    /// <param name="fadeTime">保留结束后淡出消失所需时间（秒）</param>
    /// <param name="material">枪线材质，为空时使用默认的 Sprites/Default 材质</param>
    public static BulletTracer Spawn(Vector3 start, Vector3 end, Color color, float width, float holdTime, float fadeTime, Material material = null)
    {
        GameObject go = new GameObject("BulletTracer");
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.numCapVertices = 2;
        line.alignment = LineAlignment.View;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = material != null ? material : GetDefaultMaterial();

        BulletTracer tracer = go.AddComponent<BulletTracer>();
        Color tailColor = color;
        tailColor.a *= 0.3f;
        tracer.Init(color, tailColor, width, width * 0.5f, holdTime, fadeTime);
        return tracer;
    }

    private static Material GetDefaultMaterial()
    {
        if (defaultMaterial == null)
        {
            // Sprites/Default 支持顶点色，LineRenderer 的颜色/透明度渐变可以直接生效
            defaultMaterial = new Material(Shader.Find("Sprites/Default"));
        }
        return defaultMaterial;
    }

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Init(Color startColor, Color endColor, float startWidth, float endWidth, float holdTime, float fadeTime)
    {
        this.startColor = startColor;
        this.endColor = endColor;
        this.startWidth = startWidth;
        this.endWidth = endWidth;
        this.holdTime = Mathf.Max(holdTime, 0f);
        this.fadeTime = Mathf.Max(fadeTime, 0.01f);
        elapsed = 0f;
        Apply(1f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed < holdTime)
        {
            return;
        }

        float remain = 1f - Mathf.Clamp01((elapsed - holdTime) / fadeTime);
        Apply(remain);

        if (remain <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void Apply(float remain)
    {
        Color s = startColor;
        Color e = endColor;
        s.a *= remain;
        e.a *= remain;
        lineRenderer.startColor = s;
        lineRenderer.endColor = e;
        lineRenderer.startWidth = startWidth * remain;
        lineRenderer.endWidth = endWidth * remain;
    }
}
