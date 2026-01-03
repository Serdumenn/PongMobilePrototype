using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    [Header("UI (optional but recommended)")]
    public Text playerText;
    public Text enemyText;

    [Header("Bars / Win Logic")]
    [Tooltip("Bars per side. Example: 3 bars fill; 4th goal ends the match.")]
    public int barsPerSide = 3;

    [Header("Manager Reference")]
    public manager gameManager;

    private int playerScore;
    private int enemyScore;

    public bool IsGameOver { get; private set; }

    void Awake()
    {
        if (barsPerSide < 1) barsPerSide = 3;

        if (gameManager == null)
            gameManager = FindAny<manager>();

        if (playerText == null || enemyText == null)
            AutoBindTexts();
    }

    void Start()
    {
        ResetScores();
    }

    public void ResetScores()
    {
        playerScore = 0;
        enemyScore = 0;
        IsGameOver = false;
        RefreshUI();
    }

    public bool PlayerScored()
    {
        if (IsGameOver) return true;

        playerScore++;
        RefreshUI();
        return CheckGameOver();
    }

    public bool EnemyScored()
    {
        if (IsGameOver) return true;

        enemyScore++;
        RefreshUI();
        return CheckGameOver();
    }

    public int GetPlayerScore() => playerScore;
    public int GetEnemyScore() => enemyScore;

    private bool CheckGameOver()
    {
        int winScore = barsPerSide + 1;

        if (playerScore >= winScore)
        {
            IsGameOver = true;
            if (gameManager != null) gameManager.GameEnding(true);
            return true;
        }

        if (enemyScore >= winScore)
        {
            IsGameOver = true;
            if (gameManager != null) gameManager.GameEnding(false);
            return true;
        }

        return false;
    }

    private void RefreshUI()
    {
        if (playerText != null) playerText.text = playerScore.ToString();
        if (enemyText != null) enemyText.text = enemyScore.ToString();
    }

    private void AutoBindTexts()
    {
        var texts = GetComponentsInChildren<Text>(true);
        foreach (var t in texts)
        {
            if (t == null) continue;
            string n = t.gameObject.name.ToLowerInvariant();

            if (playerText == null && n.Contains("player") && n.Contains("score"))
                playerText = t;

            if (enemyText == null && (n.Contains("enemy") || n.Contains("ai")) && n.Contains("score"))
                enemyText = t;
        }
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