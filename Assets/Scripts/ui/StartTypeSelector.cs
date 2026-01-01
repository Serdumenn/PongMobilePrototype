using UnityEngine;
using UnityEngine.UI;

public class StartTypeSelector : MonoBehaviour
{
    [Header("Buttons")]
    public Button tapToStartButton;
    public Button countdownButton;

    [Header("Optional: Images to swap sprites (leave empty = auto use button image)")]
    public Image tapToStartImage;
    public Image countdownImage;

    [Header("Sprites (optional)")]
    public Sprite normalSprite;
    public Sprite selectedSprite;

    [Header("Bring selected to front")]
    public bool bringSelectedToFront = true;

    [Header("Prefs")]
    public string prefsKey = "startType";
    public int defaultValue = 0;

    [Header("Optional: call mainMenu for sound/logging (not required)")]
    public mainMenu menu;

    void Awake()
    {
        AutoBindImagesIfNeeded();
        ApplyFromPrefs(force: true);
    }

    void OnEnable()
    {
        AutoBindImagesIfNeeded();
        ApplyFromPrefs(force: true);
    }

    void AutoBindImagesIfNeeded()
    {
        if (tapToStartButton != null && tapToStartImage == null)
            tapToStartImage = tapToStartButton.GetComponent<Image>();

        if (countdownButton != null && countdownImage == null)
            countdownImage = countdownButton.GetComponent<Image>();
    }

    public void SelectTapToStart()
    {
        if (menu != null) menu.playSound(0);

        PlayerPrefs.SetInt(prefsKey, 0);
        PlayerPrefs.Save();

        ApplyVisuals(selectedIsTap: true);
        Debug.Log("startType -> tap (0)");
    }

    public void SelectCountdown()
    {
        if (menu != null) menu.playSound(0);

        PlayerPrefs.SetInt(prefsKey, 1);
        PlayerPrefs.Save();

        ApplyVisuals(selectedIsTap: false);
        Debug.Log("startType -> countdown (1)");
    }

    void ApplyFromPrefs(bool force)
    {
        int v = PlayerPrefs.GetInt(prefsKey, defaultValue);
        ApplyVisuals(selectedIsTap: (v == 0));
    }

    void ApplyVisuals(bool selectedIsTap)
    {
        if (tapToStartButton == null || countdownButton == null) return;

        if (bringSelectedToFront)
        {
            if (selectedIsTap) tapToStartButton.transform.SetAsLastSibling();
            else countdownButton.transform.SetAsLastSibling();
        }

        if (selectedSprite != null && normalSprite != null)
        {
            if (tapToStartImage != null)
                tapToStartImage.sprite = selectedIsTap ? selectedSprite : normalSprite;

            if (countdownImage != null)
                countdownImage.sprite = selectedIsTap ? normalSprite : selectedSprite;
        }

        tapToStartButton.interactable = !selectedIsTap;
        countdownButton.interactable = selectedIsTap;
    }
}