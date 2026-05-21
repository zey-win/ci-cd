using System.Collections.Generic;

public interface IPlinkoBaskets
{
    void AddBasket(PlinkoBasket basket);
    List<PlinkoBasket> GetBaskets();
    void Clear();
}