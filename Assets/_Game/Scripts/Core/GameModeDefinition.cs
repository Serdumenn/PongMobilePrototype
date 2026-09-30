using UnityEngine;

public enum GameModeKind
{
    Classic,
    Rush
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
}
