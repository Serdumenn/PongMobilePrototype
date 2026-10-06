using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Exceptions;
using UnityEngine;

public sealed class OnlineScores : MonoBehaviour
{
    public const string ClassicBoard = "classic";
    public const string RushBoard = "rush";
    public const string DailyBoard = "daily";
    public const string CoopBoard = "coop";

    public readonly struct Row
    {
        public readonly int Rank;
        public readonly string Name;
        public readonly string Partner;
        public readonly int Score;
        public readonly bool IsMe;

        public Row(int rank, string name, string partner, int score, bool isMe)
        {
            Rank = rank;
            Name = name;
            Partner = partner;
            Score = score;
            IsMe = isMe;
        }
    }

    public enum Outcome
    {
        Ok,
        Offline,
        Missing,
        Failed
    }

    public sealed class Board
    {
        public Outcome Outcome;
        public readonly List<Row> Top = new List<Row>();
        public Row? Me;
    }

    [Serializable]
    private sealed class Meta
    {
        public string name;
        public string partner;
    }

    [SerializeField] private OnlineService online;
    [SerializeField] private int TopCount = 50;

    private bool synced;

    public int Top => TopCount;

    public static string BoardFor(GameModeDefinition mode)
    {
        if (mode == null) return null;
        return mode.Kind switch
        {
            GameModeKind.Classic => ClassicBoard,
            GameModeKind.Rush => RushBoard,
            _ => null
        };
    }

    private static Meta ParseMeta(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonUtility.FromJson<Meta>(json);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static string NameFrom(string metadata, string playerId)
    {
        var meta = ParseMeta(metadata);
        return meta != null && !string.IsNullOrEmpty(meta.name) ? meta.name : PlayerNames.ForId(playerId);
    }

    public static string PartnerFrom(string metadata)
    {
        return ParseMeta(metadata)?.partner;
    }

    public async Task<bool> SubmitAsync(string board, int score, string partner = null, bool joinIfNeeded = false)
    {
        if (string.IsNullOrEmpty(board) || score <= 0 || online == null) return false;

        try
        {
            if (!online.IsReady)
            {
                if (!joinIfNeeded && !await online.HasAccountAsync()) return false;
                await online.ConnectAsync();
                if (!online.IsReady) return false;
            }

            var meta = new Dictionary<string, string> { { "name", online.PlayerName } };
            if (!string.IsNullOrEmpty(partner)) meta["partner"] = partner;
            await LeaderboardsService.Instance.AddPlayerScoreAsync(board, score, new AddPlayerScoreOptions { Metadata = meta });
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Score upload to {board} failed: {e.Message}");
            return false;
        }
    }

    public async Task SyncBestsAsync(int classicBest, int rushBest)
    {
        if (synced) return;
        synced = true;
        if (classicBest > 0) await SubmitAsync(ClassicBoard, classicBest, null, true);
        if (rushBest > 0) await SubmitAsync(RushBoard, rushBest, null, true);
    }

    public async Task<Board> LoadAsync(string board)
    {
        var result = new Board();
        if (online == null || string.IsNullOrEmpty(board))
        {
            result.Outcome = Outcome.Failed;
            return result;
        }

        await online.ConnectAsync();
        if (!online.IsReady)
        {
            result.Outcome = OnlineService.HasInternet ? Outcome.Failed : Outcome.Offline;
            return result;
        }

        try
        {
            var page = await LeaderboardsService.Instance.GetScoresAsync(board, new GetScoresOptions { Limit = TopCount, IncludeMetadata = true });
            foreach (var entry in page.Results)
                result.Top.Add(new Row(entry.Rank + 1, NameFrom(entry.Metadata, entry.PlayerId), PartnerFrom(entry.Metadata), (int)entry.Score, entry.PlayerId == online.PlayerId));
        }
        catch (LeaderboardsException e) when (e.Reason == LeaderboardsExceptionReason.LeaderboardNotFound)
        {
            result.Outcome = Outcome.Missing;
            return result;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Loading {board} failed: {e.Message}");
            result.Outcome = OnlineService.HasInternet ? Outcome.Failed : Outcome.Offline;
            return result;
        }

        try
        {
            var mine = await LeaderboardsService.Instance.GetPlayerScoreAsync(board, new GetPlayerScoreOptions { IncludeMetadata = true });
            result.Me = new Row(mine.Rank + 1, NameFrom(mine.Metadata, mine.PlayerId), PartnerFrom(mine.Metadata), (int)mine.Score, true);
        }
        catch (Exception)
        {
            result.Me = null;
        }

        result.Outcome = Outcome.Ok;
        return result;
    }

    public async Task<Board> LoadFriendsAsync(string board, IReadOnlyList<string> friendIds)
    {
        var result = new Board();
        if (online == null || string.IsNullOrEmpty(board))
        {
            result.Outcome = Outcome.Failed;
            return result;
        }

        await online.ConnectAsync();
        if (!online.IsReady)
        {
            result.Outcome = OnlineService.HasInternet ? Outcome.Failed : Outcome.Offline;
            return result;
        }

        var ids = new List<string> { online.PlayerId };
        foreach (var id in friendIds)
            if (!string.IsNullOrEmpty(id) && !ids.Contains(id)) ids.Add(id);

        try
        {
            var scores = await LeaderboardsService.Instance.GetScoresByPlayerIdsAsync(board, ids, new GetScoresByPlayerIdsOptions { IncludeMetadata = true });
            var entries = new List<Unity.Services.Leaderboards.Models.LeaderboardEntry>(scores.Results);
            entries.Sort((a, b) => b.Score.CompareTo(a.Score));
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var row = new Row(i + 1, NameFrom(entry.Metadata, entry.PlayerId), PartnerFrom(entry.Metadata), (int)entry.Score, entry.PlayerId == online.PlayerId);
                result.Top.Add(row);
                if (row.IsMe) result.Me = row;
            }
        }
        catch (LeaderboardsException e) when (e.Reason == LeaderboardsExceptionReason.LeaderboardNotFound)
        {
            result.Outcome = Outcome.Missing;
            return result;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Loading friends on {board} failed: {e.Message}");
            result.Outcome = OnlineService.HasInternet ? Outcome.Failed : Outcome.Offline;
            return result;
        }

        result.Outcome = Outcome.Ok;
        return result;
    }

    public async Task<int> RankAsync(string board)
    {
        if (online == null || !online.IsReady) return 0;
        try
        {
            var mine = await LeaderboardsService.Instance.GetPlayerScoreAsync(board);
            return mine.Rank + 1;
        }
        catch (Exception)
        {
            return 0;
        }
    }
}
