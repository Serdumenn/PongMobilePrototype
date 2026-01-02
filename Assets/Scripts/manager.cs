using UnityEngine;
using UnityEngine.SceneManagement;

public class manager : MonoBehaviour
{
    [Header("Scene References")]
    public Ball ballRef;
    public ScoreManager scoreManager;

    [Header("UI (optional)")]
    public GameObject endUI;
    public GameObject winUI;
    public GameObject loseUI;

    private bool gameEnded;

    private void Awake()
    {
        if (ballRef == null)
        {
#if UNITY_2023_1_OR_NEWER
            ballRef = Object.FindAnyObjectByType<Ball>();
#else
            ballRef = Object.FindObjectOfType<Ball>();
#endif
        }

        if (scoreManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            scoreManager = Object.FindAnyObjectByType<ScoreManager>();
#else
            scoreManager = Object.FindObjectOfType<ScoreManager>();
#endif
        }
    }

    public void GameEnding(bool playerWon)
    {
        if (gameEnded) return;
        gameEnded = true;

        if (ballRef != null) ballRef.HardStop();

        if (endUI) endUI.SetActive(true);
        if (winUI) winUI.SetActive(playerWon);
        if (loseUI) loseUI.SetActive(!playerWon);

    }

    public void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void NewMatch()
    {
        gameEnded = false;

        if (endUI) endUI.SetActive(false);
        if (winUI) winUI.SetActive(false);
        if (loseUI) loseUI.SetActive(false);

        if (scoreManager != null) scoreManager.ResetScores();
        if (ballRef != null) ballRef.StartResetSequence();
    }
}