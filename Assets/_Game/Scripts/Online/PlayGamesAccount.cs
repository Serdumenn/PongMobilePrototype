using System;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public static class PlayGamesAccount
{
    private const int CodeTimeoutMs = 15000;

#if UNITY_ANDROID && !UNITY_EDITOR
    private static bool activated;
#endif

    public static bool Supported
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var settings = PlayGamesSettings.LoadInstance();
            return settings != null && !string.IsNullOrEmpty(settings.AppId);
#else
            return false;
#endif
        }
    }

    public static bool SignedIn
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return activated && PlayGamesPlatform.Instance.IsAuthenticated();
#else
            return false;
#endif
        }
    }

    public static string DisplayName
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return SignedIn ? PlayGamesPlatform.Instance.GetUserDisplayName() : null;
#else
            return null;
#endif
        }
    }

    public static event Action Changed;

    public static Task<bool> SignInAsync(bool interactive)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Supported) return Task.FromResult(false);
        try
        {
            if (!activated)
            {
                PlayGamesPlatform.Activate();
                activated = true;
            }

            if (PlayGamesPlatform.Instance.IsAuthenticated()) return Task.FromResult(true);

            var done = new TaskCompletionSource<bool>();
            Action<SignInStatus> callback = status =>
            {
                if (status != SignInStatus.Success) Debug.Log($"Play Games sign-in: {status}");
                done.TrySetResult(status == SignInStatus.Success);
                Changed?.Invoke();
            };
            if (interactive) PlayGamesPlatform.Instance.ManuallyAuthenticate(callback);
            else PlayGamesPlatform.Instance.Authenticate(callback);
            return done.Task;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Play Games could not start: {e.Message}");
            return Task.FromResult(false);
        }
#else
        return Task.FromResult(false);
#endif
    }

    public static async Task<string> ServerCodeAsync()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!SignedIn) return null;
        var done = new TaskCompletionSource<string>();
        try
        {
            PlayGamesPlatform.Instance.RequestServerSideAccess(false, code => done.TrySetResult(code));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Play Games server access failed: {e.Message}");
            return null;
        }

        var finished = await Task.WhenAny(done.Task, Task.Delay(CodeTimeoutMs));
        if (finished != done.Task) return null;
        string result = done.Task.Result;
        return string.IsNullOrEmpty(result) ? null : result;
#else
        await Task.CompletedTask;
        return null;
#endif
    }
}
