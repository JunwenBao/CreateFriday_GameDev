// PauseMenuWindow.cs
using Game.UI;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuWindow : UIWindow
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;

    // 只在窗口第一次创建时调用一次，适合绑定按钮
    protected override void OnCreate()
    {
        resumeButton.onClick.AddListener(Close);   // 关闭窗口 = 继续游戏
        quitButton.onClick.AddListener(QuitGame);
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}