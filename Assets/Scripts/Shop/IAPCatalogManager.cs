using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

public class IAPCatalogManager : MonoBehaviour
{
    public static IAPCatalogManager Instance { get; private set; }

    private StoreController _store;
    private bool _eventsSubscribed = false;               // чтобы не подписываться на события по 100 раз
    private readonly Dictionary<string, PendingOrder> _pending = new();

    // последние загруженные продукты
    private List<Product> _lastProducts;

    [Header("Advanced")]
    [SerializeField] private bool useServerValidation = false;

    // События для UI
    public event Action OnConnected;
    public event Action<List<Product>> OnProductsReady;
    public event Action<string> OnPurchaseSucceeded;      // productId
    public event Action<string, string> OnPurchaseFailed; // productId, reason
    public event Action<string> OnDeferred;               // productId

    private string _removeAdsProductId = "remove_ads";
    private string _removeAdsSubProductId = "remove_ads_subscription"; // subscription

    public bool ProductsReadyNow => _lastProducts != null && _lastProducts.Count > 0;
    public List<Product> GetProductsSnapshot() =>
        _lastProducts != null ? new List<Product>(_lastProducts) : new List<Product>();

    // ---------------- LIFECYCLE ----------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        await InitializeFromIapCatalogForGooglePlay();
    }

    private void OnDestroy()
    {
        UnsubscribeFromStoreEvents();
    }

    // ---------------- ПОДПИСКА ДЛЯ UI ----------------

    /// Удобная подписка: если продукты уже загружены, сразу вызовем колбэк
    public void AddProductsReadyListener(Action<List<Product>> handler, bool invokeIfReady = true)
    {
        OnProductsReady += handler;

        if (invokeIfReady && ProductsReadyNow)
            handler(GetProductsSnapshot());
    }

    // ---------------- ИНИЦИАЛИЗАЦИЯ IAP ----------------

    public async System.Threading.Tasks.Task InitializeFromIapCatalogForGooglePlay()
    {
        // Защита от повторной инициализации в одном запуске
        if (_store != null && _eventsSubscribed)
        {
            Debug.Log("[IAP] Already initialized, skip.");
            return;
        }

        _store = UnityIAPServices.StoreController(); // текущая платформа (на Android -> Google Play)

        SubscribeToStoreEvents();

        try
        {
            // Подключаемся к стору
            await _store.Connect();
        }
        catch (Exception e)
        {
            Debug.LogError($"[IAP] Connect failed: {e}");
            return;
        }

        // Тянем список продуктов из IAP Catalog и запрашиваем детали у стора
        var defs = BuildProductDefinitionsForStore("GooglePlay");
        _store.FetchProductsWithNoRetries(defs);

        // Подтянуть уже купленные (non-consumable/подписки) — для "рестора"
        _store.FetchPurchases();
    }

    // Подписка на события StoreController (делаем ОДИН раз)
    private void SubscribeToStoreEvents()
    {
        if (_store == null || _eventsSubscribed)
            return;

        _store.OnProductsFetched += HandleProductsFetched;
        _store.OnProductsFetchFailed += HandleProductsFetchFailed;
        _store.OnPurchasesFetched += HandlePurchasesFetched;
        _store.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;
        _store.OnPurchaseDeferred += HandlePurchaseDeferred;
        _store.OnPurchasePending += HandlePurchasePending;
        _store.OnPurchaseConfirmed += HandlePurchaseConfirmed;
        _store.OnPurchaseFailed += HandlePurchaseFailed;
        _store.OnStoreDisconnected += HandleStoreDisconnected;
        _store.OnCheckEntitlement += HandleCheckEntitlement;
        _eventsSubscribed = true;
    }

    // Отписка (на всякий случай, при уничтожении объекта)
    private void UnsubscribeFromStoreEvents()
    {
        if (_store == null || !_eventsSubscribed)
            return;

        _store.OnProductsFetched -= HandleProductsFetched;
        _store.OnProductsFetchFailed -= HandleProductsFetchFailed;
        _store.OnPurchasesFetched -= HandlePurchasesFetched;
        _store.OnPurchasesFetchFailed -= HandlePurchasesFetchFailed;
        _store.OnPurchaseDeferred -= HandlePurchaseDeferred;
        _store.OnPurchasePending -= HandlePurchasePending;
        _store.OnPurchaseConfirmed -= HandlePurchaseConfirmed;
        _store.OnPurchaseFailed -= HandlePurchaseFailed;
        _store.OnStoreDisconnected -= HandleStoreDisconnected;
        _store.OnCheckEntitlement -= HandleCheckEntitlement;
        _eventsSubscribed = false;
    }

    // ---------------- ОБРАБОТЧИКИ СОБЫТИЙ STORE ----------------

    private void HandleProductsFetched(List<Product> products)
    {
        Debug.Log($"[IAP] OnProductsFetched: {products?.Count ?? 0} items");
        _lastProducts = products;
        RefreshNoAdsEntitlements();

        if (products != null)
        {
            foreach (var p in products)
            {
                if (p == null) continue;
            }
        }
        OnProductsReady?.Invoke(products);
        OnConnected?.Invoke();
    }


    private void HandleProductsFetchFailed(ProductFetchFailed fail)
    {
        Debug.LogError($"[IAP] FetchProducts failed: {fail?.FailureReason} | " +
                       $"{(fail?.FailedFetchProducts != null ? fail.FailedFetchProducts.Count : 0)} products");
    }

    private void HandlePurchasesFetched(Orders orders)
    {
        RefreshNoAdsEntitlements();
        // Здесь можете восстановить энтайтлы/подписки
        // Debug.Log($"[IAP] OnPurchasesFetched: {orders?.Count ?? 0} orders");
    }

    private void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription fail)
    {
        Debug.LogWarning($"[IAP] FetchPurchases failed: {fail?.FailureReason} | {fail?.Message}");
    }

    private void HandlePurchaseDeferred(DeferredOrder deferred)
    {
        Debug.Log("[IAP] OnPurchaseDeferred");

        foreach (var item in deferred.CartOrdered.Items())
        {
            var id = item.Product.definition.id;
            Debug.Log($"[IAP]   deferred: {id}");
            OnDeferred?.Invoke(id);
        }
    }

    private void HandlePurchasePending(PendingOrder pending)
    {
        Debug.Log("[IAP] OnPurchasePending");

        // Сохраняем pending по productId
        foreach (var item in pending.CartOrdered.Items())
        {
            var id = item.Product.definition.id;
            _pending[id] = pending;
            Debug.Log($"[IAP]   pending: {id}");
        }

        if (useServerValidation)
        {
            Debug.Log("[IAP]   waiting server validation (do NOT confirm yet)");
            // TODO: отправить pending.Info.Receipt на сервер.
            // После успешной валидации вызовите ConfirmPending(productId).
        }
        else
        {
            Debug.Log("[IAP]   client-only → ConfirmPurchase now");
            _store.ConfirmPurchase(pending);
        }
    }

    private void HandlePurchaseConfirmed(Order order)
    {
        Debug.Log("[IAP] OnPurchaseConfirmed");

        if (order?.CartOrdered == null)
        {
            Debug.LogWarning("[IAP]   Order or cart is null");
            return;
        }

        // Заказ подтвержден — выдаем награды по всем позициям (обычно 1)
        foreach (var item in order.CartOrdered.Items())
        {
            var product = item.Product;
            if (product == null) continue;

            ApplyPayouts(product);
            RefreshNoAdsEntitlements();
            OnPurchaseSucceeded?.Invoke(product.definition.id);
            _pending.Remove(product.definition.id);
        }
    }

    private void HandlePurchaseFailed(FailedOrder failed)
    {
        string productId = "";
        if (failed?.CartOrdered?.Items() != null && failed.CartOrdered.Items().Count > 0)
            productId = failed.CartOrdered.Items()[0].Product.definition.id;

        var msg = failed?.Info?.TransactionID ?? "Unknown";

        OnPurchaseFailed?.Invoke(productId, msg);
        Debug.LogWarning($"[IAP] OnPurchaseFailed: {productId} | {failed?.FailureReason} | {msg}");
    }

    private void HandleStoreDisconnected(StoreConnectionFailureDescription desc)
    {
        Debug.LogWarning($"[IAP] Disconnected: {desc?.message} {desc?.Message}");
    }

    // ---------------- КАТАЛОГ ПРОДУКТОВ ----------------

    /// Собираем ProductDefinition из IAP Catalog
    private List<ProductDefinition> BuildProductDefinitionsForStore(string storeName)
    {
        var cat = ProductCatalog.LoadDefaultCatalog(); // JSON каталога из проекта
        if (cat == null || cat.allProducts == null)
        {
            Debug.LogError("[IAP] ProductCatalog is empty");
            return new List<ProductDefinition>();
        }

        var defs = new List<ProductDefinition>(cat.allProducts.Count);

        foreach (var item in cat.allProducts)
        {
            if (item == null) continue;

            // Если в каталоге задан override для конкретного стора — используем его, иначе берем общий id
            string storeSpecific = item.GetStoreID(storeName) ?? item.id;

            // Переносим payouts (если настроены в каталоге)
            var payouts = item.Payouts?.Select(p =>
                new PayoutDefinition(p.typeString, p.subtype, p.quantity, p.data)
            ).ToList();

            ProductDefinition def;

            if (payouts != null && payouts.Count > 0)
                def = new ProductDefinition(item.id, storeSpecific, item.type, true, payouts);
            else
                def = new ProductDefinition(item.id, storeSpecific, item.type, true);

            defs.Add(def);
        }

        return defs;
    }

    // ---------------- ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ UI ----------------

    public void Buy(string productId)
    {
        if (_store == null)
        {
            Debug.LogWarning("[IAP] Store not ready");
            return;
        }

        Debug.Log($"[IAP] Buy pressed: {productId}");
        _store.PurchaseProduct(productId);
    }

    // На Android обычно не требуется, но оставлено для кроссплат-кода
    public void Restore()
    {
        if (_store == null)
        {
            Debug.LogWarning("[IAP] Store not ready");
            return;
        }

        _store.RestoreTransactions((ok, err) =>
            Debug.Log($"[IAP] RestoreTransactions => {ok} {err}")
        );
    }


    public void RefreshNoAdsEntitlements()
    {
        if (_store == null) return;

        var oneTime = _store.GetProductById(_removeAdsProductId);
        if (oneTime != null) _store.CheckEntitlement(oneTime);

        var sub = _store.GetProductById(_removeAdsSubProductId);
        if (sub != null) _store.CheckEntitlement(sub);
    }



    private void HandleCheckEntitlement(Entitlement e)
    {
        var id = e?.Product?.definition?.id;
        if (string.IsNullOrEmpty(id)) return;

        bool entitled =
            e.Status == EntitlementStatus.FullyEntitled ||
            e.Status == EntitlementStatus.EntitledButNotFinished;

        if (id == _removeAdsProductId)
        {
            if (entitled)
                NoAdsManager.SetOwned(true);
        }

        // 2) подписка — только runtime
        if (id == _removeAdsSubProductId)
        {
            NoAdsManager.SetSubscriptionActive(entitled);
        }
    }




    // ---------------- Применение Payouts из каталога ----------------

    private void ApplyPayouts(Product product)
    {
        if (product?.definition?.payouts == null)
        {
            Debug.LogWarning($"[IAP] No payouts for product {product?.definition?.id}");
            return;
        }

        foreach (var payout in product.definition.payouts)
        {
            var type = payout.typeString?.ToLowerInvariant();
            var subtype = payout.subtype?.ToLowerInvariant();
            var qty = (int)Math.Round(payout.quantity);

            if (type == "currency" && (subtype == "chips" || subtype == "coins"))
            {
                GrantChips(qty);

                if (qty >= 5000)
                {
                    NoAdsManager.SetOwned(true);
                }
            }
            else if (type == "other" && subtype == "remove_ads")
            {
                if (product.definition.id == _removeAdsProductId)
                {
                    print($"ApplyPayouts: NoAdsManager.SetOwned(true)");
                    NoAdsManager.SetOwned(true);
                }
            }
        }
    }

    // ======== Ваша игровая логика (замените на свою при желании) ========
    private void GrantChips(int amount)
    {
        var balance = FindFirstObjectByType<BalanceManager>();
        if (balance != null)
        {
            balance.AddWinnings(amount, "ShopProductForRealMoney");
        }
        else
        {
            Debug.LogWarning("[IAP] BalanceManager not found");
        }

        var shopManagerInstance = FindFirstObjectByType<ShopManager>();
        if (shopManagerInstance != null)
        {
            DestroyImmediate(shopManagerInstance.gameObject);
        }

        Debug.Log($"[IAP] +{amount} chips"); // добавить валюту и сохранить
    }



    // ---------------- Работа с ценами ----------------
    public bool TryGetStorePriceLabel(string productId, out string label)
    {
        label = null;

        var p = _store?.GetProductById(productId);
        if (p?.metadata == null) return false;

        // Иногда метаданные есть, но число ещё 0 — считаем, что цена не готова
        if (p.metadata.localizedPrice > 0)
        {
            label = p.metadata.localizedPriceString; // напр. "€2.49"
            return true;
        }

        return false;
    }
}
