using UnityEngine;

public static class GameSettings
{
    public const string DifficultyKey = "difficulty";

    private const string DeprecatedStartTypeKey = "startType";
    private const string DeprecatedDiffNeedKey  = "diffNeed";

    private static bool loaded;
    private static int difficultyLevel;

    public static int DifficultyLevel
    {
        get { EnsureLoaded(); return difficultyLevel; }
        set
        {
            difficultyLevel = Mathf.Clamp(value, 0, 2);
            PlayerPrefs.SetInt(DifficultyKey, difficultyLevel);
            PlayerPrefs.Save();
            loaded = true;
        }
    }

    public static void EnsureLoaded(int difficultyDefault = 0)
    {
        if (loaded) return;

        CleanupDeprecatedKeys();

        difficultyLevel = PlayerPrefs.GetInt(DifficultyKey, Mathf.Clamp(difficultyDefault, 0, 2));
        loaded = true;
    }

    public static void ForceReload()
    {
        loaded = false;
        EnsureLoaded();
    }

    public static void CleanupDeprecatedKeys()
    {
        if (PlayerPrefs.HasKey(DeprecatedStartTypeKey)) PlayerPrefs.DeleteKey(DeprecatedStartTypeKey);
        if (PlayerPrefs.HasKey(DeprecatedDiffNeedKey))  PlayerPrefs.DeleteKey(DeprecatedDiffNeedKey);
    }
}