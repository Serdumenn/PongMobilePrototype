using System.Collections.Generic;

public readonly struct LobbyPlayer
{
    public readonly string Id;
    public readonly string Name;
    public readonly string Look;
    public readonly bool Ready;
    public readonly bool IsHost;
    public readonly bool IsYou;

    public LobbyPlayer(string id, string name, string look, bool ready, bool isHost, bool isYou)
    {
        Id = id;
        Name = name;
        Look = look;
        Ready = ready;
        IsHost = isHost;
        IsYou = isYou;
    }

    public static bool AllReady(IReadOnlyList<LobbyPlayer> players, int needed)
    {
        if (players == null || needed < 2 || players.Count < needed) return false;

        foreach (var player in players)
            if (!player.Ready) return false;
        return true;
    }

    public static void Order(List<LobbyPlayer> players)
    {
        players.Sort((a, b) =>
        {
            if (a.IsYou != b.IsYou) return a.IsYou ? -1 : 1;
            if (a.IsHost != b.IsHost) return a.IsHost ? -1 : 1;
            return string.CompareOrdinal(a.Id, b.Id);
        });
    }
}
