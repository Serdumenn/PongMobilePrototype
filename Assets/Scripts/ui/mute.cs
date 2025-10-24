using UnityEngine;

public class mute : MonoBehaviour {
    public AudioSource sfx_source;
    public AudioSource ball_source;
    public AudioSource music_source;
    public bool isSoundOn;
    public bool isMusicOn;
    public saving prefs;
    public GameObject[] icons;
    void Start() 
    {
        isSoundOn = prefs.isSoundOn;
        isMusicOn = prefs.isMusicOn;

        sfx_source.volume = isSoundOn ? 1f : 0f;
        music_source.volume = isMusicOn ? 1f : 0f;
        
        UpdateIcons();
    }

    public void ToggleMusic() {
        isMusicOn = !isMusicOn;
        music_source.volume = isMusicOn ? 1f : 0f;
        prefs.isMusicOn = isMusicOn;
        prefs.Save();
        UpdateIcons();
    }

    public void ToggleSound() {
        isSoundOn = !isSoundOn;
        sfx_source.volume = isSoundOn ? 1f : 0f;
        if (ball_source != null)
        {
            ball_source.volume = sfx_source.volume;
        }
        prefs.isSoundOn = isSoundOn;
        prefs.Save();
        UpdateIcons();
    }
    private void UpdateIcons() {
        icons[0].SetActive(isSoundOn);
        icons[1].SetActive(!isSoundOn);

        icons[2].SetActive(isMusicOn);
        icons[3].SetActive(!isMusicOn);
    }
}