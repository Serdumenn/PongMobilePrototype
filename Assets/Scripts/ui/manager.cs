using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class manager : MonoBehaviour
{
    public GameObject racket1, racket2;
    public GameObject ball;
    public GameObject win, lose;
    public GameObject gameUI, endUI, pauseMenu;
    private ball ballcs;
    private SpriteRenderer ballRenderer;

    void Awake()
    {
        if (Application.targetFrameRate != 60)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Debug.Log("Target FPS set to 60");
        }

        if (ball != null)
        {
            ballcs = ball.GetComponent<ball>();
            ballRenderer = ball.GetComponent<SpriteRenderer>();
        }
    }

    public void GameEnding(bool whoWin)
    {
        if (win != null) win.SetActive(whoWin);
        if (lose != null) lose.SetActive(!whoWin);

        if (racket1) racket1.SetActive(false);
        if (racket2) racket2.SetActive(false);
        if (ballRenderer != null) ballRenderer.enabled = false;
        if (gameUI) gameUI.SetActive(false);
        if (endUI) endUI.SetActive(true);
    }

    public void Restart()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("game");
    }

    public void GoMenu()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("menu");
    }

    public void Pause()
    {
        Time.timeScale = 0;
        if (ballRenderer != null) ballRenderer.enabled = false;
        if (gameUI) gameUI.SetActive(false);
        if (pauseMenu) pauseMenu.SetActive(true);
        if (racket1) racket1.SetActive(false);
        if (racket2) racket2.SetActive(false);
    }

    public void unPause()
    {
        Time.timeScale = 1;
        if (ballRenderer != null) ballRenderer.enabled = true;
        if (gameUI) gameUI.SetActive(true);
        if (racket1) racket1.SetActive(true);
        if (racket2) racket2.SetActive(true);
        if (pauseMenu) pauseMenu.SetActive(false);
    }
}