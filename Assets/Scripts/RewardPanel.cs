using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _rewardText;
    [SerializeField] private Button _claimButton;
    [SerializeField] private TextMeshProUGUI _claimButtonText;

    [Header("Optional")]
    [SerializeField] private Button _closeButton; // можно не задавать

    private Action _onClaim;
    private Action _onClose;

    private int _reward;


    public void Init(int reward, Action onClaim, Action onClose)
    {
        _reward = reward;
        _onClaim = onClaim;
        _onClose = onClose;

        if (_rewardText != null)
            _rewardText.text = reward.ToString();

        if (_claimButton != null)
        {
            _claimButton.onClick.RemoveAllListeners();
            _claimButton.onClick.AddListener(() => _onClaim?.Invoke());
        }

        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(Close);
        }

        // гарантируем что панель будет поверх всего в рамках родителя
        transform.SetAsLastSibling();
    }

    public void Close()
    {
        _onClose?.Invoke();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        // на всякий случай очищаем, если панель уничтожают извне
        if (_claimButton != null) _claimButton.onClick.RemoveAllListeners();
        if (_closeButton != null) _closeButton.onClick.RemoveAllListeners();
    }


    void OnDisable()
    {
        FindAnyObjectByType<BalanceManager>().AddWinnings(_reward, "EndlessAdventureReward");
    }
}
