using UnityEngine;

public sealed class GameFeel : MonoBehaviour
{
    private const int PoolSize = 48;
    private const float FreezeScale = 0.05f;
    private const float Drag = 4.5f;

    private enum Kind
    {
        Dot,
        Ring,
        Spark
    }

    private struct Particle
    {
        public SpriteRenderer View;
        public Kind Kind;
        public Vector2 Velocity;
        public Color Color;
        public float Age;
        public float Life;
        public float Size;
        public float Spin;
        public bool Active;
    }

    public static GameFeel Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private LocalMatchController Match;
    [SerializeField] private CameraPunch Punch;
    [SerializeField] private OnlineRushController OnlineRush;
    [SerializeField] private OnlineMatchController OnlineMatch;

    [Header("Sprites")]
    [SerializeField] private Sprite Dot;
    [SerializeField] private Sprite Ring;
    [SerializeField] private Sprite Spark;

    [Header("Impact")]
    [SerializeField] private int DotsPerHit = 6;
    [SerializeField] private float DotSpeed = 3.4f;
    [SerializeField] private float DotSize = 0.13f;
    [SerializeField] private float DotLife = 0.34f;
    [SerializeField] private float DotSpread = 130f;
    [SerializeField] private float RingSize = 0.95f;
    [SerializeField] private float RingLife = 0.3f;
    [SerializeField, Range(0f, 1f)] private float RingAlpha = 0.55f;
    [SerializeField] private int SortingOrder = -2;

    [Header("Perfect")]
    [SerializeField] private Color PerfectColor = new Color(1f, 0.84f, 0.35f, 1f);
    [SerializeField] private int SparksPerPerfect = 7;
    [SerializeField] private float SparkSpeed = 4.6f;
    [SerializeField] private float SparkSize = 0.26f;
    [SerializeField] private float SparkLife = 0.5f;
    [SerializeField] private float FreezeSeconds = 0.04f;

    [Header("Camera")]
    [SerializeField] private float HitPunch = 0.035f;
    [SerializeField] private float PerfectPunch = 0.08f;

    private readonly Particle[] particles = new Particle[PoolSize];
    private int cursor;
    private int burst;
    private bool frozen;
    private float freezeUntil;
    private bool perfectPending;
    private bool struckFresh;
    private Vector2 struckPoint;
    private Vector2 struckNormal = Vector2.up;

    public int ActiveCount { get; private set; }
    public bool Frozen => frozen;
    public bool OnlineRound => (OnlineRush != null && OnlineRush.IsActive) || (OnlineMatch != null && OnlineMatch.IsActive);

    private void Awake()
    {
        Instance = this;
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Match == null) Match = FindFirstObjectByType<LocalMatchController>();
        if (Punch == null) Punch = FindFirstObjectByType<CameraPunch>();
        if (OnlineRush == null) OnlineRush = FindFirstObjectByType<OnlineRushController>();
        if (OnlineMatch == null) OnlineMatch = FindFirstObjectByType<OnlineMatchController>();

        for (int i = 0; i < PoolSize; i++)
        {
            var item = new GameObject("fx_" + i.ToString("00"));
            item.transform.SetParent(transform, false);
            var view = item.AddComponent<SpriteRenderer>();
            view.enabled = false;
            particles[i].View = view;
        }
    }

    private void OnEnable()
    {
        if (Game != null) Game.HitScored += OnHitScored;
        if (Match != null) Match.HitScored += OnHitScored;
    }

    private void OnDisable()
    {
        if (Game != null) Game.HitScored -= OnHitScored;
        if (Match != null) Match.HitScored -= OnHitScored;
        Thaw();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (frozen && Time.unscaledTime >= freezeUntil) Thaw();
        if (perfectPending) PlayPendingPerfect();
        struckFresh = false;
        if (ActiveCount == 0) return;

        float step = FeelMath.Step(Time.unscaledDeltaTime);
        int active = 0;
        for (int i = 0; i < PoolSize; i++)
        {
            if (!particles[i].Active) continue;

            particles[i].Age += step;
            float t = particles[i].Age / particles[i].Life;
            if (t >= 1f)
            {
                particles[i].Active = false;
                particles[i].View.enabled = false;
                continue;
            }

            Draw(ref particles[i], t, step);
            active++;
        }

        ActiveCount = active;
    }

    private static void Draw(ref Particle p, float t, float step)
    {
        var view = p.View.transform;
        float fade = 1f - t;
        float scale;

        if (p.Kind == Kind.Ring)
        {
            scale = p.Size * (0.35f + 0.65f * (1f - fade * fade));
        }
        else
        {
            view.position += new Vector3(p.Velocity.x, p.Velocity.y, 0f) * step;
            p.Velocity *= Mathf.Max(0f, 1f - Drag * step);
            if (p.Spin != 0f) view.Rotate(0f, 0f, p.Spin * step);
            scale = p.Size * (p.Kind == Kind.Spark ? Mathf.Sin(Mathf.Clamp01(t * 1.15f) * Mathf.PI) + 0.15f : 0.4f + 0.6f * fade);
        }

        view.localScale = new Vector3(scale, scale, 1f);
        var color = p.Color;
        color.a *= fade * fade;
        p.View.color = color;
    }

    public void Impact(Vector2 point, Vector2 normal, Color color, bool paddle)
    {
        if (paddle)
        {
            struckPoint = point;
            struckNormal = normal;
            struckFresh = true;
        }

        if (Dot != null)
        {
            int count = paddle ? DotsPerHit : Mathf.Max(2, DotsPerHit / 2);
            for (int i = 0; i < count; i++)
            {
                float jitter = FeelMath.Jitter(burst * 31 + i);
                Vector2 direction = FeelMath.Fan(normal, i, count, DotSpread);
                Spawn(Kind.Dot, Dot, point, direction * (DotSpeed * (0.65f + 0.7f * jitter)), color, DotSize * (0.7f + 0.6f * jitter), DotLife * (0.8f + 0.4f * jitter), 0f, SortingOrder);
            }
        }

        if (paddle && Ring != null)
        {
            var soft = color;
            soft.a = RingAlpha;
            Spawn(Kind.Ring, Ring, point, Vector2.zero, soft, RingSize, RingLife, 0f, SortingOrder - 2);
        }

        if (paddle && Punch != null) Punch.Kick(-normal, HitPunch);
        burst++;
    }

    public void Perfect(Vector2 point, Vector2 normal)
    {
        if (Spark != null)
        {
            for (int i = 0; i < SparksPerPerfect; i++)
            {
                float jitter = FeelMath.Jitter(burst * 17 + i);
                Vector2 direction = FeelMath.Fan(normal, i, SparksPerPerfect, 170f);
                Spawn(Kind.Spark, Spark, point, direction * (SparkSpeed * (0.6f + 0.8f * jitter)), PerfectColor, SparkSize * (0.7f + 0.6f * jitter), SparkLife * (0.8f + 0.4f * jitter), 220f * (jitter - 0.5f), SortingOrder + 1);
            }
        }

        if (Ring != null) Spawn(Kind.Ring, Ring, point, Vector2.zero, PerfectColor, RingSize * 1.6f, RingLife * 1.3f, 0f, SortingOrder - 2);
        if (Punch != null) Punch.Kick(-normal, PerfectPunch);
        Freeze(FreezeSeconds);
        burst++;
    }

    public void Freeze(float seconds)
    {
        if (seconds <= 0f || OnlineRound || !Mathf.Approximately(Time.timeScale, 1f)) return;

        Time.timeScale = FreezeScale;
        freezeUntil = Time.unscaledTime + seconds;
        frozen = true;
    }

    public void Clear()
    {
        for (int i = 0; i < PoolSize; i++)
        {
            particles[i].Active = false;
            if (particles[i].View != null) particles[i].View.enabled = false;
        }

        ActiveCount = 0;
        perfectPending = false;
        struckFresh = false;
    }

    private void Thaw()
    {
        if (!frozen) return;

        frozen = false;
        if (Mathf.Approximately(Time.timeScale, FreezeScale)) Time.timeScale = 1f;
    }

    private void OnHitScored(HitResult hit)
    {
        if (hit.Perfect) perfectPending = true;
    }

    private void PlayPendingPerfect()
    {
        perfectPending = false;
        if (!struckFresh && Game != null && Game.Ball != null)
        {
            struckPoint = Game.Ball.LastContact;
            struckNormal = Game.Ball.LastNormal;
        }

        Perfect(struckPoint, struckNormal);
    }

    private void Spawn(Kind kind, Sprite sprite, Vector2 point, Vector2 velocity, Color color, float size, float life, float spin, int order)
    {
        int slot = cursor;
        cursor = (cursor + 1) % PoolSize;

        float unit = sprite.bounds.size.x > 0.0001f ? 1f / sprite.bounds.size.x : 1f;
        bool wasActive = particles[slot].Active;

        particles[slot].Kind = kind;
        particles[slot].Velocity = velocity;
        particles[slot].Color = color;
        particles[slot].Age = 0f;
        particles[slot].Life = Mathf.Max(0.05f, life);
        particles[slot].Size = size * unit;
        particles[slot].Spin = spin;
        particles[slot].Active = true;

        var view = particles[slot].View;
        view.sprite = sprite;
        view.sortingOrder = order;
        view.color = color;
        view.transform.SetPositionAndRotation(new Vector3(point.x, point.y, 0f), Quaternion.identity);
        view.transform.localScale = new Vector3(particles[slot].Size, particles[slot].Size, 1f) * (kind == Kind.Ring ? 0.35f : 1f);
        view.enabled = true;

        if (!wasActive) ActiveCount++;
    }
}
