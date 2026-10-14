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
    [SerializeField] private float HappyDuration = 0.15f;

    private Ball ball;
    private float happyUntil;

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

        if (Visual.sprite == Happy && Time.unscaledTime >= happyUntil)
            SetSprite(Idle);
    }

    public void SetExpressions(Sprite idle, Sprite happy, Sprite sad)
    {
        var current = Visual != null ? Visual.sprite : null;
        bool wasHappy = current != null && current == Happy;
        bool wasSad = current != null && current == Sad;

        Idle = idle;
        Happy = happy;
        Sad = sad;

        SetSprite(wasHappy ? Happy : wasSad ? Sad : Idle);
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
