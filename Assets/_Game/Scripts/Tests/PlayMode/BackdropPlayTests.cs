using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BackdropPlayTests : GameSceneFixture
{
    private BackdropDirector director;
    private CosmeticsService cosmetics;

    private IEnumerator Load()
    {
        yield return LoadGame();
        director = Object.FindFirstObjectByType<BackdropDirector>();
        cosmetics = Object.FindFirstObjectByType<CosmeticsService>();
        Assert.IsNotNull(director, "The scene has no BackdropDirector");
    }

    private static IEnumerator Settle()
    {
        yield return new WaitForSecondsRealtime(1.2f);
    }

    private void AssertOnly(BackdropScene scene, float level)
    {
        Assert.AreEqual(scene, director.Current);
        Assert.AreEqual(level, director.FadeOf(scene), 0.05f, scene + " is not showing");
        foreach (BackdropScene other in System.Enum.GetValues(typeof(BackdropScene)))
            if (other != scene && other != BackdropScene.Plain) Assert.Less(director.FadeOf(other), 0.05f, other + " is still showing");
    }

    [UnityTest]
    public IEnumerator World_FollowsTheSelectedMode()
    {
        yield return Load();
        foreach (var id in new[] { "arcade", "adventure", "classic", "rush" })
        {
            var mode = Game.FindMode(id);
            Game.SelectMode(mode);
            yield return Settle();
            AssertOnly(BackdropScenes.For(mode.Kind), 1f);
            Assert.Greater(director.WorldFor(director.Current).VisibleCount, 0);
        }
    }

    private static void Press(UnityEngine.UIElements.Button button)
    {
        Assert.IsNotNull(button);
        using (var e = UnityEngine.UIElements.NavigationSubmitEvent.GetPooled())
        {
            e.target = button;
            button.SendEvent(e);
        }
    }

    [UnityTest]
    public IEnumerator OverlayScreens_ShowLivingBlobs()
    {
        yield return Load();
        var root = Object.FindFirstObjectByType<UnityEngine.UIElements.UIDocument>().rootVisualElement;
        var menuScene = director.Current;

        Press(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(root, "shop-button"));
        yield return Settle();
        AssertOnly(BackdropScene.Living, 1f);
        Assert.IsTrue(UnityEngine.UIElements.UQueryExtensions.Q(root, "shop").ClassListContains("screen--living"));

        Press(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(UnityEngine.UIElements.UQueryExtensions.Q(root, "shop"), "back-button"));
        yield return Settle();
        AssertOnly(menuScene, 1f);
    }

    [UnityTest]
    public IEnumerator Run_CalmsTheWorldAndMenuRestoresIt()
    {
        yield return Load();
        Game.SelectMode(Game.FindMode("rush"));
        Game.StartGameFromMenu();
        yield return Settle();
        Assert.IsTrue(director.Calm);
        AssertOnly(BackdropScene.Rush, director.CalmLevel);
        Assert.Less(director.FadeOf(BackdropScene.Rush), 0.45f);

        Game.ReturnToMenu();
        yield return Settle();
        Assert.IsFalse(director.Calm);
        AssertOnly(BackdropScene.Rush, 1f);
    }

    private static void Pointer(UnityEngine.UIElements.VisualElement target, EventType type, Vector2 at)
    {
        var source = new Event { type = type, mousePosition = at, button = 0, clickCount = 1 };
        UnityEngine.UIElements.EventBase e = type == EventType.MouseDown ? (UnityEngine.UIElements.EventBase)UnityEngine.UIElements.PointerDownEvent.GetPooled(source)
            : type == EventType.MouseDrag ? UnityEngine.UIElements.PointerMoveEvent.GetPooled(source) : UnityEngine.UIElements.PointerUpEvent.GetPooled(source);
        e.target = target;
        target.SendEvent(e);
        e.Dispose();
    }

    [UnityTest]
    public IEnumerator MotionOff_ShowsThePlainLookAndSwipeStillWorks()
    {
        yield return Load();
        var root = Object.FindFirstObjectByType<UnityEngine.UIElements.UIDocument>().rootVisualElement;
        var settings = UnityEngine.UIElements.UQueryExtensions.Q(root, "settings");

        Press(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(root, "settings-button"));
        yield return null;
        var toggle = UnityEngine.UIElements.UQueryExtensions.Q(settings, "motion-switch");
        Assert.IsNotNull(toggle, "Settings has no Moving background switch");
        Assert.IsTrue(toggle.ClassListContains("switch--on"));
        using (var click = UnityEngine.UIElements.ClickEvent.GetPooled())
        {
            click.target = toggle;
            toggle.SendEvent(click);
        }
        Assert.IsFalse(GameSettings.MotionBackdrop);
        Assert.IsFalse(toggle.ClassListContains("switch--on"));

        Press(UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(settings, "back-button"));
        yield return Settle();
        Assert.AreEqual(BackdropScene.Plain, director.Current);
        foreach (BackdropScene scene in System.Enum.GetValues(typeof(BackdropScene)))
            Assert.Less(director.FadeOf(scene), 0.05f, scene + " still shows with motion off");

        var mode = Game.CurrentMode;
        var strip = UnityEngine.UIElements.UQueryExtensions.Q(UnityEngine.UIElements.UQueryExtensions.Q(root, "menu"), "mode-strip");
        Vector2 from = strip.worldBound.center;
        Pointer(strip, EventType.MouseDown, from);
        yield return null;
        Pointer(strip, EventType.MouseDrag, from + new Vector2(-320f, 0f));
        yield return null;
        Pointer(strip, EventType.MouseUp, from + new Vector2(-320f, 0f));
        yield return null;
        Assert.AreNotEqual(mode, Game.CurrentMode, "Swiping must still work with motion off");
    }

    [UnityTest]
    public IEnumerator Theme_RecoloursTheWorldAtOnce()
    {
        yield return Load();
        var night = cosmetics.CatalogAsset.Find("theme_night");
        cosmetics.Inventory.Grant(night.Id);
        Assert.IsTrue(cosmetics.Equip(night));
        yield return null;

        var world = director.WorldFor(BackdropScene.Arcade);
        Assert.AreEqual(night.BlobPrimary, world.Primary);
        Assert.AreEqual(night.BlobSecondary, world.Secondary);
    }
}
