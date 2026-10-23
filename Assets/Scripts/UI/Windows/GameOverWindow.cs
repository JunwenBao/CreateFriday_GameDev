using Game.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverWindow : UIWindow
{
    [SerializeField] private Button restartButton;
    [SerializeField] private Button quitButton;

    protected override void OnCreate()
    {
        restartButton.onClick.AddListener(Restart);
        quitButton.onClick.AddListener(QuitGame);
    }

    // 游戏结束后只能通过按钮离开
    protected override void OnBackPressed() { }

    // 重新加载当前场景；先关闭窗口，让 UIManager 恢复时间缩放与鼠标状态
    private void Restart()
    {
        Close();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
