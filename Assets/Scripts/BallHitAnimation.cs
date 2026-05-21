using DG.Tweening;
using UnityEngine;

public class BallHitAnimation : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag($"Ball")) return;

        transform.DOComplete();
        transform.DOPunchPosition(-transform.up * .5f, .3f, 1).SetEase(Ease.InOutQuad);
    }
}