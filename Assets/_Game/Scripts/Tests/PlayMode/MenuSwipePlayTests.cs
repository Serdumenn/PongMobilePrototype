using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public sealed class MenuSwipePlayTests : GameSceneFixture
{
    private VisualElement menu;
    private VisualElement strip;

    private IEnumerator Load()
    {
        yield return LoadGame();
        menu = Object.FindFirstObjectByType<UIDocument>().rootVisualElement.Q("menu");
        strip = menu.Q("mode-strip");
        Assert.IsNotNull(strip, "The menu has no mode strip");
        yield return null;
    }

    private void Send(EventType type, Vector2 at)
    {
        var source = new Event { type = type, mousePosition = at, button = 0, clickCount = 1 };
        EventBase e = type switch
        {
            EventType.MouseDown => PointerDownEvent.GetPooled(source),
            EventType.MouseDrag => PointerMoveEvent.GetPooled(source),
            _ => PointerUpEvent.GetPooled(source)
        };
        e.target = strip;
        strip.SendEvent(e);
        e.Dispose();
    }

    private IEnumerator Drag(float dx, float dy = 0f)
    {
        Vector2 from = strip.worldBound.center;
        Send(EventType.MouseDown, from);
        yield return null;
        Send(EventType.MouseDrag, from + new Vector2(dx * 0.5f, dy * 0.5f));
        yield return null;
        Send(EventType.MouseDrag, from + new Vector2(dx, dy));
        yield return new WaitForSecondsRealtime(0.15f);
        Send(EventType.MouseUp, from + new Vector2(dx, dy));
        yield return null;
    }

    private IEnumerator Tap(Vector2 at)
    {
        Send(EventType.MouseDown, at);
        yield return null;
        Send(EventType.MouseUp, at);
        yield return null;
    }

    private int ActiveDot()
    {
        var dots = menu.Q("mode-dots");
        for (int i = 0; i < dots.childCount; i++)
            if (dots[i].ClassListContains("mode-dot--active")) return i;
        return -1;
    }

    private int IndexOfCurrent()
    {
        for (int i = 0; i < Game.ModeList.Count; i++)
            if (Game.ModeList[i] == Game.CurrentMode) return i;
        return -1;
    }

    [UnityTest]
    public IEnumerator Arrows_AreGone()
    {
        yield return Load();
        Assert.IsNull(menu.Q("mode-prev"));
        Assert.IsNull(menu.Q("mode-next"));
        Assert.IsNotNull(menu.Q("mode-card-prev"));
        Assert.IsNotNull(menu.Q("mode-card-next"));
    }

    [UnityTest]
    public IEnumerator DragLeft_SelectsTheNextMode()
    {
        yield return Load();
        int start = IndexOfCurrent();
        yield return Drag(-320f);
        Assert.AreEqual(Game.ModeList[(start + 1) % Game.ModeList.Count], Game.CurrentMode);

        yield return new WaitForSecondsRealtime(0.6f);
        yield return Drag(320f);
        Assert.AreEqual(Game.ModeList[start], Game.CurrentMode);
    }

    [UnityTest]
    public IEnumerator ShortDrag_KeepsTheMode()
    {
        yield return Load();
        var mode = Game.CurrentMode;
        yield return Drag(-80f);
        Assert.AreEqual(mode, Game.CurrentMode, "A short drag switched the mode");
        yield return Drag(6f, 140f);
        Assert.AreEqual(mode, Game.CurrentMode, "A vertical move switched the mode");
    }

    [UnityTest]
    public IEnumerator TapOnPeek_SelectsThatMode()
    {
        yield return Load();
        int start = IndexOfCurrent();
        var right = menu.Q("mode-card-next").worldBound;
        yield return Tap(new Vector2(strip.worldBound.xMax - 30f, right.center.y));
        Assert.AreEqual(Game.ModeList[(start + 1) % Game.ModeList.Count], Game.CurrentMode);

        yield return new WaitForSecondsRealtime(0.6f);
        yield return Tap(new Vector2(strip.worldBound.xMin + 30f, right.center.y));
        Assert.AreEqual(Game.ModeList[start], Game.CurrentMode);

        yield return Tap(strip.worldBound.center);
        Assert.AreEqual(Game.ModeList[start], Game.CurrentMode, "A tap on the centre card must not switch");
    }

    [UnityTest]
    public IEnumerator ThreeQuickSwipes_KeepCardDotsAndWorldTogether()
    {
        yield return Load();
        int start = IndexOfCurrent();
        int entries = menu.Q("mode-dots").childCount;
        for (int i = 0; i < 3; i++) yield return Drag(-320f);

        int expected = (start + 3) % entries;
        var backdrop = Object.FindFirstObjectByType<BackdropDirector>();
        Assert.AreEqual(expected, ActiveDot());
        if (expected < Game.ModeList.Count)
        {
            Assert.AreEqual(Game.ModeList[expected], Game.CurrentMode);
            Assert.AreEqual(Game.CurrentMode.Title, menu.Q<Label>("mode-name").text);
            Assert.AreEqual(BackdropScenes.For(Game.CurrentMode.Kind), backdrop.Wanted);
        }
        else
        {
            Assert.AreEqual("Together", menu.Q<Label>("mode-name").text);
            Assert.AreEqual(BackdropScene.Together, backdrop.Wanted);
        }
    }

    [UnityTest]
    public IEnumerator Hint_HidesAfterTheFirstSwipe()
    {
        yield return Load();
        var hint = menu.Q("swipe-hint");
        Assert.IsFalse(hint.ClassListContains("swipe-hint--hidden"), "First-time players should see the swipe hint");

        yield return Drag(-320f);
        Assert.IsTrue(hint.ClassListContains("swipe-hint--hidden"));
        Assert.IsTrue(GameSettings.SwipeHintDone);
    }
}
