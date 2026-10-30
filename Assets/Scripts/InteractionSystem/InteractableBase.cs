using UnityEngine;

// 所有可互动对象（物品、NPC 等）的基类：负责描边高亮，具体互动逻辑由子类实现
[RequireComponent(typeof(Collider))]
public abstract class InteractableBase : MonoBehaviour
{
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField, Range(0f, 0.1f)] private float outlineWidth = 0.02f;

    private Renderer[] renderers;
    private Material outlineMaterial;
    private Material[][] originalMaterials;   // 每个 renderer 的原始材质
    private Material[][] highlightMaterials;  // 原始材质 + 末尾追加的描边材质
    private bool isHighlighted;

    protected virtual void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();

        Shader outlineShader = Shader.Find("Custom/OutlineURP");
        outlineMaterial = new Material(outlineShader);
        outlineMaterial.SetColor("_OutlineColor", outlineColor);
        outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);

        // 预先缓存两套材质数组，高亮时整块切换，避免每帧分配
        originalMaterials = new Material[renderers.Length][];
        highlightMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] baseMats = renderers[i].sharedMaterials;
            originalMaterials[i] = baseMats;

            Material[] withOutline = new Material[baseMats.Length + 1];
            baseMats.CopyTo(withOutline, 0);
            withOutline[baseMats.Length] = outlineMaterial;
            highlightMaterials[i] = withOutline;
        }
    }

    protected virtual void OnDestroy()
    {
        if (outlineMaterial != null) Destroy(outlineMaterial);
    }

    // 由 PlayerInteraction 调用，开关描边高亮
    public void SetHighlight(bool on)
    {
        if (isHighlighted == on) return;
        isHighlighted = on;

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sharedMaterials = on ? highlightMaterials[i] : originalMaterials[i];
        }
    }

    // 为 false 时 PlayerInteraction 不高亮、不响应互动键（例如 NPC 正在演出）
    public virtual bool CanInteract => true;

    // 按下互动键时由 PlayerInteraction 调用
    public abstract void Interact(PlayerInteraction player);
}
