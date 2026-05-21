using UnityEngine;

[CreateAssetMenu(fileName = "ChipsProduct", menuName = "Shop/Products/Chips")]
public class ChipsProductData : ProductData
{
    public int ChipsReward => _chipsReward;
    [SerializeField] private int _chipsReward;
}
