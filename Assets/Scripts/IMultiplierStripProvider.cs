using System.Collections.Generic;

public interface IMultiplierStripProvider
{
    void SetMultipliers(IEnumerable<PlinkoBasket> baskets);
    void ChangeMultipliers(List<float> gate_values);
}