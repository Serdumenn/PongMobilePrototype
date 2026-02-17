using UnityEngine;
using UnityEngine.UI;

public sealed class GameOverAdGate : MonoBehaviour
{
    [Header("Buttons (GameOverPanel)")]
    [SerializeField] private Button RetryButton;
    [SerializeField] private Button MenuButton;

    [Header("Refs")]
    [SerializeField] private SoloGameManager SoloGameManager;
    [SerializeField] private AdManager AdManager;

    private void Awake()
    {
        if (SoloGameManager == null) SoloGameManager = FindFirstObjectByType<SoloGameManager>();
        if (AdManager == null) AdManager = AdManager.Instance;

        Rebind(RetryButton, OnRetryClicked);
        Rebind(MenuButton, OnMenuClicked);
    }

    private void Rebind(Button Button, UnityEngine.Events.UnityAction Action)
    {
        if (Button == null) return;
        Button.onClick.RemoveAllListeners();
        Button.onClick.AddListener(Action);
    }

    private void OnRetryClicked()
    {
        SetButtons(false);

        (AdManager != null ? AdManager : AdManager.Instance)?.ShowInterstitialThen(() =>
        {
            if (SoloGameManager != null) SoloGameManager.RestartRun();
            SetButtons(true);
        });
    }

    private void OnMenuClicked()
    {
        SetButtons(false);

        (AdManager != null ? AdManager : AdManager.Instance)?.ShowInterstitialThen(() =>
        {
            if (SoloGameManager != null) SoloGameManager.ReturnToMenu();
            SetButtons(true);
        });
    }

    private void SetButtons(bool Interactable)
    {
        if (RetryButton != null) RetryButton.interactable = Interactable;
        if (MenuButton != null) MenuButton.interactable = Interactable;
    }
}