using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public sealed class CosmeticsService : MonoBehaviour
{
    private const string SaveFileName = "cosmetics.json";

    [Header("Data")]
    [SerializeField] private CosmeticCatalog Catalog;
    [SerializeField] private ShopContent Content;

    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private PurchaseService Purchases;
    [SerializeField] private RecordsService Records;

    public event Action Changed;

    public CosmeticCatalog CatalogAsset => Catalog;
    public ShopContent Shop => Content;
    public Func<DateTime> Clock { get; set; } = () => DateTime.Now;
    public int Today => ShopClock.DayIndex(Clock());
    public CosmeticInventory Inventory { get; private set; }
    public List<CosmeticItem> PendingRewards { get; } = new List<CosmeticItem>();

    private void Awake()
    {
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Purchases == null) Purchases = FindFirstObjectByType<PurchaseService>();
        if (Records == null) Records = FindFirstObjectByType<RecordsService>();

        Inventory = CosmeticInventory.Load(SaveLocation.PathFor(SaveFileName));
        EnsureEquippedDefaults();
        GrantScoreUnlocks(PlayerPrefs.GetInt(SoloScoreManager.BestScoreKey, 0), false);
        GrantSetRewards(false);
        Inventory.Save();
    }

    public int Stars => Inventory != null ? Inventory.Stars : 0;
    public int Gold => Inventory != null ? Inventory.Gold : 0;
    public int Xp => Inventory != null ? Inventory.Xp : 0;
    public int RoadLevel => Inventory != null ? Inventory.RoadLevel : 1;

    public void MarkXpSeeded()
    {
        if (Inventory == null || Inventory.XpSeeded) return;

        Inventory.MarkXpSeeded();
        Inventory.Save();
    }

    public int LevelStarsAt(int index)
    {
        return Inventory != null ? Inventory.LevelStarsAt(index) : 0;
    }

    public bool RecordLevel(int index, int stars, int gold)
    {
        if (Inventory == null) return false;

        bool better = Inventory.RecordLevel(index, stars);
        Inventory.EarnGold(gold);
        if (!better && gold <= 0) return false;

        Inventory.Save();
        Changed?.Invoke();
        return better;
    }

    public void AddProgress(int xp, int level, int gold, int stars)
    {
        if (Inventory == null) return;

        Inventory.AddXp(xp);
        Inventory.ReachLevel(level);
        Inventory.EarnGold(gold);
        if (stars > 0)
        {
            Inventory.AddStars(stars);
            GrantWhere(UnlockKind.Stars, Inventory.Stars, true);
        }
        GrantWhere(UnlockKind.Level, Inventory.RoadLevel, true);
        Inventory.Save();
        Changed?.Invoke();
    }

    public void EarnGold(int amount)
    {
        if (amount <= 0 || Inventory == null) return;

        Inventory.EarnGold(amount);
        Inventory.Save();
        Changed?.Invoke();
    }

    public bool CanBuyWithGold(CosmeticItem item)
    {
        return item != null && !Inventory.IsUnlocked(item) && OnSale(item) && Gold >= item.GoldPrice;
    }

    public bool BuyWithGold(CosmeticItem item)
    {
        if (!CanBuyWithGold(item) || !Inventory.SpendGold(item.GoldPrice)) return false;

        Inventory.Grant(item.Id);
        GrantSetRewards(true);
        Inventory.Save();
        Changed?.Invoke();
        return true;
    }

    public bool OnSale(CosmeticItem item)
    {
        if (item == null) return false;
        if (Content == null) return item.SoldForGold && item.Unlock != UnlockKind.Season;
        return ShopRules.OnSale(item, Content.Seasons, item.Unlock == UnlockKind.Showcase ? ShowcaseToday() : null, Clock());
    }

    public IReadOnlyList<CosmeticItem> ShowcaseToday()
    {
        var list = new List<CosmeticItem>();
        if (Content == null || Inventory == null) return list;

        int day = Today;
        var kept = Inventory.ShowcaseFor(day);
        if (kept != null)
        {
            foreach (var id in kept)
            {
                var item = Catalog.Find(id);
                if (item != null) list.Add(item);
            }
            return list;
        }

        list = Showcase.Pick(Catalog.Items, Inventory.IsUnlocked, Inventory.ShopSeed, day, Content.ShowcaseSize);
        var ids = new List<string>();
        foreach (var item in list) ids.Add(item.Id);
        Inventory.KeepShowcase(day, ids);
        Inventory.Save();
        return list;
    }

    public bool InShowcase(CosmeticItem item)
    {
        if (item == null) return false;
        foreach (var entry in ShowcaseToday())
            if (entry == item) return true;
        return false;
    }

    public int SecondsToNewShowcase => ShopClock.SecondsToMidnight(Clock());
    public bool DiscountReady => Inventory != null && Inventory.ShowcaseDiscountDay < Today;
    public float Discount => Content != null ? Content.AdDiscount : 0f;

    public int DiscountedPrice(CosmeticItem item)
    {
        return item != null ? ShopRules.Discounted(item.GoldPrice, Discount) : 0;
    }

    public bool CanBuyDiscounted(CosmeticItem item)
    {
        return item != null && Discount > 0f && DiscountReady && InShowcase(item) && !Inventory.IsUnlocked(item) && Gold >= DiscountedPrice(item);
    }

    public bool BuyDiscounted(CosmeticItem item)
    {
        if (!CanBuyDiscounted(item)) return false;

        int price = DiscountedPrice(item);
        if (!Inventory.UseShowcaseDiscount(Today) || !Inventory.SpendGold(price)) return false;

        Inventory.Grant(item.Id);
        GrantSetRewards(true);
        Inventory.Save();
        Changed?.Invoke();
        return true;
    }

    public void WatchAdForDiscount(CosmeticItem item, Action<bool> onDone)
    {
        var ads = AdManager.Instance;
        if (!CanBuyDiscounted(item) || ads == null)
        {
            onDone?.Invoke(false);
            return;
        }

        bool bought = false;
        ads.ShowRewarded(() => bought = BuyDiscounted(item), earned => onDone?.Invoke(earned && bought));
    }

    public SeasonDef CurrentSeason => Content != null ? SeasonWindow.Current(Content.Seasons, Clock()) : null;

    public int SeasonDaysLeft(SeasonDef season)
    {
        return SeasonWindow.DaysLeft(season, Clock());
    }

    public IReadOnlyList<CosmeticSet> Sets => Content != null ? (IReadOnlyList<CosmeticSet>)Content.Sets : Array.Empty<CosmeticSet>();

    public int SetProgress(CosmeticSet set)
    {
        return Inventory != null ? CosmeticSets.Progress(set, Inventory.IsUnlocked) : 0;
    }

    public int BoxPrice => Content != null ? Content.BoxPrice : 0;

    public Dictionary<CosmeticTier, float> BoxOdds()
    {
        if (Content == null || Inventory == null) return new Dictionary<CosmeticTier, float>();
        return SurpriseBox.Odds(Catalog.Items, Inventory.IsUnlocked, Content.BoxCommon, Content.BoxRare, Content.BoxEpic);
    }

    public bool BoxHasItems => BoxOdds().Count > 0;
    public bool CanOpenBox => Content != null && BoxHasItems && Gold >= Content.BoxPrice;

    public CosmeticItem OpenBox()
    {
        return OpenBox(UnityEngine.Random.value, UnityEngine.Random.value);
    }

    public CosmeticItem OpenBox(float tierRoll, float itemRoll)
    {
        if (!CanOpenBox) return null;

        var item = SurpriseBox.Draw(Catalog.Items, Inventory.IsUnlocked, Content.BoxCommon, Content.BoxRare, Content.BoxEpic, tierRoll, itemRoll);
        if (item == null || !Inventory.SpendGold(Content.BoxPrice)) return null;

        Inventory.Grant(item.Id);
        GrantSetRewards(true);
        Inventory.Save();
        Changed?.Invoke();
        return item;
    }

    private bool GrantSetRewards(bool announce)
    {
        bool any = false;
        foreach (var reward in CosmeticSets.RewardsDue(Sets, Inventory.IsUnlocked))
        {
            if (!Inventory.Grant(reward.Id)) continue;
            any = true;
            if (announce) PendingRewards.Add(reward);
        }
        return any;
    }

    public void AddStars(int count)
    {
        if (count <= 0 || Inventory == null) return;

        Inventory.AddStars(count);
        GrantWhere(UnlockKind.Stars, Inventory.Stars, true);
        Inventory.Save();
        Changed?.Invoke();
    }

    public CosmeticItem NextStarReward()
    {
        CosmeticItem next = null;
        foreach (var item in Catalog.Items)
        {
            if (item == null || item.Unlock != UnlockKind.Stars || Inventory.IsUnlocked(item)) continue;
            if (next == null || item.UnlockValue < next.UnlockValue) next = item;
        }
        return next;
    }

    public int CurrentDayStreak => Records != null && Records.Records != null ? Records.DayStreak : 0;

    public void RestoreInventory(string json)
    {
        var restored = CosmeticInventory.FromJson(SaveLocation.PathFor(SaveFileName), json);
        restored.KeepOrdersFrom(Inventory);
        Inventory = restored;
        EnsureEquippedDefaults();
        GrantScoreUnlocks(PlayerPrefs.GetInt(SoloScoreManager.BestScoreKey, 0), false);
        GrantWhere(UnlockKind.Stars, Inventory.Stars, false);
        GrantWhere(UnlockKind.Level, Inventory.RoadLevel, false);
        GrantSetRewards(false);
        Inventory.Save();
        ApplyAdRules();
        Changed?.Invoke();
    }

    private void Start()
    {
        if (Game != null) Game.StateChanged += OnGameStateChanged;
        ApplyAdRules();

        if (Purchases != null) Purchases.Initialize(BuildProductDefinitions(), GrantProduct);
    }

    private void OnDestroy()
    {
        if (Game != null) Game.StateChanged -= OnGameStateChanged;
    }

    public CosmeticItem Equipped(CosmeticCategory category)
    {
        var item = Catalog.Find(Inventory.GetEquipped(category));
        return item != null && Inventory.IsUnlocked(item) ? item : Catalog.DefaultFor(category);
    }

    public bool IsUnlocked(CosmeticItem item)
    {
        return Inventory.IsUnlocked(item);
    }

    public string FrameId
    {
        get
        {
            var frame = Inventory != null ? Equipped(CosmeticCategory.Frame) : null;
            return frame != null && frame.Unlock != UnlockKind.Free ? frame.Id : "";
        }
    }

    public string FrameClassFor(string frameId)
    {
        if (string.IsNullOrEmpty(frameId)) return null;
        var item = Catalog.Find(frameId);
        return item != null && item.Category == CosmeticCategory.Frame ? item.FrameClass : null;
    }

    public bool IsEquipped(CosmeticItem item)
    {
        return item != null && Equipped(item.Category) == item;
    }

    public int AdViews(CosmeticItem item)
    {
        return Inventory.AdViews(item);
    }

    public bool Equip(CosmeticItem item)
    {
        if (item == null || !Inventory.IsUnlocked(item)) return false;

        Inventory.SetEquipped(item.Category, item.Id);
        Inventory.Save();
        Changed?.Invoke();
        return true;
    }

    public void WatchAdFor(CosmeticItem item, Action<bool> onDone)
    {
        var ads = AdManager.Instance;
        if (item == null || item.Unlock != UnlockKind.Ads || ads == null)
        {
            onDone?.Invoke(false);
            return;
        }

        ads.ShowRewarded(() =>
        {
            Inventory.AddAdView(item);
            Inventory.Save();
            Changed?.Invoke();
        }, earned => onDone?.Invoke(earned));
    }

    public bool Buy(string productId)
    {
        return Purchases != null && Purchases.Buy(productId);
    }

    public string PriceOf(string productId)
    {
        return Purchases != null ? Purchases.PriceOf(productId) : null;
    }

    public CosmeticItem TakeSetReward()
    {
        for (int i = 0; i < PendingRewards.Count; i++)
        {
            var item = PendingRewards[i];
            if (item == null || item.Unlock != UnlockKind.SetReward) continue;
            PendingRewards.RemoveAt(i);
            return item;
        }
        return null;
    }

    public CosmeticItem TakeNextReward()
    {
        if (PendingRewards.Count == 0) return null;
        var item = PendingRewards[0];
        PendingRewards.RemoveAt(0);
        return item;
    }

    private void OnGameStateChanged(SoloGameManager.GameState state)
    {
        if (state != SoloGameManager.GameState.GameOver) return;

        bool scoreUnlocks = GrantScoreUnlocks(Game.ScoreManager.BestFor(SoloScoreManager.BestScoreKey), true);
        bool streakUnlocks = GrantStreakUnlocks(CurrentDayStreak);

        if (scoreUnlocks || streakUnlocks)
        {
            Inventory.Save();
            Changed?.Invoke();
        }
    }

    public bool EvaluateStreakRewards()
    {
        if (!GrantStreakUnlocks(CurrentDayStreak)) return false;

        Inventory.Save();
        Changed?.Invoke();
        return true;
    }

    private bool GrantScoreUnlocks(int best, bool announce)
    {
        return GrantWhere(UnlockKind.Score, best, announce);
    }

    private bool GrantStreakUnlocks(int streak)
    {
        return GrantWhere(UnlockKind.Streak, streak, true);
    }

    private bool GrantWhere(UnlockKind kind, int progress, bool announce)
    {
        bool any = false;
        foreach (var item in Catalog.Items)
        {
            if (item == null || item.Unlock != kind || progress < item.UnlockValue) continue;
            if (!Inventory.Grant(item.Id)) continue;

            any = true;
            if (announce) PendingRewards.Add(item);
        }
        return any;
    }

    private bool GrantProduct(string productId, string orderId)
    {
        if (Inventory.IsOrderProcessed(orderId)) return true;

        if (productId == CosmeticCatalog.PassProductId) Inventory.GrantPass();
        else if (productId == CosmeticCatalog.RemoveAdsProductId) Inventory.GrantNoAds();
        else
        {
            var item = Catalog.FindByProduct(productId);
            if (item == null) return false;
            Inventory.Grant(item.Id);
        }

        GrantSetRewards(true);
        Inventory.MarkOrderProcessed(orderId);
        if (!Inventory.Save()) return false;

        ApplyAdRules();
        Changed?.Invoke();
        return true;
    }

    private void ApplyAdRules()
    {
        if (AdManager.Instance != null) AdManager.Instance.InterstitialsDisabled = Inventory.NoAds;
    }

    private void EnsureEquippedDefaults()
    {
        foreach (CosmeticCategory category in Enum.GetValues(typeof(CosmeticCategory)))
        {
            var current = Catalog.Find(Inventory.GetEquipped(category));
            if (current == null || !Inventory.IsUnlocked(current))
            {
                var fallback = Catalog.DefaultFor(category);
                if (fallback != null) Inventory.SetEquipped(category, fallback.Id);
            }
        }
    }

    private List<ProductDefinition> BuildProductDefinitions()
    {
        var products = new List<ProductDefinition>
        {
            new ProductDefinition(CosmeticCatalog.PassProductId, ProductType.NonConsumable),
            new ProductDefinition(CosmeticCatalog.RemoveAdsProductId, ProductType.NonConsumable)
        };

        foreach (var item in Catalog.Items)
            if (item != null && item.IsPurchasable)
                products.Add(new ProductDefinition(item.ProductId, ProductType.NonConsumable));

        return products;
    }
}
