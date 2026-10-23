using Game.UI;
using UnityEngine;

// 收到游戏结束事件时打开 GameOverWindow；挂在场景中常驻的物体上（例如 UIManager 或 PauseMenuOpener 所在物体）
public class GameOverOpener : MonoBehaviour
{
    private void OnEnable() => GameEvents.GameOver += OpenGameOver;
    private void OnDisable() => GameEvents.GameOver -= OpenGameOver;

    private static void OpenGameOver() => UIManager.Instance.Open<GameOverWindow>();
}
