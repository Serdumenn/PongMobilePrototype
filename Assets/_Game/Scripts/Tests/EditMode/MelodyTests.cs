using NUnit.Framework;

public sealed class MelodyTests
{
    private static readonly int[] Pentatonic = { 0, 2, 4, 7, 9 };

    [Test]
    public void FirstHit_KeepsTheOriginalPitch()
    {
        Assert.AreEqual(0f, Melody.Semitones(0, 2f, 6f), 0.0001f);
        Assert.AreEqual(1f, Melody.Pitch(0f), 0.0001f);
        Assert.AreEqual(2f, Melody.Pitch(12f), 0.0001f);
    }

    [Test]
    public void EveryNote_StaysOnThePentatonicScale()
    {
        for (int rally = 0; rally < Melody.Length; rally++)
        {
            int note = (int)Melody.Semitones(rally, 2f, 6f) % 12;
            CollectionAssert.Contains(Pentatonic, note, "Hit " + rally);
        }
    }

    [Test]
    public void EachPhrase_LiftsTheKeyUntilTheCap()
    {
        int length = Melody.Length;

        Assert.AreEqual(0f, Melody.Lift(length - 1, 2f, 6f), 0.0001f);
        Assert.AreEqual(2f, Melody.Lift(length, 2f, 6f), 0.0001f);
        Assert.AreEqual(4f, Melody.Lift(length * 2, 2f, 6f), 0.0001f);
        Assert.AreEqual(6f, Melody.Lift(length * 10, 2f, 6f), 0.0001f);
        Assert.AreEqual(Melody.Semitones(3, 2f, 6f) + 2f, Melody.Semitones(length + 3, 2f, 6f), 0.0001f);
    }

    [Test]
    public void Melody_NeverRunsAway()
    {
        for (int rally = -5; rally < 500; rally++)
        {
            float note = Melody.Semitones(rally, 2f, 6f);
            Assert.GreaterOrEqual(note, 0f);
            Assert.LessOrEqual(note, 18f);
        }
    }
}
