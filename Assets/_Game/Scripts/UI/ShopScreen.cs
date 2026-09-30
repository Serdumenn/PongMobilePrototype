using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class ShopScreen : UIScreen
{
    private const string ActiveTabClass = "tab--active";
    private const string DialogHiddenClass = "dialog--hidden";

    private enum DialogMode
    {
        None,
        Item,
        Pass
    }

    private readonly CosmeticsService cosmetics;
    private readonly Action<string> notify;
    private readonly Sprite passArt;
    private readonly VisualElement grid;
    private readonly ScrollView scroll;
    private readonly Dictionary<CosmeticCategory, Button> tabs = new Dictionary<CosmeticCategory, Button>();
    private readonly Button passButton;
    private readonly Label passPrice;
    private readonly Button noAdsButton;
    private readonly Label noAdsPrice;

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
    private readonly Button dialogPass;

    private CosmeticCategory category = CosmeticCategory.Ball;
    private DialogMode dialogMode;
    private CosmeticItem dialogItem;
    private bool waitingForAd;
    private int unlockedCount;

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
        dialogPass = root.Q<Button>("dialog-pass");

        var golden = cosmetics.CatalogAsset.Find("ball_golden");
        passArt = golden != null ? golden.Happy : null;
        if (passArt != null) root.Q("pass-art").style.backgroundImage = new StyleBackground(passArt);

        Bind("back-button", onBack);
        tabs[CosmeticCategory.Ball] = Bind("tab-ball", () => SelectCategory(CosmeticCategory.Ball));
        tabs[CosmeticCategory.Paddle] = Bind("tab-paddle", () => SelectCategory(CosmeticCategory.Paddle));
        tabs[CosmeticCategory.Theme] = Bind("tab-theme", () => SelectCategory(CosmeticCategory.Theme));
        Bind("pass-button", OpenPassDialog);
        Bind("noads-button", () => StartPurchase(CosmeticCatalog.RemoveAdsProductId));
        Bind("dialog-primary", OnDialogPrimary);
        Bind("dialog-buy", OnDialogBuy);
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
        waitingForAd = false;
        dialog.AddToClassList(DialogHiddenClass);
    }

    protected override void OnShow()
    {
        unlockedCount = CountUnlocked();
        CloseDialog();
        SelectCategory(category);
        scroll.scrollOffset = Vector2.zero;
    }

    protected override void OnHide()
    {
        CloseDialog();
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
        category = next;
        foreach (var pair in tabs) pair.Value?.EnableInClassList(ActiveTabClass, pair.Key == category);
        Refresh();
    }

    private void OnCosmeticsChanged()
    {
        if (!IsVisible) return;

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

        passButton.style.display = inventory.HasPass ? DisplayStyle.None : DisplayStyle.Flex;
        noAdsButton.style.display = inventory.NoAds ? DisplayStyle.None : DisplayStyle.Flex;
        passPrice.text = PriceLabel(CosmeticCatalog.PassProductId);
        noAdsPrice.text = PriceLabel(CosmeticCatalog.RemoveAdsProductId);

        grid.Clear();
        foreach (var item in cosmetics.CatalogAsset.InCategory(category))
            grid.Add(BuildCard(item));

        RefreshPurchaseState();
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

        var name = new Label(item.DisplayName) { pickingMode = PickingMode.Ignore };
        name.AddToClassList("item__name");
        face.Add(name);

        if (equipped) face.Add(Status("status--equipped", "check", "Equipped"));
        else if (unlocked) face.Add(Status("status--owned", null, "Use"));
        else if (item.Unlock == UnlockKind.Score) face.Add(Status("status--score", "lock", $"Best {item.UnlockValue}"));
        else if (item.Unlock == UnlockKind.Streak) face.Add(Status("status--streak", "flame", $"{item.UnlockValue} days"));
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
        else face.Add(Status("status--pass", null, "Pass"));

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

    private static VisualElement Blob(string modifier, Color color)
    {
        var blob = new VisualElement { pickingMode = PickingMode.Ignore };
        blob.AddToClassList("swatch__blob");
        blob.AddToClassList(modifier);
        blob.style.backgroundColor = color;
        return blob;
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
        FillPassDialog();
        dialog.RemoveFromClassList(DialogHiddenClass);
    }

    private void FillItemDialog(CosmeticItem item)
    {
        if (item == null) return;

        SetDialogArt(item);
        dialogTitle.text = item.DisplayName;
        dialogDots.Clear();

        bool unlocked = cosmetics.IsUnlocked(item);
        bool streakOnly = item.Unlock == UnlockKind.Streak;
        dialogPass.style.display = unlocked || streakOnly || cosmetics.Inventory.HasPass ? DisplayStyle.None : DisplayStyle.Flex;
        SetBuy(unlocked || streakOnly ? null : item.ProductId);

        if (unlocked)
        {
            dialogText.text = $"{item.DisplayName} is yours!";
            SetPrimary(true, "check", "Equip");
            return;
        }

        switch (item.Unlock)
        {
            case UnlockKind.Ads:
                int views = cosmetics.AdViews(item);
                dialogText.text = $"Watch {item.UnlockValue} ads to unlock {item.DisplayName}.\nYour progress is saved.";
                for (int i = 0; i < item.UnlockValue; i++)
                {
                    var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                    dot.AddToClassList("dot");
                    dot.EnableInClassList("dot--filled", i < views);
                    dialogDots.Add(dot);
                }
                SetPrimary(true, "play", waitingForAd ? "Loading..." : $"Watch ad ({views}/{item.UnlockValue})");
                dialogPrimary.SetEnabled(!waitingForAd);
                break;

            case UnlockKind.Score:
                int best = PlayerPrefs.GetInt(SoloScoreManager.BestScoreKey, 0);
                dialogText.text = $"Reach a Classic best of {item.UnlockValue} to unlock.\nYour best: {best}";
                SetPrimary(false, null, null);
                break;

            case UnlockKind.Streak:
                int streak = cosmetics.CurrentDayStreak;
                dialogText.text = $"Play {item.UnlockValue} days in a row to unlock.\nCurrent streak: {streak} {(streak == 1 ? "day" : "days")}";
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

            default:
                dialogText.text = "Exclusive to Pingi Pass.";
                SetPrimary(false, null, null);
                break;
        }
    }

    private void FillPassDialog()
    {
        dialogArt.RemoveFromClassList("dialog__art--paddle");
        dialogArt.RemoveFromClassList("dialog__art--theme");
        dialogArt.Clear();
        dialogArt.style.backgroundColor = StyleKeyword.Null;
        dialogArt.style.backgroundImage = passArt != null ? new StyleBackground(passArt) : new StyleBackground(StyleKeyword.None);

        dialogTitle.text = "Pingi Pass";
        dialogText.text = "Unlock every costume, paddle and theme,\nplus the exclusive Golden set.";
        dialogDots.Clear();
        dialogPass.style.display = DisplayStyle.None;
        dialogBuy.style.display = DisplayStyle.None;

        string price = cosmetics.PriceOf(CosmeticCatalog.PassProductId);
        SetPrimary(true, null, price != null ? $"Get Pass · {price}" : "Store unavailable");
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

        var sprite = item.Category == CosmeticCategory.Ball ? item.Happy : item.PaddleSprite;
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

    private void SetBuy(string productId)
    {
        string price = string.IsNullOrEmpty(productId) ? null : cosmetics.PriceOf(productId);
        dialogBuy.style.display = price != null ? DisplayStyle.Flex : DisplayStyle.None;
        if (price != null) dialogBuyLabel.text = $"Buy · {price}";
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

        if (item.Unlock != UnlockKind.Ads || waitingForAd) return;

        var ads = AdManager.Instance;
        bool adReady = ads != null && ads.IsRewardedReady;

        waitingForAd = true;
        FillItemDialog(item);
        cosmetics.WatchAdFor(item, earned =>
        {
            waitingForAd = false;
            if (!earned) notify?.Invoke(adReady ? "The ad was closed early, so it didn't count." : "No ad available right now. Please try again in a moment.");
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
            notify?.Invoke("The store isn't available right now. Please try again later.");
            return;
        }

        if (store.IsPurchasing)
        {
            notify?.Invoke("A purchase is already in progress.");
            return;
        }

        if (!cosmetics.Buy(productId))
        {
            notify?.Invoke("This item can't be purchased right now.");
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
        RefreshPurchaseState();
        if (productId == CosmeticCatalog.RemoveAdsProductId) notify?.Invoke("Ads removed. Thank you!");
        else if (productId == CosmeticCatalog.PassProductId) notify?.Invoke("Pingi Pass unlocked. Enjoy!");
    }

    private void OnPurchaseFailed(string productId, string reason)
    {
        RefreshPurchaseState();
        notify?.Invoke(PurchaseFailureMessage(reason));
    }

    private static string PurchaseFailureMessage(string reason)
    {
        return reason switch
        {
            "UserCancelled" or "OrderCancelled" => "Purchase cancelled.",
            "Deferred" => "Your purchase is waiting for approval.",
            "ExistingPurchasePending" or "DuplicateTransaction" => "This purchase is already being processed.",
            "PurchasingUnavailable" or "StoreNotConnected" => "The store isn't available right now. Please try again later.",
            "ProductUnavailable" => "This item isn't available right now.",
            "PaymentDeclined" => "Payment was declined.",
            "UserNotAuthenticated" => "Please sign in to Google Play and try again.",
            _ => "Purchase couldn't be completed. Please try again."
        };
    }

    private string PriceLabel(string productId)
    {
        return cosmetics.PriceOf(productId) ?? "...";
    }
}
