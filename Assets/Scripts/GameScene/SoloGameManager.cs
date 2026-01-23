using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SoloGameManager : MonoBehaviour
{
    [Header("Scene Refs")]
    public SoloBall ball;
    public GameObject paddle;
    public SoloScoreManager score;

    [Header("UI (Optional)")]
    public Text scoreText;
    public Text bestText;
    public GameObject gameOverRoot;
    public Button retryButton;

    void Awake()
    {
        if (ball == null) ball = FindAnyObjectByType<SoloBall>();
        if (score == null) score = FindAnyObjectByType<SoloScoreManager>();

        if (retryButton != null)
            retryButton.onClick.AddListener(Retry);
    }

    void Start()
    {
        StartGame();
    }

    void Update()
    {
        RefreshUI();
    }

    public void StartGame()
    {
        if (gameOverRoot != null) gameOverRoot.SetActive(false);

        if (paddle != null) paddle.SetActive(true);

        if (score != null) score.ResetScore();

        if (ball != null) ball.StartRound();

        RefreshUI();
    }

    public void OnGameOver()
    {
        if (paddle != null) paddle.SetActive(false);
        if (ball != null) ball.StopAndHide();

        RefreshUI();

        if (gameOverRoot != null) gameOverRoot.SetActive(true);
    }

    void RefreshUI()
    {
        if (score == null) return;

        if (scoreText != null) scoreText.text = score.Score.ToString();
        if (bestText != null) bestText.text = score.BestScore.ToString();
    }

    public void Retry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}