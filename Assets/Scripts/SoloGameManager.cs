using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SoloGameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SoloBall SoloBall;
    [SerializeField] private SoloScoreManager Score;
    [SerializeField] private RacketController Paddle;

    [Header("Entrance")]
    [SerializeField] private GameObjectEntrance BallEntrance;
    [SerializeField] private GameObjectEntrance RacketEntrance;

    [Header("UI")]
    [SerializeField] private PanelTransition GameOverPanel;
    [SerializeField] private Button RetryButton;
    [SerializeField] private Button MenuButton;

    [Header("Single-scene Menu")]
    [SerializeField] private PanelTransition MenuPanel;
    [SerializeField] private GameObject HudPanel;
    [SerializeField] private MenuUIController MenuController;

    [Header("Behavior")]
    [SerializeField] private bool PauseTimeOnGameOver = true;
    [SerializeField] private bool StopBallWhenMenuOpen = true;

    [Header("Other")]
    [SerializeField] private List<GameObject> DisableOnGameOver = new List<GameObject>();

    private bool IsGameOverInternal;
    public bool IsGameOver => IsGameOverInternal;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        if (SoloBall == null) SoloBall = FindFirstObjectByType<SoloBall>();
        if (Score == null) Score = FindFirstObjectByType<SoloScoreManager>();
        if (Paddle == null) Paddle = FindFirstObjectByType<RacketController>();
        if (MenuController == null) MenuController = FindFirstObjectByType<MenuUIController>();

        if (BallEntrance == null && SoloBall != null)
            BallEntrance = SoloBall.GetComponent<GameObjectEntrance>();
        if (RacketEntrance == null && Paddle != null)
            RacketEntrance = Paddle.GetComponent<GameObjectEntrance>();

        if (Paddle != null) Paddle.InputEnabled = false;
        if (GameOverPanel != null) GameOverPanel.HideInstant();
    }

    private void Start()
    {
        PlayEntranceAnimations();
    }

    public void GameOver()
    {
        if (IsGameOverInternal) return;
        IsGameOverInternal = true;

        if (PauseTimeOnGameOver) Time.timeScale = 0f;

        if (GameOverPanel != null) GameOverPanel.FadeIn();

        for (int i = 0; i < DisableOnGameOver.Count; i++)
        {
            if (DisableOnGameOver[i] != null)
                DisableOnGameOver[i].SetActive(false);
        }

        if (SoloBall != null) SoloBall.StopRound();
        if (Paddle != null) Paddle.InputEnabled = false;
        if (Paddle != null) Paddle.SetVisible(false);
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
        if (GameOverPanel != null) GameOverPanel.FadeOut();

        StartNewRun();
    }

    private void OnMenuClicked()
    {
        Time.timeScale = 1f;

        if (MenuPanel != null)
        {
            if (HudPanel != null) HudPanel.SetActive(false);
            if (GameOverPanel != null) GameOverPanel.HideInstant();

            if (MenuController != null)
                MenuController.ShowMenu();
            else if (MenuPanel != null)
                MenuPanel.FadeIn();

            if (StopBallWhenMenuOpen && SoloBall != null)
                SoloBall.StopRoundKeepVisible();

            if (Paddle != null) Paddle.InputEnabled = false;

            PlayEntranceAnimations();

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
        if (Paddle != null) Paddle.InputEnabled = true;

        if (HudPanel != null) HudPanel.SetActive(true);
        if (MenuPanel != null) MenuPanel.FadeOut();

        PlayEntranceAnimations();
    }

    public void StartGameFromMenu()
    {
        Time.timeScale = 1f;
        StartNewRun();
    }

    private void PlayEntranceAnimations()
    {
        if (Paddle != null) Paddle.ResetPosition();
        if (SoloBall != null) SoloBall.SnapToServePoint();

        if (RacketEntrance != null)
            RacketEntrance.PlayEntrance(GameObjectEntrance.Direction.Left);
        if (BallEntrance != null)
            BallEntrance.PlayEntrance(GameObjectEntrance.Direction.Right);
    }

    public void HideGameObjects()
    {
        if (SoloBall != null) SoloBall.SetBallVisible(false);
        if (Paddle != null) Paddle.SetVisible(false);
    }

    public void ShowGameObjects()
    {
        if (SoloBall != null) SoloBall.SetBallVisible(true);
        if (Paddle != null) Paddle.SetVisible(true);
    }
}