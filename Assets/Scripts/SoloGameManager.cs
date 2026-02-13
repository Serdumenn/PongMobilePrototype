using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SoloGameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] SoloBall soloBall;
    [SerializeField] SoloScoreManager score;

    [Header("UI")]
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] Button retryButton;
    [SerializeField] Button menuButton;

    [Header("Single-scene Menu (Optional)")]
    [SerializeField] GameObject menuPanel;
    [SerializeField] GameObject hudPanel;

    [Header("Behavior")]
    [SerializeField] bool pauseTimeOnGameOver = true;
    [SerializeField] bool stopBallWhenMenuOpen = true;

    [Header("Other")]
    [SerializeField] List<GameObject> disableOnGameOver = new List<GameObject>();

    bool isGameOver;
    public bool IsGameOver => isGameOver;

    void Awake()
    {
        if (soloBall == null) soloBall = FindFirstObjectByType<SoloBall>();
        if (score == null) score = FindFirstObjectByType<SoloScoreManager>();

        if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
        if (menuButton != null) menuButton.onClick.AddListener(OnMenuClicked);

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (pauseTimeOnGameOver) Time.timeScale = 0f;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        for (int i = 0; i < disableOnGameOver.Count; i++)
        {
            if (disableOnGameOver[i] != null)
                disableOnGameOver[i].SetActive(false);
        }

        if (soloBall != null) soloBall.HardStopAndHide();
    }

    void OnRetryClicked()
    {
        Time.timeScale = 1f;
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        StartNewRun();
    }

    void OnMenuClicked()
    {
        Time.timeScale = 1f;

        if (menuPanel != null)
        {
            if (hudPanel != null) hudPanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            menuPanel.SetActive(true);

            if (stopBallWhenMenuOpen && soloBall != null)
                soloBall.HardStopAndHide();

            isGameOver = false;
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void StartNewRun()
    {
        isGameOver = false;

        for (int i = 0; i < disableOnGameOver.Count; i++)
        {
            if (disableOnGameOver[i] != null)
                disableOnGameOver[i].SetActive(true);
        }

        if (score != null) score.ResetScore();
        if (soloBall != null) soloBall.StartRound();

        if (hudPanel != null) hudPanel.SetActive(true);
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    public void StartGameFromMenu()
    {
        Time.timeScale = 1f;
        StartNewRun();
    }
}