using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Purchasing;

public sealed class CosmeticsService : MonoBehaviour
{
    private const string SaveFileName = "cosmetics.json";

    [Header("Data")]
    [SerializeField] private CosmeticCatalog Catalog;

    [Header("Refs")]
    [SerializeField] private SoloGameManager Game;
    [SerializeField] private PurchaseService Purchases;
    [SerializeField] private RecordsService Records;

    public event Action Changed;

    public CosmeticCatalog CatalogAsset => Catalog;
    public CosmeticInventory Inventory { get; private set; }
    public List<CosmeticItem> PendingRewards { get; } = new List<CosmeticItem>();

    private void Awake()
    {
        if (Game == null) Game = FindFirstObjectByType<SoloGameManager>();
        if (Purchases == null) Purchases = FindFirstObjectByType<PurchaseService>();
        if (Records == null) Records = FindFirstObjectByType<RecordsService>();

        Inventory = CosmeticInventory.Load(Path.Combine(Application.persistentDataPath, SaveFileName));
        EnsureEquippedDefaults();
        GrantScoreUnlocks(PlayerPrefs.GetInt(SoloScoreManager.BestScoreKey, 0), false);
        Inventory.Save();
    }

    public int CurrentDayStreak => Records != null && Records.Records != null ? Records.DayStreak : 0;

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
