using UnityEngine;

public enum StartMode
{
    TapToLaunch = 0,
    AutoAfterCountdown = 1
}

public static class GameSettings
{
    public const string DifficultyKey = "game.difficultyError";
    public const string DiffNeedKey   = "game.diffNeed";
    public const string StartModeKey  = "game.startMode";

    public const float DefaultDifficultyError = 0.30f;
    public const int   DefaultDiffNeed        = 3;
    public const StartMode DefaultStartMode   = StartMode.TapToLaunch;

    private static bool loaded;
    private static float difficultyError;
    private static int diffNeed;
    private static StartMode startMode;

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

        difficultyError = PlayerPrefs.GetFloat(DifficultyKey, difficultyDefault);
        diffNeed = PlayerPrefs.GetInt(DiffNeedKey, diffNeedDefault);
        startMode = (StartMode)PlayerPrefs.GetInt(StartModeKey, (int)startModeDefault);

        loaded = true;
    }

    public static void SetDifficultyError(float value)
    {
        EnsureLoaded();
        difficultyError = Mathf.Max(0f, value);
        PlayerPrefs.SetFloat(DifficultyKey, difficultyError);
        PlayerPrefs.Save();
    }

    public static void SetDiffNeed(int value)
    {
        EnsureLoaded();
        diffNeed = Mathf.Max(1, value);
        PlayerPrefs.SetInt(DiffNeedKey, diffNeed);
        PlayerPrefs.Save();
    }

    public static void SetStartMode(StartMode mode)
    {
        EnsureLoaded();
        startMode = mode;
        PlayerPrefs.SetInt(StartModeKey, (int)startMode);
        PlayerPrefs.Save();
    }

    public static void ForceReload()
    {
        loaded = false;
        EnsureLoaded();
    }
}