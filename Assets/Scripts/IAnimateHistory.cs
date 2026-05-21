using DG.Tweening;
using UnityEngine;

public interface IAnimateHistory
{
    void Animate(RectTransform resultRectTransform, RectTransform parentRectTransform,  int maxChildCount,
        TweenCallback OnCompleteAnimationCallback);
}