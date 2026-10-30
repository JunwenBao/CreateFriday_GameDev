using Cinemachine;
using Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// NPC 头顶提示文本：在 NPC 头顶显示一段文字，并始终朝向玩家镜头。
// 挂在 NPC 根物体上，代码中调用 Show / ShowLocalized 显示、Hide 隐藏；演出中可用 ShowOverheadTextStep。
// Text 留空时运行时会在头顶自动创建一个 TextMeshPro（3D）文本，字体跟随当前语言。
public class NPCOverheadText : MonoBehaviour
{
    [Header("Text")]
    [Tooltip("世界空间的 TextMeshPro（3D）或 World Space Canvas 下的文本，需放在 NPC 的子物体上。留空则运行时自动创建")]
    [SerializeField] private TMP_Text text;
    [Tooltip("自动创建文本时，文本底部相对 NPC 的位置")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2.1f, 0f);
    [Tooltip("自动创建文本时的字号")]
    [SerializeField] private float fontSize = 2f;
    [Tooltip("自动创建文本时的颜色")]
    [SerializeField] private Color color = Color.white;
    [Tooltip("自动创建文本时的最大行宽（米），超出自动换行")]
    [SerializeField, Min(0.1f)] private float maxWidth = 4f;

    [Header("Display")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.2f;
    [Tooltip("文本与镜头距离超过该值时隐藏，0 表示不限制")]
    [SerializeField, Min(0f)] private float maxVisibleDistance = 15f;
    [Tooltip("NPC 对话时隐藏，避免与对话框重复")]
    [SerializeField] private bool hideWhileTalking = true;

    private NPC npc;
    private bool autoCreated;
    private bool wantVisible;
    private float hideTime = float.PositiveInfinity;
    private float alpha;
    private string localizedTable;
    private string localizedKey;

    // 当前是否处于显示状态（不含渐隐过程，也不考虑对话/距离导致的临时隐藏）
    public bool IsShowing => wantVisible;

    private void Awake()
    {
        npc = GetComponent<NPC>();
        if (text != null && !IsInWorldSpace(text))
        {
            Debug.LogWarning($"{name}: NPCOverheadText 的 Text 是 Screen Space Canvas 下的 UI 文本，无法显示在 NPC 头顶，已改用自动创建的 3D 文本。" +
                             "请改用 TextMeshPro（3D）或 World Space Canvas 下的文本，或把 Text 留空。", this);
            text = null;
        }
        if (text == null)
        {
            text = CreateText();
            autoCreated = true;
        }
        text.alpha = 0f;
        text.enabled = false;
    }

    private void OnEnable()
    {
        CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        alpha = 0f;
        if (text != null) text.enabled = false;
    }

    // 显示一段文本；duration <= 0 表示一直显示直到调用 Hide。文本为空时等同于 Hide
    public void Show(string message, float duration = 0f)
    {
        localizedTable = null;
        localizedKey = null;
        SetMessage(message, duration);
    }

    // 按当前语言显示 String Table 中的文本，切换语言时自动刷新
    public void ShowLocalized(string table, string key, float duration = 0f)
    {
        localizedTable = table;
        localizedKey = key;
        SetMessage(ResolveLocalized(), duration);
    }

    public void Hide()
    {
        wantVisible = false;
        hideTime = float.PositiveInfinity;
    }

    private void SetMessage(string message, float duration)
    {
        if (string.IsNullOrEmpty(message))
        {
            Hide();
            return;
        }

        text.text = message;
        wantVisible = true;
        hideTime = duration > 0f ? Time.time + duration : float.PositiveInfinity;
    }

    private void LateUpdate()
    {
        if (wantVisible && Time.time >= hideTime) Hide();

        bool visible = wantVisible && !(hideWhileTalking && npc != null && npc.npcIsTalking) && IsInRange();
        float target = visible ? 1f : 0f;
        alpha = fadeDuration > 0f ? Mathf.MoveTowards(alpha, target, Time.deltaTime / fadeDuration) : target;

        bool render = alpha > 0f;
        if (text.enabled != render) text.enabled = render;
        if (!render) return;

        if (autoCreated) SyncFont();
        text.alpha = alpha;
        FaceCamera();
    }

    // Cinemachine 在 LateUpdate 中移动相机，相机更新后再对齐一次，避免朝向滞后一帧
    private void OnCameraUpdated(CinemachineBrain brain) => FaceCamera();

    private void FaceCamera()
    {
        if (!text.enabled) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        // 与相机朝向一致（而非看向相机位置），文本始终与屏幕平行，不会因透视而倾斜
        text.transform.rotation = cam.transform.rotation;
    }

    private bool IsInRange()
    {
        if (maxVisibleDistance <= 0f) return true;
        Camera cam = Camera.main;
        if (cam == null) return true;
        return (cam.transform.position - text.transform.position).sqrMagnitude <= maxVisibleDistance * maxVisibleDistance;
    }

    // Screen Space Canvas 下的 UI 文本坐标是屏幕像素，无法放在 NPC 头顶
    private static bool IsInWorldSpace(TMP_Text tmp)
    {
        if (tmp is TextMeshPro) return true;
        Canvas canvas = tmp.GetComponentInParent<Canvas>(true);
        return canvas != null && canvas.rootCanvas.renderMode == RenderMode.WorldSpace;
    }

    private TMP_Text CreateText()
    {
        var go = new GameObject("OverheadText");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = offset;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.rectTransform.pivot = new Vector2(0.5f, 0f);
        tmp.rectTransform.sizeDelta = new Vector2(maxWidth, 1f);
        tmp.alignment = TextAlignmentOptions.Bottom;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.fontSize = fontSize;
        tmp.color = color;
        SyncFontOf(tmp);
        return tmp;
    }

    // 自动创建的文本可能早于语言初始化，此时用的是 TMP 默认字体，这里补上当前语言的字体
    private void SyncFont() => SyncFontOf(text);

    private static void SyncFontOf(TMP_Text tmp)
    {
        TMP_FontAsset font = LocaleFontSwitcher.CurrentFont;
        if (font != null && tmp.font != font) tmp.font = font;
    }

    private void OnLocaleChanged(Locale locale)
    {
        if (string.IsNullOrEmpty(localizedKey)) return;
        text.text = ResolveLocalized();
    }

    private string ResolveLocalized()
    {
        string table = string.IsNullOrEmpty(localizedTable) ? DialogueData.DefaultTableName : localizedTable;
        return LocalizationSettings.StringDatabase.GetLocalizedString(table, localizedKey);
    }
}
