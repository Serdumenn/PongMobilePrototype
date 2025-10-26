using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class score : MonoBehaviour
{
    public int enemy, character;
    private ball ballscript;
    public GameObject ballObject;
    public Text playerText;
    public Text enemyText;
    private scoreVisuals visuals;
    public GameObject scoreBar;
    private Animator text1Animator, text2Animator;

    void Awake()
    {
        if (playerText) text1Animator = playerText.GetComponent<Animator>();
        if (enemyText)  text2Animator = enemyText.GetComponent<Animator>();

        if (scoreBar)  visuals = scoreBar.GetComponent<scoreVisuals>();
        if (ballObject) ballscript = ballObject.GetComponent<ball>();
    }

    public void ChangeScore(bool player)
    {
        if (player)
        {
            character += 1;
            if (playerText) playerText.text = character.ToString();
            if (text1Animator) StartCoroutine(TriggerAnimator(text1Animator));
            if (visuals) visuals.playerScores();
        }
        else
        {
            enemy += 1;
            if (enemyText) enemyText.text = enemy.ToString();
            if (text2Animator) StartCoroutine(TriggerAnimator(text2Animator));
            if (visuals) visuals.enemyScores();
        }
    }

    public IEnumerator TriggerAnimator(Animator animator)
    {
        yield return null;
        if (animator != null) animator.SetTrigger("change");
    }
}