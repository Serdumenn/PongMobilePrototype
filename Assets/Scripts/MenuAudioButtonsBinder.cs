using UnityEngine;
using UnityEngine.UI;

public class MenuAudioButtonsBinder : MonoBehaviour
{
    [Header("References")]
    public mute muteController;

    [Header("Buttons")]
    public Button soundButton;
    public Button musicButton;

    [Header("Optional click SFX (plays BEFORE toggling so you still hear the click)")]
    public AudioSource clickSfx;

    [Header("Optional: trigger animator 'click' on the button object")]
    public bool playClickAnim = true;
    public string clickTriggerName = "click";

    void Awake()
    {
        if (muteController == null)
        {
            muteController = Object.FindAnyObjectByType<mute>();
            if (muteController == null)
                muteController = Object.FindFirstObjectByType<mute>();
        }
    }

    void Start()
    {
        if (muteController == null)
        {
            Debug.LogError("[MenuAudioButtonsBinder] muteController not found in scene.");
            return;
        }

        muteController.Refresh();

        if (soundButton) Rebind(soundButton, ToggleSound);
        if (musicButton) Rebind(musicButton, ToggleMusic);
    }

    void Rebind(Button btn, UnityEngine.Events.UnityAction action)
    {
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    void ToggleSound()
    {
        PlayFeedback(soundButton);
        muteController.ToggleSound();
    }

    void ToggleMusic()
    {
        PlayFeedback(musicButton);
        muteController.ToggleMusic();
    }

    void PlayFeedback(Button btn)
    {
        if (clickSfx) clickSfx.Play();

        if (!playClickAnim || btn == null) return;

        var anim = btn.GetComponent<Animator>();
        if (anim != null)
            anim.SetTrigger(clickTriggerName);
    }
}