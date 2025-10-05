using UnityEngine;
using UnityEngine.UI;

public class scoreVisuals : MonoBehaviour {
    public int enemyScore, playerScore;
    public int diffNeed;
    public Image[] scorePoints;
    public score scorescript;
    private manager managerScript;
    public GameObject managerObj;
    void Start() 
    {
        managerScript = managerObj.transform.GetComponent<manager>();
        ResetVisuals();
    }

    public void playerScores() {
        playerScore += 1;
        UpdateScore();
    }

    public void enemyScores() {
        enemyScore += 1;
        UpdateScore();
    }

    void UpdateScore() 
    {
        int scoreDiff = playerScore - enemyScore;
        UpdateVisuals(scoreDiff);

        if (Mathf.Abs(scoreDiff) >= diffNeed) 
        {
            if (scoreDiff > 0) 
            {
                managerScript.GameEnding(true);
            } 
            else 
            {
                managerScript.GameEnding(false);
            }
        }
    }

    void UpdateVisuals(int diff) 
    {
        ResetVisuals();

        int absDiff = Mathf.Abs(diff);
        if (diff > 0) 
        { 
            for (int i = 0; i < absDiff && i < scorePoints.Length; i++) 
            {
                int index = scorePoints.Length - 1 - i;
                scorePoints[index].color = Color.blue;
            }
        } 
        else if (diff < 0) 
        { 
            for (int i = 0; i < absDiff && i < scorePoints.Length; i++) 
            {
                int index = i;
                scorePoints[index].color = Color.red;
            }
        }
    }
    // make every image grey
    void ResetVisuals() 
    {
        foreach (Image img in scorePoints) 
        {
            img.color = Color.gray;
        }
    }
}
