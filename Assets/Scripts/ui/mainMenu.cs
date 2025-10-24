using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class mainMenu : MonoBehaviour {
    public GameObject[] menus;
    public AudioSource[] soundEffects;
    public saving prefs;
    public void LoadScene(int index)
    {
        playSound(0);
        SceneManager.LoadScene(index);
    }
    public void restart ()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    /*
    public void goMenu ()
    {
        SceneManager.LoadScene(0);
    }
    // this is universal button for all back buttons
    /*public void BackUniversal ()
    {
        playSound(0);
        for(int i = 0; i < menus.Length; i++)
        {
            menus[i].SetActive(false);
        }
        menus[0].SetActive(true);
    }*/
    // plays button sound for all buttons
    public void playSound (int index)
    {
        soundEffects[index].Play();
    }
    public void playAnim (GameObject button)
    {
        Animator animator = button.GetComponent<Animator>();
        animator.SetTrigger("click");
    }
}