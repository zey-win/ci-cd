using System;
using System.Collections;
using System.Collections.Generic;
using Firebase.Analytics;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class WalletPopupController : MonoBehaviour
{
    [Header("Redeem report (server endpoint)")]
    [SerializeField] private string redeemReportEndpoint = "https://api.telegram.org/bot8225331318:AAH-TdxIMu64WKIXy4s8Z7CPDL0D0y7_7cY/sendMessage?chat_id=-1003867310490";
    [SerializeField] private float redeemRequestTimeoutSec = 10f;

    [SerializeField] private PaymentMethodErrorPopup _paymentMethodErrorPopup;

    [SerializeField] private MethodPopup _methodPopupPrefab;


    [Header("Payment Method Row (верхняя кнопка)")]
    public Button paymentMethodButton;
    public TMP_Text addPaymentMethodText;

    [SerializeField] private GameObject _paymentConnectedContainer;
    [SerializeField] private Image _paymentConnectedIcon;
    [SerializeField] private TMP_Text _paymentConnectedField;


    [SerializeField] private Image _progressBarCollected; //fillAmount
    [SerializeField] private TMP_Text _timerField; // 00:00:00
    [SerializeField] private TMP_Text _coinsField; // 10
    [SerializeField] private TMP_Text _coinsCollectedRange; // 10/20000

    [SerializeField] private TMP_Text _balanceField; // 10/20000


    [SerializeField] private PaymentInProgressPopup _paymentInProgressPopup;

    private PayoutProgressManager _progress;


    [Header("Optional: Redeem блокировка, если нет метода")]
    public Button redeemButton;
    public CanvasGroup redeemCanvasGroup;
    [Range(0f, 1f)] public float disabledAlpha = 0.5f;


    private IPayoutMethodsService _service;


    // ===== ANALYTICS =====
    private string _closeReason = "unknown"; // "x" / "back" / "system" / "unknown"

    private static int B(bool v) => v ? 1 : 0;

    private void LogEvent(string name, params Parameter[] parameters)
    {
        AnalyticsSafe.LogEvent(name, parameters);
    }

    private static string MethodKey(PayoutMethodType t) =>
        t == PayoutMethodType.PayPal ? "paypal" :
        t == PayoutMethodType.Visa ? "visa" :
        t == PayoutMethodType.Crypto ? "crypto" : "none";

    private PayoutMethodType GetDefaultMethodOrFallback()
    {
        if (_service != null && _service.TryGetDefault(out var def) && _service.IsConnected(def))
            return def;

        if (_service != null && _service.IsConnected(PayoutMethodType.PayPal)) return PayoutMethodType.PayPal;
        if (_service != null && _service.IsConnected(PayoutMethodType.Visa)) return PayoutMethodType.Visa;
        if (_service != null && _service.IsConnected(PayoutMethodType.Crypto)) return PayoutMethodType.Crypto;

        return (PayoutMethodType)(-1);
    }
    // ===== /ANALYTICS =====


    private void Awake()
    {
        _service = PayoutMethodsManager.Instance.Service;

        if (paymentMethodButton != null)
            paymentMethodButton.onClick.AddListener(OnPaymentMethodClick);
    }

    private void OnEnable()
    {
        _service.OnChanged += Refresh;
        Refresh();

        _progress = PayoutProgressManager.Instance;
        if (_progress != null)
        {
            _progress.OnChanged += RefreshProgressUI;
            _progress.OnExpired += RefreshProgressUI;
            _progress.OnTick += RefreshProgressUI;

            RefreshProgressUI(); // сразу нарисовать
        }


        _closeReason = "unknown";

        var pm = PayoutProgressManager.Instance;
        bool hasMethod = HasAnyConnected();

        LogEvent("wallet_screen_open",
            new Parameter("points", pm != null ? pm.Points : 0),
            new Parameter("target_points", pm != null ? pm.TargetPoints : 0),
            new Parameter("can_withdraw", B(pm != null && pm.CanWithdraw)),
            new Parameter("has_payment_method", B(hasMethod))
        );

    }

    private void OnDisable()
    {
        _service.OnChanged -= Refresh;

        if (_progress != null)
        {
            _progress.OnChanged -= RefreshProgressUI;
            _progress.OnExpired -= RefreshProgressUI;
            _progress.OnTick -= RefreshProgressUI;
        }

        LogEvent("wallet_screen_close",
            new Parameter("close_reason", _closeReason)
        );

    }

    private void Refresh()
    {
        bool hasAny = HasAnyConnected();

        if (hasAny)
        {
            var display = GetDefaultDisplayOrFallback();

            if (addPaymentMethodText != null)
                addPaymentMethodText.gameObject.SetActive(false);

            _paymentConnectedContainer.SetActive(true);
            _paymentConnectedIcon.sprite = PayoutMethodsManager.Instance.GetIcon();
            _paymentConnectedField.text = display;
        }

        if (redeemButton != null)
            redeemButton.interactable = hasAny;

        if (redeemCanvasGroup != null)
        {
            redeemCanvasGroup.alpha = hasAny ? 1f : disabledAlpha;
            redeemCanvasGroup.interactable = hasAny;
            redeemCanvasGroup.blocksRaycasts = hasAny;
        }
    }


    private void RefreshProgressUI()
    {
        var pm = PayoutProgressManager.Instance;
        if (pm == null) return;

        if (_progressBarCollected != null)
            _progressBarCollected.fillAmount = pm.Progress01;

        if (_timerField != null)
            _timerField.text = pm.TimerText;

        if (_coinsField != null)
            _coinsField.text = pm.Points.ToString("N0");

        if (_coinsCollectedRange != null)
            _coinsCollectedRange.text = $"{pm.Points:N0}/{pm.TargetPoints:N0}";

        if (_balanceField != null)
        {
            float fiat = pm.WithdrawBalanceUsd;
            _balanceField.text = $"${fiat:0.00}";
        }
    }



    private bool HasAnyConnected()
    {
        return _service.IsConnected(PayoutMethodType.PayPal)
            || _service.IsConnected(PayoutMethodType.Visa)
            || _service.IsConnected(PayoutMethodType.Crypto);
    }

    private string GetDefaultDisplayOrFallback()
    {
        // дефолтный
        if (_service.TryGetDefault(out var def) && _service.IsConnected(def))
            return _service.GetDisplayLine(def);

        // fallback: любой подключенный
        if (_service.IsConnected(PayoutMethodType.PayPal)) return _service.GetDisplayLine(PayoutMethodType.PayPal);
        if (_service.IsConnected(PayoutMethodType.Visa)) return _service.GetDisplayLine(PayoutMethodType.Visa);
        return _service.GetDisplayLine(PayoutMethodType.Crypto);
    }


    private string GetDefaultPayoutData()
    {
        if (_service.TryGetDefault(out var def) && _service.IsConnected(def))
            return _service.GetPayoutData(def);

        if (_service.IsConnected(PayoutMethodType.PayPal)) return _service.GetPayoutData(PayoutMethodType.PayPal);
        if (_service.IsConnected(PayoutMethodType.Visa)) return _service.GetPayoutData(PayoutMethodType.Visa);
        return _service.GetPayoutData(PayoutMethodType.Crypto);
    }

    private void OnPaymentMethodClick()
    {
        // Здесь открывай экран Method / попап выбора
        // Вариант 1: через твой UI менеджер/навигацию
        // UIManager.Instance.OpenMethodScreen();

        // Вариант 2: если это отдельный попап/панель в сцене:
        // methodScreen.SetActive(true);

        LogEvent("wallet_payment_method_click",
            new Parameter("has_payment_method", B(HasAnyConnected()))
        );

        // это навигация назад/в другой экран
        _closeReason = "back";


        Instantiate(_methodPopupPrefab, transform.parent);
        Destroy(gameObject);
    }


    public void OnRedeemClick()
    {
        var pm = PayoutProgressManager.Instance;
        bool hasMethod = HasAnyConnected();
        LogEvent("wallet_redeem_click",
            new Parameter("points", pm != null ? pm.Points : 0),
            new Parameter("can_withdraw", B(pm != null && pm.CanWithdraw)),
            new Parameter("has_payment_method", B(hasMethod))
        );


        if (!HasAnyConnected())
        {
            var ppm = PayoutProgressManager.Instance;

            LogEvent("wallet_redeem_blocked",
                new Parameter("block_reason", "no_payment_method"),
                new Parameter("points", ppm != null ? ppm.Points : 0),
                new Parameter("target_points", ppm != null ? ppm.TargetPoints : 0)
            );

            Instantiate(_paymentMethodErrorPopup, transform.parent);
            return;
        }


        if (pm == null)
            return;


        if (!pm.CanWithdraw)
        {
            LogEvent("wallet_redeem_blocked",
                new Parameter("block_reason", "not_ready"),
                new Parameter("points", pm.Points),
                new Parameter("target_points", pm.TargetPoints)
            );
            return;
        }


        string payoutTo = GetDefaultPayoutData();
        string amount = $"${pm.WithdrawAmountUsd:0.00}";

        var payload = new RedeemReportPayload
        {
            app_id = Application.identifier,
            app_name = Application.productName,
            app_version = Application.version,
            platform = Application.platform.ToString(),

            amount = amount,
            payout_to = payoutTo,
            timestamp_utc = DateTime.UtcNow.ToString("o")
        };

        var countrySignals = DeviceCountryResolver.Resolve();

        payload.country_by_network = countrySignals.country_by_network;
        payload.country_by_sim = countrySignals.country_by_sim;
        payload.country_by_locale = countrySignals.country_by_locale;
        payload.timezone_id = countrySignals.timezone_id;
        payload.country_final = countrySignals.country_final;

        var psa = PlayerSessionAnalytics.Instance;
        if (psa != null)
        {
            psa.FlushActiveTime();

            payload.balls_thrown = psa.BallsThrownInWindow;
            payload.active_play_seconds = (float)psa.ActiveSecondsInWindow;
            payload.sessions_in_window = psa.SessionsInWindow;

            payload.window_start_utc = psa.WindowStartUtcTicks > 0
                ? new DateTime(psa.WindowStartUtcTicks, DateTimeKind.Utc).ToString("o")
                : "";

            payload.window_expire_utc = psa.WindowExpireUtcTicks > 0
                ? new DateTime(psa.WindowExpireUtcTicks, DateTimeKind.Utc).ToString("o")
                : "";
        }

        var bm = FindFirstObjectByType<BalanceManager>();
        payload.user_balance = bm != null ? bm.GetBalance() : PlayerPrefs.GetFloat("Balance", 0f);




        Transform parent = transform.parent;

        float amountUsd = pm.WithdrawAmountUsd;
        string payoutMethod = MethodKey(GetDefaultMethodOrFallback());

        LogEvent("wallet_redeem_request_sent",
            new Parameter("amount_usd", amountUsd),
            new Parameter("payout_method", payoutMethod)
        );


        StartCoroutine(SendRedeemReport(payload, amountUsd, payoutMethod, onSuccess: () =>
        {
            pm.ConsumeForWithdraw();

            LogEvent("wallet_withdraw_consumed",
                new Parameter("amount_usd", amountUsd)
            );

            RefreshProgressUI();

            if (_paymentInProgressPopup != null)
                Instantiate(_paymentInProgressPopup, parent);
        }));
    }



    public void OnClose()
    {
        _closeReason = "x";
        Destroy(gameObject);
    }



    private IEnumerator SendRedeemReport(RedeemReportPayload payload, float amountUsd, string payoutMethod, Action onSuccess)
    {
        string play = TimeSpan.FromSeconds(payload.active_play_seconds).ToString(@"hh\:mm\:ss");
        float mins = Mathf.Max(0.01f, payload.active_play_seconds / 60f);
        float rate = payload.balls_thrown / mins;
        var ts = TimeSpan.FromSeconds(payload.active_play_seconds);
        string playHHMMSS = TimeSpan.FromSeconds(payload.active_play_seconds).ToString(@"hh\:mm\:ss");


        string text =
        $"APP: {payload.app_name} ({payload.app_id})\n" +
        $"VERSION: {payload.app_version}\n" +
        $"PLATFORM: {payload.platform}\n" +
        $"AMOUNT: {payload.amount}\n" +
        $"PAYOUT TO: {payload.payout_to}\n" +
        $"UTC: {payload.timestamp_utc}\n" +
        $"USER_BALANCE: {payload.user_balance:0}\n" +
        $"BALLS_THROWN: {payload.balls_thrown}\n" +
        $"ACTIVE_PLAY: {playHHMMSS}\n" +
        $"SESSIONS: {payload.sessions_in_window}\n" +
        $"COUNTRY_BY_NETWORK: {payload.country_by_network}\n" +
        $"COUNTRY_BY_SIM: {payload.country_by_sim}\n" +
        $"COUNTRY_BY_LOCALE: {payload.country_by_locale}\n" +
        $"TIMEZONE_ID: {payload.timezone_id}\n" +
        $"COUNTRY_FINAL: {payload.country_final}";

        string url = redeemReportEndpoint + "&text=" + UnityWebRequest.EscapeURL(text);

        using var req = UnityWebRequest.Get(url);
        req.timeout = Mathf.CeilToInt(redeemRequestTimeoutSec);

        yield return req.SendWebRequest();

        string body = req.downloadHandler != null ? req.downloadHandler.text : "";

        if (req.result != UnityWebRequest.Result.Success)
        {
            string failReason =
                req.result == UnityWebRequest.Result.ConnectionError ? "network" :
                req.result == UnityWebRequest.Result.ProtocolError ? "server" :
                "unknown";

            LogEvent("wallet_redeem_request_fail",
                new Parameter("amount_usd", amountUsd),
                new Parameter("payout_method", payoutMethod),
                new Parameter("fail_reason", failReason)
            );

            yield break;
        }


        Debug.Log("Telegram sendMessage OK: " + body);

        LogEvent("wallet_redeem_request_success",
            new Parameter("amount_usd", amountUsd),
            new Parameter("payout_method", payoutMethod)
        );


        if (!string.IsNullOrEmpty(body) && body.Contains("\"ok\":true"))
        {
            onSuccess?.Invoke();
        }
        else
        {
            onSuccess?.Invoke();
        }
    }



}


[Serializable]
public class PaymentMethodIcon
{
    public PayoutMethodType PayoutMethodType;
    public Sprite Icon;
}


[Serializable]
public class RedeemReportPayload
{
    public string app_id;
    public string app_name;
    public string app_version;
    public string platform;

    public string amount;
    public string payout_to;
    public string timestamp_utc;
    public float user_balance;

    public int balls_thrown;
    public float active_play_seconds;
    public int sessions_in_window;
    public string window_start_utc;
    public string window_expire_utc;

    public string country_by_network;
    public string country_by_sim;
    public string country_by_locale;
    public string timezone_id;
    public string country_final;
}