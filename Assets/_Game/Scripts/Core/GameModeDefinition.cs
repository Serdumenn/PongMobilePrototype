using UnityEngine;

public enum GameModeKind
{
    Classic,
    Rush,
    TableDuel,
    CoopRally,
    PartyTable,
    PortalDuel,
    RushBattle,
    LiveDuel
}

public enum FieldTopology
{
    Solo,
    TopBottom,
    FourSides,
    Portal
}

[CreateAssetMenu(menuName = "Pingi/Game Mode", fileName = "mode")]
public sealed class GameModeDefinition : ScriptableObject
{
    [field: Header("Identity")]
    [field: SerializeField] public string Id { get; private set; }
    [field: SerializeField] public GameModeKind Kind { get; private set; }
    [field: SerializeField] public string DisplayName { get; private set; }
    [field: SerializeField] public string Tagline { get; private set; }
    [field: SerializeField] public string BestScoreKey { get; private set; }
    [field: SerializeField] public string LeaderboardId { get; private set; }

    [field: Header("Unlock")]
    [field: SerializeField] public int RequiredClassicBest { get; private set; }

    [field: Header("Timed")]
    [field: SerializeField] public float DurationSeconds { get; private set; } = 60f;
    [field: SerializeField] public float MissPenaltySeconds { get; private set; } = 5f;
    [field: SerializeField] public float RespawnDelaySeconds { get; private set; } = 1f;

    [field: Header("Scoring")]
    [field: SerializeField, Range(0f, 1f)] public float PerfectZone { get; private set; } = 0.25f;
    [field: SerializeField] public int PerfectPoints { get; private set; } = 2;
    [field: SerializeField] public int ComboPoints { get; private set; } = 3;
    [field: SerializeField] public int ComboThreshold { get; private set; } = 3;

    [field: Header("Players")]
    [field: SerializeField] public FieldTopology Topology { get; private set; } = FieldTopology.Solo;
    [field: SerializeField] public int MinPlayers { get; private set; } = 1;
    [field: SerializeField] public int MaxPlayers { get; private set; } = 1;
    [field: SerializeField] public bool TabletOnly { get; private set; }

    [field: Header("Online")]
    [field: SerializeField] public bool Online { get; private set; }
    [field: SerializeField] public bool ComingSoon { get; private set; }

    [field: Header("Match")]
    [field: SerializeField] public int PointsToWin { get; private set; } = 5;
    [field: SerializeField] public int Lives { get; private set; } = 3;
    [field: SerializeField] public int ExtraBallAtHits { get; private set; } = 15;
    [field: SerializeField, Range(0f, 0.5f)] public float PerfectSlowdown { get; private set; } = 0.06f;

    public bool IsMultiplayer => MaxPlayers > 1;
}
