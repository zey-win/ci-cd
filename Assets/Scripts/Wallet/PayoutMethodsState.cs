using System;
using UnityEngine;

public enum PayoutMethodType { PayPal, Visa, Crypto }

[Serializable]
public class PayoutMethodsState
{
    public PayPalData paypal;
    public VisaData visa;
    public CryptoData crypto;

    public int defaultMethod = -1;

    public bool IsConnected(PayoutMethodType type) => type switch
    {
        PayoutMethodType.PayPal => paypal != null && paypal.isConnected,
        PayoutMethodType.Visa => visa != null && visa.isConnected,
        PayoutMethodType.Crypto => crypto != null && crypto.isConnected,
        _ => false
    };

    public bool TryGetDefault(out PayoutMethodType type)
    {
        if (defaultMethod < 0) { type = default; return false; }
        type = (PayoutMethodType)defaultMethod;
        return true;
    }

    public bool IsDefault(PayoutMethodType type) => defaultMethod == (int)type;
}

[Serializable]
public class PayPalData
{
    public bool isConnected;
    public string email;
    public long connectedAtUtcTicks;
}

[Serializable]
public class VisaData
{
    public bool isConnected;
    public string cardNumber;
    public long connectedAtUtcTicks;
}

[Serializable]
public class CryptoData
{
    public bool isConnected;
    public string chain;
    public string address;

    public long connectedAtUtcTicks;
}
