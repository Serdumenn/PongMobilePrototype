using UnityEngine;

public class saving : MonoBehaviour {
    public bool isSoundOn;
    public bool isMusicOn;
    //public int difficulty = 0;
    //public int gametypevalue;
    void Awake() {

        if (!PlayerPrefs.HasKey("isSoundOn")) {
            PlayerPrefs.SetInt("isSoundOn", 1);  // Default sound ON
            PlayerPrefs.Save();
        }
        if (!PlayerPrefs.HasKey("isMusicOn")) {
            PlayerPrefs.SetInt("isMusicOn", 1);  // Default music ON
            PlayerPrefs.Save();
        }
        /*if (!PlayerPrefs.HasKey("difficulty")) {
            PlayerPrefs.SetInt("difficulty", 1);  // Default to medium difficulty
            PlayerPrefs.Save();
        }*/

        Load();
    }

    public void Save() {
        PlayerPrefs.SetInt("isSoundOn", isSoundOn ? 1 : 0);
        PlayerPrefs.SetInt("isMusicOn", isMusicOn ? 1 : 0);
        //PlayerPrefs.SetInt("difficulty", difficulty);
        PlayerPrefs.Save();
    }

    public void Load() {
        isSoundOn = PlayerPrefs.GetInt("isSoundOn", 1) == 1;
        isMusicOn = PlayerPrefs.GetInt("isMusicOn", 1) == 1;
        //difficulty = PlayerPrefs.GetInt("difficulty", 0);
        //gametypevalue = PlayerPrefs.GetInt("gametype");
    }
}
