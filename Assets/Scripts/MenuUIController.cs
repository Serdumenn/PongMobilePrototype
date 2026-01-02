using System.Collections;
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

    [Header("Start Type (0=tap, 1=countdown)")]
    public Button tapToStartButton;
    public Button countdownButton;
    public bool bringSelectedToFront = true;

    [Header("Difficulty (CYCLE) (0=easy, 1=medium, 2=hard)")]
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;

    [Header("DiffNeed (tap to cycle)")]
    public Button diffNeedCycleButton;
    public int diffMin = 1;
    public int diffMax = 9;
    public int diffDefault = 3;

    [Header("Optional Click SFX")]
    public AudioSource clickSfx;

    const string KEY_START_TYPE = "startType";
    const string KEY_DIFFICULTY = "difficulty";
    const string KEY_DIFF_NEED  = "diffNeed";

    private Image _easyImg, _medImg, _hardImg;
    private Color _easyColor, _medColor, _hardColor;
    private Coroutine _diffVisualRoutine;

    void Awake()
    {
        SeedDefaults();
        CacheDifficultyColorsFromInspector();
        BindButtons();
        ShowMenuScreen();
        ApplyAllVisualsFromPrefs();
    }

    void SeedDefaults()
    {
        if (!PlayerPrefs.HasKey(KEY_START_TYPE)) PlayerPrefs.SetInt(KEY_START_TYPE, 0);
        if (!PlayerPrefs.HasKey(KEY_DIFFICULTY)) PlayerPrefs.SetInt(KEY_DIFFICULTY, 0);
        if (!PlayerPrefs.HasKey(KEY_DIFF_NEED))  PlayerPrefs.SetInt(KEY_DIFF_NEED, diffDefault);
        PlayerPrefs.Save();
    }

    void CacheDifficultyColorsFromInspector()
    {
        _easyImg = easyButton ? easyButton.GetComponent<Image>() : null;
        _medImg  = mediumButton ? mediumButton.GetComponent<Image>() : null;
        _hardImg = hardButton ? hardButton.GetComponent<Image>() : null;

        _easyColor = _easyImg ? _easyImg.color : Color.white;
        _medColor  = _medImg  ? _medImg.color  : Color.white;
        _hardColor = _hardImg ? _hardImg.color : Color.white;
    }

    void BindButtons()
    {
        if (playButton)         Rebind(playButton, StartMatch);
        if (openSettingsButton) Rebind(openSettingsButton, ShowSettingsScreen);
        if (backButton)         Rebind(backButton, ShowMenuScreen);

        if (tapToStartButton)   Rebind(tapToStartButton, () => SetStartType(0));
        if (countdownButton)    Rebind(countdownButton,  () => SetStartType(1));

        if (easyButton)   Rebind(easyButton,   CycleDifficulty);
        if (mediumButton) Rebind(mediumButton, CycleDifficulty);
        if (hardButton)   Rebind(hardButton,   CycleDifficulty);

        if (diffNeedCycleButton) Rebind(diffNeedCycleButton, CycleDiffNeed);
    }

    void Rebind(Button btn, UnityEngine.Events.UnityAction action)
    {
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            if (clickSfx) clickSfx.Play();
            action.Invoke();
        });
    }


    public void StartMatch()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameSceneName);
    }

    public void ShowMenuScreen()
    {
        if (menuScreenRoot) menuScreenRoot.SetActive(true);
        if (settingsScreenRoot) settingsScreenRoot.SetActive(false);
    }

    public void ShowSettingsScreen()
    {
        if (menuScreenRoot) menuScreenRoot.SetActive(false);
        if (settingsScreenRoot) settingsScreenRoot.SetActive(true);
        ApplyAllVisualsFromPrefs();
    }


    public void SetStartType(int v)
    {
        PlayerPrefs.SetInt(KEY_START_TYPE, v);
        PlayerPrefs.Save();
        ApplyStartTypeVisuals(v);
        Debug.Log($"startType -> {v}");
    }

    public void CycleDifficulty()
    {
        int current = PlayerPrefs.GetInt(KEY_DIFFICULTY, 0);
        int next = (current >= 2) ? 0 : (current + 1);

        PlayerPrefs.SetInt(KEY_DIFFICULTY, next);
        PlayerPrefs.Save();

        ApplyDifficultyVisuals(next);
        Debug.Log($"difficulty -> {next} ({(next == 0 ? "easy" : next == 1 ? "medium" : "hard")})");
    }

    public void CycleDiffNeed()
    {
        int current = PlayerPrefs.GetInt(KEY_DIFF_NEED, diffDefault);
        int next = (current >= diffMax) ? diffMin : (current + 1);

        PlayerPrefs.SetInt(KEY_DIFF_NEED, next);
        PlayerPrefs.Save();

        Debug.Log($"diffNeed -> {next}");
    }

    void ApplyAllVisualsFromPrefs()
    {
        ApplyStartTypeVisuals(PlayerPrefs.GetInt(KEY_START_TYPE, 0));
        ApplyDifficultyVisuals(PlayerPrefs.GetInt(KEY_DIFFICULTY, 0));
    }

    void ApplyStartTypeVisuals(int startType)
    {
        if (!tapToStartButton || !countdownButton) return;

        bool isTap = (startType == 0);

        if (bringSelectedToFront)
        {
            if (isTap) tapToStartButton.transform.SetAsLastSibling();
            else countdownButton.transform.SetAsLastSibling();
        }

        tapToStartButton.interactable = !isTap;
        countdownButton.interactable = isTap;
    }

    void ApplyDifficultyVisuals(int diff)
    {
        if (easyButton)   easyButton.gameObject.SetActive(diff == 0);
        if (mediumButton) mediumButton.gameObject.SetActive(diff == 1);
        if (hardButton)   hardButton.gameObject.SetActive(diff == 2);

        if (_diffVisualRoutine != null) StopCoroutine(_diffVisualRoutine);
        _diffVisualRoutine = StartCoroutine(ForceDifficultyColorsAfterEnable());
    }

    IEnumerator ForceDifficultyColorsAfterEnable()
    {
        yield return null;

        ApplyDifficultyColorsNow();

        yield return null;

        ApplyDifficultyColorsNow();
    }

    void ApplyDifficultyColorsNow()
    {
        if (_easyImg) _easyImg.color = _easyColor;
        if (_medImg)  _medImg.color  = _medColor;
        if (_hardImg) _hardImg.color = _hardColor;
    }
}