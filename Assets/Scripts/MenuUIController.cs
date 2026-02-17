using UnityEngine;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SoloGameManager GameManager;

    [Header("Roots")]
    [SerializeField] private GameObject MenuPanel;
    [SerializeField] private GameObject SettingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button PlayButton;
    [SerializeField] private Button SettingsButton;
    [SerializeField] private Button BackButton;

    [Header("Optional")]
    [SerializeField] private AudioSource ClickSfx;

    private void Awake()
    {
        if (GameManager == null) GameManager = FindFirstObjectByType<SoloGameManager>();

        Bind(PlayButton, OnPlayPressed);
        Bind(SettingsButton, OnSettingsPressed);
        Bind(BackButton, OnBackPressed);
    }

    private void Start()
    {
        Time.timeScale = 1f;
        ShowMenu();
    }

    private void Bind(Button Button, UnityEngine.Events.UnityAction Action)
    {
        if (Button == null) return;
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(Action);
    }

    private void Click()
    {
        if (ClickSfx != null) ClickSfx.Play();
    }

    private void OnPlayPressed()
    {
        Click();
        if (GameManager != null) GameManager.StartGameFromMenu();
    }

    private void OnSettingsPressed()
    {
        Click();
        ShowSettings();
    }

    private void OnBackPressed()
    {
        Click();
        ShowMenu();
    }

    public void ShowMenu()
    {
        if (MenuPanel != null) MenuPanel.SetActive(true);
        if (SettingsPanel != null) SettingsPanel.SetActive(false);
    }

    public void ShowSettings()
    {
        if (MenuPanel != null) MenuPanel.SetActive(false);
        if (SettingsPanel != null) SettingsPanel.SetActive(true);
    }
}