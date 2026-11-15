using NUnit.Framework;
using UnityEngine.UIElements;

public sealed class FrameTests
{
    [Test]
    public void ScoreMeta_CarriesTheFrame()
    {
        Assert.AreEqual("frame_crown", OnlineScores.FrameFrom("{\"name\":\"Brave Otter\",\"frame\":\"frame_crown\"}"));
        Assert.IsTrue(string.IsNullOrEmpty(OnlineScores.FrameFrom("{\"name\":\"Brave Otter\"}")), "Older entries have no frame");
        Assert.IsTrue(string.IsNullOrEmpty(OnlineScores.FrameFrom(null)));
        Assert.IsTrue(string.IsNullOrEmpty(OnlineScores.FrameFrom("not json")));
    }

    [Test]
    public void FrameLook_AddsTheClassAndBadgeAndSwapsCleanly()
    {
        var row = new VisualElement();
        row.AddToClassList("rank-row");

        FrameLook.Apply(row, "frame--crown");
        Assert.IsTrue(row.ClassListContains(FrameLook.Framed));
        Assert.IsTrue(row.ClassListContains("frame--crown"));
        var badge = row.Q(className: "frame-badge");
        Assert.IsNotNull(badge);
        Assert.IsTrue(badge.ClassListContains("icon--crown"));

        FrameLook.Apply(row, "frame--rainbow");
        Assert.IsFalse(row.ClassListContains("frame--crown"));
        Assert.IsTrue(row.ClassListContains("frame--rainbow"));
        Assert.IsNull(row.Q(className: "frame-badge"), "The rainbow frame has no badge");

        FrameLook.Apply(row, null);
        Assert.IsFalse(row.ClassListContains(FrameLook.Framed));
        Assert.IsFalse(row.ClassListContains("frame--rainbow"));
        Assert.IsTrue(row.ClassListContains("rank-row"), "Other classes stay");
    }

    [Test]
    public void EveryFrameItem_HasAStyle()
    {
        var css = System.IO.File.ReadAllText("Assets/_Game/UI/Theme/Components.uss");
        foreach (var item in TestData.LoadCatalog().InCategory(CosmeticCategory.Frame))
        {
            if (item.Unlock == UnlockKind.Free) continue;
            StringAssert.StartsWith("frame--", item.FrameClass, item.Id);
            StringAssert.Contains("." + item.FrameClass + " ", css, item.Id);
        }
    }
}
