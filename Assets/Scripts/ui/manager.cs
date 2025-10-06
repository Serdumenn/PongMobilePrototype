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
        // --- FPS ve VSync ayarı ---
        if (Application.targetFrameRate != 60)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Debug.Log("Target FPS set to 60");
        }

        // --- Top referansları ---
        ballcs = ball.GetComponent<ball>();
        ballRenderer = ball.GetComponent<SpriteRenderer>();
    }

    public void GameEnding(bool whoWin)
    {
        if (whoWin)
        {
            win.SetActive(true);
        }
        else
        {
            lose.SetActive(true);
        }

        // disable some elements
        racket1.SetActive(false);
        racket2.SetActive(false);
        if (ballRenderer != null) ballRenderer.enabled = false;
        gameUI.SetActive(false);
        endUI.SetActive(true);
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
        // Yeni ball.cs içinde coroutineRunning yok, onun yerine coroutine kontrolü yapmıyoruz artık.
        // Sadece topu durdurup gizliyoruz:
        Time.timeScale = 0;
        if (ballRenderer != null) ballRenderer.enabled = false;
        gameUI.SetActive(false);
        pauseMenu.SetActive(true);
        racket1.SetActive(false);
        racket2.SetActive(false);
    }

    public void unPause()
    {
        // Reset coroutine’ini elle yeniden başlatmaya gerek yok — top kendi mantığıyla resetleniyor.
        Time.timeScale = 1;
        if (ballRenderer != null) ballRenderer.enabled = true;
        gameUI.SetActive(true);
        racket1.SetActive(true);
        racket2.SetActive(true);
        pauseMenu.SetActive(false);
    }
}
