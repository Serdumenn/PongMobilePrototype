using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.CloudCode;
using UnityEngine;

public sealed class OnlineGhosts : MonoBehaviour
{
    public const string ShareScript = "ghost_share";
    public const string LoadScript = "ghost_get";

    public enum Outcome
    {
        Ok,
        Offline,
        NotFound,
        Expired,
        Rejected,
        Failed
    }

    public readonly struct Shared
    {
        public readonly Outcome Outcome;
        public readonly string Code;
        public readonly int Days;

        public Shared(Outcome outcome, string code = null, int days = 0)
        {
            Outcome = outcome;
            Code = code;
            Days = days;
        }
    }

    public readonly struct Loaded
    {
        public readonly Outcome Outcome;
        public readonly GhostRun Run;
        public readonly string Name;

        public Loaded(Outcome outcome, GhostRun run = null, string name = null)
        {
            Outcome = outcome;
            Run = run;
            Name = name;
        }
    }

    [Serializable]
    private sealed class ShareReply
    {
        public bool ok;
        public string code;
        public string reason;
        public int days;
    }

    [Serializable]
    private sealed class LoadReply
    {
        public bool found;
        public string reason;
        public string run;
        public string name;
    }

    [SerializeField] private OnlineService online;

    public bool Busy { get; private set; }

    private void Awake()
    {
        if (online == null) online = FindFirstObjectByType<OnlineService>();
    }

    public async Task<Shared> ShareAsync(GhostRun run)
    {
        if (run == null || online == null || Busy) return new Shared(Outcome.Failed);

        var bytes = run.ToBytes();
        var packed = GhostRun.FromBytes(bytes);
        if (packed == null || packed.Problem() != null) return new Shared(Outcome.Rejected);

        Busy = true;
        try
        {
            if (!await ConnectAsync()) return new Shared(Unreachable());

            var args = new Dictionary<string, object>
            {
                { "run", Convert.ToBase64String(bytes) },
                { "name", online.PlayerName ?? string.Empty }
            };
            string json = await CloudCodeService.Instance.CallEndpointAsync(ShareScript, args);
            var reply = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<ShareReply>(json);
            if (reply == null) return new Shared(Outcome.Failed);
            if (!reply.ok)
            {
                Debug.LogWarning($"Ghost share refused: {reply.reason}");
                return new Shared(Outcome.Rejected);
            }

            return GhostCode.IsValid(reply.code)
                ? new Shared(Outcome.Ok, reply.code, reply.days > 0 ? reply.days : GhostCode.ValidDays)
                : new Shared(Outcome.Failed);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Ghost share failed: {e.Message}");
            return new Shared(FailureFrom(e));
        }
        finally
        {
            Busy = false;
        }
    }

    public async Task<Loaded> LoadAsync(string code)
    {
        code = GhostCode.Normalize(code);
        if (!GhostCode.IsValid(code)) return new Loaded(Outcome.NotFound);
        if (online == null || Busy) return new Loaded(Outcome.Failed);

        Busy = true;
        try
        {
            if (!await ConnectAsync()) return new Loaded(Unreachable());

            string json = await CloudCodeService.Instance.CallEndpointAsync(LoadScript, new Dictionary<string, object> { { "code", code } });
            var reply = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<LoadReply>(json);
            if (reply == null) return new Loaded(Outcome.Failed);
            if (!reply.found) return new Loaded(reply.reason == "expired" ? Outcome.Expired : Outcome.NotFound);

            var run = GhostRun.FromBase64(reply.run);
            if (run == null || run.Problem() != null) return new Loaded(Outcome.Failed);
            return new Loaded(Outcome.Ok, run, string.IsNullOrEmpty(reply.name) ? Loc.T("A friend") : reply.name);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Ghost load failed: {e.Message}");
            return new Loaded(FailureFrom(e));
        }
        finally
        {
            Busy = false;
        }
    }

    private async Task<bool> ConnectAsync()
    {
        if (!online.IsReady) await online.ConnectAsync();
        return online.IsReady;
    }

    private static Outcome Unreachable()
    {
        return OnlineService.HasInternet ? Outcome.Failed : Outcome.Offline;
    }

    private static Outcome FailureFrom(Exception e)
    {
        if (e is CloudCodeException cloud && cloud.Reason == CloudCodeExceptionReason.NoInternetConnection) return Outcome.Offline;
        return Unreachable();
    }
}
