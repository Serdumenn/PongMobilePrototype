using System;
using UnityEngine.UIElements;

public sealed class GhostResultScreen : UIScreen
{
    private readonly VisualElement mascot;
    private readonly Label title;
    private readonly Label caption;
    private readonly Label myScore;
    private readonly Label ghostScore;
    private readonly Label ghostName;
    private readonly ScoreSpark spark;
    private readonly Button sendButton;
    private readonly Label sendLabel;
    private readonly Button retryButton;
    private readonly Button homeButton;

    private bool canSend;
    private bool sending;

    public GhostResultScreen(VisualElement root, Action onSend, Action onRetry, Action onHome) : base(root)
    {
        mascot = root.Q("mascot");
        title = root.Q<Label>("title");
        caption = root.Q<Label>("caption");
        myScore = root.Q<Label>("my-score");
        ghostScore = root.Q<Label>("ghost-score");
        ghostName = root.Q<Label>("ghost-name");
        sendLabel = root.Q<Label>("send-label");

        spark = new ScoreSpark();
        root.Q("spark-slot").Add(spark);

        sendButton = Bind("send-button", onSend);
        retryButton = Bind("retry-button", onRetry);
        homeButton = Bind("home-button", onHome);
    }

    public void Present(int mine, GhostRun myRun, GhostRun ghost, string name, CosmeticItem myBall)
    {
        int theirs = ghost != null ? ghost.Score : 0;
        title.text = mine > theirs ? Loc.T("You beat the ghost!") : mine < theirs ? Loc.T("The ghost wins") : Loc.T("It's a tie!");
        caption.text = Loc.T("Ghost Challenge · {0}", name);
        myScore.text = mine.ToString();
        ghostScore.text = theirs.ToString();
        ghostName.text = name;

        if (myBall != null) UiFactory.SetPicture(mascot, mine > theirs ? myBall.Happy : mine < theirs ? myBall.Sad : myBall.Idle);

        spark.SetRuns(myRun, mine, ghost);
        canSend = myRun != null && mine > 0;
        sending = false;
        RefreshSend();
        SetInteractable(true);
        Show();
    }

    public void SetSending(bool value)
    {
        sending = value;
        RefreshSend();
    }

    public void SetInteractable(bool interactable)
    {
        retryButton?.SetEnabled(interactable);
        homeButton?.SetEnabled(interactable);
        RefreshSend(interactable);
    }

    private void RefreshSend(bool interactable = true)
    {
        sendButton.SetEnabled(interactable && canSend && !sending);
        sendLabel.text = sending ? Loc.T("Creating a code…") : canSend ? Loc.T("Send my run back") : Loc.T("No points to send");
    }
}
