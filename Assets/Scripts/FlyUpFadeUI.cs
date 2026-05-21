using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class FlyUpFadeUI : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float moveUp = 120f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;
    [SerializeField] private Ease fadeEase = Ease.OutQuad;

    [Header("Optional")]
    [SerializeField] private bool deactivateOnComplete = false;

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _startAnchoredPos;
    private Sequence _sequence;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _startAnchoredPos = _rectTransform.anchoredPosition;
    }

    public void Play()
    {
        _sequence?.Kill();

        // Сброс в исходное состояние
        _rectTransform.anchoredPosition = _startAnchoredPos;
        _canvasGroup.alpha = 1f;

        _sequence = DOTween.Sequence();

        _sequence.Join(
            _rectTransform
                .DOAnchorPosY(_startAnchoredPos.y + moveUp, duration)
                .SetEase(moveEase)
        );

        _sequence.Join(
            _canvasGroup
                .DOFade(0f, duration)
                .SetEase(fadeEase)
        );

        if (deactivateOnComplete)
        {
            _sequence.OnComplete(() => gameObject.SetActive(false));
        }
    }

    public void ResetState()
    {
        _sequence?.Kill();
        _rectTransform.anchoredPosition = _startAnchoredPos;
        _canvasGroup.alpha = 1f;
    }

    private void OnDisable()
    {
        _sequence?.Kill();
    }
}