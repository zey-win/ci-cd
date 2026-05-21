using UnityEngine;

public class GameButtonsContainer : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform _container;
    [SerializeField] private float _offsetY = 120f;

    private Vector2 _baseAnchoredPos;

    private void Awake()
    {
        if (_container == null)
            _container = GetComponent<RectTransform>();

        _baseAnchoredPos = _container.anchoredPosition;
    }

    private void OnEnable()
    {
        NoAdsManager.OnChanged += HandleNoAdsChanged;
        HandleNoAdsChanged(NoAdsManager.IsOwned);
    }

    private void OnDisable()
    {
        NoAdsManager.OnChanged -= HandleNoAdsChanged;
    }

    private void HandleNoAdsChanged(bool noAdsOwned)
    {
        var target = _baseAnchoredPos;

        if (noAdsOwned)
            target.y -= _offsetY;

        _container.anchoredPosition = target;
    }
}
