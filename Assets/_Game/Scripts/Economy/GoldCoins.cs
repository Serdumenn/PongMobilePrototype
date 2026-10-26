using System;
using UnityEngine;

public sealed class GoldCoins : MonoBehaviour
{
    private const int SeedSalt = 0x1B873593;
    private const int PoolSize = 3;
    private const float PopSeconds = 0.25f;

    private struct Slot
    {
        public Transform Holder;
        public SpriteRenderer View;
        public float Age;
        public int Value;
        public bool Active;
    }

    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private FieldLayout Layout;
    [SerializeField] private GameFeel Feel;

    [Header("Art")]
    [SerializeField] private Sprite Coin;
    [SerializeField] private Color Sparkle = new Color(1f, 0.84f, 0.35f, 1f);

    [Header("Timing")]
    [SerializeField] private float FirstAt = 3f;
    [SerializeField] private float GapMin = 3.5f;
    [SerializeField] private float GapMax = 6f;
    [SerializeField] private float Life = 5f;
    [SerializeField, Range(0.1f, 1f)] private float EndScale = 0.45f;

    [Header("Value")]
    [SerializeField] private int Value = 5;
    [SerializeField] private int BigValue = 15;
    [SerializeField, Range(0f, 1f)] private float BigChance = 0.15f;

    [Header("Layout")]
    [SerializeField] private float Size = 0.62f;
    [SerializeField] private float BigSize = 0.82f;
    [SerializeField] private float EdgeMargin = 0.6f;
    [SerializeField] private float TopMargin = 1.4f;
    [SerializeField] private int Order = 3;

    public event Action<int> Collected;

    private readonly Slot[] coins = new Slot[PoolSize];
    private MatchRandom rng;
    private SoloGameManager.GameState lastState;
    private float runTime;
    private float nextAt;
    private bool launched;
    private bool running;

    public bool Running => running;

    public int ActiveCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < coins.Length; i++)
                if (coins[i].Active) count++;
            return count;
        }
    }

    private Ball MainBall => Game != null ? Game.Ball : null;

    private void Awake()
    {
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Layout == null) Layout = FindFirstObjectByType<FieldLayout>();
        if (Feel == null) Feel = FindFirstObjectByType<GameFeel>();

        for (int i = 0; i < coins.Length; i++)
        {
            var holder = new GameObject("coin_" + i).transform;
            holder.SetParent(transform, false);
            var trigger = holder.gameObject.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = BigSize * 0.5f;
            holder.gameObject.AddComponent<CoinTrigger>().Setup(this, i);

            var view = new GameObject("View").AddComponent<SpriteRenderer>();
            view.transform.SetParent(holder, false);
            view.sprite = Coin;
            view.sortingOrder = Order;

            coins[i].Holder = holder;
            coins[i].View = view;
            holder.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (Game != null) Game.StateChanged += OnStateChanged;
        if (MainBall != null) MainBall.Launched += OnLaunched;
    }

    private void OnDisable()
    {
        if (Game != null) Game.StateChanged -= OnStateChanged;
        if (MainBall != null) MainBall.Launched -= OnLaunched;
        Clear();
        running = false;
    }

    private void OnStateChanged(SoloGameManager.GameState state)
    {
        if (state == SoloGameManager.GameState.Playing && lastState != SoloGameManager.GameState.Paused) StartRun();
        else if (state == SoloGameManager.GameState.Menu || state == SoloGameManager.GameState.GameOver)
        {
            Clear();
            running = false;
        }

        lastState = state;
    }

    private void StartRun()
    {
        Clear();
        var mode = Game.RunMode;
        running = mode != null && mode.Kind == GameModeKind.Arcade;
        rng = new MatchRandom(unchecked(Game.RunSeed ^ SeedSalt));
        runTime = 0f;
        nextAt = FirstAt;
        launched = false;
    }

    private void OnLaunched()
    {
        if (running) launched = true;
    }

    private void Update()
    {
        if (!running || Game == null || Game.State != SoloGameManager.GameState.Playing) return;

        float step = Time.deltaTime;
        if (launched) runTime += step;

        for (int i = 0; i < coins.Length; i++)
            if (coins[i].Active) Animate(ref coins[i], step);

        if (!launched || runTime < nextAt || rng == null) return;

        Rect area = Area();
        bool big = rng.Value() < BigChance;
        Spawn(new Vector2(rng.Range(area.xMin, area.xMax), rng.Range(area.yMin, area.yMax)), big);
        nextAt = runTime + rng.Range(GapMin, GapMax);
    }

    public Rect Area()
    {
        Rect field = Layout != null ? Layout.Field : new Rect(-2.5f, -5f, 5f, 10f);
        float xMin = field.xMin + EdgeMargin;
        float xMax = Mathf.Max(xMin, field.xMax - EdgeMargin);
        float yMin = field.center.y - 1f;
        float yMax = Mathf.Max(yMin, field.yMax - TopMargin);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    public bool Spawn(Vector2 at, bool big)
    {
        for (int i = 0; i < coins.Length; i++)
        {
            if (coins[i].Active) continue;

            coins[i].Active = true;
            coins[i].Age = 0f;
            coins[i].Value = big ? BigValue : Value;
            coins[i].Holder.position = new Vector3(at.x, at.y, 0f);
            coins[i].Holder.gameObject.SetActive(true);
            Animate(ref coins[i], 0f);
            return true;
        }

        return false;
    }

    public void Touched(int index, Collider2D other)
    {
        if (index < 0 || index >= coins.Length || !coins[index].Active) return;
        if (other == null || MainBall == null || other.GetComponentInParent<Ball>() != MainBall) return;

        Take(index);
    }

    public void TakeAll()
    {
        for (int i = 0; i < coins.Length; i++)
            if (coins[i].Active) Take(i);
    }

    private void Take(int index)
    {
        int value = coins[index].Value;
        Vector2 at = coins[index].Holder.position;
        Hide(ref coins[index]);

        if (Feel != null)
        {
            Feel.Flash(at);
            Feel.Impact(at, Vector2.up, Sparkle, false);
        }

        Collected?.Invoke(value);
    }

    private void Animate(ref Slot coin, float step)
    {
        coin.Age += step;
        if (coin.Age >= Life)
        {
            Hide(ref coin);
            return;
        }

        float pop = FeelMath.Overshoot(coin.Age / PopSeconds);
        float shrink = Mathf.Lerp(1f, EndScale, Mathf.Clamp01((coin.Age - PopSeconds) / Mathf.Max(0.01f, Life - PopSeconds)));
        float size = (coin.Value > Value ? BigSize : Size) * pop * shrink;
        float unit = Coin != null && Coin.bounds.size.x > 0.0001f ? size / Coin.bounds.size.x : size;
        coin.View.transform.localScale = new Vector3(unit, unit, 1f);
        coin.View.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(coin.Age * 4f) * 8f);

        float fade = Mathf.Clamp01((Life - coin.Age) / 0.6f);
        coin.View.color = new Color(1f, 1f, 1f, fade);
    }

    private static void Hide(ref Slot coin)
    {
        coin.Active = false;
        if (coin.Holder != null) coin.Holder.gameObject.SetActive(false);
    }

    public void Clear()
    {
        for (int i = 0; i < coins.Length; i++)
            if (coins[i].Active) Hide(ref coins[i]);
    }
}
