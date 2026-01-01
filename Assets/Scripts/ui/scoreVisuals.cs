using UnityEngine;
using UnityEngine.UI;

public class scoreVisuals : MonoBehaviour
{
    public int enemyScore, playerScore;
    public int diffNeed = 3;

    public Image[] scorePoints;
    public score scorescript;

    private manager managerScript;
    public GameObject managerObj;

    void Start()
    {
        GameSettings.EnsureLoaded();
        diffNeed = GameSettings.DiffNeed;

        if (managerObj != null)
            managerScript = managerObj.GetComponent<manager>();

        ResetVisuals();
    }

    public void playerScores()
    {
        playerScore += 1;
        UpdateScore();
    }

    public void enemyScores()
    {
        enemyScore += 1;
        UpdateScore();
    }

    void UpdateScore()
    {
        int scoreDiff = playerScore - enemyScore;
        UpdateVisuals(scoreDiff);

        if (Mathf.Abs(scoreDiff) >= diffNeed && managerScript != null)
            managerScript.GameEnding(scoreDiff > 0);
    }

    void UpdateVisuals(int diff)
    {
        ResetVisuals();
        if (scorePoints == null || scorePoints.Length == 0) return;

        int absDiff = Mathf.Abs(diff);

        if (diff > 0)
        {
            for (int i = 0; i < absDiff && i < scorePoints.Length; i++)
            {
                int index = scorePoints.Length - 1 - i;
                if (scorePoints[index] != null) scorePoints[index].color = Color.blue;
            }
        }
        else if (diff < 0)
        {
            for (int i = 0; i < absDiff && i < scorePoints.Length; i++)
            {
                int index = i;
                if (scorePoints[index] != null) scorePoints[index].color = Color.red;
            }
        }
    }

    void ResetVisuals()
    {
        if (scorePoints == null) return;
        foreach (var img in scorePoints)
            if (img != null) img.color = Color.gray;
    }
}