using UnityEngine;

public static class HapticManager
{
    public static void Light()
    {
        Vibrate(30, 150);
    }

    public static void Soft()
    {
        Vibrate(20, 100);
    }

    public static void Medium()
    {
        Vibrate(50, 255);
    }

    private static void Vibrate(long milliseconds, int amplitude)
    {
        if (PlayerPrefs.GetInt(SettingsPanelController.HapticsKey, 1) == 0) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (vibrator == null) return;

                if (GetApiLevel() >= 26)
                {
                    using (var effect = new AndroidJavaClass("android.os.VibrationEffect"))
                    {
                        var vibrationEffect = effect.CallStatic<AndroidJavaObject>(
                            "createOneShot", milliseconds, amplitude);
                        vibrator.Call("vibrate", vibrationEffect);
                    }
                }
                else
                {
                    vibrator.Call("vibrate", milliseconds);
                }
            }
        }
        catch (System.Exception) { }
#endif
    }

    private static int GetApiLevel()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            return version.GetStatic<int>("SDK_INT");
        }
#else
        return 0;
#endif
    }
}

