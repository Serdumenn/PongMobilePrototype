using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    [Header("Scenes")]
    public string gameSceneName = "game";

    [Header("Roots")]
    public GameObject menuScreenRoot;
    public GameObject settingsScreenRoot;

    [Header("Navigation Buttons")]
    public Button playButton;
    public Button openSettingsButton;
    public Button backButton;

    [Header("Difficulty (0=easy, 1=medium, 2=hard)")]
    public Button difficultyCycleButton;

    [Header("Optional Click SFX")]
    public AudioSource clickSfx;

    const string KEY_DIFFICULTY = "difficulty";

    void Start()
    {
        Rebind(playButton, StartGame);
        Rebind(openSettingsButton, OpenSettings);
        Rebind(backButton, BackToMenu);
        if (difficultyCycleButton) Rebind(difficultyCycleButton, CycleDifficulty);

        ShowMenu();
    }

    void Rebind(Button btn, UnityEngine.Events.UnityAction action)
    {
        if (!btn) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    void Click()
    {
        if (clickSfx) clickSfx.Play();
    }

    public void StartGame()
    {
        Click();
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSettings()
    {
        Click();
        ShowSettings();
    }

    public void BackToMenu()
    {
        Click();
        ShowMenu();
    }

    void ShowMenu()
    {
        if (menuScreenRoot) menuScreenRoot.SetActive(true);
        if (settingsScreenRoot) settingsScreenRoot.SetActive(false);
    }

    void ShowSettings()
    {
        if (menuScreenRoot) menuScreenRoot.SetActive(false);
        if (settingsScreenRoot) settingsScreenRoot.SetActive(true);
    }

    public void CycleDifficulty()
    {
        Click();
        int current = PlayerPrefs.GetInt(KEY_DIFFICULTY, 0);
        int next = (current >= 2) ? 0 : (current + 1);
        PlayerPrefs.SetInt(KEY_DIFFICULTY, next);
        PlayerPrefs.Save();
    }
}