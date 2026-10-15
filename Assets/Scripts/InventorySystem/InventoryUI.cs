using StarterAssets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 挂在常驻激活的 Canvas 上（不要挂在 panel 自身，否则关闭后收不到按键）
public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("Grid")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform slotContainer;   // 建议挂 GridLayoutGroup
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Detail")]
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private Button useButton;

    [Header("Player")]
    [SerializeField] private StarterAssetsInputs playerInputs;
    [SerializeField] private DialogueTrigger dialogueTrigger;

    private InventorySlotUI[] slotViews;
    private int selectedIndex = -1;

    public bool IsOpen => panel.activeSelf;

    // 用 Start 保证 Inventory.Awake 已补齐格子
    private void Start()
    {
        slotViews = new InventorySlotUI[inventory.Slots.Count];
        for (int i = 0; i < slotViews.Length; i++)
        {
            slotViews[i] = Instantiate(slotPrefab, slotContainer);
            slotViews[i].Init(i, Select);
        }

        useButton.onClick.AddListener(() => inventory.UseAt(selectedIndex));
        inventory.OnChanged += Refresh;

        panel.SetActive(false);
        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.OnChanged -= Refresh;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (IsOpen)
                SetOpen(false);
            else if (dialogueTrigger == null || !dialogueTrigger.IsDialogueActive)
                SetOpen(true);
        }
        else if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            SetOpen(false);
        }
    }

    private void SetOpen(bool open)
    {
        panel.SetActive(open);
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;

        if (playerInputs != null)
        {
            // 清掉残留输入，避免打开背包时角色继续走/开枪
            playerInputs.cursorLocked = !open;
            playerInputs.cursorInputForLook = !open;
            playerInputs.MoveInput(Vector2.zero);
            playerInputs.LookInput(Vector2.zero);
            playerInputs.SprintInput(false);
            playerInputs.AimInput(false);
            playerInputs.ShootInput(false);
#if ENABLE_INPUT_SYSTEM
            PlayerInput playerInput = playerInputs.GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = !open;
#endif
        }

        if (open) Refresh();
    }

    private void Refresh()
    {
        for (int i = 0; i < slotViews.Length; i++)
            slotViews[i].Refresh(inventory.Slots[i]);

        Select(selectedIndex);
    }

    private void Select(int index)
    {
        selectedIndex = index;
        for (int i = 0; i < slotViews.Length; i++)
            slotViews[i].SetSelected(i == index);

        InventorySlot slot = index >= 0 && index < inventory.Slots.Count ? inventory.Slots[index] : null;
        bool hasItem = slot != null && !slot.IsEmpty;

        detailIcon.enabled = hasItem;
        detailIcon.sprite = hasItem ? slot.item.Icon : null;
        detailName.text = hasItem ? slot.item.ItemName : string.Empty;
        detailDescription.text = hasItem ? slot.item.Description : string.Empty;
        useButton.interactable = hasItem && slot.item.IsUsable;
    }
}
