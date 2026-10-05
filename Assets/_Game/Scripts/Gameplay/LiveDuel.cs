using System;
using UnityEngine;

public sealed class LiveDuel : MonoBehaviour
{
    private const byte CenterX = 128;

    [Header("Refs")]
    [SerializeField] private SoloGameManager Solo;
    [SerializeField] private FieldLayout Layout;
    [SerializeField] private Ball MainBall;
    [SerializeField] private Paddle MainPaddle;
    [SerializeField] private CosmeticsService Cosmetics;
    [SerializeField] private LocalMatchController LocalMatch;

    [Header("Feel")]
    [SerializeField] private float CorrectionDecay = 18f;
    [SerializeField] private float TiltSmooth = 15f;
    [SerializeField] private float MaxTiltDeg = 22f;
    [SerializeField] private float MaxCorrection = 1.5f;

    public event Action<int, bool> Hit;
    public event Action<bool> Goal;
    public event Action<int> Desync;

    public LiveSession Session { get; private set; }
    public bool Running { get; private set; }
    public bool IsHost { get; private set; }
    public bool AwaitingMyServe => Session != null && Running && Session.State.Phase == LivePhase.Serve && Session.State.Server == MySide;
    public Vector3 BallViewPosition => ballView != null ? ballView.transform.position : Vector3.zero;
    public bool BallShown => ballView != null && ballView.enabled;
    public Vector3 MyPaddleViewPosition => myPaddle != null ? myPaddle.transform.position : Vector3.zero;
    public Vector3 OpponentPaddleViewPosition => opponentPaddle != null ? opponentPaddle.transform.position : Vector3.zero;

    private byte MySide => IsHost ? (byte)0 : (byte)1;

    private IMatchLink link;
    private SpriteRenderer ballView;
    private SpriteRenderer myPaddle;
    private SpriteRenderer opponentPaddle;
    private Sprite myBall;
    private Sprite opponentBall;
    private Vector3 ballOffset;
    private float opponentOffset;
    private float myTilt;
    private float opponentTilt;
    private byte lastX = CenterX;
    private Vector3 ballScale;
    private Vector3 paddleScale;

    private void Awake()
    {
        if (Solo == null) Solo = FindFirstObjectByType<SoloGameManager>();
        if (Layout == null) Layout = FindFirstObjectByType<FieldLayout>();
        if (MainBall == null) MainBall = Solo != null ? Solo.Ball : FindFirstObjectByType<Ball>();
        if (MainPaddle == null) MainPaddle = FindFirstObjectByType<Paddle>();
        if (Cosmetics == null) Cosmetics = FindFirstObjectByType<CosmeticsService>();
        if (LocalMatch == null) LocalMatch = FindFirstObjectByType<LocalMatchController>();
    }

    private void OnDestroy()
    {
        Detach();
    }

    public void Prepare()
    {
        if (Layout != null) Layout.SetTopology(FieldTopology.Live);
        if (Solo != null) Solo.HideGameObjects();
        if (MainBall != null) MainBall.StopRound();
        if (MainPaddle != null)
        {
            MainPaddle.ResetToDefault();
            MainPaddle.InputEnabled = false;
        }
        HideSolo();
    }

    private void HideSolo()
    {
        if (MainBall != null) MainBall.SetBallVisible(false);
        if (MainPaddle != null) MainPaddle.SetVisible(false);
    }

    public void Begin(IMatchLink matchLink, int seed, bool hostServes, int pointsToWin, string myLook, string opponentLook)
    {
        Detach();
        link = matchLink;
        IsHost = link != null && link.IsHost;

        Session = new LiveSession(seed, hostServes, IsHost, pointsToWin, data => link?.SendLive(data));
        Session.Predicted += OnPredicted;
        Session.ConfirmedEvent += OnConfirmed;
        Session.DesyncDetected += OnDesync;
        if (link != null) link.LiveReceived += OnLive;

        Running = false;
        lastX = CenterX;
        ballOffset = Vector3.zero;
        opponentOffset = 0f;
        myTilt = 0f;
        opponentTilt = 0f;

        Prepare();
        EnsureViews();
        myBall = BallSprite(myLook);
        opponentBall = BallSprite(opponentLook);
        if (myPaddle != null) myPaddle.sprite = PaddleSprite(true);
        if (opponentPaddle != null) opponentPaddle.sprite = PaddleSprite(false);
        Render(0f);
        SetViews(true);
    }

    public void Go()
    {
        if (Session != null) Running = true;
    }

    public void Freeze()
    {
        Running = false;
        if (ballView != null) ballView.enabled = false;
    }

    public void End()
    {
        Running = false;
        Detach();
        Session = null;
        SetViews(false);
        if (MainPaddle != null) MainPaddle.SetVisible(true);
    }

    private void Detach()
    {
        if (link != null) link.LiveReceived -= OnLive;
        link = null;

        if (Session == null) return;
        Session.Predicted -= OnPredicted;
        Session.ConfirmedEvent -= OnConfirmed;
        Session.DesyncDetected -= OnDesync;
    }

    private void OnLive(byte[] data)
    {
        Session?.Receive(data);
    }

    private void Update()
    {
        if (Session == null) return;

        HideSolo();
        if (Running) Session.Update(Time.unscaledDeltaTime, ReadInput());
        else Session.Pump();

        Render(Time.unscaledDeltaTime);
    }

    private float Scale => Layout != null && Layout.Field.width > 0f ? Layout.Field.width / (LiveSim.HalfWidth.ToFloat() * 2f) : 1f;

    private Vector3 ToWorld(float x, float y)
    {
        if (!IsHost)
        {
            x = -x;
            y = -y;
        }

        Vector2 center = Layout != null ? Layout.Field.center : Vector2.zero;
        float s = Scale;
        return new Vector3(center.x + x * s, center.y + y * s, 0f);
    }

    private ushort ReadInput()
    {
        bool touch = false;
        var cam = Camera.main;
        if (MainPaddle != null && MainPaddle.Controller != null && cam != null && MainPaddle.Controller.TryGetTarget(MainPaddle, cam, out float worldX))
        {
            touch = true;
            float center = Layout != null ? Layout.Field.center.x : 0f;
            float courtX = (worldX - center) / Mathf.Max(0.01f, Scale);
            if (!IsHost) courtX = -courtX;
            lastX = LiveInput.FromCourtX(courtX);
        }

        return LiveInput.Pack(lastX, touch);
    }

    private void Render(float deltaTime)
    {
        if (Session == null || ballView == null) return;

        var state = Session.State;
        float decay = Mathf.Exp(-CorrectionDecay * deltaTime);

        if (Session.Corrected)
        {
            var before = Session.BeforeCorrection;
            if (before.BallVisible && state.BallVisible)
            {
                Vector3 offset = ToWorld(before.BallX.ToFloat(), before.BallY.ToFloat()) - ToWorld(state.BallX.ToFloat(), state.BallY.ToFloat()) + ballOffset;
                ballOffset = offset.magnitude < MaxCorrection * Scale ? offset : Vector3.zero;
            }

            float oldX = (IsHost ? before.GuestX : before.HostX).ToFloat();
            float newX = (IsHost ? state.GuestX : state.HostX).ToFloat();
            opponentOffset += (oldX - newX) * Scale * (IsHost ? 1f : -1f);
        }

        ballOffset *= decay;
        opponentOffset *= decay;

        bool ballShown = state.BallVisible && (Running || state.Phase == LivePhase.Serve);
        ballView.enabled = ballShown;
        if (ballShown)
        {
            ballView.transform.position = ToWorld(state.BallX.ToFloat(), state.BallY.ToFloat()) + ballOffset;
            ballView.sprite = state.LastHitter == MySide ? myBall : opponentBall;
        }
        else ballOffset = Vector3.zero;

        float paddleY = LiveSim.PaddleY.ToFloat();
        Fix mine = IsHost ? state.HostX : state.GuestX;
        Fix theirs = IsHost ? state.GuestX : state.HostX;
        Fix myVel = IsHost ? state.HostVel : -state.GuestVel;
        Fix theirVel = IsHost ? state.GuestVel : -state.HostVel;

        myPaddle.transform.position = ToWorld(mine.ToFloat(), IsHost ? -paddleY : paddleY);
        Vector3 opponentPos = ToWorld(theirs.ToFloat(), IsHost ? paddleY : -paddleY);
        opponentPaddle.transform.position = opponentPos + new Vector3(opponentOffset, 0f, 0f);

        float k = deltaTime > 0f ? Mathf.Clamp01(TiltSmooth * deltaTime) : 1f;
        myTilt = Mathf.Lerp(myTilt, -Mathf.Clamp(myVel.ToFloat() / 10f, -1f, 1f) * MaxTiltDeg, k);
        opponentTilt = Mathf.Lerp(opponentTilt, Mathf.Clamp(theirVel.ToFloat() / 10f, -1f, 1f) * MaxTiltDeg, k);
        myPaddle.transform.rotation = Quaternion.Euler(0f, 0f, myTilt);
        opponentPaddle.transform.rotation = Quaternion.Euler(0f, 0f, 180f + opponentTilt);

        float s = Scale;
        ballView.transform.localScale = ballScale * s;
        myPaddle.transform.localScale = paddleScale * s;
        opponentPaddle.transform.localScale = paddleScale * s;
    }

    private void OnPredicted(LiveEvents events)
    {
        if (!Running) return;

        if (events.HostHit || events.GuestHit)
        {
            bool mine = IsHost ? events.HostHit : events.GuestHit;
            if (mine) HapticManager.Light();
            Hit?.Invoke(Session.State.Rally, mine);
        }

        if (events.Wall) AudioManager.PlayOne(Sfx.WallBounce);
    }

    private void OnConfirmed(LiveSession.Confirmed confirmed)
    {
        if (confirmed.Missed == 0) return;

        bool hostMissed = confirmed.Missed == 1;
        if (hostMissed == IsHost) HapticManager.Medium();
        Goal?.Invoke(hostMissed);
    }

    private void OnDesync(int atTick)
    {
        Debug.LogWarning($"Live duel desync at tick {atTick}. The host's state wins.");
        Desync?.Invoke(atTick);
    }

    private void EnsureViews()
    {
        if (ballView != null) return;

        var parent = MainBall != null ? MainBall.transform.parent : transform;
        var ballSource = MainBall != null ? MainBall.GetComponentInChildren<SpriteRenderer>() : null;
        var paddleSource = MainPaddle != null ? MainPaddle.Visual : null;

        ballView = CreateView("live_ball", parent, ballSource, 0);
        myPaddle = CreateView("live_paddle_me", parent, paddleSource, 0);
        opponentPaddle = CreateView("live_paddle_them", parent, paddleSource, 0);
        Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
        ballScale = Relative(ballSource != null ? ballSource.transform.lossyScale : Vector3.one, parentScale);
        paddleScale = Relative(paddleSource != null ? paddleSource.transform.lossyScale : Vector3.one, parentScale);
    }

    private static Vector3 Relative(Vector3 scale, Vector3 parent)
    {
        return new Vector3(scale.x / Mathf.Max(0.0001f, parent.x), scale.y / Mathf.Max(0.0001f, parent.y), 1f);
    }

    private static SpriteRenderer CreateView(string name, Transform parent, SpriteRenderer source, int orderOffset)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        var view = go.AddComponent<SpriteRenderer>();
        if (source != null)
        {
            view.sprite = source.sprite;
            view.sortingLayerID = source.sortingLayerID;
            view.sortingOrder = source.sortingOrder + orderOffset;
        }
        view.enabled = false;
        return view;
    }

    private void SetViews(bool visible)
    {
        if (ballView != null) ballView.enabled = visible && Session != null && Session.State.BallVisible;
        if (myPaddle != null) myPaddle.enabled = visible;
        if (opponentPaddle != null) opponentPaddle.enabled = visible;
    }

    private Sprite BallSprite(string lookId)
    {
        var catalog = Cosmetics != null ? Cosmetics.CatalogAsset : null;
        var look = catalog != null && !string.IsNullOrEmpty(lookId) ? catalog.Find(lookId) : null;
        if (look == null && Cosmetics != null) look = Cosmetics.Equipped(CosmeticCategory.Ball);
        if (look != null && look.Idle != null) return look.Idle;
        var source = MainBall != null ? MainBall.GetComponentInChildren<SpriteRenderer>() : null;
        return source != null ? source.sprite : null;
    }

    private Sprite PaddleSprite(bool mine)
    {
        if (mine)
        {
            var equipped = Cosmetics != null ? Cosmetics.Equipped(CosmeticCategory.Paddle) : null;
            if (equipped != null && equipped.PaddleSprite != null) return equipped.PaddleSprite;
        }
        else if (LocalMatch != null)
        {
            var equipped = Cosmetics != null ? Cosmetics.Equipped(CosmeticCategory.Paddle) : null;
            string avoid = equipped != null && equipped.Id.Contains("coral") ? "coral" : "teal";
            string wanted = avoid == "coral" ? "teal" : "coral";
            Sprite fallback = null;
            foreach (FieldSide side in Enum.GetValues(typeof(FieldSide)))
            {
                var player = LocalMatch.PaddleSpriteFor(side);
                if (player == null) continue;
                if (player.name.Contains(wanted)) return player;
                if (fallback == null && !player.name.Contains(avoid)) fallback = player;
            }
            if (fallback != null) return fallback;
        }

        return MainPaddle != null && MainPaddle.Visual != null ? MainPaddle.Visual.sprite : null;
    }
}
