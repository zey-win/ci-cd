using UnityEngine;

public abstract class ProductData : ScriptableObject
{
    public string Key => _key;
    public Sprite Icon => _icon;

    public PriceType PriceType => _priceType;
    public float Price => _price;

    public BadgeType BadgeType => _badgeType;
    public bool HasNoAdsBonus => _hasNoAdsBonus;


    [SerializeField] private string _key;
    [SerializeField] private Sprite _icon;

    [SerializeField] private PriceType _priceType;
    [SerializeField] private float _price;

    [SerializeField] private BadgeType _badgeType;

    [SerializeField] private bool _hasNoAdsBonus;
}



public enum PriceType
{
    USD,
    AD,
    COINS
}


public enum BadgeType
{
    NONE,
    MOST_POPULAR,
    BEST_VALUE
}