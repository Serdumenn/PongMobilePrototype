public sealed class OnlineMatchRules
{
    public bool Coop { get; }
    public int PointsToWin { get; }
    public int HostScore { get; private set; }
    public int GuestScore { get; private set; }
    public int Lives { get; private set; }
    public int Passes { get; private set; }
    public bool HostServes { get; private set; }
    public bool Ended { get; private set; }
    public bool HostWon { get; private set; }

    public OnlineMatchRules(bool coop, int pointsToWin, int lives, bool hostServesFirst)
    {
        Coop = coop;
        PointsToWin = pointsToWin < 1 ? 1 : pointsToWin;
        Lives = lives < 1 ? 1 : lives;
        HostServes = hostServesFirst;
    }

    public void Miss(bool hostMissed)
    {
        if (Ended) return;

        HostServes = hostMissed;

        if (Coop)
        {
            Lives--;
            if (Lives <= 0)
            {
                Lives = 0;
                Ended = true;
            }
            return;
        }

        if (hostMissed) GuestScore++;
        else HostScore++;

        if (HostScore >= PointsToWin || GuestScore >= PointsToWin)
        {
            Ended = true;
            HostWon = HostScore > GuestScore;
        }
    }

    public void Pass()
    {
        if (!Ended) Passes++;
    }

    public void Forfeit(bool hostLeft)
    {
        if (Ended) return;
        Ended = true;
        HostWon = !hostLeft;
    }

    public void Apply(MatchMessage score)
    {
        HostScore = score.HostScore;
        GuestScore = score.GuestScore;
        Lives = score.Lives;
        Passes = score.Passes;
        HostServes = score.HostServes;
        Ended = score.Ended;
        HostWon = score.HostWon;
    }

    public int ScoreFor(bool host)
    {
        return host ? HostScore : GuestScore;
    }
}
