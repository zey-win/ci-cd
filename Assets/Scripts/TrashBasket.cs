using UnityEngine;

public class TrashBasket : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag($"Ball")) return;
        var ball = col.GetComponent<PlinkoBall>();
        BallPooling.Pool.Release(ball);

        var pinkoResult = new PlinkoResult(ball, ball.Bet, 0);
        FindAnyObjectByType<GameManager>().OnResult(pinkoResult);
    }
}
