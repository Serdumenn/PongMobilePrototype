using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public sealed class OnlineService : MonoBehaviour
{
    public enum Status
    {
        Offline,
        Connecting,
        Ready,
        Failed
    }

    public Status State { get; private set; } = Status.Offline;
    public string PlayerId { get; private set; }
    public string PlayerName { get; private set; }
    public bool IsReady => State == Status.Ready;

    public event Action<Status> StateChanged;

    private Task connecting;

    public static bool HasInternet => Application.internetReachability != NetworkReachability.NotReachable;

    public Task ConnectAsync()
    {
        if (State == Status.Ready && AuthenticationService.Instance.IsSignedIn) return Task.CompletedTask;
        if (connecting != null && !connecting.IsCompleted) return connecting;
        connecting = Connect();
        return connecting;
    }

    public async Task<bool> HasAccountAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync(Options());
            var auth = AuthenticationService.Instance;
            return auth.IsSignedIn || auth.SessionTokenExists;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Online services could not start: {e.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteDataAsync()
    {
        if (!await HasAccountAsync()) return false;

        await ConnectAsync();
        if (!IsReady) throw new InvalidOperationException("Not signed in");

        await AuthenticationService.Instance.DeleteAccountAsync();
        AuthenticationService.Instance.ClearSessionToken();
        PlayerId = null;
        PlayerName = null;
        SetState(Status.Offline);
        return true;
    }

    private async Task Connect()
    {
        if (!HasInternet)
        {
            SetState(Status.Offline);
            return;
        }

        SetState(Status.Connecting);
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync(Options());

            var auth = AuthenticationService.Instance;
            auth.Expired -= OnExpired;
            auth.Expired += OnExpired;
            if (!auth.IsSignedIn) await auth.SignInAnonymouslyAsync();

            PlayerId = auth.PlayerId;
            PlayerName = PlayerNames.ForId(PlayerId);
            SetState(Status.Ready);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Online sign-in failed: {e.Message}");
            SetState(HasInternet ? Status.Failed : Status.Offline);
        }
    }

    private static InitializationOptions Options()
    {
        var options = new InitializationOptions();
#if UNITY_EDITOR
        options.SetProfile(PlayerNames.ProfileFor(Application.dataPath));
#endif
        return options;
    }

    private void OnExpired()
    {
        SetState(Status.Offline);
    }

    private void SetState(Status state)
    {
        if (State == state) return;
        State = state;
        StateChanged?.Invoke(state);
    }

    private void OnDestroy()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized)
            AuthenticationService.Instance.Expired -= OnExpired;
    }
}
