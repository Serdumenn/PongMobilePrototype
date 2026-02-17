using UnityEngine;

public class Saving : MonoBehaviour
{
    public bool isSoundOn;

    void Awake()
    {
        if (!PlayerPrefs.HasKey("isSoundOn")) PlayerPrefs.SetInt("isSoundOn", 1);
        PlayerPrefs.Save();
        Load();
    }

    public void Save()
    {
        PlayerPrefs.SetInt("isSoundOn", isSoundOn ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        isSoundOn = PlayerPrefs.GetInt("isSoundOn", 1) == 1;
    }
}