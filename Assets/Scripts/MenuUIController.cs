using UnityEngine;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SoloGameManager GameManager;

    [Header("Panels")]
    [SerializeField] private PanelTransition MenuPanel;
    [SerializeField] private PanelTransition SettingsPanel;

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

        if (MenuPanel != null) MenuPanel.ShowInstant();
        if (SettingsPanel != null) SettingsPanel.HideInstant();
    }

    private void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void Click()
    {
        if (ClickSfx != null) ClickSfx.Play();
    }

    private void OnPlayPressed()
    {
        Click();
        if (MenuPanel != null) MenuPanel.FadeOut();
        if (GameManager != null) GameManager.StartGameFromMenu();
    }

    private void OnSettingsPressed()
    {
        Click();
        if (MenuPanel != null) MenuPanel.SlideOut(PanelTransition.Direction.Left);
        if (SettingsPanel != null) SettingsPanel.SlideIn(PanelTransition.Direction.Right);
        if (GameManager != null) GameManager.HideGameObjects();
    }

    private void OnBackPressed()
    {
        Click();
        if (SettingsPanel != null) SettingsPanel.SlideOut(PanelTransition.Direction.Right);
        if (MenuPanel != null) MenuPanel.SlideIn(PanelTransition.Direction.Left);
        if (GameManager != null) GameManager.ShowGameObjects();
    }

    public void ShowMenu()
    {
        if (SettingsPanel != null) SettingsPanel.HideInstant();
        if (MenuPanel != null) MenuPanel.FadeIn();
    }

    public void ShowMenuInstant()
    {
        if (SettingsPanel != null) SettingsPanel.HideInstant();
        if (MenuPanel != null) MenuPanel.ShowInstant();
    }
}