using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ProductCard : MonoBehaviour
{
    [SerializeField] protected Image _iconField;

    [SerializeField] protected Text _priceField;

    protected Action _onClickCallback;




    public virtual void Init(ProductData productData, Action onClickCallback)
    {
        _iconField.sprite = productData.Icon;


        if (productData.PriceType == PriceType.USD)
        {
            _priceField.text = $"${productData.Price}";
        }

        _onClickCallback = onClickCallback;
    }


    public void OnClick()
    {
        if (_onClickCallback != null)
        {
            _onClickCallback?.Invoke();
        }
    }


    public void SetPriceText(string text)
    {
        if (_priceField != null) _priceField.text = text;
    }
}

