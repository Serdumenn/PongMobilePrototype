using UnityEngine;

public class saving : MonoBehaviour
{
    public bool isSoundOn;
    public bool isMusicOn;

    void Awake()
    {
        if (!PlayerPrefs.HasKey("isSoundOn")) PlayerPrefs.SetInt("isSoundOn", 1);
        if (!PlayerPrefs.HasKey("isMusicOn")) PlayerPrefs.SetInt("isMusicOn", 1);
        PlayerPrefs.Save();
        Load();
    }

    public void Save()
    {
        PlayerPrefs.SetInt("isSoundOn", isSoundOn ? 1 : 0);
        PlayerPrefs.SetInt("isMusicOn", isMusicOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        isSoundOn = PlayerPrefs.GetInt("isSoundOn", 1) == 1;
        isMusicOn = PlayerPrefs.GetInt("isMusicOn", 1) == 1;
    }
}