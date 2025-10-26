using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class mainMenu : MonoBehaviour
{
    public GameObject[] menus;
    public AudioSource[] soundEffects;
    public saving prefs;

    public void LoadScene(int index)
    {
        playSound(0);
        SceneManager.LoadScene(index);
    }

    public void restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void playSound(int index)
    {
        if (soundEffects == null || index < 0 || index >= soundEffects.Length) return;
        var src = soundEffects[index];
        if (src != null) src.Play();
    }

    public void playAnim(GameObject button)
    {
        if (button == null) return;
        var animator = button.GetComponent<Animator>();
        if (animator != null) animator.SetTrigger("click");
    }
}