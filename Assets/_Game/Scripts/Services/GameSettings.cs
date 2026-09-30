using UnityEngine;

public static class GameSettings
{
    private const string HapticsKey = "HapticsEnabled";
    private const string HintRunsKey = "HintRunsShown";

    public static bool HapticsEnabled
    {
        get => PlayerPrefs.GetInt(HapticsKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(HapticsKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static int HintRunsShown
    {
        get => PlayerPrefs.GetInt(HintRunsKey, 0);
        set => PlayerPrefs.SetInt(HintRunsKey, value);
    }
}
