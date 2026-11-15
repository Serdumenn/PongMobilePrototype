using UnityEngine;

public sealed class GameFeel : MonoBehaviour
{
    private const int PoolSize = 96;
    private const float FreezeScale = 0.05f;
    private const float Drag = 4.5f;
    private const float ConfettiDrag = 1.2f;
    private const float ConfettiFade = 0.3f;
    private const float ConfettiScatter = 18f;

    private enum Kind
    {
        Dot,
        Ring,
        Spark,
        Confetti,
        Shape
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
        public float Phase;
        public float Gravity;
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
    [SerializeField] private Sprite Confetti;

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

    [Header("Costume")]
    [SerializeField] private float ShapeScale = 1.4f;
    [SerializeField] private float ShapeSpin = 260f;
    [SerializeField] private float TwinkleSize = 0.1f;
    [SerializeField] private float TwinkleLife = 0.42f;
    [SerializeField] private float TwinkleDrift = 0.5f;

    [Header("Flash")]
    [SerializeField] private Color FlashColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private float FlashSize = 1.3f;

    [Header("Confetti")]
    [SerializeField] private Color[] Palette =
    {
        new Color(1f, 0.42f, 0.34f, 1f),
        new Color(0.18f, 0.77f, 0.71f, 1f),
        new Color(1f, 0.79f, 0.24f, 1f),
        new Color(0.61f, 0.42f, 1f, 1f),
        new Color(1f, 0.56f, 0.64f, 1f),
        new Color(0.31f, 0.66f, 1f, 1f)
    };
    [SerializeField] private float ConfettiSize = 0.14f;
    [SerializeField] private float ConfettiLife = 1.9f;
    [SerializeField] private float ConfettiGravity = 7f;
    [SerializeField] private float ConfettiFlutter = 9f;
    [SerializeField] private int ConfettiOrder = 20;

    [Header("Shop effects")]
    [SerializeField] private float StyledLife = 1.8f;
    [SerializeField] private float TraceSize = 0.16f;
    [SerializeField] private float TraceLife = 0.6f;
    [SerializeField] private float TraceScatter = 0.4f;
    [SerializeField] private float TraceLift = 1.5f;
    [SerializeField] private float ShapeDrag = 1.4f;
    [SerializeField] private float PartySize = 0.22f;
    [SerializeField] private float PartyLife = 1.9f;
    [SerializeField] private float FireworkReach = 1.4f;
    [SerializeField] private float FireworkSpeed = 3.2f;
    [SerializeField] private float FireworkGravity = 1.6f;
    [SerializeField] private float FireworkGap = 0.28f;
    [SerializeField] private float PingiSize = 0.6f;
    [SerializeField] private float PingiHop = 7.5f;
    [SerializeField] private float PingiGravity = 9f;
    [SerializeField] private float RainSpeed = 1.6f;
    [SerializeField] private float RainGravity = 1.8f;
    [SerializeField] private float RainSpread = 0.9f;
    [SerializeField] private float RainLife = 2.6f;
    [SerializeField] private float FountainSpeed = 7f;
    [SerializeField] private float FountainGravity = 6f;
    [SerializeField] private float FountainSpread = 70f;

    [Header("Camera")]
    [SerializeField] private float HitPunch = 0.035f;
    [SerializeField] private float PerfectPunch = 0.08f;

    private readonly Particle[] particles = new Particle[PoolSize];
    private int cursor;
    private int burst;
    private bool warping;
    private float warpScale = 1f;
    private float warpSet = 1f;
    private float warpHoldUntil;
    private float warpEaseUntil;
    private float warpEase;
    private bool perfectPending;
    private bool struckFresh;
    private Vector2 struckPoint;
    private Vector2 struckNormal = Vector2.up;

    public int ActiveCount { get; private set; }
    public CosmeticItem PartyLook { get; set; }
    public Sprite PartyFace { get; set; }
    public bool PartyStyled => PartyLook != null && PartyLook.Unlock != UnlockKind.Free && PartyLook.Style != CelebrationStyle.Confetti;
    public bool Slowed => warping;
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
        EndWarp();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (warping) StepWarp();
        if (perfectPending) PlayPendingPerfect();
        struckFresh = false;
        if (ActiveCount == 0) return;

        float step = FeelMath.Step(Time.unscaledDeltaTime);
        int active = 0;
        for (int i = 0; i < PoolSize; i++)
        {
            if (!particles[i].Active) continue;

            if (particles[i].Age < 0f)
            {
                particles[i].Age += step;
                if (particles[i].Age >= 0f) particles[i].View.enabled = true;
                active++;
                continue;
            }

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

    private void Draw(ref Particle p, float t, float step)
    {
        var view = p.View.transform;
        float fade = 1f - t;
        var color = p.Color;

        if (p.Kind == Kind.Confetti)
        {
            p.Velocity += Vector2.down * (ConfettiGravity * step);
            p.Velocity *= Mathf.Max(0f, 1f - ConfettiDrag * step);
            view.position += new Vector3(p.Velocity.x, p.Velocity.y, 0f) * step;
            view.Rotate(0f, 0f, p.Spin * step);

            float flip = 0.25f + 0.75f * Mathf.Abs(Mathf.Cos(p.Age * ConfettiFlutter + p.Phase));
            view.localScale = new Vector3(p.Size * flip, p.Size, 1f);
            color.a *= Mathf.Clamp01(fade / ConfettiFade);
            p.View.color = color;
            return;
        }

        if (p.Kind == Kind.Shape)
        {
            p.Velocity += Vector2.down * (p.Gravity * step);
            p.Velocity *= Mathf.Max(0f, 1f - ShapeDrag * step);
            view.position += new Vector3(p.Velocity.x, p.Velocity.y, 0f) * step;
            if (p.Spin != 0f) view.Rotate(0f, 0f, p.Spin * step);

            float pop = Mathf.Min(1f, p.Age / 0.12f);
            view.localScale = new Vector3(p.Size * pop, p.Size * pop, 1f);
            color.a *= Mathf.Clamp01(fade / ConfettiFade);
            p.View.color = color;
            return;
        }

        float scale;
        if (p.Kind == Kind.Ring)
        {
            scale = p.Size * (0.35f + 0.65f * (1f - fade * fade));
        }
        else
        {
            if (p.Gravity != 0f) p.Velocity += Vector2.down * (p.Gravity * step);
            view.position += new Vector3(p.Velocity.x, p.Velocity.y, 0f) * step;
            p.Velocity *= Mathf.Max(0f, 1f - Drag * step);
            if (p.Spin != 0f) view.Rotate(0f, 0f, p.Spin * step);
            scale = p.Size * (p.Kind == Kind.Spark ? Mathf.Sin(Mathf.Clamp01(t * 1.15f) * Mathf.PI) + 0.15f : 0.4f + 0.6f * fade);
        }

        view.localScale = new Vector3(scale, scale, 1f);
        color.a *= fade * fade;
        p.View.color = color;
    }

    public void Impact(Vector2 point, Vector2 normal, Color color, bool paddle, Sprite shape = null)
    {
        if (paddle)
        {
            struckPoint = point;
            struckNormal = normal;
            struckFresh = true;
        }

        var piece = shape != null ? shape : Dot;
        if (piece != null)
        {
            int count = paddle ? DotsPerHit : Mathf.Max(2, DotsPerHit / 2);
            float size = shape != null ? DotSize * ShapeScale : DotSize;
            for (int i = 0; i < count; i++)
            {
                float jitter = FeelMath.Jitter(burst * 31 + i);
                Vector2 direction = FeelMath.Fan(normal, i, count, DotSpread);
                float spin = shape != null ? ShapeSpin * (jitter - 0.5f) * 2f : 0f;
                Spawn(Kind.Dot, piece, point, direction * (DotSpeed * (0.65f + 0.7f * jitter)), color, size * (0.7f + 0.6f * jitter), DotLife * (0.8f + 0.4f * jitter), spin, SortingOrder);
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

    public void StyledImpact(Vector2 point, Vector2 normal, CosmeticItem look)
    {
        if (look == null) return;

        struckPoint = point;
        struckNormal = normal;
        struckFresh = true;

        var shape = look.HitShape != null ? look.HitShape : Dot;
        var colors = look.Colors != null && look.Colors.Length > 0 ? look.Colors : Palette;
        if (shape != null)
        {
            int count = Mathf.Clamp(look.HitCount > 0 ? look.HitCount : DotsPerHit, 1, 14);
            for (int i = 0; i < count; i++)
            {
                float jitter = FeelMath.Jitter(burst * 31 + i);
                Vector2 direction = FeelMath.Fan(normal, i, count, DotSpread);
                float speed = DotSpeed * Mathf.Max(0.1f, look.HitSpeed) * (0.65f + 0.7f * jitter);
                int slot = Spawn(Kind.Dot, shape, point, direction * speed, colors[i % colors.Length], DotSize * ShapeScale * (0.7f + 0.6f * jitter), DotLife * StyledLife * (0.8f + 0.4f * jitter), ShapeSpin * (jitter - 0.5f) * 2f, SortingOrder);
                particles[slot].Gravity = look.HitGravity;
            }
        }

        if (Ring != null)
        {
            var soft = colors[0];
            soft.a = RingAlpha;
            Spawn(Kind.Ring, Ring, point, Vector2.zero, soft, RingSize, RingLife, 0f, SortingOrder - 2);
        }

        if (Punch != null) Punch.Kick(-normal, HitPunch);
        burst++;
    }

    public void Trace(Vector2 point, Vector2 drift, Color color, Sprite shape)
    {
        if (shape == null) return;

        float jitter = FeelMath.Jitter(burst * 13 + 7);
        var scatter = new Vector2(jitter - 0.5f, FeelMath.Jitter(burst * 5 + 1) - 0.5f) * TraceScatter;
        int slot = Spawn(Kind.Spark, shape, point, drift + scatter, color, TraceSize * (0.75f + 0.5f * jitter), TraceLife, ShapeSpin * (jitter - 0.5f) * 0.6f, SortingOrder - 1);
        particles[slot].Gravity = -drift.y * TraceLift;
        burst++;
    }

    public void Party(Vector2 center, int count)
    {
        if (!PartyStyled) return;

        var look = PartyLook;
        var colors = look.Colors != null && look.Colors.Length > 0 ? look.Colors : Palette;
        var shape = look.CelebrationShape != null ? look.CelebrationShape : Confetti;
        if (shape == null) shape = Dot;
        count = Mathf.Clamp(count, 12, 40);

        switch (look.Style)
        {
            case CelebrationStyle.Fireworks:
                Fireworks(center, shape, colors, count);
                break;
            case CelebrationStyle.PingiParty:
                PingiParty(center, colors, count);
                break;
            case CelebrationStyle.Rain:
                Rain(shape, colors, count);
                break;
            default:
                Fountain(center, shape, colors, count);
                break;
        }

        burst++;
    }

    public int CountUsing(Sprite sprite)
    {
        if (sprite == null) return 0;
        int count = 0;
        for (int i = 0; i < PoolSize; i++)
            if (particles[i].Active && particles[i].View.sprite == sprite) count++;
        return count;
    }

    private static readonly Vector2[] FireworkSpots = { new Vector2(-0.9f, 0.5f), new Vector2(0.95f, 1.1f), new Vector2(0.05f, -0.35f) };

    private void Fireworks(Vector2 center, Sprite shape, Color[] colors, int count)
    {
        int per = Mathf.Max(12, count / 3);
        for (int b = 0; b < FireworkSpots.Length; b++)
        {
            Vector2 at = center + FireworkSpots[b] * FireworkReach;
            var color = colors[(b + burst) % colors.Length];
            for (int i = 0; i < per; i++)
            {
                float jitter = FeelMath.Jitter(burst * 29 + b * 11 + i);
                Vector2 direction = Quaternion.Euler(0f, 0f, i * 360f / per + b * 13f) * Vector2.right;
                int slot = Spawn(Kind.Shape, shape, at, direction * (FireworkSpeed * (0.85f + 0.3f * jitter)), color, PartySize * 0.7f, PartyLife * 0.7f, 0f, ConfettiOrder, b * FireworkGap);
                particles[slot].Gravity = FireworkGravity;
            }
        }
    }

    private void PingiParty(Vector2 center, Color[] colors, int count)
    {
        var face = PartyFace != null ? PartyFace : Dot;
        if (face == null) return;

        var view = ViewRect();
        int faces = Mathf.Clamp(count / 4, 6, 8);
        for (int i = 0; i < faces; i++)
        {
            float jitter = FeelMath.Jitter(burst * 37 + i);
            bool left = i % 2 == 0;
            float x = left ? view.xMin + PingiSize * 0.6f : view.xMax - PingiSize * 0.6f;
            float y = Mathf.Lerp(view.yMin + view.height * 0.25f, view.yMax - view.height * 0.2f, (i / 2 + 0.5f) / Mathf.Ceil(faces / 2f));
            var velocity = new Vector2((left ? 1f : -1f) * 0.6f * jitter, PingiHop * 0.5f * (0.85f + 0.3f * jitter));
            int slot = Spawn(Kind.Shape, face, new Vector2(x, y), velocity, Color.white, PingiSize, PartyLife, (left ? -1f : 1f) * 90f * jitter, SortingOrder - 3, (i / 2) * 0.14f);
            particles[slot].Gravity = PingiGravity * 0.6f;
        }

        if (Spark == null) return;
        for (int i = 0; i < 6; i++)
        {
            float jitter = FeelMath.Jitter(burst * 41 + i);
            Vector2 direction = FeelMath.Fan(Vector2.up, i, 6, 160f);
            int slot = Spawn(Kind.Shape, Spark, center, direction * (FireworkSpeed * (0.8f + 0.4f * jitter)), colors[i % colors.Length], PartySize * 0.8f, PartyLife * 0.6f, 200f * (jitter - 0.5f), ConfettiOrder, 0.15f);
            particles[slot].Gravity = FireworkGravity;
        }
    }

    private void Rain(Sprite shape, Color[] colors, int count)
    {
        var view = ViewRect();
        for (int i = 0; i < count; i++)
        {
            float jitter = FeelMath.Jitter(burst * 43 + i);
            float spread = FeelMath.Jitter(burst * 53 + i * 3);
            float x = Mathf.Lerp(view.xMin + 0.2f, view.xMax - 0.2f, (i + jitter) / count);
            var velocity = new Vector2((spread - 0.5f) * 0.8f, -RainSpeed * (0.7f + 0.6f * jitter));
            int slot = Spawn(Kind.Shape, shape, new Vector2(x, view.yMax + 0.3f), velocity, colors[i % colors.Length], PartySize * (0.8f + 0.4f * jitter), RainLife, 260f * (jitter - 0.5f), ConfettiOrder, spread * RainSpread);
            particles[slot].Gravity = RainGravity;
        }
    }

    private void Fountain(Vector2 center, Sprite shape, Color[] colors, int count)
    {
        var view = ViewRect();
        var origin = new Vector2(center.x, view.yMin + 0.2f);
        for (int i = 0; i < count; i++)
        {
            float jitter = FeelMath.Jitter(burst * 47 + i);
            Vector2 direction = FeelMath.Fan(Vector2.up, i, count, FountainSpread);
            int slot = Spawn(Kind.Shape, shape, origin, direction * (FountainSpeed * (0.75f + 0.5f * jitter)), colors[i % colors.Length], PartySize * (0.8f + 0.4f * jitter), PartyLife, 300f * (jitter - 0.5f), ConfettiOrder, (i % 4) * 0.06f);
            particles[slot].Gravity = FountainGravity;
        }
    }

    private static Rect ViewRect()
    {
        var cam = Camera.main;
        if (cam == null || !cam.orthographic) return new Rect(-3f, -5f, 6f, 10f);

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        Vector2 middle = cam.transform.position;
        return new Rect(middle.x - width / 2f, middle.y - height / 2f, width, height);
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

    public void Twinkle(Vector2 point, Color color, Sprite shape = null)
    {
        var piece = shape != null ? shape : Spark;
        if (piece == null) return;

        float jitter = FeelMath.Jitter(burst * 13 + 5);
        var drift = new Vector2(FeelMath.Jitter(burst * 7 + 3) - 0.5f, jitter - 0.3f) * TwinkleDrift;
        Spawn(Kind.Spark, piece, point, drift, color, TwinkleSize * (0.7f + 0.6f * jitter), TwinkleLife, ShapeSpin * (jitter - 0.5f), SortingOrder - 1);
        burst++;
    }

    public void Flash(Vector2 point)
    {
        if (Ring != null) Spawn(Kind.Ring, Ring, point, Vector2.zero, FlashColor, RingSize * FlashSize, RingLife * 1.2f, 0f, SortingOrder - 2);
        burst++;
    }

    public void Celebrate(Vector2 origin, Vector2 direction, int count, float spread, float speed)
    {
        var sprite = Confetti != null ? Confetti : Dot;
        if (sprite == null || count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            float jitter = FeelMath.Jitter(burst * 43 + i);
            float tilt = (FeelMath.Jitter(burst * 59 + i * 7) - 0.5f) * ConfettiScatter;
            Vector2 aim = Quaternion.Euler(0f, 0f, tilt) * FeelMath.Fan(direction, i, count, spread);
            var color = Palette != null && Palette.Length > 0 ? Palette[(i + burst) % Palette.Length] : Color.white;

            int slot = Spawn(Kind.Confetti, sprite, origin, aim * (speed * (0.55f + 0.7f * jitter)), color, ConfettiSize * (0.75f + 0.5f * jitter), ConfettiLife * (0.8f + 0.4f * jitter), 520f * (jitter - 0.5f), ConfettiOrder);
            particles[slot].Phase = jitter * 6.28f;
            particles[slot].View.transform.rotation = Quaternion.Euler(0f, 0f, jitter * 360f);
        }

        burst++;
    }

    public void Freeze(float seconds)
    {
        Warp(FreezeScale, seconds, 0f);
    }

    public void Warp(float scale, float hold, float ease)
    {
        if (hold <= 0f && ease <= 0f) return;
        if (OnlineRound) return;

        float now = Time.timeScale;
        if (warping ? !Mathf.Approximately(now, warpSet) : !Mathf.Approximately(now, 1f)) return;

        scale = Mathf.Clamp(scale, 0.01f, 1f);
        if (warping && scale > warpScale && Time.unscaledTime < warpHoldUntil) return;

        warping = true;
        warpScale = scale;
        warpEase = Mathf.Max(0f, ease);
        warpHoldUntil = Time.unscaledTime + Mathf.Max(0f, hold);
        warpEaseUntil = warpHoldUntil + warpEase;
        SetScale(scale);
    }

    public void Release()
    {
        EndWarp();
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

    private void StepWarp()
    {
        if (!Mathf.Approximately(Time.timeScale, warpSet))
        {
            warping = false;
            return;
        }

        float now = Time.unscaledTime;
        if (now >= warpEaseUntil)
        {
            EndWarp();
            return;
        }

        if (now < warpHoldUntil) return;

        float t = Mathf.Clamp01((now - warpHoldUntil) / Mathf.Max(0.0001f, warpEase));
        SetScale(Mathf.Lerp(warpScale, 1f, t * t * (3f - 2f * t)));
    }

    private void EndWarp()
    {
        if (!warping) return;

        warping = false;
        if (Mathf.Approximately(Time.timeScale, warpSet)) Time.timeScale = 1f;
    }

    private void SetScale(float scale)
    {
        Time.timeScale = scale;
        warpSet = Time.timeScale;
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

    private int Spawn(Kind kind, Sprite sprite, Vector2 point, Vector2 velocity, Color color, float size, float life, float spin, int order, float delay = 0f)
    {
        int slot = cursor;
        cursor = (cursor + 1) % PoolSize;

        float unit = sprite.bounds.size.x > 0.0001f ? 1f / sprite.bounds.size.x : 1f;
        bool wasActive = particles[slot].Active;

        particles[slot].Kind = kind;
        particles[slot].Velocity = velocity;
        particles[slot].Color = color;
        particles[slot].Age = delay > 0f ? -delay : 0f;
        particles[slot].Gravity = 0f;
        particles[slot].Life = Mathf.Max(0.05f, life);
        particles[slot].Size = size * unit;
        particles[slot].Spin = spin;
        particles[slot].Phase = 0f;
        particles[slot].Active = true;

        var view = particles[slot].View;
        view.sprite = sprite;
        view.sortingOrder = order;
        view.color = color;
        view.transform.SetPositionAndRotation(new Vector3(point.x, point.y, 0f), Quaternion.identity);
        view.transform.localScale = new Vector3(particles[slot].Size, particles[slot].Size, 1f) * (kind == Kind.Ring || kind == Kind.Shape ? 0.35f : 1f);
        view.enabled = delay <= 0f;

        if (!wasActive) ActiveCount++;
        return slot;
    }
}
