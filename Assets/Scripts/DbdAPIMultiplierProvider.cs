using System.Collections.Generic;
using UnityEngine;

public class DbdAPIMultiplierProvider : MonoBehaviour, IMultiplierStripProvider
{
    public List<float> multipliers = new List<float>();

    public void SetMultipliers(IEnumerable<PlinkoBasket> baskets)
    {
        var basketList = new List<PlinkoBasket>(baskets);
        var basketLenght = basketList.Count;

        for (var i = 0; i < multipliers.Count && i < basketLenght; i++)
        {
            var ev = multipliers[i];
            basketList[i].SetMultiplier(ev);
        }
    }

    public void ChangeMultipliers(List<float> gate_values)
    {
        multipliers = gate_values;
    }
}