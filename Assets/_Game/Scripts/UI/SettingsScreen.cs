using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SettingsScreen : UIScreen
{
    private const string SwitchOnClass = "switch--on";
    private const string ArmedClass = "is-armed";
    private const long ConfirmMs = 2500;

    private sealed class ConfirmPill
    {
        private readonly Button button;
        private readonly Label label;
        private readonly string idleText;
        private IVisualElementScheduledItem disarmJob;

        public bool Armed { get; private set; }

        public ConfirmPill(Button button, Label label, string idleText)
        {
            this.button = button;
            this.label = label;
            this.idleText = idleText;
        }

        public void Arm()
        {
            Armed = true;
            button.AddToClassList(ArmedClass);
            label.text = Loc.T("Tap again");
            disarmJob?.Pause();
            disarmJob = button.schedule.Execute(Disarm).StartingIn(ConfirmMs);
        }

        public void Disarm()
        {
            Armed = false;
            disarmJob?.Pause();
            button?.RemoveFromClassList(ArmedClass);
            if (label != null) label.text = Loc.T(idleText);
        }

        public void SetEnabled(bool enabled)
        {
            button?.SetEnabled(enabled);
        }
    }

    private readonly VisualElement vibrationSwitch;
    private readonly VisualElement soundSwitch;
    private readonly Label bestValue;
    private readonly Label languageValue;
    private readonly VisualElement languageDialog;
    private readonly VisualElement languageList;
    private readonly ConfirmPill reset;
    private readonly ConfirmPill deleteOnline;
    private readonly Func<int> getBest;
    private readonly Action onReset;
    private readonly Action onDeleteOnline;
    private readonly OnlineService online;
    private readonly Action<string> toast;
    private readonly Label gamesValue;
    private readonly Label gamesLabel;
    private readonly Button gamesButton;
    private readonly Button playerIdButton;
    private readonly VisualElement privacyRow;
    private readonly VisualElement privacyDivider;
    private bool signingIn;

    public SettingsScreen(VisualElement root, Action onBack, Func<int> getBest, Action onReset, Action onDeleteOnline, OnlineService online,
        Action<string> toast) : base(root)
    {
        this.online = online;
        this.toast = toast;
        this.getBest = getBest;
        this.onReset = onReset;
        this.onDeleteOnline = onDeleteOnline;

        vibrationSwitch = root.Q("vibration-switch");
        soundSwitch = root.Q("sound-switch");
        bestValue = root.Q<Label>("best-value");
        languageValue = root.Q<Label>("language-value");
        languageDialog = root.Q("language-dialog");
        languageList = root.Q("language-list");
        root.Q<Label>("version-label").text = $"Pingi Pongi · v{Application.version}";

        vibrationSwitch.RegisterCallback<ClickEvent>(_ => ToggleVibration());
        soundSwitch.RegisterCallback<ClickEvent>(_ => ToggleSound());
        Bind("back-button", onBack);
        reset = new ConfirmPill(Bind("reset-button", OnResetPressed), root.Q<Label>("reset-label"), "Reset");
        deleteOnline = new ConfirmPill(Bind("online-button", OnDeleteOnlinePressed), root.Q<Label>("online-label"), "Delete");
        Bind("language-row", OpenLanguages);
        Bind("language-cancel", CloseLanguages);
        root.Q("language-scrim").RegisterCallback<ClickEvent>(_ => CloseLanguages());

        gamesValue = root.Q<Label>("games-value");
        gamesLabel = root.Q<Label>("games-label");
        gamesButton = Bind("games-button", SignInToPlayGames);
        playerIdButton = Bind("player-id", CopyPlayerId);
        privacyRow = Bind("privacy-row", ShowPrivacy);
        Bind("privacy-policy", () => Application.OpenURL(AppLinks.PrivacyPolicy));
        Bind("rate-link", () => Application.OpenURL(AppLinks.StorePage));
        privacyDivider = root.Q("privacy-divider");
        if (AdManager.Instance != null) AdManager.Instance.PrivacyChanged += RefreshPrivacy;
        PlayGamesAccount.Changed += RefreshAccount;
        if (online != null)
        {
            online.StateChanged += _ => RefreshAccount();
            online.AccountChanged += RefreshAccount;
        }
    }

    private void RefreshAccount()
    {
        if (gamesValue == null) return;

        bool shown = PlayGamesAccount.Supported || Application.isEditor;
        Root.Q("games-row").style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        Root.Q("games-divider").style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;

        bool linked = online != null && online.PlayGamesLinked;
        string name = PlayGamesAccount.DisplayName;
        if (!PlayGamesAccount.Supported) gamesValue.text = Loc.T("Available on Android");
        else if (linked) gamesValue.text = string.IsNullOrEmpty(name) ? Loc.T("Connected") : Loc.T("Connected · {0}", name);
        else if (PlayGamesAccount.SignedIn) gamesValue.text = Loc.T("Connecting…");
        else gamesValue.text = Loc.T("Not connected");

        gamesButton.style.display = linked ? DisplayStyle.None : DisplayStyle.Flex;
        gamesButton.SetEnabled(PlayGamesAccount.Supported && !signingIn);
        gamesLabel.text = signingIn ? "…" : Loc.T("Sign in");

        string id = online != null && online.IsReady ? online.PlayerId : null;
        playerIdButton.text = string.IsNullOrEmpty(id) ? string.Empty : Loc.T("Player ID: {0}", id);
        playerIdButton.style.display = string.IsNullOrEmpty(id) ? DisplayStyle.None : DisplayStyle.Flex;
    }

    private void RefreshPrivacy()
    {
        bool required = AdManager.Instance != null && AdManager.Instance.PrivacyOptionsRequired;
        privacyRow.style.display = required ? DisplayStyle.Flex : DisplayStyle.None;
        privacyDivider.style.display = required ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void ShowPrivacy()
    {
        if (AdManager.Instance == null) return;
        AdManager.Instance.ShowPrivacyOptions(_ => RefreshPrivacy());
    }

    private async void SignInToPlayGames()
    {
        if (online == null || signingIn) return;

        signingIn = true;
        RefreshAccount();
        bool linked = await online.SignInWithPlayGamesAsync();
        signingIn = false;
        RefreshAccount();
        toast?.Invoke(linked ? Loc.T("Signed in to Google Play Games") : Loc.T("Couldn't sign in to Google Play Games. Please try again."));
    }

    private void CopyPlayerId()
    {
        if (online == null || string.IsNullOrEmpty(online.PlayerId)) return;
        NativeShare.Copy(online.PlayerId);
        toast?.Invoke(Loc.T("Player ID copied"));
    }

    public bool IsLanguageOpen => !languageDialog.ClassListContains("dialog--hidden");

    public void CloseLanguages()
    {
        languageDialog.AddToClassList("dialog--hidden");
    }

    private void OpenLanguages()
    {
        languageList.Clear();
        foreach (string code in Loc.Codes)
        {
            var option = new Button { focusable = false, name = $"language-{code}" };
            option.RemoveFromClassList(Button.ussClassName);
            option.AddToClassList("language-option");
            option.EnableInClassList("language-option--selected", code == Loc.Language);
            option.Add(UiFactory.Text(Loc.NativeName(code), "language-option__name"));
            if (code == Loc.Language) option.Add(UiFactory.Element("icon icon--check"));
            string picked = code;
            option.clicked += () => PickLanguage(picked);
            languageList.Add(option);
        }
        languageDialog.RemoveFromClassList("dialog--hidden");
    }

    private void PickLanguage(string code)
    {
        UiFeedback.Tap();
        CloseLanguages();
        if (code != Loc.Language) Loc.SetLanguage(code);
        RefreshLanguage();
    }

    private void RefreshLanguage()
    {
        languageValue.text = Loc.NativeName(Loc.Language);
        reset.Disarm();
        deleteOnline.Disarm();
    }

    public void SetOnlineBusy(bool busy)
    {
        deleteOnline.SetEnabled(!busy);
    }

    protected override void OnShow()
    {
        vibrationSwitch.EnableInClassList(SwitchOnClass, GameSettings.HapticsEnabled);
        soundSwitch.EnableInClassList(SwitchOnClass, GameSettings.SoundEnabled);
        RefreshBest();
        RefreshLanguage();
        RefreshAccount();
        RefreshPrivacy();
        deleteOnline.SetEnabled(true);
    }

    protected override void OnHide()
    {
        CloseLanguages();
        reset.Disarm();
        deleteOnline.Disarm();
    }

    private void ToggleVibration()
    {
        bool enabled = !GameSettings.HapticsEnabled;
        GameSettings.HapticsEnabled = enabled;
        vibrationSwitch.EnableInClassList(SwitchOnClass, enabled);
        if (enabled) HapticManager.Soft();
    }

    private void ToggleSound()
    {
        bool enabled = !GameSettings.SoundEnabled;
        GameSettings.SoundEnabled = enabled;
        soundSwitch.EnableInClassList(SwitchOnClass, enabled);

        if (enabled) AudioManager.PlayOne(Sfx.UiTap);
        else if (AudioManager.Instance != null) AudioManager.Instance.StopAll();
    }

    private void OnResetPressed()
    {
        if (!reset.Armed)
        {
            reset.Arm();
            return;
        }

        reset.Disarm();
        onReset?.Invoke();
        HapticManager.Medium();
        RefreshBest();
    }

    private void OnDeleteOnlinePressed()
    {
        if (!deleteOnline.Armed)
        {
            deleteOnline.Arm();
            return;
        }

        deleteOnline.Disarm();
        HapticManager.Medium();
        onDeleteOnline?.Invoke();
    }

    private void RefreshBest()
    {
        int best = getBest != null ? getBest() : 0;
        bestValue.text = best.ToString();
        reset.SetEnabled(best > 0);
    }
}
