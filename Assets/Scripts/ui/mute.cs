using UnityEngine;

public class mute : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource sfx_source;
    public AudioSource ball_source;
    public AudioSource music_source;

    [Header("State")]
    public bool isSoundOn;
    public bool isMusicOn;

    [Header("Prefs (saving)")]
    public saving prefs;

    [Header("Icons: 0=soundOn, 1=soundOff, 2=musicOn, 3=musicOff")]
    public GameObject[] icons;

    void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (PersistentMusic.Instance != null && PersistentMusic.Instance.MusicSource != null)
            music_source = PersistentMusic.Instance.MusicSource;

        if (prefs != null)
        {
            isSoundOn = prefs.isSoundOn;
            isMusicOn = prefs.isMusicOn;
        }
        else
        {
            isSoundOn = PlayerPrefs.GetInt("isSoundOn", 1) == 1;
            isMusicOn = PlayerPrefs.GetInt("isMusicOn", 1) == 1;
        }

        ApplyVolumes();
        UpdateIcons();
    }

    public void ToggleMusic()
    {
        if (PersistentMusic.Instance != null && PersistentMusic.Instance.MusicSource != null)
            music_source = PersistentMusic.Instance.MusicSource;

        isMusicOn = !isMusicOn;

        if (music_source) music_source.volume = isMusicOn ? 1f : 0f;

        SavePrefs();
        UpdateIcons();
    }

    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;

        float v = isSoundOn ? 1f : 0f;
        if (sfx_source) sfx_source.volume = v;
        if (ball_source) ball_source.volume = v;

        SavePrefs();
        UpdateIcons();
    }

    void ApplyVolumes()
    {
        float sfxV = isSoundOn ? 1f : 0f;
        float musicV = isMusicOn ? 1f : 0f;

        if (sfx_source) sfx_source.volume = sfxV;
        if (ball_source) ball_source.volume = sfxV;

        if (PersistentMusic.Instance != null && PersistentMusic.Instance.MusicSource != null)
            music_source = PersistentMusic.Instance.MusicSource;

        if (music_source) music_source.volume = musicV;
    }

    void SavePrefs()
    {
        if (prefs != null)
        {
            prefs.isSoundOn = isSoundOn;
            prefs.isMusicOn = isMusicOn;
            prefs.Save();
        }
        else
        {
            PlayerPrefs.SetInt("isSoundOn", isSoundOn ? 1 : 0);
            PlayerPrefs.SetInt("isMusicOn", isMusicOn ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    void UpdateIcons()
    {
        if (icons == null || icons.Length < 4) return;

        if (icons[0]) icons[0].SetActive(isSoundOn);
        if (icons[1]) icons[1].SetActive(!isSoundOn);

        if (icons[2]) icons[2].SetActive(isMusicOn);
        if (icons[3]) icons[3].SetActive(!isMusicOn);
    }
}