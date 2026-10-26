using System;
using UnityEngine;

public sealed class RunPurse : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private CosmeticsService Cosmetics;
    [SerializeField] private GoldCoins Coins;

    [Header("Rates")]
    [SerializeField] private int ScorePerGold = 5;

    public event Action Changed;

    private SoloGameManager.GameState lastState;

    public int Collected { get; private set; }
    public int LastRunGold { get; private set; }
    public bool Doubled { get; private set; }
    public bool CanDouble => LastRunGold > 0 && !Doubled;

    private void Awake()
    {
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Cosmetics == null) Cosmetics = FindFirstObjectByType<CosmeticsService>();
        if (Coins == null) Coins = FindFirstObjectByType<GoldCoins>();
    }

    private void OnEnable()
    {
        if (Game != null) Game.StateChanged += OnStateChanged;
        if (Coins != null) Coins.Collected += OnCollected;
    }

    private void OnDisable()
    {
        if (Game != null) Game.StateChanged -= OnStateChanged;
        if (Coins != null) Coins.Collected -= OnCollected;
    }

    public static int ScoreGold(int score, int perGold)
    {
        return perGold > 0 ? Mathf.Max(0, score) / perGold : 0;
    }

    private void OnStateChanged(SoloGameManager.GameState state)
    {
        if (state == SoloGameManager.GameState.Playing && lastState != SoloGameManager.GameState.Paused)
        {
            Collected = 0;
            LastRunGold = 0;
            Doubled = false;
            Changed?.Invoke();
        }
        else if (state == SoloGameManager.GameState.GameOver)
        {
            Settle();
        }

        lastState = state;
    }

    private void OnCollected(int amount)
    {
        if (amount <= 0) return;

        Collected += amount;
        Changed?.Invoke();
    }

    private void Settle()
    {
        int score = Game != null && Game.ScoreManager != null ? Game.ScoreManager.Score : 0;
        LastRunGold = Collected + ScoreGold(score, ScorePerGold);
        Doubled = false;
        if (Cosmetics != null) Cosmetics.EarnGold(LastRunGold);
        Changed?.Invoke();
    }

    public void ApplyDouble()
    {
        if (!CanDouble) return;

        Doubled = true;
        if (Cosmetics != null) Cosmetics.EarnGold(LastRunGold);
        Changed?.Invoke();
    }
}
