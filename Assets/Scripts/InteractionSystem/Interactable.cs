using UnityEngine;

public enum InteractableType
{
    None,
    Pickup,
    Door,
    Message,
    NPC
}

// 挂在可互动物品上：负责描边高亮，并在被互动时执行逻辑（当前仅打印文字）
[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    [SerializeField] private InteractableType interactableType = InteractableType.None;
    [SerializeField] private string interactionMessage = "你与物品发生了互动！";
    [SerializeField] private DialogueData dialogue;   // Message 类型持有的对话数据
    [SerializeField] private Color outlineColor = Color.yellow;
    [SerializeField, Range(0f, 0.1f)] private float outlineWidth = 0.02f;

    private Renderer[] renderers;
    private Material outlineMaterial;
    private Material[][] originalMaterials;   // 每个 renderer 的原始材质
    private Material[][] highlightMaterials;  // 原始材质 + 末尾追加的描边材质
    private bool isHighlighted;

    private void Awake()
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
            for (int j = 0; j < baseMats.Length; j++)
            {
                withOutline[j] = baseMats[j];
            }
            withOutline[baseMats.Length] = outlineMaterial;
            highlightMaterials[i] = withOutline;
        }
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

    // 按下 E 键时触发的互动逻辑，dialogueTrigger 由玩家侧传入
    public void Interact(DialogueTrigger dialogueTrigger, Animator playerAnimator)
    {
        if (interactableType == InteractableType.Message)
        {
            if (dialogue != null && dialogueTrigger != null)
            {
                dialogueTrigger.StartItemDialogue(dialogue);
            }
            else
            {
                Debug.Log(interactionMessage);
            }
        }
        else if(interactableType == InteractableType.Pickup)
        {
            if (playerAnimator != null)
            {
                playerAnimator.SetTrigger("Pickup");
            }

            Destroy(this.gameObject, 0.8f);
        }
    }
}