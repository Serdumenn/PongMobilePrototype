using UnityEngine;
using TMPro;

public class SoloScoreManager : MonoBehaviour
{
    public const string BestScoreKey = "bestScore";

    [Header("UI")]
    [SerializeField] private TMP_Text HudScoreText;
    [SerializeField] private TMP_Text GameOverScoreText;
    [SerializeField] private TMP_Text MenuBestScoreText;

    [Header("Debug")]
    public bool logScoreChanges = false;

    public int Score { get; private set; }
    public int BestScore { get; private set; }
    public bool IsGameOver { get; private set; }

    void Awake()
    {
        BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        ResetScore();
        UpdateMenuBestText();
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
        }

        UpdateHudText();
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        PlayerPrefs.Save();

        if (GameOverScoreText != null)
            GameOverScoreText.text = Score.ToString();

        UpdateMenuBestText();
    }

    private void UpdateHudText()
    {
        if (HudScoreText != null)
            HudScoreText.text = Score.ToString();
    }

    private void UpdateMenuBestText()
    {
        if (MenuBestScoreText == null) return;

        if (BestScore > 0)
        {
            MenuBestScoreText.gameObject.SetActive(true);
            MenuBestScoreText.text = $"Best Score: {BestScore}";
        }
        else
        {
            MenuBestScoreText.gameObject.SetActive(false);
        }
    }

    /// <summary>Debug: PlayerPrefs best score sıfırlama.</summary>
    public static void ClearBestScore()
    {
        PlayerPrefs.DeleteKey(BestScoreKey);
        PlayerPrefs.Save();
    }
}
