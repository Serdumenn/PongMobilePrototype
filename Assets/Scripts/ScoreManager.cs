using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    [Header("UI Text (optional)")]
    public Text playerText;
    public Text enemyText;

    [Header("Win Condition")]
    public int diffNeed = 3;

    [Header("Game Manager")]
    public manager gameManager;

    private int playerScore;
    private int enemyScore;

    private void Awake()
    {
        TryLoadDiffNeed();

        if (gameManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            gameManager = Object.FindAnyObjectByType<manager>();
#else
            gameManager = Object.FindObjectOfType<manager>();
#endif
        }
    }

    private void Start()
    {
        RefreshUI();
    }

    private void TryLoadDiffNeed()
    {
        try
        {
            GameSettings.EnsureLoaded();
            diffNeed = GameSettings.DiffNeed;
        }
        catch { /* none */ }
    }

    public void PlayerScored()
    {
        playerScore++;
        RefreshUI();
        CheckEnd();
    }

    public void EnemyScored()
    {
        enemyScore++;
        RefreshUI();
        CheckEnd();
    }

    public void ResetScores()
    {
        playerScore = 0;
        enemyScore = 0;
        RefreshUI();
    }

    public int GetPlayerScore() => playerScore;
    public int GetEnemyScore() => enemyScore;

    private void RefreshUI()
    {
        if (playerText) playerText.text = playerScore.ToString();
        if (enemyText) enemyText.text = enemyScore.ToString();
    }

    private void CheckEnd()
    {
        int diff = playerScore - enemyScore;
        if (gameManager != null && Mathf.Abs(diff) >= diffNeed)
        {
            gameManager.GameEnding(diff > 0);
        }
    }
}