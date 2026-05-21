using Firebase.Analytics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class PayoutPopupBase : MonoBehaviour
{
    [Header("Common UI")]
    [SerializeField] protected Button submitButton;
    [SerializeField] protected Button closeButton;
    [SerializeField] protected Button deleteButton;

    [SerializeField] protected TMP_Text errorText;

    [SerializeField] private DeleteConfirmPopup _deleteConfirmPopup;


    protected IPayoutMethodsService Service;

    public abstract PayoutMethodType MethodType { get; }

    // ===== ANALYTICS =====
    private void LogEvent(string name, params Parameter[] parameters)
    {
        AnalyticsSafe.LogEvent(name, parameters);
    }

    private static string MethodKey(PayoutMethodType t) =>
        t == PayoutMethodType.PayPal ? "paypal" :
        t == PayoutMethodType.Visa ? "visa" :
        t == PayoutMethodType.Crypto ? "crypto" : "none";
    // ===== /ANALYTICS =====


    protected virtual void Awake()
    {
        Service = PayoutMethodsManager.Instance.Service;

        if (submitButton != null)
            submitButton.onClick.AddListener(HandleSubmit);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (deleteButton != null)
            deleteButton.onClick.AddListener(HandleDelete);
    }

    protected virtual void OnEnable()
    {
        SetError(null);
        PopulateFromState();
    }


    void OnDisable()
    {
        submitButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();
        deleteButton.onClick.RemoveAllListeners();
    }

    public virtual void Close() => Destroy(gameObject);


    private void HandleSubmit()
    {
        LogEvent("payout_method_submit_click",
            new Parameter("method_type", MethodKey(MethodType))
        );

        SetError(null);

        if (TrySubmit(out var error))
        {
            LogEvent("payout_method_submit_success",
                new Parameter("method_type", MethodKey(MethodType))
            );
            // TrySubmit внутри вызывает Service.Connect..., а сервис сохраняет state в репозиторий
            Close();
        }
        else
        {
            LogEvent("payout_method_submit_fail",
                new Parameter("method_type", MethodKey(MethodType)),
                new Parameter("error_type", "validation")
            );

            // остаёмся открытыми
            SetError(error);
        }
    }


    private void HandleDelete()
    {
        LogEvent("payout_method_delete_click",
            new Parameter("method_type", MethodKey(MethodType))
        );


        DeleteConfirmPopup deleteConfirmPopupInstance = Instantiate(_deleteConfirmPopup, transform);
        deleteConfirmPopupInstance.Init(
        () =>
        {
            Destroy(deleteConfirmPopupInstance.gameObject);
        },

        () =>
        {
            SetError(null);

            LogEvent("payout_method_delete_confirm",
                new Parameter("method_type", MethodKey(MethodType))
            );

            Service.Disconnect(MethodType);

            LogEvent("payout_method_disconnected",
                new Parameter("method_type", MethodKey(MethodType))
            );

            Close();

        });
    }


    protected void SetError(string message)
    {
        if (errorText == null) return;

        bool has = !string.IsNullOrEmpty(message);
        errorText.gameObject.SetActive(has);
        if (has) errorText.text = message;
    }

    protected abstract void PopulateFromState();


    protected abstract bool TrySubmit(out string error);
}
