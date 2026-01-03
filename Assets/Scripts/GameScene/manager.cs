using UnityEngine;
using UnityEngine.SceneManagement;

public class manager : MonoBehaviour
{
    public GameObject endScreen;
    public GameObject winEndScreen;
    public GameObject loseEndScreen;

    public GameObject ball;
    public GameObject racket;
    public GameObject enemy;

    [Header("Optional")]
    public ScoreManager scoreManager;

    private ball ballcs;

    void Start()
    {
        if (ball != null)
            ballcs = ball.GetComponent<ball>();

        if (scoreManager == null)
            scoreManager = FindAny<ScoreManager>();
    }

    public void GameEnding(bool playerWon)
    {
        Time.timeScale = 0f;

        if (endScreen != null) endScreen.SetActive(true);
        if (winEndScreen != null) winEndScreen.SetActive(playerWon);
        if (loseEndScreen != null) loseEndScreen.SetActive(!playerWon);

        if (ballcs != null)
            ballcs.HardStopAndHide();
        else if (ball != null)
            ball.SetActive(false);

        if (racket != null) racket.SetActive(false);
        if (enemy != null) enemy.SetActive(false);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private static T FindAny<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return FindAnyObjectByType<T>();
#else
        return FindObjectOfType<T>();
#endif
    }
}