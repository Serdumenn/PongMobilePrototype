using UnityEngine;
using UnityEngine.UIElements;

public sealed class GhostRaceScreen : UIScreen
{
    private readonly VisualElement card;
    private readonly VisualElement ball;
    private readonly Label ghostName;
    private readonly Label score;

    private GhostRace race;

    public GhostRaceScreen(VisualElement root) : base(root)
    {
        card = root.Q("ghost-card");
        ball = root.Q("ghost-ball");
        ghostName = root.Q<Label>("ghost-name");
        score = root.Q<Label>("ghost-score");
    }

    public void Present(GhostRace source, Sprite ballSprite)
    {
        if (race != source)
        {
            if (race != null) race.GhostScoreChanged -= SetScore;
            race = source;
            if (race != null) race.GhostScoreChanged += SetScore;
        }

        ghostName.text = race != null ? race.GhostName : Loc.T("Ghost");
        UiFactory.SetPicture(ball, ballSprite);
        SetScore(race != null ? race.GhostScore : 0);
        SetLeading(false);
        Show();
    }

    public void SetLeading(bool ghostAhead)
    {
        card.EnableInClassList("ghost-card--ahead", ghostAhead);
    }

    private void SetScore(int value)
    {
        score.text = value.ToString();
    }
}
