using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 挂在玩家上（与 PlayerInteraction 同一物体）：附近有可互动对象时，在头顶显示按键提示图标。
// 图标画在屏幕空间，大小固定、不会被场景遮挡；未指定图片时显示一个默认的按键方块。
[RequireComponent(typeof(PlayerInteraction))]
public class InteractionPrompt : MonoBehaviour
{
    [Header("Icon")]
    [Tooltip("提示图片，图片导入设置的 Texture Type 需为 Sprite (2D and UI)。留空则显示默认的按键方块")]
    [SerializeField] private Sprite icon;
    [Tooltip("图标尺寸，单位为 1920x1080 参考分辨率下的像素")]
    [SerializeField] private Vector2 size = new Vector2(64f, 64f);

    [Header("Position")]
    [Tooltip("图标跟随的物体，留空则跟随玩家")]
    [SerializeField] private Transform anchor;
    [Tooltip("图标底边中点相对 Anchor 的世界空间偏移（米）")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 2.1f, 0f);

    [Header("Display")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.15f;

    private PlayerInteraction interaction;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform iconRect;
    private float alpha;

    private void Awake()
    {
        interaction = GetComponent<PlayerInteraction>();
        if (anchor == null) anchor = transform;
        CreateUI();
    }

    private void OnEnable() => CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);

    private void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
        alpha = 0f;
        if (canvas != null) canvas.enabled = false;
    }

    private void LateUpdate()
    {
        float target = interaction.CanInteractNow ? 1f : 0f;
        alpha = fadeDuration > 0f ? Mathf.MoveTowards(alpha, target, Time.deltaTime / fadeDuration) : target;
        canvasGroup.alpha = alpha;
        UpdatePosition();
    }

    // Cinemachine 在 LateUpdate 中移动相机，相机更新后再定位一次，避免图标滞后一帧而抖动
    private void OnCameraUpdated(CinemachineBrain brain) => UpdatePosition();

    private void UpdatePosition()
    {
        Camera cam = Camera.main;
        bool visible = alpha > 0f && cam != null;
        Vector3 screenPos = Vector3.zero;
        if (visible)
        {
            screenPos = cam.WorldToScreenPoint(anchor.position + offset);
            visible = screenPos.z > 0f;   // z <= 0 表示在镜头后方
        }

        if (canvas.enabled != visible) canvas.enabled = visible;
        if (visible) iconRect.position = new Vector3(screenPos.x, screenPos.y, 0f);
    }

    private void CreateUI()
    {
        var root = new GameObject("InteractionPrompt", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        root.layer = LayerMask.NameToLayer("UI");
        root.transform.SetParent(transform, false);

        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.enabled = false;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroup = root.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        iconRect = CreateChild("Icon", root.transform);
        iconRect.pivot = new Vector2(0.5f, 0f);
        iconRect.sizeDelta = size;
        var image = iconRect.gameObject.AddComponent<Image>();
        image.sprite = icon;
        image.preserveAspect = true;
        if (icon != null) return;

        // 默认按键方块：半透明黑底 + 互动键字母
        image.color = new Color(0f, 0f, 0f, 0.6f);
        RectTransform labelRect = CreateChild("Key", iconRect);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = interaction.InteractKey.ToString();
        label.fontSize = size.y * 0.6f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
    }

    private static RectTransform CreateChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        return rect;
    }
}
