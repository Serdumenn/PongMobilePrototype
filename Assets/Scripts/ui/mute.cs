using UnityEngine;

public class mute : MonoBehaviour
{
    public AudioSource sfx_source;
    public AudioSource ball_source;
    public AudioSource music_source;
    public bool isSoundOn;
    public bool isMusicOn;
    public saving prefs;
    public GameObject[] icons;

    void Start()
    {
        if (prefs != null)
        {
            isSoundOn = prefs.isSoundOn;
            isMusicOn = prefs.isMusicOn;
        }

        if (sfx_source) sfx_source.volume = isSoundOn ? 1f : 0f;
        if (music_source) music_source.volume = isMusicOn ? 1f : 0f;
        if (ball_source) ball_source.volume = isSoundOn ? 1f : 0f;

        UpdateIcons();
    }

    public void ToggleMusic()
    {
        isMusicOn = !isMusicOn;
        if (music_source) music_source.volume = isMusicOn ? 1f : 0f;
        if (prefs != null) { prefs.isMusicOn = isMusicOn; prefs.Save(); }
        UpdateIcons();
    }

    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        float v = isSoundOn ? 1f : 0f;
        if (sfx_source) sfx_source.volume = v;
        if (ball_source) ball_source.volume = v;
        if (prefs != null) { prefs.isSoundOn = isSoundOn; prefs.Save(); }
        UpdateIcons();
    }

    private void UpdateIcons()
    {
        if (icons == null || icons.Length < 4) return;
        if (icons[0]) icons[0].SetActive(isSoundOn);
        if (icons[1]) icons[1].SetActive(!isSoundOn);
        if (icons[2]) icons[2].SetActive(isMusicOn);
        if (icons[3]) icons[3].SetActive(!isMusicOn);
    }
}