using System.Collections.Generic;
using UnityEngine;

public class RiskCurveStripMultiplierProvider : MonoBehaviour, IMultiplierStripProvider
{
    [SerializeField] private List<AnimationCurve> _curves;
    private int _levelOfRisk;

    public void SetMultipliers(IEnumerable<PlinkoBasket> baskets)
    {
        if (_levelOfRisk >= _curves.Count) return;

        var basketList = new List<PlinkoBasket>(baskets);
        var basketLenght = basketList.Count;

        for (int i = 0; i < basketLenght; i++)
        {
            var t = (float)i / basketLenght;
            var ev = _curves[_levelOfRisk].Evaluate(t);
            basketList[i].SetMultiplier(ev);
        }
    }

    public void ChangeMultipliers(List<float> gate_values)
    {
    }


    public void ChangeMultipliers(int risk)
    {
        _levelOfRisk = risk % _curves.Count;
    }
}