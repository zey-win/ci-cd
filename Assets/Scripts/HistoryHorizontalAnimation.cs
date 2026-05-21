using DG.Tweening;
using UnityEngine;

public class HistoryHorizontalAnimation : MonoBehaviour, IAnimateHistory
{
    public void Animate(RectTransform resultRectTransform, RectTransform parentRectTransform, int maxChildCount,
        TweenCallback OnCompleteAnimationCallback)
    {
        var rect = parentRectTransform.rect;
        var resultHeight = rect.width / maxChildCount;
        resultRectTransform.sizeDelta = new Vector2(0f, rect.height);
        resultRectTransform.DOSizeDelta(new Vector2(resultHeight, 79f), 0.3f)
            .OnComplete(OnCompleteAnimationCallback);
    }
}