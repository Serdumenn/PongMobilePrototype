using UnityEngine;

public class SoloScoreManager : MonoBehaviour
{
    public const string BestScoreKey = "bestScore";

    public int Score { get; private set; }
    public int BestScore { get; private set; }
    public bool IsGameOver { get; private set; }

    void Awake()
    {
        BestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        Score = 0;
        IsGameOver = false;
    }

    public void ResetScore()
    {
        Score = 0;
        IsGameOver = false;
    }

    public void AddPoint()
    {
        if (IsGameOver) return;

        Score += 1;

        if (Score > BestScore)
        {
            BestScore = Score;
            PlayerPrefs.SetInt(BestScoreKey, BestScore);
            PlayerPrefs.Save();
        }
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
    }
}