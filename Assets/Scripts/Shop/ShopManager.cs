using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using Firebase.Analytics;
using UnityEngine.UI;
using System.Collections;

public class ShopManager : MonoBehaviour
{

    [Header("Focus on IAP when not enough coins")]
    [SerializeField] private ScrollRect _moneyScrollRect;   // скролл списка coins за USD
    [SerializeField] private float _blinkDuration = 3f;
    [SerializeField, Range(0f, 1f)] private float _blinkMinAlpha = 0.25f;
    [SerializeField] private float _blinkSpeed = 6f; // частота "мигания"

    private Coroutine _blinkRoutine;
    private ChipsProductCard _cheapestUsdCard;


    [SerializeField] private Transform _chipsCardsContainer;
    [SerializeField] private ChipsProductCard _chipsCardPrefab;
    private List<ChipsProductData> _chipsProductDatas = new List<ChipsProductData>();
    private readonly Dictionary<string, ChipsProductCard> _chipsProductCardsByKey = new();



    [SerializeField] private Transform _ballsCardsContainer;

    [SerializeField] private BallProductCard _ballCardPrefab;

    private List<BallProductData> _ballsProductDatas = new List<BallProductData>();


    // ===== ANALYTICS =====
    private string _closeReason = "unknown"; // "x" / "system" / "unknown"

    private void LogEvent(string name, params Parameter[] parameters)
    {
        AnalyticsSafe.LogEvent(name, parameters);
    }
    // ===== /ANALYTICS =====




    private void Start()
    {
        _chipsProductDatas = Resources.LoadAll<ChipsProductData>("Shop/Coins")
            .OrderBy(p => p.ChipsReward == 0)
            .ThenBy(p => p.ChipsReward)
            .ToList();


        _ballsProductDatas = Resources.LoadAll<BallProductData>("Shop/Balls")
            .OrderBy(p => p.Price)
            .ToList();


        _chipsProductDatas.ForEach(product =>
        {
            var card = Instantiate(_chipsCardPrefab, _chipsCardsContainer.transform);
            _chipsProductCardsByKey[product.Key] = card;

            card.Init(product, () => OnClickByProduct(product));

            if (product.PriceType == PriceType.USD)
            {
                string label;

                var iap = IAPCatalogManager.Instance;
                if (iap != null && iap.TryGetStorePriceLabel(product.Key, out var storeLabel))
                {
                    label = storeLabel; // реальная локализованная цена
                }
                else if (product.Price > 0f)
                {
                    label = $"${product.Price:0.##}"; // фолбэк из ScriptableObject, только если он не 0
                }
                else
                {
                    label = "…"; // плейсхолдер, пока цена не подтянулась
                }

                card.SetPriceText(label);
            }

        });


        _ballsProductDatas.ForEach(product =>
        {
            var card = Instantiate(_ballCardPrefab, _ballsCardsContainer.transform);
            card.Init(product, () => OnClickByProduct(product));
        });


        _closeReason = "unknown";
        LogEvent("shop_screen_open");

        _cheapestUsdCard = GetCheapestUsdCardFallback();
    }


    private ChipsProductCard GetCheapestUsdCardFallback()
    {
        var cheapestData = _chipsProductDatas.FirstOrDefault(d => d.ChipsReward == 2000);

        if (cheapestData == null) return null;

        return _chipsProductCardsByKey.TryGetValue(cheapestData.Key, out var card) ? card : null;
    }



    private void OnEnable()
    {
        var iap = IAPCatalogManager.Instance;
        if (iap == null) return;

        iap.AddProductsReadyListener(HandleProductsReady, invokeIfReady: true);
    }

    private void OnDisable()
    {
        var iap = IAPCatalogManager.Instance;
        if (iap != null) iap.OnProductsReady -= HandleProductsReady;
    }


    private void HandleProductsReady(List<Product> products)
    {
        foreach (var p in products)
        {
            string pid = p.definition.id;
            if (_chipsProductCardsByKey.TryGetValue(pid, out var card))
            {
                card.SetPriceText(p.metadata.localizedPriceString);
            }
        }


        Product cheapest = null;
        foreach (var p in products)
        {
            if (_chipsProductCardsByKey.ContainsKey(p.definition.id))
            {
                if (cheapest == null || p.metadata.localizedPrice < cheapest.metadata.localizedPrice)
                    cheapest = p;
            }
        }

        if (cheapest != null && _chipsProductCardsByKey.TryGetValue(cheapest.definition.id, out var cheapestCard))
            _cheapestUsdCard = cheapestCard;
        else
            _cheapestUsdCard = GetCheapestUsdCardFallback();
    }


    private void OnClickByProduct(ProductData productData)
    {
        if (productData is ChipsProductData chips)
        {
            LogEvent("shop_product_click",
                new Parameter("product_type", "coins"),
                new Parameter("product_key", chips.Key)
            );
        }
        else if (productData is BallProductData ball && ball.Ball != null)
        {
            LogEvent("shop_product_click",
                new Parameter("product_type", "ball"),
                new Parameter("product_key", ball.Ball.Key)
            );
        }


        if (productData is ChipsProductData)
        {

            if (productData.PriceType == PriceType.AD)
            {
                var chipsProductData = (ChipsProductData)productData;

                LogEvent("shop_coins_rewarded_ad_click",
                    new Parameter("coins_amount", chipsProductData.ChipsReward),
                    new Parameter("product_key", chipsProductData.Key)
                );


                AdManager adManager = FindFirstObjectByType<AdManager>();

                if (adManager != null)
                {
                    adManager.ShowRewardedAd(() =>
                    {
                        var chips = (ChipsProductData)productData;

                        LogEvent("shop_coins_rewarded_ad_success",
                            new Parameter("coins_amount", chips.ChipsReward),
                            new Parameter("product_key", chips.Key)
                        );

                        FindAnyObjectByType<BalanceManager>().AddWinnings(chips.ChipsReward, "ShopProductForAd");

                        LogEvent("shop_coins_reward_granted",
                            new Parameter("amount", chips.ChipsReward),
                            new Parameter("source", "rewarded_ad"),
                            new Parameter("product_key", chips.Key)
                        );
                    });

                }
            }
            else if (productData.PriceType == PriceType.USD)
            {
                var chipsProductData = (ChipsProductData)productData;

                LogEvent("shop_iap_purchase_click",
                    new Parameter("product_key", chipsProductData.Key),
                    new Parameter("coins_amount", chipsProductData.ChipsReward)
                );

                IAPCatalogManager.Instance.Buy(productData.Key);
            }
        }

        if (productData is BallProductData ballProduct)
        {
            var skins = BallSkinsManager.Instance;
            if (skins == null || ballProduct.Ball == null) return;

            string id = ballProduct.Ball.Key;

            if (skins.IsUnlocked(id))
            {
                LogEvent("shop_ball_select",
                    new Parameter("ball_id", id)
                );

                skins.SetActive(id);
                return;
            }


            if (ballProduct.PriceType != PriceType.COINS)
            {
                Debug.LogWarning("BallProduct should be SOFT price type.");
                return;
            }

            int price = Mathf.RoundToInt(ballProduct.Price);

            var balance = FindAnyObjectByType<BalanceManager>();
            if (balance == null) return;

            LogEvent("shop_ball_purchase_attempt_for_coins",
                new Parameter("ball_id", id),
                new Parameter("price", price),
                new Parameter("currency", "coins")
            );

            if (balance.TrySpend(price, $"ShopProduct: Ball-{id}"))
            {
                LogEvent("shop_ball_purchase_success_for_coins",
                    new Parameter("ball_id", id),
                    new Parameter("price_coins", price)
                );

                skins.Unlock(id);
                skins.SetActive(id);
            }
            else
            {
                LogEvent("shop_ball_purchase_fail_for_coins",
                    new Parameter("ball_id", id),
                    new Parameter("price_coins", price),
                    new Parameter("fail_reason", "not_enough_coins")
                );

                Debug.Log("Not enough coins");
                FocusMoneyScrollAndBlinkCheapest();
            }

            return;
        }
    }


    public void OnExit()
    {
        _closeReason = "x";

        LogEvent("shop_screen_close",
            new Parameter("close_reason", _closeReason)
        );

        Destroy(gameObject);
    }



    private void FocusMoneyScrollAndBlinkCheapest()
    {
        if (_moneyScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            _moneyScrollRect.StopMovement();
            _moneyScrollRect.verticalNormalizedPosition = 1f;
        }

        var card = _cheapestUsdCard ?? GetCheapestUsdCardFallback();
        if (card == null) return;

        if (_blinkRoutine != null)
            StopCoroutine(_blinkRoutine);

        _blinkRoutine = StartCoroutine(BlinkCanvasGroup(card.gameObject, _blinkDuration, _blinkMinAlpha, _blinkSpeed));
    }

    private IEnumerator BlinkCanvasGroup(GameObject go, float duration, float minAlpha, float speed)
    {
        if (go == null) yield break;

        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();

        float startAlpha = cg.alpha;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;

            float p = Mathf.PingPong(t * speed, 1f);
            cg.alpha = Mathf.Lerp(minAlpha, 1f, p);

            yield return null;
        }

        cg.alpha = startAlpha;
        _blinkRoutine = null;
    }


}
