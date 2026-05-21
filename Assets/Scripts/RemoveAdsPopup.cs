using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;
using Firebase.Analytics;

public class RemoveAdsPopup : Popup
{
    [Header("IAP")]
    [SerializeField] private string _removeAdsProductId = "remove_ads";          // one-time
    [SerializeField] private string _removeAdsSubProductId = "remove_ads_subscription";  // subscription
    [SerializeField] private float _fallbackUsd = 2.99f;

    [Header("Trial UI text")]
    [SerializeField] private int _trialDays = 3;
    [SerializeField] private string _perPeriodLabel = "per week"; // или "в месяц"
    [TextArea]
    [SerializeField] private string _trialInfoFormat = "{0}-day free trial, then {1} {2}";
    // пример результата: "3-day free trial, then $5.99 per month"

    [Header("UI")]
    [SerializeField] private Button _buyButton;          // верхняя кнопка (one-time)
    [SerializeField] private Button _subscribeButton;    // нижняя кнопка (trial/sub)
    [SerializeField] private Button _closeButton;

    [SerializeField] private Text _oneTimePriceText; // текст на верхней кнопке: "$2.99"
    [SerializeField] private Text _trialInfoText;    // мелкий текст снизу: "3-day free trial, then ..."

    private IAPCatalogManager _iap;

    // ===== ANALYTICS =====
    private float _openedAtRealtime;
    private string _closeReason = "unknown";
    private bool _closeLogged;

    private static int B(bool v) => v ? 1 : 0;

    private void LogEvent(string name, params Parameter[] parameters)
    {
        AnalyticsSafe.LogEvent(name, parameters);
    }

    private Product FindProduct(List<Product> products, string productId)
    {
        if (string.IsNullOrEmpty(productId)) return null;
        return products?.FirstOrDefault(p => p != null && p.definition != null && p.definition.id == productId);
    }

    private void AddPriceParams(List<Parameter> list, Product p)
    {
        if (p == null || p.metadata == null) return;

        // localizedPrice — decimal, Parameter принимает double
        double price = (double)p.metadata.localizedPrice;
        if (price > 0)
            list.Add(new Parameter("price", price));

        if (!string.IsNullOrEmpty(p.metadata.isoCurrencyCode))
            list.Add(new Parameter("currency", p.metadata.isoCurrencyCode));

        if (!string.IsNullOrEmpty(p.metadata.localizedPriceString))
            list.Add(new Parameter("price_str", p.metadata.localizedPriceString));
    }
    // ===== /ANALYTICS =====

    private void Awake()
    {
        if (_buyButton != null) _buyButton.onClick.AddListener(OnBuyClick);
        if (_subscribeButton != null) _subscribeButton.onClick.AddListener(OnSubscribeClick);
        if (_closeButton != null) _closeButton.onClick.AddListener(OnCloseClick);
    }

    private void OnEnable()
    {
        _openedAtRealtime = Time.realtimeSinceStartup;
        _closeReason = "unknown";
        _closeLogged = false;

        bool alreadyOwned = NoAdsManager.IsOwned;

        LogEvent("remove_ads_popup_open",
            new Parameter("already_owned", B(alreadyOwned)),
            new Parameter("trial_days", _trialDays),
            new Parameter("per_period_label", _perPeriodLabel ?? "")
        );

        if (alreadyOwned)
        {
            _closeReason = "already_owned";
            CloseImmediate();
            return;
        }

        _iap = IAPCatalogManager.Instance;

        SetOneTimePrice("…");
        SetTrialInfo("…");

        RefreshLabels();

        if (_iap != null)
        {
            _iap.AddProductsReadyListener(HandleProductsReady, invokeIfReady: true);
            _iap.OnPurchaseSucceeded += HandlePurchaseSucceeded;
            _iap.OnPurchaseFailed += HandlePurchaseFailed;
            _iap.OnDeferred += HandleDeferred;
        }
    }

    private void OnDisable()
    {
        if (!_closeLogged)
        {
            _closeLogged = true;

            float seconds = Mathf.Max(0f, Time.realtimeSinceStartup - _openedAtRealtime);

            LogEvent("remove_ads_popup_close",
                new Parameter("close_reason", _closeReason ?? "unknown"),
                new Parameter("screen_seconds", seconds)
            );
        }

        if (_iap != null)
        {
            _iap.OnProductsReady -= HandleProductsReady;

            _iap.OnPurchaseSucceeded -= HandlePurchaseSucceeded;
            _iap.OnPurchaseFailed -= HandlePurchaseFailed;
            _iap.OnDeferred -= HandleDeferred;
        }
    }

    private void OnCloseClick()
    {
        _closeReason = "close_button";
        Close();
    }

    private void HandleProductsReady(List<Product> products)
    {
        RefreshLabels(products);

        var snapshot = products ?? _iap?.GetProductsSnapshot();
        var one = FindProduct(snapshot, _removeAdsProductId);
        var sub = FindProduct(snapshot, _removeAdsSubProductId);

        var ps = new List<Parameter>
        {
            new Parameter("count", snapshot?.Count ?? 0),
        };

        AddPriceParams(ps, one);
        if (sub != null && sub.metadata != null)
        {
            double subPrice = (double)sub.metadata.localizedPrice;
            if (subPrice > 0) ps.Add(new Parameter("sub_price", subPrice));
            if (!string.IsNullOrEmpty(sub.metadata.isoCurrencyCode)) ps.Add(new Parameter("sub_currency", sub.metadata.isoCurrencyCode));
            if (!string.IsNullOrEmpty(sub.metadata.localizedPriceString)) ps.Add(new Parameter("sub_price_str", sub.metadata.localizedPriceString));
        }

        LogEvent("remove_ads_products_ready", ps.ToArray());
    }

    private void RefreshLabels(List<Product> products = null)
    {
        products ??= _iap?.GetProductsSnapshot();

        var oneTimeLabel = GetStorePriceLabel(products, _removeAdsProductId, fallback: $"{_fallbackUsd:0.##} USD");
        SetOneTimePrice(oneTimeLabel);

        var subLabel = GetStorePriceLabel(products, _removeAdsSubProductId, fallback: "1.99 USD");

        // Собираем строку “3 days free trial, then $X per month”
        if (!string.IsNullOrEmpty(subLabel) && subLabel != "…")
            SetTrialInfo(string.Format(_trialInfoFormat, _trialDays, subLabel, _perPeriodLabel));
        else
            SetTrialInfo("…");
    }

    private string GetStorePriceLabel(List<Product> products, string productId, string fallback)
    {
        if (_iap == null) return fallback;

        var p = products?.FirstOrDefault(x => x?.definition != null && x.definition.id == productId);
        if (p != null && p.metadata != null && p.metadata.localizedPrice > 0)
            return p.metadata.localizedPriceString;

        if (_iap.TryGetStorePriceLabel(productId, out var label) && !string.IsNullOrEmpty(label))
            return label;

        return fallback;
    }

    private void SetOneTimePrice(string label)
    {
        if (_oneTimePriceText != null) _oneTimePriceText.text = label;
    }

    private void SetTrialInfo(string text)
    {
        if (_trialInfoText != null) _trialInfoText.text = text;
    }

    private void OnBuyClick()
    {
        if (NoAdsManager.IsOwned) { _closeReason = "already_owned"; Close(); return; }
        if (_iap == null) { Debug.LogWarning("[RemoveAdsPopup] IAPCatalogManager not ready"); return; }

        LogPurchaseClick(_removeAdsProductId, "one_time");

        _buyButton.interactable = false;
        _iap.Buy(_removeAdsProductId);
    }

    private void OnSubscribeClick()
    {
        if (NoAdsManager.IsOwned) { _closeReason = "already_owned"; Close(); return; }
        if (_iap == null) { Debug.LogWarning("[RemoveAdsPopup] IAPCatalogManager not ready"); return; }

        LogPurchaseClick(_removeAdsSubProductId, "subscription");

        _subscribeButton.interactable = false;
        _iap.Buy(_removeAdsSubProductId);
    }

    private void LogPurchaseClick(string productId, string purchaseType)
    {
        var snapshot = _iap?.GetProductsSnapshot();
        var p = FindProduct(snapshot, productId);

        var ps = new List<Parameter>
        {
            new Parameter("product_id", productId ?? ""),
            new Parameter("purchase_type", purchaseType ?? "")
        };

        AddPriceParams(ps, p);

        if (purchaseType == "subscription")
        {
            ps.Add(new Parameter("trial_days", _trialDays));
            ps.Add(new Parameter("per_period_label", _perPeriodLabel ?? ""));
        }

        LogEvent("remove_ads_purchase_click", ps.ToArray());
    }

    private void HandlePurchaseSucceeded(string productId)
    {
        var snapshot = _iap?.GetProductsSnapshot();
        var p = FindProduct(snapshot, productId);

        string purchaseType =
            productId == _removeAdsProductId ? "one_time" :
            productId == _removeAdsSubProductId ? "subscription" : "unknown";

        var ps = new List<Parameter>
        {
            new Parameter("product_id", productId ?? ""),
            new Parameter("purchase_type", purchaseType)
        };
        AddPriceParams(ps, p);
        LogEvent("remove_ads_purchase_success", ps.ToArray());

        _closeReason = "purchase_success";

        if (productId == _removeAdsProductId)
        {
            NoAdsManager.SetOwned(true);
        }
        else if (productId == _removeAdsSubProductId)
        {
            NoAdsManager.SetSubscriptionActive(true);
        }

        var ad = FindFirstObjectByType<AdManager>();
        if (ad != null) ad.SetAdsDisabled(true);

        Close();
    }

    private void HandlePurchaseFailed(string productId, string reason)
    {
        if (productId != _removeAdsProductId && productId != _removeAdsSubProductId) return;

        var snapshot = _iap?.GetProductsSnapshot();
        var p = FindProduct(snapshot, productId);

        string purchaseType = productId == _removeAdsProductId ? "one_time" : "subscription";

        var ps = new List<Parameter>
        {
            new Parameter("product_id", productId ?? ""),
            new Parameter("purchase_type", purchaseType),
            new Parameter("fail_reason", reason ?? "")
        };
        AddPriceParams(ps, p);
        LogEvent("remove_ads_purchase_fail", ps.ToArray());

        _closeReason = "purchase_failed";

        Debug.LogWarning($"[RemoveAdsPopup] Purchase failed ({productId}): {reason}");
        if (_buyButton != null) _buyButton.interactable = true;
        if (_subscribeButton != null) _subscribeButton.interactable = true;
    }

    private void HandleDeferred(string productId)
    {
        if (productId != _removeAdsProductId && productId != _removeAdsSubProductId) return;

        string purchaseType = productId == _removeAdsProductId ? "one_time" : "subscription";

        LogEvent("remove_ads_purchase_deferred",
            new Parameter("product_id", productId ?? ""),
            new Parameter("purchase_type", purchaseType)
        );

        _closeReason = "purchase_deferred";

        Debug.Log($"[RemoveAdsPopup] Purchase deferred ({productId})");
        if (_buyButton != null) _buyButton.interactable = true;
        if (_subscribeButton != null) _subscribeButton.interactable = true;
    }

    public void Close() => Destroy(gameObject);

    private void CloseImmediate()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
