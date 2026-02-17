using System.Collections;
using UnityEngine;

public class SoloBall : MonoBehaviour
{
    [Header("Launch")]
    [SerializeField] private float LaunchSpeed = 8f;
    [SerializeField] private float MaxSpeed = 16f;
    [SerializeField] private float SpeedIncreasePercent = 0.02f;
    [SerializeField] private float ResetDelay = 0.75f;

    [Header("Stability")]
    [SerializeField] private float MinVerticalDot = 0.30f;

    [Tooltip("Cooldown used for wall speed-ups (prevents too fast acceleration on jittery contacts).")]
    [SerializeField] private float SpeedUpCooldown = 0.12f;

    [Header("Variation (Optional)")]
    [SerializeField, Range(0f, 1f)] private float PaddleAimStrength = 0.6f;

    [Header("Anti-Stick")]
    [SerializeField] private float PaddleSeparation = 0.03f;

    [Header("Serve Point (IMPORTANT)")]
    [SerializeField] private Transform ServePoint;

    [Header("Effects (Optional)")]
    [SerializeField] private GameObject TrailEffect;
    [SerializeField] private AudioSource BounceSound;

    [Header("Refs")]
    [SerializeField] private SoloScoreManager Score;
    [SerializeField] private SoloGameManager Game;

    [Header("Control")]
    [Tooltip("If true, the ball launches automatically when enabled. For a MenuPanel-first flow, keep this OFF and let SoloGameManager call StartRound().")]
    [SerializeField] private bool AutoStart = false;

    private Rigidbody2D Rb;
    private Collider2D Col;
    private SpriteRenderer Sr;

    private Vector2 InitialPos;
    private Coroutine ResetRoutine;
    private bool IsStopping;

    private float LastWallSpeedUpTime;
    private float LastRacketHitTime;

    private const float RacketHitCooldown = 0.05f;

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Col = GetComponent<Collider2D>();
        Sr = GetComponent<SpriteRenderer>();

        if (Score == null) Score = FindFirstObjectByType<SoloScoreManager>();
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();

        InitialPos = (ServePoint != null) ? (Vector2)ServePoint.position : Rb.position;

        Rb.gravityScale = 0f;
        Rb.freezeRotation = true;
        Rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        Rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Start()
    {
        if (AutoStart) StartRound();
        else StopAndHide();
    }

    private void OnDisable()
    {
        if (ResetRoutine != null)
        {
            StopCoroutine(ResetRoutine);
            ResetRoutine = null;
        }
    }

    private void FixedUpdate()
    {
        if (IsStopping) return;
        ClampMaxSpeed();
        EnforceMinVertical();
    }

    public void StartRound()
    {
        IsStopping = false;

        if (ResetRoutine != null) StopCoroutine(ResetRoutine);
        ResetRoutine = StartCoroutine(ResetAndServe());
    }

    private IEnumerator ResetAndServe()
    {
        Rb.linearVelocity = Vector2.zero;
        Rb.angularVelocity = 0f;
        Rb.simulated = false;

        Col.enabled = false;
        if (Sr != null) Sr.enabled = false;
        if (TrailEffect != null) TrailEffect.SetActive(false);

        Vector2 ServePos = (ServePoint != null) ? (Vector2)ServePoint.position : InitialPos;
        Rb.position = ServePos;

        yield return new WaitForSeconds(ResetDelay);

        if (Score != null && Score.IsGameOver) yield break;

        if (Sr != null) Sr.enabled = true;
        Col.enabled = true;
        Rb.simulated = true;

        LaunchDownRandom();
        if (TrailEffect != null) TrailEffect.SetActive(true);
    }

    private void LaunchDownRandom()
    {
        float Angle = Random.Range(25f, 55f);
        float SignX = Random.value < 0.5f ? -1f : 1f;

        Vector2 Dir = (Vector2)(Quaternion.Euler(0, 0, Angle * SignX) * Vector2.down);

        if (Mathf.Abs(Dir.y) < MinVerticalDot)
            Dir = new Vector2(Dir.x, Mathf.Sign(Dir.y == 0 ? -1f : Dir.y) * MinVerticalDot).normalized;

        Rb.linearVelocity = Dir.normalized * LaunchSpeed;
    }

    private void OnCollisionEnter2D(Collision2D C)
    {
        if (IsStopping || C == null || C.collider == null) return;

        if (C.collider.CompareTag("bottom"))
        {
            HandleBottomHit();
            return;
        }

        if (C.collider.CompareTag("wall"))
        {
            PlayBounce();
            TryWallSpeedUp();
            return;
        }

        if (C.collider.CompareTag("racket"))
        {
            HandleRacketHit(C);
            return;
        }
    }

    private void OnTriggerEnter2D(Collider2D Other)
    {
        if (IsStopping || Other == null) return;

        if (Other.CompareTag("bottom"))
            HandleBottomHit();
    }

    private void HandleBottomHit()
    {
        if (Score != null) Score.GameOver();
        if (Game != null) Game.GameOver();
        StopAndHide();
    }

    private void HandleRacketHit(Collision2D C)
    {
        if (Time.time - LastRacketHitTime < RacketHitCooldown) return;
        LastRacketHitTime = Time.time;

        PlayBounce();

        if (Score != null) Score.AddPoint();

        Vector2 V0 = Rb.linearVelocity;
        float Spd = V0.magnitude;

        Vector2 N = C.GetContact(0).normal;
        Vector2 Dir = Vector2.Reflect((Spd > 0.001f ? V0 / Spd : Vector2.down), N);

        Dir.y = Mathf.Abs(Dir.y);

        if (PaddleAimStrength > 0f)
        {
            Collider2D Paddle = C.collider;
            float BallX = Rb.position.x;

            Bounds Bounds = Paddle.bounds;
            float Half = Mathf.Max(0.0001f, Bounds.extents.x);
            float Offset01 = Mathf.Clamp((BallX - Bounds.center.x) / Half, -1f, 1f);

            Dir.x += Offset01 * (PaddleAimStrength * 0.35f);
        }

        Dir = Dir.normalized;
        if (Dir.y < MinVerticalDot)
            Dir = new Vector2(Dir.x, MinVerticalDot).normalized;

        Rb.linearVelocity = Dir * Mathf.Clamp(Spd, 0f, MaxSpeed);

        SeparateFromPaddle(C.collider);
    }

    private void SeparateFromPaddle(Collider2D Paddle)
    {
        if (Paddle == null) return;
        float YTop = Paddle.bounds.max.y + PaddleSeparation;
        Rb.position = new Vector2(Rb.position.x, YTop);
    }

    private void TryWallSpeedUp()
    {
        if (SpeedIncreasePercent <= 0f) return;
        if (Time.time - LastWallSpeedUpTime < SpeedUpCooldown) return;

        LastWallSpeedUpTime = Time.time;

        Vector2 V = Rb.linearVelocity;
        float Spd = V.magnitude;
        if (Spd <= 0.001f) return;

        float NewSpd = Mathf.Min(MaxSpeed, Spd * (1f + SpeedIncreasePercent));
        Rb.linearVelocity = V.normalized * NewSpd;
    }

    private void ClampMaxSpeed()
    {
        Vector2 V = Rb.linearVelocity;
        float Spd = V.magnitude;
        if (Spd > MaxSpeed && Spd > 0.001f)
            Rb.linearVelocity = V / Spd * MaxSpeed;
    }

    private void EnforceMinVertical()
    {
        Vector2 V = Rb.linearVelocity;
        float Spd = V.magnitude;
        if (Spd <= 0.001f) return;

        Vector2 Dir = V / Spd;
        float AbsY = Mathf.Abs(Dir.y);
        if (AbsY >= MinVerticalDot) return;

        float SignY = Mathf.Sign(Dir.y);
        if (SignY == 0f) SignY = 1f;

        Vector2 NewDir = new Vector2(Dir.x, SignY * MinVerticalDot).normalized;
        Rb.linearVelocity = NewDir * Spd;
    }

    private void PlayBounce()
    {
        if (BounceSound != null) BounceSound.Play();
    }

    public void StopAndHide()
    {
        IsStopping = true;

        if (ResetRoutine != null)
        {
            StopCoroutine(ResetRoutine);
            ResetRoutine = null;
        }

        Rb.linearVelocity = Vector2.zero;
        Rb.angularVelocity = 0f;
        Rb.simulated = false;

        Col.enabled = false;
        if (Sr != null) Sr.enabled = false;
        if (TrailEffect != null) TrailEffect.SetActive(false);
    }
}