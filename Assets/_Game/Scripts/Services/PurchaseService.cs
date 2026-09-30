using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public sealed class PurchaseService : MonoBehaviour
{
    public static PurchaseService Instance { get; private set; }

    public event Action ProductsChanged;
    public event Action<string> PurchaseSucceeded;
    public event Action<string, string> PurchaseFailed;

    private StoreController store;
    private List<ProductDefinition> definitions;
    private Func<string, string, bool> grant;
    private string pendingProductId;

    public bool IsReady { get; private set; }
    public bool IsPurchasing => !string.IsNullOrEmpty(pendingProductId);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public async void Initialize(List<ProductDefinition> products, Func<string, string, bool> grantAndSave)
    {
        if (store != null) return;

        definitions = products;
        grant = grantAndSave;
        store = UnityIAPServices.StoreController();

#if UNITY_EDITOR
        EnsureEditorEventSystem();
#endif

        store.OnPurchasePending += OnPurchasePending;
        store.OnPurchaseConfirmed += OnPurchaseConfirmed;
        store.OnPurchaseFailed += OnPurchaseFailed;
        store.OnPurchaseDeferred += OnPurchaseDeferred;
        store.OnStoreConnected += OnStoreConnected;
        store.OnStoreDisconnected += OnStoreDisconnected;
        store.OnProductsFetched += OnProductsFetched;
        store.OnProductsFetchFailed += OnProductsFetchFailed;
        store.OnPurchasesFetched += OnPurchasesFetched;
        store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;

        try
        {
            await store.Connect();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[IAP] Connect failed: {e.Message}");
        }
    }

#if UNITY_EDITOR
    private static void EnsureEditorEventSystem()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;

        var go = new GameObject("EditorEventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        DontDestroyOnLoad(go);
    }
#endif

    public string PriceOf(string productId)
    {
        if (!IsReady || string.IsNullOrEmpty(productId)) return null;
        var product = store.GetProductById(productId);
        return product != null && product.availableToPurchase ? product.metadata.localizedPriceString : null;
    }

    public bool Buy(string productId)
    {
        if (!IsReady || !string.IsNullOrEmpty(pendingProductId)) return false;

        var product = store.GetProductById(productId);
        if (product == null || !product.availableToPurchase) return false;

        pendingProductId = productId;
        store.PurchaseProduct(product);
        return true;
    }

    private void OnStoreConnected()
    {
        store.FetchProducts(definitions);
    }

    private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
    {
        IsReady = false;
        Debug.LogWarning($"[IAP] Store disconnected: {failure.Message}");
        ProductsChanged?.Invoke();
    }

    private void OnProductsFetched(List<Product> products)
    {
        IsReady = true;
        store.FetchPurchases();
        ProductsChanged?.Invoke();
    }

    private void OnProductsFetchFailed(ProductFetchFailed failure)
    {
        Debug.LogWarning($"[IAP] Product fetch failed: {failure.FailureReason}");
    }

    private void OnPurchasesFetched(Orders orders)
    {
        foreach (var order in orders.ConfirmedOrders)
            foreach (var item in order.CartOrdered.Items())
                grant?.Invoke(item.Product.definition.id, order.Info.TransactionID);

        ProductsChanged?.Invoke();
    }

    private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
    {
        Debug.LogWarning($"[IAP] Purchases fetch failed: {failure.Message}");
    }

    private void OnPurchasePending(PendingOrder order)
    {
        bool saved = true;
        string productId = null;

        foreach (var item in order.CartOrdered.Items())
        {
            productId = item.Product.definition.id;
            if (grant == null || !grant(productId, order.Info.TransactionID)) saved = false;
        }

        if (!saved)
        {
            Debug.LogError($"[IAP] Grant for {productId} was not saved; purchase left pending for re-delivery.");
            return;
        }

        store.ConfirmPurchase(order);
    }

    private void OnPurchaseConfirmed(Order order)
    {
        string productId = FirstProductId(order);

        switch (order)
        {
            case ConfirmedOrder:
                ClearPending(productId);
                PurchaseSucceeded?.Invoke(productId);
                break;
            case FailedOrder failed:
                ClearPending(productId);
                PurchaseFailed?.Invoke(productId, failed.FailureReason.ToString());
                break;
        }
    }

    private void OnPurchaseFailed(FailedOrder order)
    {
        string productId = FirstProductId(order);
        ClearPending(productId);
        PurchaseFailed?.Invoke(productId, order.FailureReason.ToString());
    }

    private void OnPurchaseDeferred(DeferredOrder order)
    {
        ClearPending(FirstProductId(order));
        PurchaseFailed?.Invoke(FirstProductId(order), "Deferred");
    }

    private void ClearPending(string productId)
    {
        if (productId == null || productId == pendingProductId) pendingProductId = null;
    }

    private static string FirstProductId(Order order)
    {
        foreach (var item in order.CartOrdered.Items()) return item.Product.definition.id;
        return null;
    }
}
