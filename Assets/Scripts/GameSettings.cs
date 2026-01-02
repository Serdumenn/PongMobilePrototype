using UnityEngine;

public enum StartMode
{
    TapToLaunch = 0,
    AutoAfterCountdown = 1
}

public static class GameSettings
{
    // Menu keys (canonical)
    public const string DifficultyKey = "difficulty"; // int: 0/1/2
    public const string DiffNeedKey   = "diffNeed";   // int
    public const string StartModeKey  = "startType";  // int: 0/1

    // Legacy keys (fallback)
    private const string LegacyDifficultyKey = "game.difficultyError"; // float
    private const string LegacyDiffNeedKey   = "game.diffNeed";        // int
    private const string LegacyStartModeKey  = "game.startMode";       // int

    public const float DefaultDifficultyError = 0.30f;
    public const int DefaultDiffNeed = 3;
    public const StartMode DefaultStartMode = StartMode.TapToLaunch;

    private static bool loaded;
    private static int difficultyLevel;       // 0/1/2
    private static float difficultyError;     // mapped float
    private static int diffNeed;
    private static StartMode startMode;

    public static int DifficultyLevel
    {
        get { EnsureLoaded(); return difficultyLevel; }
    }

    public static float DifficultyError
    {
        get { EnsureLoaded(); return difficultyError; }
    }

    public static int DiffNeed
    {
        get { EnsureLoaded(); return diffNeed; }
    }

    public static StartMode CurrentStartMode
    {
        get { EnsureLoaded(); return startMode; }
    }

    public static void EnsureLoaded(
        float difficultyDefault = DefaultDifficultyError,
        int diffNeedDefault = DefaultDiffNeed,
        StartMode startModeDefault = DefaultStartMode)
    {
        if (loaded) return;

        // Difficulty: menu int -> mapped float error
        int d = PlayerPrefs.GetInt(DifficultyKey, -1);
        if (d >= 0)
        {
            difficultyLevel = Mathf.Clamp(d, 0, 2);
            difficultyError = MapDifficultyToError(difficultyLevel);
        }
        else
        {
            // Fallback: old float key (if exists)
            difficultyError = PlayerPrefs.GetFloat(LegacyDifficultyKey, difficultyDefault);
            difficultyLevel = MapErrorToDifficulty(difficultyError);
        }

        // diffNeed
        diffNeed = PlayerPrefs.GetInt(DiffNeedKey, -1);
        if (diffNeed < 0)
            diffNeed = PlayerPrefs.GetInt(LegacyDiffNeedKey, diffNeedDefault);
        diffNeed = Mathf.Max(1, diffNeed);

        // start mode
        int sm = PlayerPrefs.GetInt(StartModeKey, -1);
        if (sm >= 0)
            startMode = (StartMode)Mathf.Clamp(sm, 0, 1);
        else
            startMode = (StartMode)PlayerPrefs.GetInt(LegacyStartModeKey, (int)startModeDefault);

        loaded = true;
    }

    public static void ForceReload()
    {
        loaded = false;
        EnsureLoaded();
    }

    public static void SetStartMode(StartMode mode)
    {
        EnsureLoaded();
        startMode = mode;
        PlayerPrefs.SetInt(StartModeKey, (int)mode);
        // keep legacy in sync (optional but harmless)
        PlayerPrefs.SetInt(LegacyStartModeKey, (int)mode);
        PlayerPrefs.Save();
    }

    public static void SetDiffNeed(int value)
    {
        EnsureLoaded();
        diffNeed = Mathf.Max(1, value);
        PlayerPrefs.SetInt(DiffNeedKey, diffNeed);
        PlayerPrefs.SetInt(LegacyDiffNeedKey, diffNeed);
        PlayerPrefs.Save();
    }

    public static void SetDifficultyLevel(int level01or2)
    {
        EnsureLoaded();
        difficultyLevel = Mathf.Clamp(level01or2, 0, 2);
        difficultyError = MapDifficultyToError(difficultyLevel);
        PlayerPrefs.SetInt(DifficultyKey, difficultyLevel);
        PlayerPrefs.SetFloat(LegacyDifficultyKey, difficultyError);
        PlayerPrefs.Save();
    }

    private static float MapDifficultyToError(int level)
    {
        // Easy -> more error, Hard -> less error
        // Bu değerleri AI içinde de kullanabilirsin (aimError gibi)
        return level switch
        {
            0 => 0.40f, // easy
            1 => 0.25f, // medium
            2 => 0.15f, // hard
            _ => DefaultDifficultyError
        };
    }

    private static int MapErrorToDifficulty(float err)
    {
        // Rough inverse mapping
        if (err >= 0.33f) return 0;
        if (err >= 0.20f) return 1;
        return 2;
    }
}