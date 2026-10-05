using NUnit.Framework;

[SetUpFixture]
public sealed class EnglishFixture
{
    private string previous;

    [OneTimeSetUp]
    public void UseEnglish()
    {
        previous = Loc.Language;
        Loc.SetLanguage(Loc.English, false);
    }

    [OneTimeTearDown]
    public void Restore()
    {
        Loc.SetLanguage(previous, false);
    }
}
