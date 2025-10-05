using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class score : MonoBehaviour {
    public int enemy, character;
    private ball ballscript;
    public GameObject ballObject;
    public Text playerText;
    public Text enemyText;
    private scoreVisuals visuals;
    public GameObject scoreBar;
    private Animator text1Animator, text2Animator;
    void Awake ()
    {
        text1Animator = playerText.GetComponent<Animator>();
        text2Animator = enemyText.GetComponent<Animator>();
        visuals = scoreBar.transform.GetComponent<scoreVisuals>();
        ballscript = ballObject.transform.GetComponent<ball>(); 
    }
    public void ChangeScore(bool player)
    {

        if (player)
        {
            print("charcter score");
            character += 1;
            playerText.text = character.ToString();
            TriggerAnimator(text1Animator);
            visuals.playerScores();
        }
        if (!player)
        {
            print("enemy score");
            enemy +=1;
            enemyText.text = enemy.ToString();
            TriggerAnimator(text2Animator);
            visuals.enemyScores();
        }
    }
    public IEnumerator TriggerAnimator(Animator animator)
    {
        yield return null; // Wait one frame
        animator.SetTrigger("change");
    }
}
