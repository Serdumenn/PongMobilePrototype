using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class mainMenu : MonoBehaviour
{
    [Header("Optional UI panels (if you use multiple menu screens)")]
    public GameObject[] menus;

    [Header("UI click / menu sfx (optional)")]
    public AudioSource[] soundEffects;

    [Header("Audio prefs seeder (optional)")]
    public saving prefs;

    private const string SCENE_MENU = "menu";
    private const string SCENE_GAME = "game";

    private const string KEY_START_TYPE = "startType";
    private const string KEY_DIFF_NEED  = "diffNeed";
    private const string KEY_DIFFICULTY = "difficulty";

    [Header("Default game settings")]
    public int defaultDiffNeed = 3;

    [Header("DiffNeed cycle (tap-to-advance)")]
    public int minDiffNeed = 1;
    public int maxDiffNeed = 9;

    [Header("Start Type UI (assign these two buttons)")]
    public Button tapToStartButton;
    public Button countdownButton;
    public bool bringSelectedToFront = true;

    [Header("Optional sprites for Start Type selected state")]
    public Sprite startTypeNormalSprite;
    public Sprite startTypeSelectedSprite;

    private void Awake()
    {
        SeedGameSettingsIfMissing();
    }

    private void Start()
    {
        ApplyStartTypeVisuals(PlayerPrefs.GetInt(KEY_START_TYPE, 0));
    }

    private void SeedGameSettingsIfMissing()
    {
        if (!PlayerPrefs.HasKey(KEY_START_TYPE)) PlayerPrefs.SetInt(KEY_START_TYPE, 0);
        if (!PlayerPrefs.HasKey(KEY_DIFF_NEED))  PlayerPrefs.SetInt(KEY_DIFF_NEED, defaultDiffNeed);
        if (!PlayerPrefs.HasKey(KEY_DIFFICULTY)) PlayerPrefs.SetInt(KEY_DIFFICULTY, 0);
        PlayerPrefs.Save();
    }

    public void StartMatch()
    {
        playSound(0);
        Time.timeScale = 1;
        SceneManager.LoadScene(SCENE_GAME);
    }

    public void BackToMenu()
    {
        playSound(0);
        Time.timeScale = 1;
        SceneManager.LoadScene(SCENE_MENU);
    }

    public void LoadScene(int index)
    {
        playSound(0);
        Time.timeScale = 1;
        SceneManager.LoadScene(index);
    }

    public void restart()
    {
        playSound(0);
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ShowMenu(int menuIndex)
    {
        playSound(0);
        if (menus == null || menus.Length == 0) return;

        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i] != null) menus[i].SetActive(i == menuIndex);
        }
    }

    public void SelectTapToStart()
    {
        playSound(0);
        PlayerPrefs.SetInt(KEY_START_TYPE, 0);
        PlayerPrefs.Save();
        ApplyStartTypeVisuals(0);
        Debug.Log("startType -> tap (0)");
    }

    public void SelectCountdown()
    {
        playSound(0);
        PlayerPrefs.SetInt(KEY_START_TYPE, 1);
        PlayerPrefs.Save();
        ApplyStartTypeVisuals(1);
        Debug.Log("startType -> countdown (1)");
    }

    public void CycleDiffNeed()
    {
        playSound(0);

        int current = PlayerPrefs.GetInt(KEY_DIFF_NEED, defaultDiffNeed);
        int next = (current >= maxDiffNeed) ? minDiffNeed : (current + 1);

        PlayerPrefs.SetInt(KEY_DIFF_NEED, next);
        PlayerPrefs.Save();

        Debug.Log($"diffNeed -> {next}");
    }

    public void SetDifficultyEasy()
    {
        playSound(0);
        PlayerPrefs.SetInt(KEY_DIFFICULTY, 0);
        PlayerPrefs.Save();
        Debug.Log("difficulty -> easy (0)");
    }

    public void SetDifficultyMedium()
    {
        playSound(0);
        PlayerPrefs.SetInt(KEY_DIFFICULTY, 1);
        PlayerPrefs.Save();
        Debug.Log("difficulty -> medium (1)");
    }

    public void SetDifficultyHard()
    {
        playSound(0);
        PlayerPrefs.SetInt(KEY_DIFFICULTY, 2);
        PlayerPrefs.Save();
        Debug.Log("difficulty -> hard (2)");
    }

    private void ApplyStartTypeVisuals(int startType)
    {
        if (tapToStartButton == null || countdownButton == null) return;

        bool isTap = (startType == 0);

        if (bringSelectedToFront)
        {
            if (isTap) tapToStartButton.transform.SetAsLastSibling();
            else countdownButton.transform.SetAsLastSibling();
        }

        tapToStartButton.interactable = !isTap;
        countdownButton.interactable = isTap;

        if (startTypeNormalSprite != null && startTypeSelectedSprite != null)
        {
            var tapImg = tapToStartButton.GetComponent<Image>();
            var cntImg = countdownButton.GetComponent<Image>();

            if (tapImg != null) tapImg.sprite = isTap ? startTypeSelectedSprite : startTypeNormalSprite;
            if (cntImg != null) cntImg.sprite = isTap ? startTypeNormalSprite : startTypeSelectedSprite;
        }
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