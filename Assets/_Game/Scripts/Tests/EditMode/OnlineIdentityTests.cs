using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

public sealed class OnlineIdentityTests
{
    [Test]
    public void PlayerNames_SameIdGivesSameName()
    {
        Assert.AreEqual(PlayerNames.ForId("aBcD1234efGH"), PlayerNames.ForId("aBcD1234efGH"));
        Assert.AreEqual(PlayerNames.ForId(null), PlayerNames.ForId(string.Empty));
    }

    [Test]
    public void PlayerNames_AreAdjectiveThenAnimal()
    {
        for (int i = 0; i < 500; i++)
        {
            string name = PlayerNames.ForId($"player-{i}");
            var parts = name.Split(' ');

            Assert.AreEqual(2, parts.Length, name);
            CollectionAssert.Contains(PlayerNames.AdjectiveList.ToList(), parts[0]);
            CollectionAssert.Contains(PlayerNames.AnimalList.ToList(), parts[1]);
            Assert.LessOrEqual(name.Length, 16, name);
        }
    }

    [Test]
    public void PlayerNames_ListsHaveNoDuplicates()
    {
        CollectionAssert.AllItemsAreUnique(PlayerNames.AdjectiveList);
        CollectionAssert.AllItemsAreUnique(PlayerNames.AnimalList);
        Assert.AreEqual(1024, PlayerNames.Combinations);
    }

    [Test]
    public void PlayerNames_SpreadAcrossCombinations()
    {
        var seen = new HashSet<string>();
        for (int i = 0; i < 20000; i++) seen.Add(PlayerNames.ForId($"id{i}"));

        Assert.Greater(seen.Count, 950);
    }

    [Test]
    public void ProfileFor_IsValidAndDiffersPerPath()
    {
        string main = PlayerNames.ProfileFor("C:/Projects/Pingi/Assets");
        string virtualPlayer = PlayerNames.ProfileFor("C:/Projects/Pingi/Library/VP/mppm1/Assets");

        Assert.IsTrue(Regex.IsMatch(main, "^[a-z0-9]{1,30}$"), main);
        Assert.AreEqual(main, PlayerNames.ProfileFor("C:/Projects/Pingi/Assets"));
        Assert.AreNotEqual(main, virtualPlayer);
    }
}
