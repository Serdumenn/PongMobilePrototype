using UnityEngine;

public class SoloScoreManager : MonoBehaviour
{
    public const string BestScoreKey = "bestScore";

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

        if (logScoreChanges)
            Debug.Log($"[SoloScoreManager] Reset. Best={BestScore}");
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

        if (logScoreChanges)
            Debug.Log($"[SoloScoreManager] Score={Score}, Best={BestScore}");
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        if (logScoreChanges)
            Debug.Log($"[SoloScoreManager] GameOver. Final={Score}, Best={BestScore}");
    }
}