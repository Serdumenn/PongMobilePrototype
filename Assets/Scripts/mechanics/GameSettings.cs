using UnityEngine;

public static class GameSettings
{
    public const string KEY_DIFFICULTY = "difficulty";
    public const string KEY_DIFFNEED   = "diffNeed";
    public const string KEY_STARTTYPE  = "startType";

    public static int GetDifficulty(int defaultValue = 0)
        => GetIntAny(defaultValue, KEY_DIFFICULTY, "Difficulty", "difficultyIndex", "diff", "aiDifficulty");

    public static int GetDiffNeed(int defaultValue = 3)
        => GetIntAny(defaultValue, KEY_DIFFNEED, "DiffNeed", "diff_need", "scoreDiffNeed");

    public static int GetStartType(int defaultValue = 0)
        => GetIntAny(defaultValue, KEY_STARTTYPE, "StartType", "start_type", "roundStartType");

    public static void SetDifficulty(int value)
    {
        PlayerPrefs.SetInt(KEY_DIFFICULTY, Mathf.Clamp(value, 0, 2));
        PlayerPrefs.Save();
    }

    public static void SetDiffNeed(int value)
    {
        PlayerPrefs.SetInt(KEY_DIFFNEED, Mathf.Max(1, value));
        PlayerPrefs.Save();
    }

    public static void SetStartType(int value)
    {
        PlayerPrefs.SetInt(KEY_STARTTYPE, Mathf.Clamp(value, 0, 1));
        PlayerPrefs.Save();
    }

    private static int GetIntAny(int defaultValue, params string[] keys)
    {
        for (int i = 0; i < keys.Length; i++)
        {
            if (PlayerPrefs.HasKey(keys[i]))
                return PlayerPrefs.GetInt(keys[i], defaultValue);
        }
        return defaultValue;
    }
}