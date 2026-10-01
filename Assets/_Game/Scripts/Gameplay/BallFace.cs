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

    [Header("Punch")]
    [SerializeField] private float PunchScale = 1.18f;
    [SerializeField] private float PunchDuration = 0.14f;

    private Ball ball;
    private Vector3 baseScale;
    private float happyUntil;
    private float punchStart = -1f;

    private void Awake()
    {
        ball = GetComponent<Ball>();
        if (Visual == null) Visual = GetComponentInChildren<SpriteRenderer>();
        if (Visual != null) baseScale = Visual.transform.localScale;
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

        if (punchStart < 0f) return;

        float t = Mathf.Clamp01((Time.unscaledTime - punchStart) / Mathf.Max(0.01f, PunchDuration));
        float eased = 1f - (1f - t) * (1f - t);
        Visual.transform.localScale = baseScale * Mathf.Lerp(PunchScale, 1f, eased);

        if (t >= 1f) punchStart = -1f;
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
        punchStart = Time.unscaledTime;
    }

    private void OnMissed()
    {
        SetSprite(Sad);
        punchStart = -1f;
        if (Visual != null) Visual.transform.localScale = baseScale;
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
