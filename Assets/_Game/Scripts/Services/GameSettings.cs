using UnityEngine;

public static class GameSettings
{
    private const string HapticsKey = "HapticsEnabled";
    private const string SoundKey = "SoundEnabled";
    private const string HintRunsKey = "HintRunsShown";
    private const string OnlineLookKey = "OnlineLook";
    private const string MotionBackdropKey = "MotionBackdrop";
    private const string SwipeHintDoneKey = "SwipeHintDone";

    public static bool MotionBackdrop
    {
        get => PlayerPrefs.GetInt(MotionBackdropKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(MotionBackdropKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool SwipeHintDone
    {
        get => PlayerPrefs.GetInt(SwipeHintDoneKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(SwipeHintDoneKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool HapticsEnabled
    {
        get => PlayerPrefs.GetInt(HapticsKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(HapticsKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool SoundEnabled
    {
        get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static int HintRunsShown
    {
        get => PlayerPrefs.GetInt(HintRunsKey, 0);
        set => PlayerPrefs.SetInt(HintRunsKey, value);
    }

    public static string OnlineLook
    {
        get => PlayerPrefs.GetString(OnlineLookKey, string.Empty);
        set
        {
            PlayerPrefs.SetString(OnlineLookKey, value ?? string.Empty);
            PlayerPrefs.Save();
        }
    }
}
