using System;
using System.Collections.Generic;
using UnityEngine;

public enum BackdropMotionKind
{
    Still,
    Rise,
    Twinkle,
    Pan,
    Rally,
    Pulse,
    Wobble,
    Streak
}

public enum BackdropTint
{
    Primary,
    Secondary,
    Mix,
    Mist,
    Accent,
    Custom,
    White
}

public sealed class BackdropWorld : MonoBehaviour
{
    [Serializable]
    public sealed class Piece
    {
        [field: SerializeField] public Sprite Sprite { get; private set; }
        [field: SerializeField] public BackdropMotionKind Motion { get; private set; }
        [field: SerializeField] public int Count { get; private set; } = 1;
        [field: SerializeField] public Vector2 Width { get; private set; } = new Vector2(0.1f, 0.1f);
        [field: SerializeField] public float Speed { get; private set; } = 1f;
        [field: SerializeField] public BackdropTint Tint { get; private set; }
        [field: SerializeField] public float Alpha { get; private set; } = 0.6f;
        [field: SerializeField] public Rect Area { get; private set; } = new Rect(0f, 0f, 1f, 1f);
        [field: SerializeField] public int Order { get; private set; }
        [field: SerializeField] public float Spin { get; private set; }
        [field: SerializeField] public float Tilt { get; private set; }
        [field: SerializeField] public float Trail { get; private set; }
        [field: SerializeField] public Color Color { get; private set; } = Color.white;
        [field: SerializeField] public float Spacing { get; private set; }

        public bool Tiled => Count > 1 && Spacing > 0f;
    }

    private sealed class Element
    {
        public Piece Piece;
        public Transform Transform;
        public SpriteRenderer View;
        public Vector2 Home;
        public float Width;
        public float Phase;
        public float Angle;
        public float Shade = 1f;
    }

    [Header("Look")]
    [SerializeField] private Color Accent = new Color(1f, 0.79f, 0.24f, 1f);
    [SerializeField] private int BaseOrder = -90;
    [SerializeField] private int Seed = 7;

    [Header("Pieces")]
    [SerializeField] private List<Piece> Pieces = new List<Piece>();

    private readonly List<Element> elements = new List<Element>();
    private Camera cam;
    private Color primary = Color.white;
    private Color secondary = Color.white;
    private float clock;
    private bool shown = true;

    public float Fade { get; set; }
    public float Pace { get; set; } = 1f;
    public Color Primary => primary;
    public Color Secondary => secondary;

    public int VisibleCount
    {
        get
        {
            int count = 0;
            foreach (var element in elements)
                if (element.View.enabled && element.View.color.a > 0.01f) count++;
            return count;
        }
    }

    private void Awake()
    {
        cam = Camera.main;
        var random = new System.Random(Seed);
        foreach (var piece in Pieces)
        {
            if (piece == null || piece.Sprite == null) continue;
            for (int i = 0; i < Mathf.Max(1, piece.Count); i++) elements.Add(Build(piece, random, i));
        }
        Render();
    }

    private Element Build(Piece piece, System.Random random, int index)
    {
        var holder = new GameObject(piece.Sprite.name + "_" + index);
        holder.transform.SetParent(transform, false);
        var view = holder.AddComponent<SpriteRenderer>();
        view.sprite = piece.Sprite;
        view.sortingOrder = BaseOrder + piece.Order;

        float u = (float)random.NextDouble();
        float v = (float)random.NextDouble();
        int count = Mathf.Max(1, piece.Count);
        float spread = count > 1 ? (index + u * 0.6f) / count : u;
        var element = new Element
        {
            Piece = piece,
            Transform = holder.transform,
            View = view,
            Home = new Vector2(piece.Area.xMin + piece.Area.width * spread, piece.Area.yMin + piece.Area.height * v),
            Width = Mathf.Lerp(piece.Width.x, piece.Width.y, (float)random.NextDouble()),
            Phase = (float)random.NextDouble() * Mathf.PI * 2f,
            Angle = piece.Tilt
        };

        if (piece.Tiled) element.Home.x = piece.Area.xMin + piece.Spacing * (index + 0.5f);
        if (piece.Trail > 0f)
        {
            element.Phase = -index * piece.Trail;
            element.Shade = Mathf.Pow(0.55f, index);
        }
        return element;
    }

    public void SetPalette(Color primaryColor, Color secondaryColor)
    {
        primary = primaryColor;
        secondary = secondaryColor;
        Render();
    }

    public Color TintOf(BackdropTint tint)
    {
        return tint switch
        {
            BackdropTint.Primary => primary,
            BackdropTint.Secondary => secondary,
            BackdropTint.Mix => Color.Lerp(primary, secondary, 0.5f),
            BackdropTint.Mist => Color.Lerp(primary, Color.white, 0.6f),
            BackdropTint.Accent => Accent,
            _ => Color.white
        };
    }

    private void Update()
    {
        clock += Time.unscaledDeltaTime * Mathf.Max(0f, Pace);
        Render();
    }

    private void Render()
    {
        bool visible = Fade > 0.001f;
        if (!visible && !shown) return;
        shown = visible;

        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        float viewWidth = cam.orthographicSize * cam.aspect * 2f;
        float depth = -cam.transform.position.z;
        foreach (var element in elements)
        {
            element.View.enabled = visible;
            if (!visible) continue;
            Place(element, viewWidth, depth);
        }
    }

    private void Place(Element element, float viewWidth, float depth)
    {
        var piece = element.Piece;
        Rect area = piece.Area;
        float t = clock * piece.Speed;
        Vector2 spot = element.Home;
        float size = 1f;
        float alpha = 1f;
        float halfWidth = element.Width * 0.5f;

        switch (piece.Motion)
        {
            case BackdropMotionKind.Rise:
                spot.y = BackdropMotion.Wrap(element.Home.y + t, area.yMin - 0.12f, area.yMax + 0.12f);
                spot.x += 0.02f * Mathf.Sin(clock * 1.3f + element.Phase);
                element.Angle = 12f * Mathf.Sin(clock * 1.1f + element.Phase);
                break;
            case BackdropMotionKind.Twinkle:
                alpha = BackdropMotion.Twinkle(t, element.Phase);
                size = 0.4f + 0.6f * alpha;
                break;
            case BackdropMotionKind.Pan:
                spot.x = piece.Tiled
                    ? BackdropMotion.Wrap(element.Home.x + t, area.xMin, area.xMin + piece.Spacing * piece.Count)
                    : BackdropMotion.Wrap(element.Home.x + t, area.xMin - halfWidth, area.xMax + halfWidth);
                break;
            case BackdropMotionKind.Streak:
                spot.x = piece.Tiled
                    ? BackdropMotion.Wrap(element.Home.x - t, area.xMin, area.xMin + piece.Spacing * piece.Count)
                    : BackdropMotion.Wrap(element.Home.x - t, area.xMin - halfWidth, area.xMax + halfWidth);
                break;
            case BackdropMotionKind.Rally:
                Vector2 rally = BackdropMotion.Rally(t + element.Phase, 5f);
                spot = new Vector2(area.xMin + area.width * rally.x, area.yMin + area.height * rally.y);
                break;
            case BackdropMotionKind.Pulse:
                float grow = BackdropMotion.Pulse(t + element.Phase, 1f);
                size = 0.3f + 5f * grow;
                alpha = 1f - grow;
                break;
            case BackdropMotionKind.Wobble:
                element.Angle += piece.Spin * Time.unscaledDeltaTime * Pace;
                break;
        }

        Vector3 world = cam.ViewportToWorldPoint(new Vector3(spot.x, spot.y, depth));
        element.Transform.position = new Vector3(world.x, world.y, transform.position.z);
        element.Transform.rotation = Quaternion.Euler(0f, 0f, element.Angle);
        Vector3 scale = Scale(element, viewWidth) * size;
        if (piece.Motion == BackdropMotionKind.Wobble)
        {
            Vector2 wobble = BackdropMotion.Wobble(t, element.Phase);
            scale = new Vector3(scale.x * wobble.x, scale.y * wobble.y, 1f);
        }
        element.Transform.localScale = scale;

        Color color = piece.Tint == BackdropTint.Custom ? piece.Color : TintOf(piece.Tint);
        color.a *= Mathf.Clamp01(piece.Alpha * alpha * element.Shade * Fade);
        element.View.color = color;
    }

    private static Vector3 Scale(Element element, float viewWidth)
    {
        float spriteWidth = element.View.sprite != null ? element.View.sprite.bounds.size.x : 1f;
        float unit = viewWidth * element.Width / Mathf.Max(0.0001f, spriteWidth);
        return new Vector3(unit, unit, 1f);
    }
}
