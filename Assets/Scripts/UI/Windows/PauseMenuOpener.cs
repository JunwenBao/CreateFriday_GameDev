// PauseMenuOpener.cs（放在 Scripts/UI/Windows 下）
using Game.UI;
using UnityEngine;

public class PauseMenuOpener : MonoBehaviour
{
    private void OnEnable() => UIEvents.BackUnhandled += OpenPauseMenu;
    private void OnDisable() => UIEvents.BackUnhandled -= OpenPauseMenu;

    private void OpenPauseMenu() => UIManager.Instance.Open<PauseMenuWindow>();
}