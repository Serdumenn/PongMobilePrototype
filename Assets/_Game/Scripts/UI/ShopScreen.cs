using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class ShopScreen : UIScreen
{
    private const string ActiveTabClass = "tab--active";
    private const string ActiveFilterClass = "filter--active";
    private const string DialogHiddenClass = "dialog--hidden";
    private const long ShakeMs = 110;
    private const long RevealMs = 460;

    private enum DialogMode
    {
        None,
        Item,
        Pass
    }

    private enum EffectView
    {
        Trail,
        Hit,
        Party,
        Frame,
        Sets,
        Box
    }

    private readonly CosmeticsService cosmetics;
    private readonly Action<string> notify;
    private readonly Sprite passArt;
    private readonly VisualElement grid;
    private readonly ScrollView scroll;
    private readonly Dictionary<CosmeticCategory, Button> tabs = new Dictionary<CosmeticCategory, Button>();
    private readonly Dictionary<EffectView, Button> filters = new Dictionary<EffectView, Button>();
    private readonly Button effectTab;
    private readonly Button passButton;
    private readonly Label passPrice;
    private readonly Button noAdsButton;
    private readonly Label noAdsPrice;

    private readonly VisualElement showcasePanel;
    private readonly VisualElement showcaseRow;
    private readonly Label showcaseTimer;
    private readonly Label showcaseHint;
    private readonly VisualElement seasonPanel;
    private readonly VisualElement seasonRow;
    private readonly Label seasonTitle;
    private readonly Label seasonDays;
    private readonly VisualElement effectFilters;
    private readonly VisualElement setsList;
    private readonly VisualElement boxPanel;
    private readonly VisualElement gift;
    private readonly VisualElement boxPrize;
    private readonly VisualElement boxPrizeArt;
    private readonly Label boxPrizeName;
    private readonly Label boxPrizeTier;
    private readonly Label boxOdds;
    private readonly Label boxOpenLabel;
    private readonly Button boxOpen;

    private readonly VisualElement dialog;
    private readonly VisualElement dialogArt;
    private readonly Label dialogTitle;
    private readonly Label dialogText;
    private readonly VisualElement dialogDots;
    private readonly Button dialogPrimary;
    private readonly VisualElement dialogPrimaryIcon;
    private readonly Label dialogPrimaryLabel;
    private readonly Button dialogBuy;
    private readonly Label dialogBuyLabel;
    private readonly Button dialogGold;
    private readonly Label dialogGoldLabel;
    private readonly Label goldBalance;
    private readonly Button dialogPass;

    private CosmeticCategory category = CosmeticCategory.Ball;
    private bool effects;
    private EffectView effectView = EffectView.Trail;
    private DialogMode dialogMode;
    private CosmeticItem dialogItem;
    private bool dialogDiscount;
    private bool waitingForAd;
    private int unlockedCount;
    private int shownDay = -1;
    private IVisualElementScheduledItem ticker;

    public bool IsDialogOpen => dialogMode != DialogMode.None;

    public ShopScreen(VisualElement root, CosmeticsService cosmetics, Action onBack, Action<string> notify) : base(root)
    {
        this.cosmetics = cosmetics;
        this.notify = notify;

        grid = root.Q("grid");
        scroll = root.Q<ScrollView>("shop-scroll");
        passButton = root.Q<Button>("pass-button");
        passPrice = root.Q<Label>("pass-price");
        noAdsButton = root.Q<Button>("noads-button");
        noAdsPrice = root.Q<Label>("noads-price");

        showcasePanel = root.Q("showcase");
        showcaseRow = root.Q("showcase-row");
        showcaseTimer = root.Q<Label>("showcase-timer");
        showcaseHint = root.Q<Label>("showcase-hint");
        seasonPanel = root.Q("season");
        seasonRow = root.Q("season-row");
        seasonTitle = root.Q<Label>("season-title");
        seasonDays = root.Q<Label>("season-days");
        effectFilters = root.Q("effect-filters");
        setsList = root.Q("sets");
        boxPanel = root.Q("box");
        gift = root.Q("box-gift");
        boxPrize = root.Q("box-prize");
        boxPrizeArt = root.Q("box-prize-art");
        boxPrizeName = root.Q<Label>("box-prize-name");
        boxPrizeTier = root.Q<Label>("box-prize-tier");
        boxOdds = root.Q<Label>("box-odds");
        boxOpenLabel = root.Q<Label>("box-open-label");
        boxOpen = root.Q<Button>("box-open");

        dialog = root.Q("dialog");
        dialogArt = root.Q("dialog-art");
        dialogTitle = root.Q<Label>("dialog-title");
        dialogText = root.Q<Label>("dialog-text");
        dialogDots = root.Q("dialog-dots");
        dialogPrimary = root.Q<Button>("dialog-primary");
        dialogPrimaryIcon = root.Q("dialog-primary-icon");
        dialogPrimaryLabel = root.Q<Label>("dialog-primary-label");
        dialogBuy = root.Q<Button>("dialog-buy");
        dialogBuyLabel = root.Q<Label>("dialog-buy-label");
        dialogGold = root.Q<Button>("dialog-gold");
        dialogGoldLabel = root.Q<Label>("dialog-gold-label");
        goldBalance = root.Q<Label>("gold-balance");
        dialogPass = root.Q<Button>("dialog-pass");

        var golden = cosmetics.CatalogAsset.Find("ball_golden");
        passArt = golden != null ? golden.Happy : null;
        if (passArt != null) root.Q("pass-art").style.backgroundImage = new StyleBackground(passArt);

        Bind("back-button", onBack);
        tabs[CosmeticCategory.Ball] = Bind("tab-ball", () => SelectCategory(CosmeticCategory.Ball));
        tabs[CosmeticCategory.Paddle] = Bind("tab-paddle", () => SelectCategory(CosmeticCategory.Paddle));
        tabs[CosmeticCategory.Theme] = Bind("tab-theme", () => SelectCategory(CosmeticCategory.Theme));
        effectTab = Bind("tab-effect", SelectEffects);
        filters[EffectView.Trail] = Bind("filter-trail", () => SelectFilter(EffectView.Trail));
        filters[EffectView.Hit] = Bind("filter-hit", () => SelectFilter(EffectView.Hit));
        filters[EffectView.Party] = Bind("filter-party", () => SelectFilter(EffectView.Party));
        filters[EffectView.Frame] = Bind("filter-frame", () => SelectFilter(EffectView.Frame));
        filters[EffectView.Sets] = Bind("filter-sets", () => SelectFilter(EffectView.Sets));
        filters[EffectView.Box] = Bind("filter-box", () => SelectFilter(EffectView.Box));
        Bind("box-open", OpenBox);
        Bind("pass-button", OpenPassDialog);
        Bind("noads-button", () => StartPurchase(CosmeticCatalog.RemoveAdsProductId));
        Bind("dialog-primary", OnDialogPrimary);
        Bind("dialog-buy", OnDialogBuy);
        Bind("dialog-gold", OnDialogGold);
        Bind("dialog-pass", OpenPassDialog);
        root.Q("dialog-scrim").RegisterCallback<ClickEvent>(_ => CloseDialog());

        cosmetics.Changed += OnCosmeticsChanged;
        var purchases = PurchaseService.Instance;
        if (purchases != null)
        {
            purchases.ProductsChanged += OnCosmeticsChanged;
            purchases.PurchaseSucceeded += OnPurchaseSucceeded;
            purchases.PurchaseFailed += OnPurchaseFailed;
        }
    }

    public void CloseDialog()
    {
        dialogMode = DialogMode.None;
        dialogItem = null;
        dialogDiscount = false;
        waitingForAd = false;
        dialog.AddToClassList(DialogHiddenClass);
    }

    protected override void OnShow()
    {
        unlockedCount = CountUnlocked();
        CloseDialog();
        ResetBox();
        if (effects) SelectEffects();
        else SelectCategory(category);
        scroll.scrollOffset = Vector2.zero;

        if (ticker == null) ticker = Root.schedule.Execute(Tick).Every(1000);
        else ticker.Resume();
    }

    protected override void OnHide()
    {
        CloseDialog();
        ticker?.Pause();
    }

    private int CountUnlocked()
    {
        int count = cosmetics.Inventory.NoAds ? 1 : 0;
        foreach (var item in cosmetics.CatalogAsset.Items)
            if (item != null && cosmetics.IsUnlocked(item)) count++;
        return count;
    }

    private void SelectCategory(CosmeticCategory next)
    {
        effects = false;
        category = next;
        ShowTabs();
        Refresh();
    }

    private void SelectEffects()
    {
        effects = true;
        ShowTabs();
        Refresh();
    }

    private void SelectFilter(EffectView view)
    {
        effectView = view;
        if (view == EffectView.Box) ResetBox();
        Refresh();
    }

    private void ShowTabs()
    {
        foreach (var pair in tabs) pair.Value?.EnableInClassList(ActiveTabClass, !effects && pair.Key == category);
        effectTab?.EnableInClassList(ActiveTabClass, effects);
    }

    private CosmeticCategory ShownCategory => !effects ? category : effectView switch
    {
        EffectView.Hit => CosmeticCategory.Hit,
        EffectView.Party => CosmeticCategory.Celebration,
        EffectView.Frame => CosmeticCategory.Frame,
        _ => CosmeticCategory.Trail
    };

    private bool ShowsGrid => !effects || effectView <= EffectView.Frame;

    private bool Detached()
    {
        if (Root.panel != null) return false;

        cosmetics.Changed -= OnCosmeticsChanged;
        var purchases = PurchaseService.Instance;
        if (purchases != null)
        {
            purchases.ProductsChanged -= OnCosmeticsChanged;
            purchases.PurchaseSucceeded -= OnPurchaseSucceeded;
            purchases.PurchaseFailed -= OnPurchaseFailed;
        }
        ticker?.Pause();
        return true;
    }

    private void OnCosmeticsChanged()
    {
        if (Detached() || !IsVisible) return;

        int count = CountUnlocked();
        if (count > unlockedCount) AudioManager.PlayOne(Sfx.Unlock);
        unlockedCount = count;

        Refresh();
        if (dialogMode == DialogMode.Item) FillItemDialog(dialogItem);
        else if (dialogMode == DialogMode.Pass) FillPassDialog();
    }

    private void Refresh()
    {
        var inventory = cosmetics.Inventory;
        ShowGoldBalance();

        passButton.style.display = inventory.HasPass || effects ? DisplayStyle.None : DisplayStyle.Flex;
        noAdsButton.style.display = inventory.NoAds ? DisplayStyle.None : DisplayStyle.Flex;
        passPrice.text = PriceLabel(CosmeticCatalog.PassProductId);
        noAdsPrice.text = PriceLabel(CosmeticCatalog.RemoveAdsProductId);

        RefreshShowcase();
        RefreshSeason();

        Show(effectFilters, effects);
        foreach (var pair in filters) pair.Value?.EnableInClassList(ActiveFilterClass, pair.Key == effectView);

        Show(grid, ShowsGrid);
        grid.Clear();
        if (ShowsGrid)
            foreach (var item in cosmetics.CatalogAsset.InCategory(ShownCategory))
                grid.Add(BuildCard(item));

        bool sets = effects && effectView == EffectView.Sets;
        Show(setsList, sets);
        if (sets) RefreshSets();

        bool box = effects && effectView == EffectView.Box;
        Show(boxPanel, box);
        if (box) RefreshBox();

        RefreshPurchaseState();
    }

    private static void Show(VisualElement element, bool shown)
    {
        if (element != null) element.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void Tick()
    {
        if (!IsVisible) return;
        if (cosmetics.Today != shownDay)
        {
            Refresh();
            return;
        }
        ShowTimer();
    }

    private void ShowTimer()
    {
        if (showcaseTimer == null) return;
        int left = Mathf.Max(0, cosmetics.SecondsToNewShowcase);
        showcaseTimer.text = (left / 3600).ToString("00") + ":" + (left % 3600 / 60).ToString("00") + ":" + (left % 60).ToString("00");
    }

    private void RefreshShowcase()
    {
        if (showcasePanel == null) return;

        shownDay = cosmetics.Today;
        var picks = cosmetics.ShowcaseToday();
        Show(showcasePanel, picks.Count > 0);
        showcaseRow.Clear();

        bool open = false;
        foreach (var item in picks)
        {
            showcaseRow.Add(BuildCard(item));
            open |= !cosmetics.IsUnlocked(item);
        }

        int percent = Mathf.RoundToInt(cosmetics.Discount * 100f);
        showcaseHint.text = open && cosmetics.DiscountReady && percent > 0
            ? Loc.T("Watch an ad for {0}% off one pick today.", percent)
            : Loc.T("New picks every day.");
        ShowTimer();
    }

    private void RefreshSeason()
    {
        if (seasonPanel == null) return;

        var season = cosmetics.CurrentSeason;
        Show(seasonPanel, season != null);
        seasonRow.Clear();
        if (season == null) return;

        seasonTitle.text = season.Title;
        int days = cosmetics.SeasonDaysLeft(season);
        seasonDays.text = Loc.Plural("{0} day left", "{0} days left", days);
        foreach (var item in season.Items)
            if (item != null) seasonRow.Add(BuildCard(item));
    }

    private void RefreshSets()
    {
        setsList.Clear();
        foreach (var set in cosmetics.Sets)
        {
            if (set == null) continue;

            int have = cosmetics.SetProgress(set);
            bool done = set.Reward != null && cosmetics.IsUnlocked(set.Reward);

            var panel = Element("shop-panel set-panel");
            panel.EnableInClassList("set-panel--done", done);

            var head = Element("shop-panel__head");
            head.Add(Text(set.Title, "shop-panel__title"));
            head.Add(Text(have + "/" + set.Items.Count, "set-panel__count"));
            panel.Add(head);

            var bar = Element("set-panel__bar");
            var fill = Element("set-panel__fill");
            fill.style.width = Length.Percent(100f * have / Mathf.Max(1, set.Items.Count));
            bar.Add(fill);
            panel.Add(bar);

            var parts = Element("set-panel__parts");
            foreach (var part in set.Items)
                if (part != null) parts.Add(SetPart(part));
            panel.Add(parts);

            if (set.Reward != null)
            {
                var reward = Element("set-panel__reward");
                var art = BuildArt(set.Reward, "set-panel__reward-art");
                reward.Add(art);
                reward.Add(Text(done ? Loc.T("Collected: {0}", set.Reward.Title) : Loc.T("Reward: {0}", set.Reward.Title), "set-panel__reward-label"));
                panel.Add(reward);
            }

            setsList.Add(panel);
        }
    }

    private VisualElement SetPart(CosmeticItem item)
    {
        bool owned = cosmetics.IsUnlocked(item);
        var part = new Button();
        part.RemoveFromClassList(Button.ussClassName);
        part.AddToClassList("set-part");
        part.EnableInClassList("set-part--missing", !owned);
        part.focusable = false;
        part.clicked += () =>
        {
            UiFeedback.Tap();
            OnCardClicked(item);
        };

        part.Add(BuildArt(item, "set-part__art"));
        if (owned)
        {
            var check = Element("set-part__check");
            check.Add(Element("icon icon--check"));
            part.Add(check);
        }
        return part;
    }

    private void RefreshBox()
    {
        var odds = cosmetics.BoxOdds();
        if (odds.Count == 0)
        {
            boxOdds.text = Loc.T("You have everything the box can give!");
            boxOpenLabel.text = Loc.T("All collected");
            boxOpen.SetEnabled(false);
            return;
        }

        var parts = new List<string>();
        foreach (var tier in new[] { CosmeticTier.Common, CosmeticTier.Rare, CosmeticTier.Epic })
            if (odds.TryGetValue(tier, out float share)) parts.Add(Loc.T("{0} {1}%", TierName(tier), Mathf.RoundToInt(share)));
        boxOdds.text = string.Join(" · ", parts);
        boxOpenLabel.text = Loc.T("Open · {0}", GoldText(cosmetics.BoxPrice));
        boxOpen.SetEnabled(cosmetics.CanOpenBox);
    }

    private void ResetBox()
    {
        gift?.RemoveFromClassList("gift--open");
        gift?.RemoveFromClassList("gift--shake");
        boxPrize?.RemoveFromClassList("box-prize--show");
    }

    private void OpenBox()
    {
        if (!cosmetics.CanOpenBox)
        {
            notify?.Invoke(cosmetics.BoxHasItems ? Loc.T("Not enough gold yet.") : Loc.T("You have everything the box can give!"));
            return;
        }

        ResetBox();
        var item = cosmetics.OpenBox();
        if (item == null) return;

        HapticManager.Medium();
        boxPrizeName.text = item.Title;
        boxPrizeTier.text = TierName(item.Tier);
        boxPrizeTier.EnableInClassList("box-prize__tier--rare", item.Tier == CosmeticTier.Rare);
        boxPrizeTier.EnableInClassList("box-prize__tier--epic", item.Tier == CosmeticTier.Epic);
        SetArt(boxPrizeArt, item);

        gift.AddToClassList("gift--shake");
        Root.schedule.Execute(() => gift.RemoveFromClassList("gift--shake")).StartingIn(ShakeMs);
        Root.schedule.Execute(() => gift.AddToClassList("gift--shake")).StartingIn(ShakeMs * 2);
        Root.schedule.Execute(() =>
        {
            gift.RemoveFromClassList("gift--shake");
            gift.AddToClassList("gift--open");
            boxPrize.AddToClassList("box-prize--show");
            AudioManager.PlayOne(Sfx.Unlock);
        }).StartingIn(RevealMs);

        AnnounceSetRewards();
    }

    private void AnnounceSetRewards()
    {
        CosmeticItem reward;
        while ((reward = cosmetics.TakeSetReward()) != null)
            notify?.Invoke(Loc.T("Set complete! {0} is yours.", reward.Title));
    }

    private VisualElement BuildCard(CosmeticItem item)
    {
        bool unlocked = cosmetics.IsUnlocked(item);
        bool equipped = cosmetics.IsEquipped(item);

        var card = new Button();
        card.RemoveFromClassList(Button.ussClassName);
        card.AddToClassList("item");
        card.EnableInClassList("item--equipped", equipped);
        card.focusable = false;
        card.clicked += () =>
        {
            UiFeedback.Tap();
            OnCardClicked(item);
        };

        var face = new VisualElement { pickingMode = PickingMode.Ignore };
        face.AddToClassList("item__face");
        card.Add(face);

        var art = BuildArt(item, "item__art");
        art.EnableInClassList("item__art--locked", !unlocked);
        face.Add(art);

        var name = new Label(item.Title) { pickingMode = PickingMode.Ignore };
        name.AddToClassList("item__name");
        face.Add(name);

        if (equipped) face.Add(Status("status--equipped", "check", Loc.T("Equipped")));
        else if (unlocked) face.Add(Status("status--owned", null, Loc.T("Use")));
        else if (item.Unlock == UnlockKind.Score) face.Add(Status("status--score", "lock", Loc.T("Best {0}", item.UnlockValue)));
        else if (item.Unlock == UnlockKind.Streak) face.Add(Status("status--streak", "flame", Loc.Plural("{0} day", "{0} days", item.UnlockValue)));
        else if (item.Unlock == UnlockKind.Stars) face.Add(Status("status--stars", "star", $"{Mathf.Min(cosmetics.Stars, item.UnlockValue)}/{item.UnlockValue}"));
        else if (item.Unlock == UnlockKind.Level) face.Add(Status("status--level", "level", Loc.T("Level {0}", item.UnlockValue)));
        else if (item.Unlock == UnlockKind.Ads)
        {
            int views = cosmetics.AdViews(item);
            face.Add(Status("status--ads", "play", $"{views}/{item.UnlockValue}"));

            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("item__bar");
            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("item__bar-fill");
            fill.style.width = Length.Percent(100f * views / Mathf.Max(1, item.UnlockValue));
            bar.Add(fill);
            face.Add(bar);
        }
        else if (cosmetics.OnSale(item)) face.Add(Status("status--gold", "coin", GoldText(item.GoldPrice)));
        else if (item.Unlock == UnlockKind.Showcase) face.Add(Status("status--showcase", "clock", Loc.T("Showcase")));
        else if (item.Unlock == UnlockKind.Season) face.Add(Status("status--season", "lock", Loc.T("Season")));
        else if (item.Unlock == UnlockKind.SetReward) face.Add(Status("status--set", "star", Loc.T("Set reward")));
        else face.Add(Status("status--pass", null, Loc.T("Pass")));

        if (!unlocked && item.Unlock == UnlockKind.Showcase && cosmetics.InShowcase(item))
            card.Add(Text(Loc.T("Only here"), "item__ribbon"));

        return card;
    }

    private VisualElement BuildArt(CosmeticItem item, string baseClass)
    {
        var art = new VisualElement { pickingMode = PickingMode.Ignore };

        if (item.Category == CosmeticCategory.Theme)
        {
            art.AddToClassList("swatch");
            art.style.backgroundColor = item.BackgroundColor;
            art.Add(Blob("swatch__blob--a", item.BlobPrimary));
            art.Add(Blob("swatch__blob--b", item.BlobSecondary));
            return art;
        }

        art.AddToClassList(baseClass);
        if (item.Category == CosmeticCategory.Paddle) art.AddToClassList(baseClass + "--paddle");
        if (item.Preview != null) art.style.backgroundImage = new StyleBackground(item.Preview);
        return art;
    }

    private void SetArt(VisualElement target, CosmeticItem item)
    {
        target.Clear();
        target.style.backgroundColor = StyleKeyword.Null;
        if (item.Category == CosmeticCategory.Theme)
        {
            target.style.backgroundImage = new StyleBackground(StyleKeyword.None);
            var swatch = BuildArt(item, "item__art");
            swatch.style.width = Length.Percent(100);
            swatch.style.height = Length.Percent(100);
            target.Add(swatch);
            return;
        }

        var sprite = item.Category == CosmeticCategory.Ball ? item.Happy : item.Preview;
        target.style.backgroundImage = sprite != null ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);
    }

    private static VisualElement Blob(string modifier, Color color)
    {
        var blob = new VisualElement { pickingMode = PickingMode.Ignore };
        blob.AddToClassList("swatch__blob");
        blob.AddToClassList(modifier);
        blob.style.backgroundColor = color;
        return blob;
    }

    private static VisualElement Element(string classes)
    {
        var element = new VisualElement { pickingMode = PickingMode.Ignore };
        foreach (var name in classes.Split(' ')) element.AddToClassList(name);
        return element;
    }

    private static Label Text(string text, string classes)
    {
        var label = new Label(text) { pickingMode = PickingMode.Ignore };
        foreach (var name in classes.Split(' ')) label.AddToClassList(name);
        return label;
    }

    private static VisualElement Status(string modifier, string icon, string text)
    {
        var status = new VisualElement { pickingMode = PickingMode.Ignore };
        status.AddToClassList("item__status");
        status.AddToClassList(modifier);

        if (!string.IsNullOrEmpty(icon))
        {
            var glyph = new VisualElement { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList("icon");
            glyph.AddToClassList("icon--" + icon);
            status.Add(glyph);
        }

        var label = new Label(text) { pickingMode = PickingMode.Ignore };
        label.AddToClassList("item__status-label");
        status.Add(label);
        return status;
    }

    private static string GoldText(int amount)
    {
        return amount.ToString("N0", Loc.Culture);
    }

    private static string TierName(CosmeticTier tier)
    {
        return tier switch
        {
            CosmeticTier.Common => Loc.T("Common"),
            CosmeticTier.Rare => Loc.T("Rare"),
            CosmeticTier.Epic => Loc.T("Epic"),
            CosmeticTier.Legendary => Loc.T("Legendary"),
            _ => ""
        };
    }

    private void OnCardClicked(CosmeticItem item)
    {
        if (cosmetics.IsUnlocked(item))
        {
            cosmetics.Equip(item);
            return;
        }

        dialogMode = DialogMode.Item;
        dialogItem = item;
        FillItemDialog(item);
        dialog.RemoveFromClassList(DialogHiddenClass);
    }

    private void OpenPassDialog()
    {
        if (cosmetics.Inventory.HasPass) return;

        dialogMode = DialogMode.Pass;
        dialogItem = null;
        dialogDiscount = false;
        FillPassDialog();
        dialog.RemoveFromClassList(DialogHiddenClass);
    }

    private void FillItemDialog(CosmeticItem item)
    {
        if (item == null) return;

        SetDialogArt(item);
        dialogTitle.text = item.Title;
        dialogDots.Clear();

        bool unlocked = cosmetics.IsUnlocked(item);
        bool earnedOnly = item.Unlock == UnlockKind.Streak || item.Unlock == UnlockKind.Stars || item.Unlock == UnlockKind.Level || item.Unlock == UnlockKind.SetReward;
        bool passOffer = !unlocked && !earnedOnly && item.PassCovers && !cosmetics.Inventory.HasPass;
        dialogPass.style.display = passOffer ? DisplayStyle.Flex : DisplayStyle.None;
        SetBuy(unlocked || earnedOnly ? null : item.ProductId);
        SetGoldOffer(unlocked ? null : item);

        if (unlocked)
        {
            dialogDiscount = false;
            dialogText.text = Loc.T("{0} is yours!", item.Title);
            SetPrimary(true, "check", Loc.T("Equip"));
            return;
        }

        int percent = Mathf.RoundToInt(cosmetics.Discount * 100f);
        dialogDiscount = percent > 0 && cosmetics.DiscountReady && cosmetics.InShowcase(item);
        if (dialogDiscount)
        {
            int price = cosmetics.DiscountedPrice(item);
            dialogText.text = Loc.T("Today's pick! Watch an ad to get it {0}% off.", percent);
            SetPrimary(true, "play", waitingForAd ? Loc.T("Loading…") : Loc.T("{0}% off · {1} gold", percent, GoldText(price)));
            dialogPrimary.SetEnabled(!waitingForAd && cosmetics.Gold >= price);
            return;
        }

        switch (item.Unlock)
        {
            case UnlockKind.Ads:
                int views = cosmetics.AdViews(item);
                dialogText.text = Loc.T("Watch {0} ads to unlock {1}.\nYour progress is saved.", item.UnlockValue, item.Title);
                for (int i = 0; i < item.UnlockValue; i++)
                {
                    var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                    dot.AddToClassList("dot");
                    dot.EnableInClassList("dot--filled", i < views);
                    dialogDots.Add(dot);
                }
                SetPrimary(true, "play", waitingForAd ? Loc.T("Loading…") : Loc.T("Watch ad ({0}/{1})", views, item.UnlockValue));
                dialogPrimary.SetEnabled(!waitingForAd);
                break;

            case UnlockKind.Score:
                int best = PlayerPrefs.GetInt(SoloScoreManager.BestScoreKey, 0);
                dialogText.text = Loc.T("Reach a Classic best of {0} to unlock.\nYour best: {1}", item.UnlockValue, best);
                SetPrimary(false, null, null);
                break;

            case UnlockKind.Streak:
                int streak = cosmetics.CurrentDayStreak;
                dialogText.text = Loc.T("Play {0} days in a row to unlock.", item.UnlockValue) + "\n" + Loc.Plural("Current streak: {0} day", "Current streak: {0} days", streak);
                for (int i = 0; i < item.UnlockValue; i++)
                {
                    var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                    dot.AddToClassList("dot");
                    dot.AddToClassList("dot--streak");
                    dot.EnableInClassList("dot--filled", i < streak);
                    dialogDots.Add(dot);
                }
                SetPrimary(false, null, null);
                break;

            case UnlockKind.Stars:
                dialogText.text = Loc.T("Earn {0} mission stars to unlock.\nYour stars: {1}", item.UnlockValue, cosmetics.Stars);
                SetPrimary(false, null, null);
                break;

            case UnlockKind.Level:
                dialogText.text = Loc.T("Reach level {0} on the Pingi Road to unlock.\nYour level: {1}", item.UnlockValue, cosmetics.RoadLevel);
                SetPrimary(false, null, null);
                break;

            case UnlockKind.Gold:
                dialogText.text = Loc.T("Unlock {0} with the gold you win in games.", item.Title);
                SetPrimary(false, null, null);
                break;

            case UnlockKind.Showcase:
                dialogText.text = cosmetics.InShowcase(item) ? Loc.T("Only in today's picks!") : Loc.T("Shows up in today's picks on some days. Check back tomorrow!");
                SetPrimary(false, null, null);
                break;

            case UnlockKind.Season:
                var season = SeasonOf(item);
                string festival = season != null ? season.Title : "";
                dialogText.text = cosmetics.OnSale(item) ? Loc.T("Festival item! Get it before {0} ends.", festival) : Loc.T("Comes back with {0}.", festival);
                SetPrimary(false, null, null);
                break;

            case UnlockKind.SetReward:
                var set = CosmeticSets.SetOf(cosmetics.Sets, item);
                dialogText.text = Loc.T("Complete the {0} to get it.", set != null ? set.Title : "");
                SetPrimary(false, null, null);
                break;

            default:
                dialogText.text = Loc.T("Exclusive to Pingi Pass.");
                SetPrimary(false, null, null);
                break;
        }
    }

    private SeasonDef SeasonOf(CosmeticItem item)
    {
        var content = cosmetics.Shop;
        if (content == null) return null;
        foreach (var season in content.Seasons)
        {
            if (season == null) continue;
            foreach (var part in season.Items)
                if (part == item) return season;
        }
        return null;
    }

    private void FillPassDialog()
    {
        dialogArt.RemoveFromClassList("dialog__art--paddle");
        dialogArt.RemoveFromClassList("dialog__art--theme");
        dialogArt.Clear();
        dialogArt.style.backgroundColor = StyleKeyword.Null;
        dialogArt.style.backgroundImage = passArt != null ? new StyleBackground(passArt) : new StyleBackground(StyleKeyword.None);

        dialogTitle.text = "Pingi Pass";
        dialogText.text = Loc.T("Unlock every costume, paddle and theme,\nplus the exclusive Golden set.");
        dialogDots.Clear();
        dialogPass.style.display = DisplayStyle.None;
        dialogBuy.style.display = DisplayStyle.None;
        if (dialogGold != null) dialogGold.style.display = DisplayStyle.None;

        string price = cosmetics.PriceOf(CosmeticCatalog.PassProductId);
        SetPrimary(true, null, price != null ? Loc.T("Get Pass · {0}", price) : Loc.T("Store unavailable"));
        dialogPrimary.SetEnabled(price != null && !IsPurchasing);
    }

    private void SetDialogArt(CosmeticItem item)
    {
        dialogArt.Clear();
        dialogArt.EnableInClassList("dialog__art--paddle", item.Category == CosmeticCategory.Paddle);
        dialogArt.EnableInClassList("dialog__art--theme", item.Category == CosmeticCategory.Theme);

        if (item.Category == CosmeticCategory.Theme)
        {
            dialogArt.style.backgroundImage = new StyleBackground(StyleKeyword.None);
            var swatch = BuildArt(item, "item__art");
            swatch.style.width = Length.Percent(100);
            swatch.style.height = Length.Percent(100);
            swatch.style.borderTopLeftRadius = swatch.style.borderTopRightRadius = 120;
            swatch.style.borderBottomLeftRadius = swatch.style.borderBottomRightRadius = 120;
            dialogArt.Add(swatch);
            return;
        }

        var sprite = item.Category == CosmeticCategory.Ball ? item.Happy : item.Category == CosmeticCategory.Paddle ? item.PaddleSprite : item.Preview;
        dialogArt.style.backgroundImage = sprite != null ? new StyleBackground(sprite) : new StyleBackground(StyleKeyword.None);
    }

    private void SetPrimary(bool visible, string icon, string label)
    {
        dialogPrimary.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        dialogPrimary.SetEnabled(true);
        if (!visible) return;

        dialogPrimaryIcon.style.display = icon != null ? DisplayStyle.Flex : DisplayStyle.None;
        dialogPrimaryIcon.RemoveFromClassList("icon--play");
        dialogPrimaryIcon.RemoveFromClassList("icon--check");
        if (icon != null) dialogPrimaryIcon.AddToClassList("icon--" + icon);
        dialogPrimaryLabel.text = label;
    }

    public string GoldBalanceText => goldBalance != null ? goldBalance.text : null;
    public bool GoldOfferShown => dialogGold != null && dialogGold.style.display != DisplayStyle.None;

    private void ShowGoldBalance()
    {
        if (goldBalance != null) goldBalance.text = cosmetics.Gold.ToString();
    }

    private void SetGoldOffer(CosmeticItem item)
    {
        if (dialogGold == null) return;

        bool offered = item != null && cosmetics.OnSale(item);
        dialogGold.style.display = offered ? DisplayStyle.Flex : DisplayStyle.None;
        if (!offered) return;

        dialogGoldLabel.text = Loc.T("Unlock for {0} gold", item.GoldPrice);
        dialogGold.SetEnabled(cosmetics.Gold >= item.GoldPrice);
    }

    private void OnDialogGold()
    {
        var item = dialogItem;
        if (item == null || !cosmetics.BuyWithGold(item)) return;

        cosmetics.Equip(item);
        AnnounceSetRewards();
        FillItemDialog(item);
    }

    private void SetBuy(string productId)
    {
        string price = string.IsNullOrEmpty(productId) ? null : cosmetics.PriceOf(productId);
        dialogBuy.style.display = price != null ? DisplayStyle.Flex : DisplayStyle.None;
        if (price != null) dialogBuyLabel.text = Loc.T("Buy · {0}", price);
        dialogBuy.SetEnabled(!IsPurchasing);
    }

    private void OnDialogPrimary()
    {
        if (dialogMode == DialogMode.Pass)
        {
            StartPurchase(CosmeticCatalog.PassProductId);
            return;
        }

        var item = dialogItem;
        if (item == null) return;

        if (cosmetics.IsUnlocked(item))
        {
            cosmetics.Equip(item);
            CloseDialog();
            return;
        }

        if (waitingForAd) return;

        var ads = AdManager.Instance;
        bool adReady = ads != null && ads.IsRewardedReady;

        if (dialogDiscount)
        {
            waitingForAd = true;
            FillItemDialog(item);
            cosmetics.WatchAdForDiscount(item, bought =>
            {
                waitingForAd = false;
                if (bought)
                {
                    cosmetics.Equip(item);
                    AnnounceSetRewards();
                }
                else notify?.Invoke(adReady ? Loc.T("The ad was closed early, so it didn't count.") : Loc.T("No ad available right now. Please try again in a moment."));
                if (dialogItem == item) FillItemDialog(item);
            });
            return;
        }

        if (item.Unlock != UnlockKind.Ads) return;

        waitingForAd = true;
        FillItemDialog(item);
        cosmetics.WatchAdFor(item, earned =>
        {
            waitingForAd = false;
            if (!earned) notify?.Invoke(adReady ? Loc.T("The ad was closed early, so it didn't count.") : Loc.T("No ad available right now. Please try again in a moment."));
            if (dialogItem == item) FillItemDialog(item);
        });
    }

    private void OnDialogBuy()
    {
        if (dialogItem != null && dialogItem.IsPurchasable) StartPurchase(dialogItem.ProductId);
    }

    private static bool IsPurchasing => PurchaseService.Instance != null && PurchaseService.Instance.IsPurchasing;

    private void StartPurchase(string productId)
    {
        var store = PurchaseService.Instance;
        if (store == null || !store.IsReady)
        {
            notify?.Invoke(Loc.T("The store isn't available right now. Please try again later."));
            return;
        }

        if (store.IsPurchasing)
        {
            notify?.Invoke(Loc.T("A purchase is already in progress."));
            return;
        }

        if (!cosmetics.Buy(productId))
        {
            notify?.Invoke(Loc.T("This item can't be purchased right now."));
            return;
        }

        RefreshPurchaseState();
    }

    private void RefreshPurchaseState()
    {
        bool busy = IsPurchasing;
        passButton.SetEnabled(!busy);
        noAdsButton.SetEnabled(!busy);
        dialogBuy.SetEnabled(!busy);
        if (dialogMode == DialogMode.Pass) dialogPrimary.SetEnabled(!busy && cosmetics.PriceOf(CosmeticCatalog.PassProductId) != null);
    }

    private void OnPurchaseSucceeded(string productId)
    {
        if (Detached()) return;
        RefreshPurchaseState();
        AnnounceSetRewards();
        if (productId == CosmeticCatalog.RemoveAdsProductId) notify?.Invoke(Loc.T("Ads removed. Thank you!"));
        else if (productId == CosmeticCatalog.PassProductId) notify?.Invoke(Loc.T("Pingi Pass unlocked. Enjoy!"));
    }

    private void OnPurchaseFailed(string productId, string reason)
    {
        if (Detached()) return;
        RefreshPurchaseState();
        notify?.Invoke(PurchaseFailureMessage(reason));
    }

    private static string PurchaseFailureMessage(string reason)
    {
        return reason switch
        {
            "UserCancelled" or "OrderCancelled" => Loc.T("Purchase cancelled."),
            "Deferred" => Loc.T("Your purchase is waiting for approval."),
            "ExistingPurchasePending" or "DuplicateTransaction" => Loc.T("This purchase is already being processed."),
            "PurchasingUnavailable" or "StoreNotConnected" => Loc.T("The store isn't available right now. Please try again later."),
            "ProductUnavailable" => Loc.T("This item isn't available right now."),
            "PaymentDeclined" => Loc.T("Payment was declined."),
            "UserNotAuthenticated" => Loc.T("Please sign in to Google Play and try again."),
            _ => Loc.T("Purchase couldn't be completed. Please try again.")
        };
    }

    private string PriceLabel(string productId)
    {
        return cosmetics.PriceOf(productId) ?? "...";
    }
}
