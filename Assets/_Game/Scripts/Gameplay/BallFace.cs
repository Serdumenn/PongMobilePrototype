using UnityEngine;

[RequireComponent(typeof(Ball))]
public sealed class BallFace : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer Visual;

    [Header("Expressions")]
    [SerializeField] private Sprite Idle;
    [SerializeField] private Sprite Happy;
    [SerializeField] private Sprite Sad;
    [SerializeField] private Sprite Wow;
    [SerializeField] private float HappyDuration = 0.15f;
    [SerializeField, Range(0f, 1f)] private float WowHeat = 0.4f;

    private Ball ball;
    private float happyUntil;

    public bool Thrilled => Wow != null && ball != null && ball.InPlay && FeelMath.Warmth(ball.Heat) >= WowHeat;
    private Sprite Resting => Thrilled ? Wow : Idle;

    private void Awake()
    {
        ball = GetComponent<Ball>();
        if (Visual == null) Visual = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        ball.PaddleHit += OnPaddleHit;
        ball.Missed += OnMissed;
        ball.Launched += ShowIdle;
        ball.RoundReset += ShowIdle;
        ShowIdle();
    }

    private void OnDisable()
    {
        ball.PaddleHit -= OnPaddleHit;
        ball.Missed -= OnMissed;
        ball.Launched -= ShowIdle;
        ball.RoundReset -= ShowIdle;
    }

    private void Update()
    {
        if (Visual == null) return;

        var shown = Visual.sprite;
        bool resting = shown == Idle || shown == Wow;
        bool happyOver = shown == Happy && Time.unscaledTime >= happyUntil;
        if (!resting && !happyOver) return;

        var next = Resting;
        if (shown != next) SetSprite(next);
    }

    public void SetExpressions(Sprite idle, Sprite happy, Sprite sad, Sprite wow = null)
    {
        var current = Visual != null ? Visual.sprite : null;
        bool wasHappy = current != null && current == Happy;
        bool wasSad = current != null && current == Sad;

        Idle = idle;
        Happy = happy;
        Sad = sad;
        Wow = wow;

        SetSprite(wasHappy ? Happy : wasSad ? Sad : Resting);
    }

    private void OnPaddleHit()
    {
        SetSprite(Happy);
        happyUntil = Time.unscaledTime + HappyDuration;
    }

    private void OnMissed()
    {
        SetSprite(Sad);
    }

    private void ShowIdle()
    {
        SetSprite(Idle);
    }

    private void SetSprite(Sprite sprite)
    {
        if (Visual != null && sprite != null) Visual.sprite = sprite;
    }
}
