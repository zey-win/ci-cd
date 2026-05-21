using UnityEngine;

public class LerpOffset : MonoBehaviour, IOffset
{
    [Min(1)] public int startValue = 8;
    [Min(1)] public int endValue = 16;

    public Vector3 startOffset = new Vector3(-2, 0, 0);
    public Vector3 endOffset = new Vector3(-3, 0, 0);


    public Vector3 GetOffset(int rows)
    {
        float t = Mathf.InverseLerp(8f, 16f, rows);
        return Vector3.Lerp(startOffset, endOffset, t);
    }
}