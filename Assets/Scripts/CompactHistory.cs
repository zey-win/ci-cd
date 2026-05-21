using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CompactHistory : MonoBehaviour
{
    [SerializeField] private HistoryItem resultPrefab;
    [SerializeField] private int maxChildCount;

    private IAnimateHistory _animateHistory;

    private void Awake()
    {
        _animateHistory = GetComponent<IAnimateHistory>();
        PlinkoBasket.OnBallHit += AddResult;
    }

    private void OnDestroy()
    {
        PlinkoBasket.OnBallHit -= AddResult;
    }

    public void AddResult(PlinkoBasket basket)
    {
        HistoryItem resultGameObject = Instantiate(resultPrefab, transform);
        resultGameObject.transform.SetAsFirstSibling();
        resultGameObject.Init(basket.Multiplier.ToString("F2"), basket.SpriteRenderer.color);

        var resultRectTransform = resultGameObject.GetComponent<RectTransform>();
        var rectTransform = GetComponent<RectTransform>();

        _animateHistory?.Animate(resultRectTransform, rectTransform, maxChildCount, DestroyLastChildIfNeeded);
    }

    public void DestroyLastChildIfNeeded()
    {
        if (transform.childCount > maxChildCount)
        {
            Destroy(transform.GetChild(maxChildCount).gameObject);
        }
    }
}