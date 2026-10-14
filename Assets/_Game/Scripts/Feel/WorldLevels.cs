using System;
using UnityEngine;

public sealed class WorldLevels : MonoBehaviour
{
    private const int PoolSize = 28;

    private struct Mote
    {
        public SpriteRenderer View;
        public Vector2 Position;
        public float Speed;
        public float Phase;
        public float Size;
        public float Alpha;
        public float Spin;
        public bool Leaving;
        public bool Active;
    }

    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private BackdropFeel Backdrop;
    [SerializeField] private BallFeel BallLook;
    [SerializeField] private Camera WorldCamera;

    [Header("Levels")]
    [SerializeField] private int[] Thresholds = { 10, 25, 50, 100 };
    [SerializeField] private int[] MotesPerLevel = { 0, 7, 13, 19, 26 };
    [SerializeField] private Sprite[] Shapes;
    [SerializeField] private float LevelPulse = 2.5f;

    [Header("Motes")]
    [SerializeField] private float Size = 0.11f;
    [SerializeField] private float Rise = 0.4f;
    [SerializeField] private float Sway = 0.22f;
    [SerializeField, Range(0f, 1f)] private float WhiteAlpha = 0.6f;
    [SerializeField, Range(0f, 1f)] private float AccentAlpha = 0.45f;
    [SerializeField] private float Fade = 1.2f;
    [SerializeField] private int Order = -80;

    public event Action<int> LevelChanged;

    private readonly Mote[] motes = new Mote[PoolSize];
    private int level;
    private int seed;

    public int Level => level;
    public int VisibleMotes { get; private set; }

    private int Target => MotesPerLevel != null && MotesPerLevel.Length > 0
        ? Mathf.Min(PoolSize, MotesPerLevel[Mathf.Clamp(level, 0, MotesPerLevel.Length - 1)])
        : 0;

    private void Awake()
    {
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Backdrop == null) Backdrop = FindFirstObjectByType<BackdropFeel>();
        if (BallLook == null) BallLook = FindFirstObjectByType<BallFeel>();
        if (WorldCamera == null) WorldCamera = Camera.main;

        for (int i = 0; i < PoolSize; i++)
        {
            var item = new GameObject("mote_" + i.ToString("00"));
            item.transform.SetParent(transform, false);
            var view = item.AddComponent<SpriteRenderer>();
            view.sortingOrder = Order;
            view.enabled = false;
            motes[i].View = view;
        }
    }

    private void OnEnable()
    {
        if (Game == null) return;

        Game.StateChanged += OnStateChanged;
        if (Game.ScoreManager != null) Game.ScoreManager.ScoreChanged += OnScoreChanged;
    }

    private void OnDisable()
    {
        if (Game == null) return;

        Game.StateChanged -= OnStateChanged;
        if (Game.ScoreManager != null) Game.ScoreManager.ScoreChanged -= OnScoreChanged;
    }

    private void OnStateChanged(SoloGameManager.GameState state)
    {
        if (state == SoloGameManager.GameState.Menu) SetLevel(0, false);
    }

    private void OnScoreChanged(int score)
    {
        bool playing = Game != null && Game.State == SoloGameManager.GameState.Playing;
        int next = playing ? RunMarks.Level(score, Thresholds) : 0;
        SetLevel(next, playing && next > level);
    }

    private void SetLevel(int next, bool celebrate)
    {
        if (next == level) return;

        level = next;
        if (celebrate && Backdrop != null) Backdrop.Pulse(LevelPulse);
        LevelChanged?.Invoke(level);
    }

    private void Update()
    {
        if (WorldCamera == null) return;

        float step = FeelMath.Step(Time.unscaledDeltaTime);
        float halfHeight = WorldCamera.orthographicSize;
        float halfWidth = halfHeight * WorldCamera.aspect;
        Vector2 center = WorldCamera.transform.position;
        float top = center.y + halfHeight + Size * 2f;
        float bottom = center.y - halfHeight - Size * 2f;

        Balance(center, halfWidth, halfHeight, bottom);

        int visible = 0;
        for (int i = 0; i < PoolSize; i++)
        {
            if (!motes[i].Active) continue;

            ref var mote = ref motes[i];
            mote.Phase += step;
            mote.Position.y += mote.Speed * step;
            mote.Alpha = Mathf.MoveTowards(mote.Alpha, mote.Leaving ? 0f : 1f, step / Mathf.Max(0.01f, Fade));

            if (mote.Leaving && mote.Alpha <= 0f)
            {
                mote.Active = false;
                mote.View.enabled = false;
                continue;
            }

            if (mote.Position.y > top) Spawn(ref mote, center, halfWidth, halfHeight, bottom, false);

            float sway = Mathf.Sin(mote.Phase * 0.9f + i) * Sway;
            var view = mote.View;
            view.transform.position = new Vector3(mote.Position.x + sway, mote.Position.y, 0f);
            view.transform.rotation = Quaternion.Euler(0f, 0f, mote.Phase * mote.Spin);

            bool white = i % 2 == 0 || BallLook == null;
            var color = white ? Color.white : BallLook.AccentColor;
            color.a = (white ? WhiteAlpha : AccentAlpha) * mote.Alpha;
            view.color = color;
            visible++;
        }

        VisibleMotes = visible;
    }

    private void Balance(Vector2 center, float halfWidth, float halfHeight, float bottom)
    {
        int alive = 0;
        for (int i = 0; i < PoolSize; i++)
            if (motes[i].Active && !motes[i].Leaving) alive++;

        int target = Target;
        for (int i = 0; i < PoolSize && alive < target; i++)
        {
            if (motes[i].Active && !motes[i].Leaving) continue;
            if (Spawn(ref motes[i], center, halfWidth, halfHeight, bottom, true)) alive++;
        }

        for (int i = PoolSize - 1; i >= 0 && alive > target; i--)
        {
            if (!motes[i].Active || motes[i].Leaving) continue;
            motes[i].Leaving = true;
            alive--;
        }
    }

    private bool Spawn(ref Mote mote, Vector2 center, float halfWidth, float halfHeight, float bottom, bool anywhere)
    {
        seed++;
        float a = FeelMath.Jitter(seed * 3 + 1);
        float b = FeelMath.Jitter(seed * 5 + 2);
        float c = FeelMath.Jitter(seed * 7 + 3);

        var sprite = PickShape(c);
        if (sprite == null) return false;

        bool fresh = !mote.Active || mote.Leaving;
        mote.Position = new Vector2(center.x + (a * 2f - 1f) * halfWidth, anywhere ? center.y + (b * 2f - 1f) * halfHeight : bottom);
        mote.Speed = Rise * (0.6f + 0.8f * b);
        mote.Phase = c * 6.28f;
        mote.Size = Size * (0.7f + 0.6f * a);
        mote.Spin = (c - 0.5f) * 90f;
        mote.Leaving = false;
        if (fresh && anywhere) mote.Alpha = 0f;
        mote.Active = true;

        var view = mote.View;
        view.sprite = sprite;
        float unit = sprite.bounds.size.x > 0.0001f ? mote.Size / sprite.bounds.size.x : mote.Size;
        view.transform.localScale = new Vector3(unit, unit, 1f);
        view.enabled = true;
        return true;
    }

    private Sprite PickShape(float roll)
    {
        if (Shapes == null || Shapes.Length == 0) return null;

        int unlocked = Mathf.Clamp(level, 1, Shapes.Length);
        int index = Mathf.Min(unlocked - 1, Mathf.FloorToInt(roll * unlocked));
        return Shapes[index];
    }
}
