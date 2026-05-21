using DG.Tweening;
using TMPro;
using UnityEngine;

public class BalanceDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text _balanceText;
    private float current;
    public bool enableAnimation = true;
    public float animationTime = 0.5f;
    public Ease animationEase = Ease.InOutSine;

    private BalanceManager _balanceManager;

    private void Awake()
    {
    }

    private void OnEnable()
    {
        _balanceManager = FindFirstObjectByType<BalanceManager>();
        _balanceManager.OnBalanceChanged += ChangeText;
    }

    private void OnDisable()
    {
        _balanceManager.OnBalanceChanged -= ChangeText;
    }

    private void ChangeText(float balance)
    {
        if (enableAnimation)
            DOTween.To(() => current, x => current = x, balance, animationTime).SetDelay(0.2f)
                .SetEase(animationEase)
                .OnUpdate(() => _balanceText.text = current.ToString("F2"));
        else
            _balanceText.text = balance.ToString("F2");
    }
}