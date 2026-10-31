using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class FieldObstacles : MonoBehaviour
{
    private const int PoolSize = 6;
    private const float BumpSeconds = 0.3f;

    private struct Slot
    {
        public Rigidbody2D Body;
        public SpriteRenderer View;
        public Vector2 Home;
        public float Phase;
        public float Bump;
        public bool Active;
    }

    [Header("Refs")]
    [SerializeField] private FieldLayout Layout;
    [SerializeField] private GameFeel Feel;

    [Header("Art")]
    [SerializeField] private Sprite Bumper;
    [SerializeField] private Color Burst = new Color(0.31f, 0.66f, 1f, 1f);

    [Header("Layout")]
    [SerializeField] private float Size = 0.62f;
    [SerializeField] private float TopGap = 3.6f;
    [SerializeField] private float EdgeMargin = 0.5f;
    [SerializeField] private float SwaySpeed = 1.1f;
    [SerializeField] private int Order = 2;

    public event Action<int> Bumped;

    private readonly Slot[] slots = new Slot[PoolSize];
    private float sway;
    private float swayTime;

    public int Count
    {
        get
        {
            int count = 0;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].Active) count++;
            return count;
        }
    }

    public Vector2 PositionOf(int index)
    {
        return index >= 0 && index < slots.Length && slots[index].Body != null ? slots[index].Body.position : Vector2.zero;
    }

    private void Awake()
    {
        if (Layout == null) Layout = FindFirstObjectByType<FieldLayout>();
        if (Feel == null) Feel = FindFirstObjectByType<GameFeel>();

        for (int i = 0; i < slots.Length; i++)
        {
            var holder = new GameObject("bumper_" + i);
            holder.layer = gameObject.layer;
            holder.transform.SetParent(transform, false);

            var body = holder.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var circle = holder.AddComponent<CircleCollider2D>();
            circle.radius = Size * 0.5f;
            holder.AddComponent<ObstacleBody>().Setup(this, i);

            var view = new GameObject("View").AddComponent<SpriteRenderer>();
            view.transform.SetParent(holder.transform, false);
            view.sprite = Bumper;
            view.sortingOrder = Order;

            slots[i].Body = body;
            slots[i].View = view;
            holder.SetActive(false);
        }
    }

    public void Show(IReadOnlyList<Vector2> spots, float swayAmount)
    {
        Hide();
        if (spots == null) return;

        sway = Mathf.Max(0f, swayAmount);
        swayTime = 0f;
        Rect field = Layout != null ? Layout.Field : new Rect(-2.5f, -5f, 5f, 10f);
        int count = Mathf.Min(spots.Count, slots.Length);
        for (int i = 0; i < count; i++)
        {
            var home = FieldSpots.ToWorld(field, spots[i], EdgeMargin + Size * 0.5f, TopGap);
            slots[i].Active = true;
            slots[i].Home = home;
            slots[i].Phase = i * 2.1f;
            slots[i].Bump = 0f;
            slots[i].Body.position = home;
            slots[i].Body.transform.position = home;
            slots[i].Body.gameObject.SetActive(true);
            Scale(ref slots[i]);
        }
    }

    public void Hide()
    {
        sway = 0f;
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].Active = false;
            if (slots[i].Body != null) slots[i].Body.gameObject.SetActive(false);
        }
    }

    public void Touched(int index, Collision2D collision)
    {
        if (index < 0 || index >= slots.Length || !slots[index].Active) return;
        if (collision == null || collision.collider.GetComponentInParent<Ball>() == null) return;

        slots[index].Bump = 1f;
        Vector2 at = collision.contactCount > 0 ? collision.GetContact(0).point : slots[index].Body.position;
        if (Feel != null) Feel.Impact(at, (at - slots[index].Body.position).normalized, Burst, false);
        Bumped?.Invoke(index);
    }

    private void FixedUpdate()
    {
        if (sway <= 0f) return;

        swayTime += Time.fixedDeltaTime * SwaySpeed;
        Rect field = Layout != null ? Layout.Field : new Rect(-2.5f, -5f, 5f, 10f);
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].Active) continue;
            float x = FieldSpots.Sway(slots[i].Home.x, sway, swayTime + slots[i].Phase, field, EdgeMargin + Size * 0.5f);
            slots[i].Body.MovePosition(new Vector2(x, slots[i].Home.y));
        }
    }

    private void Update()
    {
        float step = FeelMath.Step(Time.unscaledDeltaTime);
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].Active || slots[i].Bump <= 0f) continue;
            slots[i].Bump = Mathf.Max(0f, slots[i].Bump - step / BumpSeconds);
            Scale(ref slots[i]);
        }
    }

    private void Scale(ref Slot slot)
    {
        float size = Size * (1f + 0.18f * Mathf.Sin(slot.Bump * Mathf.PI));
        float unit = Bumper != null && Bumper.bounds.size.x > 0.0001f ? size / Bumper.bounds.size.x : size;
        slot.View.transform.localScale = new Vector3(unit, unit, 1f);
    }
}
