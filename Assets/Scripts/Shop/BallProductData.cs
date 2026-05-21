using UnityEngine;

[CreateAssetMenu(fileName = "BallProduct", menuName = "Shop/Products/Ball")]
public class BallProductData : ProductData
{
    public BallData Ball => _ball;
    [SerializeField] private BallData _ball;
}
