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
    Unlock
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
