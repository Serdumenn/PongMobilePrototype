using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    [Header("UI (optional but recommended)")]
    public Text playerText;
    public Text enemyText;

    [Header("Win Condition")]
    public int diffNeed = 3;
    public manager gameManager;

    private int playerScore;
    private int enemyScore;

    void Awake()
    {
        // manager auto-find
        if (gameManager == null)
            gameManager = FindAny<manager>();

        // UI auto-bind (best-effort)
        if (playerText == null || enemyText == null)
            AutoBindTexts();
    }

    void Start()
    {
        GameSettings.ForceReload();
        diffNeed = GameSettings.DiffNeed;
        RefreshUI();
    }

    public void ResetScores()
    {
        playerScore = 0;
        enemyScore = 0;
        RefreshUI();
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
            gameManager.GameEnding(diff > 0);
    }

    private void AutoBindTexts()
    {
        // Canvas altındaki Text'leri tarayıp isimden yakalamaya çalışır
        var texts = GetComponentsInChildren<Text>(true);
        foreach (var t in texts)
        {
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