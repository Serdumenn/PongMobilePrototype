using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SoloGameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SoloBall SoloBall;
    [SerializeField] private SoloScoreManager Score;

    [Header("UI")]
    [SerializeField] private GameObject GameOverPanel;
    [SerializeField] private Button RetryButton;
    [SerializeField] private Button MenuButton;

    [Header("Single-scene Menu")]
    [SerializeField] private GameObject MenuPanel;
    [SerializeField] private GameObject HudPanel;

    [Header("Behavior")]
    [SerializeField] private bool PauseTimeOnGameOver = true;
    [SerializeField] private bool StopBallWhenMenuOpen = true;

    [Header("Other")]
    [SerializeField] private List<GameObject> DisableOnGameOver = new List<GameObject>();

    private bool IsGameOverInternal;
    public bool IsGameOver => IsGameOverInternal;

    private void Awake()
    {
        if (SoloBall == null) SoloBall = FindFirstObjectByType<SoloBall>();
        if (Score == null) Score = FindFirstObjectByType<SoloScoreManager>();

        if (GameOverPanel != null) GameOverPanel.SetActive(false);
    }

    public void GameOver()
    {
        if (IsGameOverInternal) return;
        IsGameOverInternal = true;

        if (PauseTimeOnGameOver) Time.timeScale = 0f;

        if (GameOverPanel != null) GameOverPanel.SetActive(true);

        for (int i = 0; i < DisableOnGameOver.Count; i++)
        {
            if (DisableOnGameOver[i] != null)
                DisableOnGameOver[i].SetActive(false);
        }

        if (SoloBall != null) SoloBall.StopRound();
    }

    public void RestartRun()
    {
        OnRetryClicked();
    }

    public void ReturnToMenu()
    {
        OnMenuClicked();
    }

    private void OnRetryClicked()
    {
        Time.timeScale = 1f;
        if (GameOverPanel != null) GameOverPanel.SetActive(false);

        StartNewRun();
    }

    private void OnMenuClicked()
    {
        Time.timeScale = 1f;

        if (MenuPanel != null)
        {
            if (HudPanel != null) HudPanel.SetActive(false);
            if (GameOverPanel != null) GameOverPanel.SetActive(false);

            MenuPanel.SetActive(true);

            if (StopBallWhenMenuOpen && SoloBall != null)
                SoloBall.StopRound();

            IsGameOverInternal = false;
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void StartNewRun()
    {
        IsGameOverInternal = false;

        for (int i = 0; i < DisableOnGameOver.Count; i++)
        {
            if (DisableOnGameOver[i] != null)
                DisableOnGameOver[i].SetActive(true);
        }

        if (Score != null) Score.ResetScore();
        if (SoloBall != null) SoloBall.StartRound();

        if (HudPanel != null) HudPanel.SetActive(true);
        if (MenuPanel != null) MenuPanel.SetActive(false);
    }

    public void StartGameFromMenu()
    {
        Time.timeScale = 1f;
        StartNewRun();
    }
}