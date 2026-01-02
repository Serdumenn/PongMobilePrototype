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

    private ball ballcs;
    private SpriteRenderer ballRenderer;

    [Header("Optional")]
    public ScoreManager scoreManager;

    void Start()
    {
        if (ball != null)
        {
            ballcs = ball.GetComponent<ball>();
            ballRenderer = ball.GetComponent<SpriteRenderer>();
        }

        if (scoreManager == null)
            scoreManager = FindAny<ScoreManager>();
    }

    public void GameEnding(bool playerWon)
    {
        Time.timeScale = 0f;

        if (endScreen != null) endScreen.SetActive(true);
        if (winEndScreen != null) winEndScreen.SetActive(playerWon);
        if (loseEndScreen != null) loseEndScreen.SetActive(!playerWon);

        // gameplay objects stop
        if (ballcs != null) ballcs.enabled = false;

        if (ball != null) ball.SetActive(false);
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