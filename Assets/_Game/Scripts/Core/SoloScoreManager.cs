using System;
using UnityEngine;

public sealed class SoloScoreManager : MonoBehaviour
{
    public const string BestScoreKey = "bestScore";

    public event Action<int> ScoreChanged;

    public int Score { get; private set; }
    public int BestScore { get; private set; }
    public int PreviousBest { get; private set; }
    public bool IsNewBest { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool Unranked { get; set; }

    private string bestKey = BestScoreKey;

    private void Awake()
    {
        BestScore = PlayerPrefs.GetInt(bestKey, 0);
        ResetScore();
    }

    public void SetBestKey(string key)
    {
        bestKey = string.IsNullOrEmpty(key) ? BestScoreKey : key;
        BestScore = PlayerPrefs.GetInt(bestKey, 0);
        PreviousBest = BestScore;
    }

    public int BestFor(string key)
    {
        return PlayerPrefs.GetInt(string.IsNullOrEmpty(key) ? BestScoreKey : key, 0);
    }

    public void ResetScore()
    {
        Score = 0;
        IsGameOver = false;
        IsNewBest = false;
        PreviousBest = BestScore;
        ScoreChanged?.Invoke(Score);
    }

    public void AddPoints(int points)
    {
        if (IsGameOver || points <= 0) return;

        Score += points;

        if (!Unranked && Score > BestScore)
        {
            BestScore = Score;
            PlayerPrefs.SetInt(bestKey, BestScore);
        }

        ScoreChanged?.Invoke(Score);
    }

    public void GameOver()
    {
        if (IsGameOver) return;

        IsGameOver = true;
        IsNewBest = !Unranked && Score > 0 && Score > PreviousBest;
        PlayerPrefs.Save();
    }

    public void ResetBests(params string[] keys)
    {
        foreach (var key in keys) PlayerPrefs.DeleteKey(string.IsNullOrEmpty(key) ? BestScoreKey : key);
        PlayerPrefs.Save();

        BestScore = 0;
        PreviousBest = 0;
    }
}
