using DG.Tweening;
using UnityEngine;

public class HistoryVerticalAnimation : MonoBehaviour, IAnimateHistory
{
    public void Animate(RectTransform resultRectTransform, RectTransform parentRectTransform, int maxChildCount,
        TweenCallback OnCompleteAnimationCallback)
    {
        var rect = parentRectTransform.rect;
        var resultHeight = rect.height / maxChildCount;
        resultRectTransform.sizeDelta = new Vector2(rect.width, 0f);
        resultRectTransform.DOSizeDelta(new Vector2(parentRectTransform.rect.width, resultHeight), 0.3f)
            .OnComplete(OnCompleteAnimationCallback);
    }
}