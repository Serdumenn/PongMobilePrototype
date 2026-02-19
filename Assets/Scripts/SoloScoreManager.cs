using UnityEngine;
using TMPro;

public class SoloScoreManager : MonoBehaviour
{
    public const string BestScoreKey = "bestScore";

    [Header("UI")]
    [SerializeField] private TMP_Text HudScoreText;
    [SerializeField] private TMP_Text GameOverScoreText;

    [Header("Debug")]
    public bool logScoreChanges = false;

    public int Score { get; private set; }
    public int BestScore { get; private set; }
    public bool IsGameOver { get; private set; }

    void Awake()
    {
        BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        ResetScore();
    }

    public void ResetScore()
    {
        Score = 0;
        IsGameOver = false;
        UpdateHudText();
    }

    public void AddPoint()
    {
        if (IsGameOver) return;

        Score++;

        if (Score > BestScore)
        {
            BestScore = Score;
            PlayerPrefs.SetInt(BestScoreKey, BestScore);
            PlayerPrefs.Save();
        }

        UpdateHudText();
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        if (GameOverScoreText != null)
        {
            bool isNewRecord = (Score >= BestScore);
            GameOverScoreText.text = isNewRecord
                ? $"BEST: {BestScore}"
                : $"{Score}";
        }
    }

    private void UpdateHudText()
    {
        if (HudScoreText != null)
            HudScoreText.text = Score.ToString();
    }
}