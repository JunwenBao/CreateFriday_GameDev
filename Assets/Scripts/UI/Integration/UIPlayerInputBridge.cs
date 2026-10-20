using StarterAssets;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.UI
{
    /// <summary>
    /// 把 UI 的输入需求同步给 StarterAssets 玩家：需要鼠标时镜头不再跟随鼠标，
    /// UIOnly 窗口打开期间停用玩家输入。挂在带 StarterAssetsInputs 的玩家上。
    /// </summary>
    [RequireComponent(typeof(StarterAssetsInputs))]
    public class UIPlayerInputBridge : MonoBehaviour
    {
        private StarterAssetsInputs _inputs;
#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private bool _defaultCursorLocked;
        private bool _defaultCursorInputForLook;
        private bool _blocked;

        private void Awake()
        {
            _inputs = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#endif
            _defaultCursorLocked = _inputs.cursorLocked;
            _defaultCursorInputForLook = _inputs.cursorInputForLook;
        }

        private void OnEnable() => UIEvents.InputModeChanged += Apply;

        private void OnDisable() => UIEvents.InputModeChanged -= Apply;

        // 放在 Start：PlayerInput 已在 OnEnable 中启用输入，不会覆盖这里的屏蔽
        private void Start()
        {
            if (UIManager.Instance != null)
                Apply(UIManager.Instance.InputMode);
        }

        private void Apply(UIInputMode mode)
        {
            bool uiNeedsCursor = mode != UIInputMode.Game;
            // StarterAssetsInputs 在游戏窗口重新获得焦点时会按 cursorLocked 重新锁定鼠标
            _inputs.cursorLocked = _defaultCursorLocked && !uiNeedsCursor;
            _inputs.cursorInputForLook = _defaultCursorInputForLook && !uiNeedsCursor;
            if (uiNeedsCursor)
                _inputs.LookInput(Vector2.zero);

            bool block = mode == UIInputMode.UIOnly;
            if (block == _blocked)
                return;
            _blocked = block;

            if (block)
            {
                // 停用输入后收不到松开事件，手动清掉按住中的状态
                _inputs.MoveInput(Vector2.zero);
                _inputs.JumpInput(false);
                _inputs.SprintInput(false);
                _inputs.AimInput(false);
                _inputs.ShootInput(false);
                _inputs.DrawWeaponInput(false);
            }

#if ENABLE_INPUT_SYSTEM
            if (_playerInput != null)
            {
                if (block)
                    _playerInput.DeactivateInput();
                else
                    _playerInput.ActivateInput();
            }
#endif
        }
    }
}
