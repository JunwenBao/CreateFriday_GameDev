using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

// 让 NPC 渐渐消失：模型逐渐变透明，结束后隐藏模型、关闭碰撞体和辅助瞄准，并且不再可互动。
// 需要彻底移除 NPC 时，可在 NPCSequence 的 On Finished 里把它 SetActive(false)。
// 支持 URP Lit / Simple Lit 材质（渐隐时临时复制为透明材质），其他 Shader 的模型会在结束时直接隐藏。
[Serializable]
public class FadeOutStep : SequenceStep
{
    [Tooltip("等待多少秒后才开始渐隐（等待期间 NPC 保持原样）")]
    [SerializeField, Min(0f)] private float delay;
    [Tooltip("渐隐时长（秒）")]
    [SerializeField, Min(0f)] private float duration = 1.5f;
    [Tooltip("透明度低于该值时关闭影子（透明材质的影子不会随之变淡）")]
    [SerializeField, Range(0f, 1f)] private float shadowOffAlpha = 0.5f;

    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private class RendererState
    {
        public Renderer renderer;
        public Material[] originalMaterials;
        public Material[] fadeMaterials;
        public Color[] baseColors;
        public ShadowCastingMode shadowMode;
    }

    [NonSerialized] private List<RendererState> states;

    public override IEnumerator Run(SequenceContext context)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        NPC npc = context.Npc;
        npc.IsHidden = true;
        npc.SetHighlight(false);
        if (npc.TryGetComponent(out NPCOverheadText overhead)) overhead.Hide();

        BeginFade(npc);

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            ApplyAlpha(1f - Mathf.Clamp01(timer / duration));
            yield return null;
        }

        FinishHidden(npc);
    }

    // 渐隐中途演出被停止：恢复原样
    public override void Cancel(SequenceContext context)
    {
        RestoreRenderers(true);
        if (context.Npc != null) context.Npc.IsHidden = false;
    }

    private void BeginFade(NPC npc)
    {
        states = new List<RendererState>();

        foreach (Renderer r in npc.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || !IsModelRenderer(r)) continue;

            Material[] originals = r.sharedMaterials;
            var state = new RendererState
            {
                renderer = r,
                originalMaterials = originals,
                fadeMaterials = new Material[originals.Length],
                baseColors = new Color[originals.Length],
                shadowMode = r.shadowCastingMode,
            };

            bool anyFadeable = false;
            for (int i = 0; i < originals.Length; i++)
            {
                Material src = originals[i];
                if (src == null || !src.HasProperty(SurfaceId) || !src.HasProperty(BaseColorId))
                {
                    state.fadeMaterials[i] = src;
                    continue;
                }

                Material m = new Material(src) { name = src.name + " (Fade)" };
                MakeTransparent(m);
                state.fadeMaterials[i] = m;
                state.baseColors[i] = src.GetColor(BaseColorId);
                anyFadeable = true;
            }

            if (anyFadeable) r.sharedMaterials = state.fadeMaterials;
            states.Add(state);
        }
    }

    private void ApplyAlpha(float alpha)
    {
        if (states == null) return;

        foreach (RendererState s in states)
        {
            if (s.renderer == null) continue;

            for (int i = 0; i < s.fadeMaterials.Length; i++)
            {
                Material m = s.fadeMaterials[i];
                if (m == null || m == s.originalMaterials[i]) continue;
                Color c = s.baseColors[i];
                c.a *= alpha;
                m.SetColor(BaseColorId, c);
            }

            if (alpha < shadowOffAlpha) s.renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    private void FinishHidden(NPC npc)
    {
        RestoreRenderers(false);

        foreach (Collider c in npc.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (AimAssistTarget t in npc.GetComponentsInChildren<AimAssistTarget>()) t.enabled = false;
    }

    // 换回原材质并销毁临时材质；visible 为 false 时同时关闭这些渲染器
    private void RestoreRenderers(bool visible)
    {
        if (states == null) return;

        foreach (RendererState s in states)
        {
            if (s.renderer != null)
            {
                s.renderer.sharedMaterials = s.originalMaterials;
                s.renderer.shadowCastingMode = s.shadowMode;
                s.renderer.enabled = visible;
            }
            for (int i = 0; i < s.fadeMaterials.Length; i++)
            {
                Material m = s.fadeMaterials[i];
                if (m != null && m != s.originalMaterials[i]) UnityEngine.Object.Destroy(m);
            }
        }

        states = null;
    }

    // 只处理角色模型，头顶文字（TextMeshPro）由 NPCOverheadText 自己隐藏
    private static bool IsModelRenderer(Renderer r)
    {
        if (r is SkinnedMeshRenderer) return true;
        if (r is not MeshRenderer) return false;
        return r.GetComponent<TMP_Text>() == null && r.GetComponent<TMP_SubMesh>() == null;
    }

    // 与 URP 材质面板把 Surface Type 切到 Transparent（Alpha 混合）时的设置一致；
    // 保留 ZWrite，避免透明时看到身体内侧和背面
    private static void MakeTransparent(Material m)
    {
        m.SetFloat(SurfaceId, 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 1f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        m.DisableKeyword("_ALPHAMODULATE_ON");
        m.renderQueue = (int)RenderQueue.Transparent;
    }
}
