using System.Collections;
using TMPro;
using UnityEngine;

public class WalletController : MonoBehaviour
{
    [SerializeField] private GameObject _iconsContainer;
    [SerializeField] private GameObject _paypalIcon;
    [SerializeField] private GameObject _visaIcon;
    [SerializeField] private GameObject _cryptoIcon;
    [SerializeField] protected TMP_Text _timerField;
    [SerializeField] private TMP_Text _pointsField;
    [SerializeField] private TMP_Text _balanceField;

    [SerializeField] private WalletPopupController _walletPopupControllerPrefab;




    private IEnumerator Start()
    {
        PayoutMethodsManager.Instance.Service.OnChanged += OnPaymentMethodsChanged;
        OnPaymentMethodsChanged();

        yield return new WaitUntil(() => PayoutProgressManager.Instance != null);

        var pm = PayoutProgressManager.Instance;
        if (pm != null)
        {
            pm.OnChanged += UpdatePayoutUI;
            pm.OnExpired += UpdatePayoutUI;
            pm.OnTick += UpdatePayoutUI;

            UpdatePayoutUI();
        }
    }


    private void OnDestroy()
    {
        PayoutMethodsManager.Instance.Service.OnChanged -= OnPaymentMethodsChanged;
    }


    private void OnDisable()
    {
        var pm = PayoutProgressManager.Instance;
        if (pm != null)
        {
            pm.OnChanged -= UpdatePayoutUI;
            pm.OnExpired -= UpdatePayoutUI;
            pm.OnTick -= UpdatePayoutUI;
        }
    }


    public void OnClick()
    {
        Instantiate(_walletPopupControllerPrefab, transform.parent);
    }


    private void OnPaymentMethodsChanged()
    {
        bool isPaypalIconActive = false;
        bool isVisaIconActive = false;
        bool isCryptoIconActive = false;


        if (PayoutMethodsManager.Instance.Service.State.defaultMethod == -1)
        {
            _iconsContainer.SetActive(false);
        }
        else if (PayoutMethodsManager.Instance.Service.State.IsDefault(PayoutMethodType.PayPal))
        {
            _iconsContainer.SetActive(true);
            isPaypalIconActive = true;
        }
        else if (PayoutMethodsManager.Instance.Service.State.IsDefault(PayoutMethodType.Visa))
        {
            _iconsContainer.SetActive(true);
            isVisaIconActive = true;
        }
        else if (PayoutMethodsManager.Instance.Service.State.IsDefault(PayoutMethodType.Crypto))
        {
            _iconsContainer.SetActive(true);
            isCryptoIconActive = true;
        }

        _paypalIcon.SetActive(isPaypalIconActive);
        _visaIcon.SetActive(isVisaIconActive);
        _cryptoIcon.SetActive(isCryptoIconActive);
    }


    private void UpdatePayoutUI()
    {
        var pm = PayoutProgressManager.Instance;
        if (pm == null) return;

        if (_pointsField != null) _pointsField.text = pm.PointsText;
        if (_timerField != null) _timerField.text = pm.TimerText;
        if (_balanceField != null)
        {
            if (_balanceField != null)
                _balanceField.text = $"${pm.WithdrawBalanceUsd:0.00}";
        }
    }
}
