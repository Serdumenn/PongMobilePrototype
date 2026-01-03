using UnityEngine;
using UnityEngine.UI;

public class scoreVisuals : MonoBehaviour
{
    [Header("Visual Only (reads from ScoreManager)")]
    public Image[] scorePoints;

    [Header("Optional refs")]
    public ScoreManager scoreManager;

    [Header("Colors")]
    public Color playerColor = Color.blue;
    public Color enemyColor = Color.red;
    public Color neutralColor = Color.gray;

    void Awake()
    {
        if (scoreManager == null)
            scoreManager = FindAny<ScoreManager>();
    }

    void Update()
    {
        if (scoreManager == null || scorePoints == null || scorePoints.Length < 2)
            return;

        int total = scorePoints.Length;
        int perSide = total / 2;
        if (perSide <= 0) return;

        for (int i = 0; i < total; i++)
            if (scorePoints[i] != null) scorePoints[i].color = neutralColor;

        int playerScore = scoreManager.GetPlayerScore();
        int enemyScore = scoreManager.GetEnemyScore();

        int enemyFill = Mathf.Min(enemyScore, perSide);
        for (int i = 0; i < enemyFill; i++)
        {
            int idx = (perSide - 1) - i;
            if (idx >= 0 && idx < total && scorePoints[idx] != null)
                scorePoints[idx].color = enemyColor;
        }

        int playerFill = Mathf.Min(playerScore, perSide);
        for (int i = 0; i < playerFill; i++)
        {
            int idx = perSide + i;
            if (idx >= 0 && idx < total && scorePoints[idx] != null)
                scorePoints[idx].color = playerColor;
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