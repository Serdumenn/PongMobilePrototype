using UnityEngine;

public sealed class BackdropDirector : MonoBehaviour
{
    [Header("Worlds")]
    [SerializeField] private BackdropWorld Arcade;
    [SerializeField] private BackdropWorld Adventure;
    [SerializeField] private BackdropWorld Classic;
    [SerializeField] private BackdropWorld Rush;
    [SerializeField] private BackdropWorld Together;
    [SerializeField] private BackdropWorld Living;

    [Header("Feel")]
    [SerializeField] private float FadeSeconds = 0.65f;
    [SerializeField, Range(0f, 1f)] private float CalmFade = 0.35f;
    [SerializeField, Range(0f, 1f)] private float CalmPace = 0.3f;

    private static readonly BackdropScene[] Scenes = (BackdropScene[])System.Enum.GetValues(typeof(BackdropScene));

    private BackdropScene wanted = BackdropScene.Plain;
    private BackdropWorld[] all;

    public BackdropScene Current => GameSettings.MotionBackdrop ? wanted : BackdropScene.Plain;
    public BackdropScene Wanted => wanted;
    public bool Calm { get; set; }
    public float CalmLevel => CalmFade;

    private BackdropWorld[] All => all ??= new[] { Arcade, Adventure, Classic, Rush, Together, Living };

    public void Show(BackdropScene scene)
    {
        wanted = scene;
    }

    public void SetPalette(Color primary, Color secondary)
    {
        foreach (var world in All)
            if (world != null) world.SetPalette(primary, secondary);
    }

    public BackdropWorld WorldFor(BackdropScene scene)
    {
        return scene switch
        {
            BackdropScene.Arcade => Arcade,
            BackdropScene.Adventure => Adventure,
            BackdropScene.Classic => Classic,
            BackdropScene.Rush => Rush,
            BackdropScene.Together => Together,
            BackdropScene.Living => Living,
            _ => null
        };
    }

    public float FadeOf(BackdropScene scene)
    {
        var world = WorldFor(scene);
        return world != null ? world.Fade : 0f;
    }

    private void Update()
    {
        var current = Current;
        float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, FadeSeconds);
        foreach (var scene in Scenes)
        {
            var world = WorldFor(scene);
            if (world == null) continue;

            float target = scene == current ? (Calm ? CalmFade : 1f) : 0f;
            world.Fade = Mathf.MoveTowards(world.Fade, target, step);
            world.Pace = Calm ? CalmPace : 1f;
        }
    }
}
