using System;
using UnityEngine;
using UnityEngine.Audio;

public enum Sfx
{
    PaddleHit,
    WallBounce,
    Perfect,
    UiTap,
    UiBack,
    NewBest,
    GameOver,
    Unlock,
    CountdownTick,
    CountdownGo,
    Point,
    MatchWin,
    Portal,
    Reaction,
    AttackSend,
    AttackWarn,
    Shield
}

public sealed class AudioManager : MonoBehaviour
{
    [Serializable]
    private struct Sound
    {
        public Sfx Id;
        public AudioClip Clip;
        [Range(0f, 1f)] public float Volume;
        [Range(0f, 0.2f)] public float PitchJitter;
    }

    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioMixerGroup Output;
    [SerializeField] private Sound[] Sounds = Array.Empty<Sound>();
    [SerializeField, Range(1, 16)] private int Voices = 6;

    [Header("Rally")]
    [SerializeField] private float SemitonesPerHit = 0.5f;
    [SerializeField] private float MaxSemitones = 12f;

    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private LocalMatchController Match;
    [SerializeField] private OnlineMatchController Online;

    private AudioSource[] sources;
    private int nextSource;
    private int rally;

    public static void PlayOne(Sfx id)
    {
        if (Instance != null) Instance.Play(id);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        sources = new AudioSource[Mathf.Max(1, Voices)];
        for (int i = 0; i < sources.Length; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = Output;
            sources[i] = source;
        }
    }

    private void Start()
    {
        if (Online == null) Online = FindFirstObjectByType<OnlineMatchController>();
        if (Online != null)
        {
            Online.CountdownTick += OnCountdown;
            Online.PaddleHit += OnOnlineHit;
        }

        if (Match == null) Match = FindFirstObjectByType<LocalMatchController>();
        if (Match != null)
        {
            Match.HitScored += OnHitScored;
            Match.PointLost += OnPointLost;
            Match.CountdownTick += OnCountdown;
            Match.StateChanged += OnMatchState;
        }

        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Game == null) return;

        Game.HitScored += OnHitScored;
        Game.StateChanged += OnStateChanged;
        if (Game.Ball != null)
        {
            Game.Ball.WallHit += OnWallHit;
            Game.Ball.RoundReset += ResetRally;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (Online != null)
        {
            Online.CountdownTick -= OnCountdown;
            Online.PaddleHit -= OnOnlineHit;
        }

        if (Match != null)
        {
            Match.HitScored -= OnHitScored;
            Match.PointLost -= OnPointLost;
            Match.CountdownTick -= OnCountdown;
            Match.StateChanged -= OnMatchState;
        }

        if (Game == null) return;

        Game.HitScored -= OnHitScored;
        Game.StateChanged -= OnStateChanged;
        if (Game.Ball != null)
        {
            Game.Ball.WallHit -= OnWallHit;
            Game.Ball.RoundReset -= ResetRally;
        }
    }

    public void Play(Sfx id, float pitch = 1f)
    {
        if (!GameSettings.SoundEnabled || sources == null) return;

        for (int i = 0; i < Sounds.Length; i++)
        {
            if (Sounds[i].Id != id || Sounds[i].Clip == null) continue;

            var source = sources[nextSource];
            nextSource = (nextSource + 1) % sources.Length;

            float jitter = Sounds[i].PitchJitter;
            source.pitch = pitch * (1f + UnityEngine.Random.Range(-jitter, jitter));
            source.PlayOneShot(Sounds[i].Clip, Sounds[i].Volume);
            return;
        }
    }

    public void StopAll()
    {
        if (sources == null) return;
        foreach (var source in sources) source.Stop();
    }

    private void OnHitScored(HitResult hit)
    {
        float semitones = Mathf.Min(rally * SemitonesPerHit, MaxSemitones);
        rally++;

        Play(Sfx.PaddleHit, Mathf.Pow(2f, semitones / 12f));
        if (hit.Perfect) Play(Sfx.Perfect);
    }

    private void OnOnlineHit(int rallyCount)
    {
        float semitones = Mathf.Min(Mathf.Max(0, rallyCount - 1) * SemitonesPerHit, MaxSemitones);
        Play(Sfx.PaddleHit, Mathf.Pow(2f, semitones / 12f));
    }

    private void OnPointLost(Participant participant)
    {
        ResetRally();

        var rules = Match != null ? Match.Rules : null;
        bool over = rules != null && (rules.Winner != null || (rules.IsTeamMatch && rules.TeamLives <= 0));
        if (!over) Play(Sfx.Point);
    }

    private void OnCountdown(int value)
    {
        Play(value > 0 ? Sfx.CountdownTick : Sfx.CountdownGo);
    }

    private void OnMatchState(LocalMatchController.MatchState state)
    {
        if (state != LocalMatchController.MatchState.Result || Match.Rules == null) return;
        Play(Match.Rules.IsTeamMatch && !Match.NewBestRally ? Sfx.GameOver : Sfx.MatchWin);
    }

    private void OnWallHit()
    {
        Play(Sfx.WallBounce);
    }

    private void ResetRally()
    {
        rally = 0;
    }

    private void OnStateChanged(SoloGameManager.GameState state)
    {
        if (state != SoloGameManager.GameState.GameOver) return;

        bool newBest = Game.ScoreManager != null && Game.ScoreManager.IsNewBest;
        Play(newBest ? Sfx.NewBest : Sfx.GameOver);
    }
}
