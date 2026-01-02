using UnityEngine;
using UnityEngine.UI;

public class scoreVisuals : MonoBehaviour
{
    [Header("Visual Only (reads from ScoreManager)")]
    public Image[] scorePoints;

    [Header("Optional refs")]
    public ScoreManager scoreManager;

    [Tooltip("If you want a centered bar: right side = player lead, left side = enemy lead")]
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
        if (scoreManager == null || scorePoints == null || scorePoints.Length == 0)
            return;

        int diff = scoreManager.GetPlayerScore() - scoreManager.GetEnemyScore();
        DrawDiff(diff);
    }

    private void DrawDiff(int diff)
    {
        // Basit mantık: tümünü nötrle, sonra fark kadar boyayalım.
        foreach (var img in scorePoints)
            if (img) img.color = neutralColor;

        int n = Mathf.Min(Mathf.Abs(diff), scorePoints.Length);

        if (diff > 0)
        {
            // player leads -> sağdan boya
            for (int i = 0; i < n; i++)
            {
                int idx = scorePoints.Length - 1 - i;
                if (scorePoints[idx]) scorePoints[idx].color = playerColor;
            }
        }
        else if (diff < 0)
        {
            // enemy leads -> soldan boya
            for (int i = 0; i < n; i++)
            {
                int idx = i;
                if (scorePoints[idx]) scorePoints[idx].color = enemyColor;
            }
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