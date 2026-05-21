using System.Collections.Generic;

public class LinearBasketStrip : IPlinkoBaskets
{
    private readonly List<PlinkoBasket> _baskets;

    public LinearBasketStrip()
    {
        _baskets = new List<PlinkoBasket>();
    }

    public void AddBasket(PlinkoBasket basket)
    {
        _baskets.Add(basket);
    }

    public List<PlinkoBasket> GetBaskets()
    {
        return _baskets;
    }

    public void Clear()
    {
        _baskets.Clear();
    }
}