using UnityEngine;
using UnityEngine.Pool;

public class BallPooling : MonoBehaviour
{
    [SerializeField] private PlinkoBall _plinkoBallPrefab;
    [SerializeField] private BallModifier _modifier;
    public static ObjectPool<PlinkoBall> Pool { get; private set; }

    private void Awake()
    {
        Pool = new ObjectPool<PlinkoBall>(CreateBall, OnGetBall, OnReleaseBall, OnDestroyBall, false, 100, 150);
    }

    private PlinkoBall CreateBall() => Instantiate(_plinkoBallPrefab);

    private void OnGetBall(PlinkoBall ball)
    {
        if (ball == null)
            return;

        ball.transform.SetParent(null);
        ball.Activate();
        _modifier.PrepareBall(ball);
    }

    private void OnReleaseBall(PlinkoBall ball)
    {
        ball.transform.SetParent(transform);
        ball.Deactivate();
    }


    private void OnDestroyBall(PlinkoBall ball)
    {
        if (ball != null)
            Destroy(ball.gameObject);
    }
}