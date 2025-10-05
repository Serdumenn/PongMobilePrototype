using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class manager : MonoBehaviour {
    public GameObject racket1 , racket2;
    public GameObject ball;
    public GameObject win, lose;
    public GameObject gameUI, endUI, pauseMenu;
    private ball ballcs;
    void Awake ()
    {
        ballcs = ball.GetComponent<ball>();
    }
    public void GameEnding (bool whoWin)
    {
        if(whoWin)
        {
            win.SetActive(true);
        }
        if(!whoWin) 
        {
            lose.SetActive(true);
        }
        // disable some elements
        racket1.SetActive(false);
        racket2.SetActive(false);
        ballcs.spriteRenderer.enabled = false;
        gameUI.SetActive(false);
        endUI.SetActive(true);
    }
    public void Restart ()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("game");
    }
    public void GoMenu ()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("menu");
    }
    public void Pause ()
    {
        if(ballcs.coroutineRunning)
        {
            ballcs.StopAllCoroutines();
        }
        Time.timeScale = 0;
        ballcs.spriteRenderer.enabled = false;
        gameUI.SetActive(false);
        pauseMenu.SetActive(true);
        racket1.SetActive(false);
        racket2.SetActive(false);
    }
    public void unPause ()
    {
        if(ballcs.coroutineRunning)
        {
            ballcs.StartCoroutine("ResetBallWithCountdown");
        }
        Time.timeScale = 1;
        ballcs.spriteRenderer.enabled = true;
        gameUI.SetActive(true);
        racket1.SetActive(true);
        racket2.SetActive(true);
        pauseMenu.SetActive(false);
    }
}
