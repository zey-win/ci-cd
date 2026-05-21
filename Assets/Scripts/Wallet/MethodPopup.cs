using Firebase.Analytics;
using UnityEngine;

public class MethodPopup : MonoBehaviour
{
    [SerializeField] private WalletPopupController _walletPopupPrefab;

    public MethodRowView paypalRow;
    public MethodRowView visaRow;
    public MethodRowView cryptoRow;

    [Header("Popups")]
    public PayoutPopupBase paypalPopupPrefab;
    public PayoutPopupBase visaPopupPrefab;
    public PayoutPopupBase cryptoPopupPrefab;

    private IPayoutMethodsService _service;


    // ===== ANALYTICS =====
    private string _closeReason = "unknown"; // "back" / "system" / "unknown"

    private static int B(bool v) => v ? 1 : 0;

    private void LogEvent(string name, params Parameter[] parameters)
    {
        AnalyticsSafe.LogEvent(name, parameters);
    }

    private static string MethodKey(PayoutMethodType t) =>
        t == PayoutMethodType.PayPal ? "paypal" :
        t == PayoutMethodType.Visa ? "visa" :
        t == PayoutMethodType.Crypto ? "crypto" : "none";
    // ===== /ANALYTICS =====


    private void Awake()
    {
        _service = PayoutMethodsManager.Instance.Service;

        paypalRow.Init(OnOpenConnectPopup, OnRowClicked);
        visaRow.Init(OnOpenConnectPopup, OnRowClicked);
        cryptoRow.Init(OnOpenConnectPopup, OnRowClicked);
    }

    private void OnEnable()
    {
        _service.OnChanged += Refresh;
        Refresh();

        _closeReason = "unknown";

        string def = "none";
        if (_service.TryGetDefault(out var d) && _service.IsConnected(d))
            def = MethodKey(d);

        LogEvent("payment_method_screen_open",
            new Parameter("default_method", def),
            new Parameter("connected_paypal", B(_service.IsConnected(PayoutMethodType.PayPal))),
            new Parameter("connected_visa", B(_service.IsConnected(PayoutMethodType.Visa))),
            new Parameter("connected_crypto", B(_service.IsConnected(PayoutMethodType.Crypto)))
        );

    }

    private void OnDisable()
    {
        _service.OnChanged -= Refresh;
        LogEvent("payment_method_screen_close",
            new Parameter("close_reason", _closeReason)
        );

    }

    private void Refresh()
    {
        paypalRow.Render(_service);
        visaRow.Render(_service);
        cryptoRow.Render(_service);
    }


    public void OnOpenWalletPopup()
    {
        _closeReason = "back";
        Instantiate(_walletPopupPrefab, transform.parent);
        Destroy(gameObject);
    }

    private void OnRowClicked(PayoutMethodType type)
    {
        bool isConnected = _service.IsConnected(type);

        LogEvent("payment_method_row_click",
            new Parameter("method_type", MethodKey(type)),
            new Parameter("is_connected", B(isConnected))
        );

        // по требованию: если подключён — клик делает дефолтным
        if (_service.IsConnected(type))
        {
            LogEvent("payment_method_set_default",
                new Parameter("method_type", MethodKey(type))
            );

            _service.SetDefault(type);
            return;
        }

        // если не подключён — можно открыть connect popup
        OpenPopup(type);
        print($"OnRowClicked() - {type}");
    }



    private void OnOpenConnectPopup(PayoutMethodType type)
    {
        OpenPopup(type);
    }


    private void OpenPopup(PayoutMethodType type)
    {
        PayoutPopupBase payoutPopup = null;

        switch (type)
        {
            case PayoutMethodType.PayPal: payoutPopup = paypalPopupPrefab; break;
            case PayoutMethodType.Visa: payoutPopup = visaPopupPrefab; break;
            case PayoutMethodType.Crypto: payoutPopup = cryptoPopupPrefab; break;
        }


        string mode = _service.IsConnected(type) ? "edit" : "connect";

        LogEvent("payout_method_popup_open",
            new Parameter("method_type", MethodKey(type)),
            new Parameter("mode", mode)
        );


        Instantiate(payoutPopup, transform);
    }
}
