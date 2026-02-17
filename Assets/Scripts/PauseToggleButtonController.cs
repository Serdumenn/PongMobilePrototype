using UnityEngine;
using UnityEngine.UI;

public sealed class PauseToggleButtonController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button PauseToggleButton;
    [SerializeField] private Image PauseToggleIcon;
    [SerializeField] private Sprite PauseSprite;
    [SerializeField] private Sprite PlaySprite;

    [Header("Refs")]
    [SerializeField] private SoloGameManager SoloGameManager;

    private bool IsPaused;

    private void Awake()
    {
        if (SoloGameManager == null) SoloGameManager = FindFirstObjectByType<SoloGameManager>();

        if (PauseToggleButton != null)
        {
            PauseToggleButton.onClick.RemoveAllListeners();
            PauseToggleButton.onClick.AddListener(TogglePause);
        }

        SetPaused(false);
    }

    public void TogglePause()
    {
        if (SoloGameManager != null && SoloGameManager.IsGameOver) return;

        SetPaused(!IsPaused);
    }

    public void SetPaused(bool ShouldPause)
    {
        IsPaused = ShouldPause;

        Time.timeScale = IsPaused ? 0f : 1f;

        if (PauseToggleIcon != null)
            PauseToggleIcon.sprite = IsPaused ? PlaySprite : PauseSprite;
    }
}