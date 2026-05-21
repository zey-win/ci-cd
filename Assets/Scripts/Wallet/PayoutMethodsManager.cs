using System.Collections.Generic;
using UnityEngine;

public class PayoutMethodsManager : MonoBehaviour
{
    public static PayoutMethodsManager Instance { get; private set; }

    public IPayoutMethodsService Service { get; private set; }

    [SerializeField] private List<PaymentMethodIcon> _paymentMethodIcons = new List<PaymentMethodIcon>();



    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        var repo = new FilePayoutMethodsRepository();
        Service = new PayoutMethodsService(repo);
    }



    private PayoutMethodType GetDefaultTypeOrFallback()
    {
        if (Service.TryGetDefault(out var def) && Service.IsConnected(def))
            return def;

        if (Service.IsConnected(PayoutMethodType.PayPal)) return PayoutMethodType.PayPal;
        if (Service.IsConnected(PayoutMethodType.Visa)) return PayoutMethodType.Visa;
        return PayoutMethodType.Crypto;
    }

    public Sprite GetIcon()
    {
        PayoutMethodType payoutMethodType = GetDefaultTypeOrFallback();
        var entry = _paymentMethodIcons.Find(x => x.PayoutMethodType == payoutMethodType);
        return entry != null ? entry.Icon : null;
    }
}
