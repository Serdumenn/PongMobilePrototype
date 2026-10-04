using UnityEngine;

public static class NativeShare
{
    public static void Copy(string text)
    {
        GUIUtility.systemCopyBuffer = text ?? string.Empty;
    }

    public static bool Text(string text, string title)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var intentClass = new AndroidJavaClass("android.content.Intent");
            using var intent = new AndroidJavaObject("android.content.Intent");
            intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
            intent.Call<AndroidJavaObject>("setType", "text/plain");
            intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);

            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, title);
            activity.Call("startActivity", chooser);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Share failed: {e.Message}");
        }
#endif
        Copy(text);
        return false;
    }
}
