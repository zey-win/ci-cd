using System;
using UnityEngine;

public class BallProductCard : ProductCard
{
    [SerializeField] private GameObject _priceContainer;
    [SerializeField] private GameObject _selectedText;

    private BallProductData _ballProduct;

    public override void Init(ProductData productData, Action onClickCallback)
    {
        base.Init(productData, onClickCallback);

        _ballProduct = productData as BallProductData;

        Refresh();

        var skins = BallSkinsManager.Instance;
        if (skins != null)
        {
            skins.OnActiveSkinChanged += HandleSkinChanged;
            skins.OnSkinUnlocked += HandleSkinChanged;
        }
    }

    private void OnDestroy()
    {
        var skins = BallSkinsManager.Instance;
        if (skins != null)
        {
            skins.OnActiveSkinChanged -= HandleSkinChanged;
            skins.OnSkinUnlocked -= HandleSkinChanged;
        }
    }

    private void HandleSkinChanged(string _)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_ballProduct == null || _ballProduct.Ball == null)
            return;

        string id = _ballProduct.Ball.Key;
        var skins = BallSkinsManager.Instance;

        bool unlocked = skins != null && skins.IsUnlocked(id);
        bool selected = skins != null && skins.IsActive(id);

        // Цена видна только если НЕ куплено
        if (_priceContainer != null)
            _priceContainer.SetActive(!unlocked);

        // Selected виден только если активен
        if (_selectedText != null)
            _selectedText.SetActive(selected);

        // Иконка
        if (_iconField != null)
            _iconField.sprite = _ballProduct.Icon;

        // Цена (если показываем)
        if (!unlocked && _priceField != null)
        {
            // Тут зависит от твоего enum. Если у тебя COINS — используй COINS.
            // Если SOFT — используй SOFT.
            if (_ballProduct.PriceType == PriceType.USD)
                _priceField.text = $"${_ballProduct.Price:0.##}";
            else
                _priceField.text = $"{Mathf.RoundToInt(_ballProduct.Price)}";
        }
    }
}
