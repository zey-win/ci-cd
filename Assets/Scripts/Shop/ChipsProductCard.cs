using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChipsProductCard : ProductCard
{
    [SerializeField] protected TextMeshProUGUI _chipsCount;

    [SerializeField] private GameObject _freePriceContainer;


    [SerializeField] private Image _badgeField;
    [SerializeField] private Image _noAdsBonus;


    [SerializeField] private Sprite _mostPopularSprtie;
    [SerializeField] private Sprite _bestValueSprite;




    public override void Init(ProductData productData, Action onClickCallback)
    {
        base.Init(productData, onClickCallback);


        _iconField.sprite = productData.Icon;
        _chipsCount.text = ((ChipsProductData)productData).ChipsReward.ToString();

        if (productData.PriceType == PriceType.USD)
        {
            _priceField.text = $"${productData.Price}";
        }
        else if (productData.PriceType == PriceType.AD)
        {
            _priceField.gameObject.SetActive(false);
            _freePriceContainer.SetActive(true);
        }

        if (productData.BadgeType != BadgeType.NONE)
        {
            _badgeField.gameObject.SetActive(true);
            _badgeField.sprite = productData.BadgeType == BadgeType.MOST_POPULAR ? _mostPopularSprtie : _bestValueSprite;
        }

        if (productData.HasNoAdsBonus)
            _noAdsBonus.gameObject.SetActive(true);

        _onClickCallback = onClickCallback;
    }
}
