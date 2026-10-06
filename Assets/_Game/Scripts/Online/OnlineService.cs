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
    public bool PlayGamesLinked { get; private set; }

    public event Action<Status> StateChanged;
    public event Action AccountChanged;

    private Task connecting;

    public static bool HasInternet => Application.internetReachability != NetworkReachability.NotReachable;
    public static bool Disabled { get; set; }

    private async void Start()
    {
        if (Disabled) return;

        bool playGames = PlayGamesAccount.Supported && await PlayGamesAccount.SignInAsync(false);
        if (Disabled) return;
        if (playGames || await HasAccountAsync()) await ConnectAsync();
    }

    public async Task<bool> SignInWithPlayGamesAsync()
    {
        if (Disabled || !PlayGamesAccount.Supported) return false;
        if (!await PlayGamesAccount.SignInAsync(true)) return false;

        if (IsReady) await LinkPlayGamesAsync();
        else await ConnectAsync();
        return PlayGamesLinked;
    }

    public Task ConnectAsync()
    {
        if (State == Status.Ready && AuthenticationService.Instance.IsSignedIn) return Task.CompletedTask;
        if (connecting != null && !connecting.IsCompleted) return connecting;
        connecting = Connect();
        return connecting;
    }

    public async Task<bool> HasAccountAsync()
    {
        if (Disabled) return false;
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
        PlayGamesLinked = false;
        SetState(Status.Offline);
        AccountChanged?.Invoke();
        return true;
    }

    private async Task Connect()
    {
        if (Disabled || !HasInternet)
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
            if (!auth.IsSignedIn && !auth.SessionTokenExists) await TrySignInWithPlayGamesAsync();
            if (!auth.IsSignedIn) await auth.SignInAnonymouslyAsync();

            PlayerId = auth.PlayerId;
            PlayerName = PlayerNames.ForId(PlayerId);
            SetState(Status.Ready);
            _ = LinkPlayGamesAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Online sign-in failed: {e.Message}");
            SetState(HasInternet ? Status.Failed : Status.Offline);
        }
    }

    private static async Task TrySignInWithPlayGamesAsync()
    {
        string code = await PlayGamesAccount.ServerCodeAsync();
        if (code == null) return;
        try
        {
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(code);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Play Games account sign-in failed: {e.Message}");
        }
    }

    private async Task LinkPlayGamesAsync()
    {
        var auth = AuthenticationService.Instance;
        try
        {
            var info = await auth.GetPlayerInfoAsync();
            PlayGamesLinked = HasPlayGames(info);
            if (PlayGamesLinked || !PlayGamesAccount.SignedIn) return;

            string code = await PlayGamesAccount.ServerCodeAsync();
            if (code == null) return;

            try
            {
                await auth.LinkWithGooglePlayGamesAsync(code);
                PlayGamesLinked = true;
            }
            catch (AuthenticationException e) when (e.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
            {
                await SwitchToPlayGamesAccountAsync();
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Linking Play Games failed: {e.Message}");
        }
        finally
        {
            AccountChanged?.Invoke();
        }
    }

    private async Task SwitchToPlayGamesAccountAsync()
    {
        string code = await PlayGamesAccount.ServerCodeAsync();
        if (code == null) return;

        var auth = AuthenticationService.Instance;
        auth.SignOut(true);
        SetState(Status.Offline);
        SetState(Status.Connecting);
        try
        {
            await auth.SignInWithGooglePlayGamesAsync(code);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Switching to the Play Games account failed: {e.Message}");
            await auth.SignInAnonymouslyAsync();
        }

        PlayerId = auth.PlayerId;
        PlayerName = PlayerNames.ForId(PlayerId);
        PlayGamesLinked = HasPlayGames(await auth.GetPlayerInfoAsync());
        SetState(Status.Ready);
    }

    public static bool HasPlayGames(PlayerInfo info)
    {
        if (info?.Identities == null) return false;
        foreach (var identity in info.Identities)
            if (identity != null && identity.TypeId == PlayGamesProvider) return true;
        return false;
    }

    private const string PlayGamesProvider = "google-play-games";

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
