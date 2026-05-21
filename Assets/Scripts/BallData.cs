using UnityEngine;

public enum BallColliderType
{
    Circle,
    Edge
}


[CreateAssetMenu(fileName = "BallData", menuName = "Cosmetics/Ball")]
public class BallData : ScriptableObject
{
    public string Key => _key;
    public Sprite Preview => _preview;
    public BallColliderType ColliderType => _colliderType;


    [SerializeField] private string _key;
    [SerializeField] private Sprite _preview;
    [Header("Physics")]
    [SerializeField] private BallColliderType _colliderType = BallColliderType.Circle;
}
