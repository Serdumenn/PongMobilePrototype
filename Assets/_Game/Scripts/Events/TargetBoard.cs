using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TargetBoard : MonoBehaviour
{
    private const int PoolSize = 7;
    private const float PopSeconds = 0.25f;

    private struct Slot
    {
        public Transform Holder;
        public SpriteRenderer View;
        public CircleCollider2D Trigger;
        public Vector2 Home;
        public float Phase;
        public float Age;
        public bool Active;
    }

    [Header("Refs")]
    [SerializeField] private FieldLayout Layout;
    [SerializeField] private GameFeel Feel;

    [Header("Art")]
    [SerializeField] private Sprite Target;
    [SerializeField] private Color Burst = new Color(1f, 0.42f, 0.34f, 1f);

    [Header("Layout")]
    [SerializeField] private float Size = 0.7f;
    [SerializeField] private float TopGap = 3.6f;
    [SerializeField] private float Rise = 0.45f;
    [SerializeField] private float EdgeMargin = 0.55f;
    [SerializeField] private int Order = 3;
    [SerializeField] private float SwaySpeed = 1.3f;

    public event Action<int> Hit;

    private readonly Slot[] slots = new Slot[PoolSize];
    private float sway;
    private float swayTime;
    private float scale = 1f;

    public int Remaining
    {
        get
        {
            int count = 0;
            for (int i = 0; i < slots.Length; i++)
                if (slots[i].Active) count++;
            return count;
        }
    }

    private void Awake()
    {
        if (Layout == null) Layout = FindFirstObjectByType<FieldLayout>();
        if (Feel == null) Feel = FindFirstObjectByType<GameFeel>();

        for (int i = 0; i < slots.Length; i++)
        {
            var holder = new GameObject("target_" + i).transform;
            holder.SetParent(transform, false);
            var trigger = holder.gameObject.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = Size * 0.5f;
            holder.gameObject.AddComponent<TargetTrigger>().Setup(this, i);

            var view = new GameObject("View").AddComponent<SpriteRenderer>();
            view.transform.SetParent(holder, false);
            view.sprite = Target;
            view.sortingOrder = Order;

            slots[i].Holder = holder;
            slots[i].View = view;
            slots[i].Trigger = trigger;
            holder.gameObject.SetActive(false);
        }
    }

    public void Show(int count)
    {
        Hide();
        count = Mathf.Clamp(count, 1, slots.Length);

        Rect field = Layout != null ? Layout.Field : new Rect(-2.5f, -5f, 5f, 10f);
        float left = field.xMin + EdgeMargin + Size * 0.5f;
        float right = field.xMax - EdgeMargin - Size * 0.5f;
        float top = field.yMax - TopGap;
        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (float)i / (count - 1) : 0.5f;
            float arc = 1f - Mathf.Pow(2f * t - 1f, 2f);
            Place(i, new Vector2(Mathf.Lerp(left, right, t), top - Rise * (1f - arc)));
        }
    }

    public void Show(IReadOnlyList<Vector2> spots, float swayAmount, float sizeScale = 1f)
    {
        Hide();
        scale = Mathf.Clamp(sizeScale, 0.3f, 2f);
        sway = Mathf.Max(0f, swayAmount);
        swayTime = 0f;

        Rect field = Layout != null ? Layout.Field : new Rect(-2.5f, -5f, 5f, 10f);
        float edge = EdgeMargin + Size * 0.5f;
        int count = Mathf.Min(spots.Count, slots.Length);
        for (int i = 0; i < count; i++) Place(i, FieldSpots.ToWorld(field, spots[i], edge, TopGap));
    }

    private void Place(int index, Vector2 spot)
    {
        slots[index].Active = true;
        slots[index].Age = -index * 0.06f;
        slots[index].Home = spot;
        slots[index].Phase = index * 1.7f;
        slots[index].Holder.position = spot;
        slots[index].Trigger.radius = Size * scale * 0.5f;
        slots[index].Holder.gameObject.SetActive(true);
        Animate(ref slots[index], 0f);
    }

    public void Hide()
    {
        sway = 0f;
        scale = 1f;
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].Active = false;
            if (slots[i].Holder != null) slots[i].Holder.gameObject.SetActive(false);
        }
    }

    public void Touched(int index, Collider2D other)
    {
        if (index < 0 || index >= slots.Length || !slots[index].Active) return;
        if (other == null || other.GetComponentInParent<Ball>() == null) return;

        Take(index);
    }

    public void TakeAll()
    {
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].Active) Take(i);
    }

    private void Take(int index)
    {
        Vector2 at = slots[index].Holder.position;
        slots[index].Active = false;
        slots[index].Holder.gameObject.SetActive(false);

        if (Feel != null)
        {
            Feel.Flash(at);
            Feel.Impact(at, Vector2.down, Burst, true);
        }

        Hit?.Invoke(index);
    }

    private void Update()
    {
        float step = FeelMath.Step(Time.unscaledDeltaTime);
        swayTime += Time.deltaTime * SwaySpeed;
        Rect field = Layout != null ? Layout.Field : new Rect(-2.5f, -5f, 5f, 10f);
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].Active) continue;

            Animate(ref slots[i], step);
            if (sway <= 0f) continue;
            float x = FieldSpots.Sway(slots[i].Home.x, sway, swayTime + slots[i].Phase, field, EdgeMargin + Size * 0.5f);
            slots[i].Holder.position = new Vector3(x, slots[i].Home.y, 0f);
        }
    }

    private void Animate(ref Slot slot, float step)
    {
        slot.Age += step;
        float pop = slot.Age <= 0f ? 0f : FeelMath.Overshoot(slot.Age / PopSeconds);
        float pulse = 1f + 0.05f * Mathf.Sin(slot.Age * 4f);
        float size = Size * scale * pop * pulse;
        float unit = Target != null && Target.bounds.size.x > 0.0001f ? size / Target.bounds.size.x : size;
        slot.View.transform.localScale = new Vector3(unit, unit, 1f);
    }
}
