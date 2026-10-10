using System;
using UnityEngine;

public static class InAppReview
{
    public static event Action Requested;

    public static void Request()
    {
        Requested?.Invoke();
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() => Launch(activity)));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Review request failed: {e.Message}");
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void Launch(AndroidJavaObject activity)
    {
        try
        {
            using var factory = new AndroidJavaClass("com.google.android.play.core.review.ReviewManagerFactory");
            var manager = factory.CallStatic<AndroidJavaObject>("create", activity);
            var request = manager.Call<AndroidJavaObject>("requestReviewFlow");
            request.Call<AndroidJavaObject>("addOnCompleteListener", new CompleteListener(task =>
            {
                if (!task.Call<bool>("isSuccessful")) return;
                var info = task.Call<AndroidJavaObject>("getResult");
                manager.Call<AndroidJavaObject>("launchReviewFlow", activity, info);
            }));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Review flow failed: {e.Message}");
        }
    }

    private sealed class CompleteListener : AndroidJavaProxy
    {
        private readonly Action<AndroidJavaObject> done;

        public CompleteListener(Action<AndroidJavaObject> done) : base("com.google.android.gms.tasks.OnCompleteListener")
        {
            this.done = done;
        }

        public void onComplete(AndroidJavaObject task)
        {
            try
            {
                done(task);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Review flow failed: {e.Message}");
            }
        }
    }
#endif
}
