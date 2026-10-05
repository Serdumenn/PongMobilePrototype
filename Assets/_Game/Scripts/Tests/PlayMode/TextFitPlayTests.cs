using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

public sealed class TextFitPlayTests : GameSceneFixture
{
    private const string LongText = "Tägliche Herausforderung";
    private const string TextClass = "card__title";

    private VisualElement stage;

    private static VisualElement Root => Object.FindFirstObjectByType<UIDocument>().rootVisualElement;

    private static IEnumerator Frames(int count)
    {
        for (int i = 0; i < count; i++) yield return null;
    }

    private VisualElement Stage(float width, FlexDirection direction)
    {
        stage?.RemoveFromHierarchy();
        stage = new VisualElement();
        stage.style.position = Position.Absolute;
        stage.style.left = 0;
        stage.style.top = 0;
        stage.style.width = width;
        stage.style.flexDirection = direction;
        stage.style.alignItems = Align.Center;
        Root.Add(stage);
        return stage;
    }

    private static Label Text(VisualElement parent, string text)
    {
        var label = new Label(text);
        label.AddToClassList(TextClass);
        label.style.whiteSpace = WhiteSpace.NoWrap;
        parent.Add(label);
        return label;
    }

    private static float Natural(TextElement label)
    {
        return label.MeasureTextSize(label.text, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x;
    }

    private IEnumerator Measure(string text, System.Action<float, float> result)
    {
        var probe = Stage(4000f, FlexDirection.Column);
        probe.AddToClassList(TextFit.SkipClass);
        var label = Text(probe, text);
        yield return Frames(3);
        result(Natural(label), label.resolvedStyle.fontSize);
        probe.RemoveFromHierarchy();
    }

    [TearDown]
    public void RemoveStage()
    {
        stage?.RemoveFromHierarchy();
    }

    [UnityTest]
    public IEnumerator LongText_ShrinksToItsBox_AndGrowsBackWhenShorter()
    {
        yield return LoadGame();
        float natural = 0f, baseSize = 0f;
        yield return Measure(LongText, (w, s) => { natural = w; baseSize = s; });

        var label = Text(Stage(natural / 1.3f, FlexDirection.Column), LongText);
        yield return Frames(6);

        Assert.Less(label.resolvedStyle.fontSize, baseSize, "The long text is made smaller");
        Assert.GreaterOrEqual(label.resolvedStyle.fontSize, Mathf.Ceil(baseSize * TextFit.MinScale), "It never goes below the floor");
        Assert.LessOrEqual(Natural(label), label.contentRect.width + 1f, "The long text now fits its box");

        label.text = "Daily";
        yield return Frames(6);
        Assert.AreEqual(baseSize, label.resolvedStyle.fontSize, 0.01f, "A short text goes back to the normal size");
        Assert.AreEqual(StyleKeyword.Null, label.style.fontSize.keyword, "No size is left on the element");
    }

    [UnityTest]
    public IEnumerator TextNextToAnIcon_LeavesRoomForTheIcon()
    {
        yield return LoadGame();
        float natural = 0f;
        yield return Measure(LongText, (w, s) => natural = w);

        var row = Stage(natural, FlexDirection.Row);
        var icon = new VisualElement();
        icon.style.width = natural * 0.2f;
        icon.style.height = 40f;
        icon.style.flexShrink = 0f;
        row.Add(icon);
        var label = Text(row, LongText);
        label.style.flexShrink = 0f;
        yield return Frames(8);

        Assert.LessOrEqual(icon.layout.width + Natural(label), row.contentRect.width + 1f, "Icon and text fit the row together");
    }

    [UnityTest]
    public IEnumerator EmptyNeighbour_StillLetsTheRowFit()
    {
        yield return LoadGame();
        float natural = 0f;
        yield return Measure(LongText, (w, s) => natural = w);

        var row = Stage(natural, FlexDirection.Row);
        var label = Text(row, LongText);
        label.style.flexGrow = 1f;
        var empty = Text(row, string.Empty);
        empty.style.width = natural * 0.2f;
        yield return Frames(8);

        Assert.LessOrEqual(Natural(label) + empty.layout.width, row.contentRect.width + 1f, "An empty label next to the text does not stop the fit");
    }

    [UnityTest]
    public IEnumerator Tabs_ShareOneSize()
    {
        yield return LoadGame();
        float natural = 0f, baseSize = 0f;
        yield return Measure(LongText, (w, s) => { natural = w; baseSize = s; });

        var tabs = Stage(natural * 1.5f, FlexDirection.Row);
        tabs.AddToClassList(TextFit.GroupClass);
        var shortTab = Text(tabs, "Online");
        var longTab = Text(tabs, LongText);
        foreach (var tab in new[] { shortTab, longTab })
        {
            tab.style.flexGrow = 1f;
            tab.style.flexBasis = 0f;
        }
        yield return Frames(8);

        Assert.Less(longTab.resolvedStyle.fontSize, baseSize, "The long tab is made smaller");
        Assert.AreEqual(longTab.resolvedStyle.fontSize, shortTab.resolvedStyle.fontSize, 0.01f, "Both tabs use the same size");
        Assert.LessOrEqual(Natural(longTab), longTab.contentRect.width + 1f, "The long tab fits");
    }

    [UnityTest]
    public IEnumerator WrappingAndNameTexts_AreLeftAlone()
    {
        yield return LoadGame();
        float baseSize = 0f;
        yield return Measure(LongText, (w, s) => baseSize = s);

        var box = Stage(200f, FlexDirection.Column);
        var wrapping = Text(box, LongText);
        wrapping.style.whiteSpace = WhiteSpace.Normal;
        var name = Text(box, LongText);
        name.style.overflow = Overflow.Hidden;
        name.style.textOverflow = TextOverflow.Ellipsis;
        yield return Frames(8);

        Assert.AreEqual(baseSize, wrapping.resolvedStyle.fontSize, 0.01f, "Wrapping text keeps its size");
        Assert.AreEqual(baseSize, name.resolvedStyle.fontSize, 0.01f, "Names keep their size and get an ellipsis");
    }
}
