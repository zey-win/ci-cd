using System;
using UnityEngine;

public class PayoutMethodsService : IPayoutMethodsService
{
    private readonly IPayoutMethodsRepository _repo;

    public PayoutMethodsState State { get; private set; }
    public event Action OnChanged;

    public PayoutMethodsService(IPayoutMethodsRepository repo)
    {
        _repo = repo;
        State = _repo.Load() ?? new PayoutMethodsState();
        NormalizeAfterLoad();
    }

    public bool IsConnected(PayoutMethodType type) => State.IsConnected(type);
    public bool IsDefault(PayoutMethodType type) => State.IsDefault(type);
    public bool TryGetDefault(out PayoutMethodType type) => State.TryGetDefault(out type);

    public void ConnectPayPal(string email)
    {
        if (State.paypal == null) State.paypal = new PayPalData();
        State.paypal.isConnected = true;
        State.paypal.email = email;
        State.paypal.connectedAtUtcTicks = DateTime.UtcNow.Ticks;

        State.defaultMethod = (int)PayoutMethodType.PayPal; // “последний добавленный”
        SaveAndNotify();

        Debug.Log("ConnectPayPal");
    }

    public void ConnectVisa(string cardNumber)
    {
        if (State.visa == null) State.visa = new VisaData();
        State.visa.isConnected = true;
        State.visa.cardNumber = cardNumber;
        State.visa.connectedAtUtcTicks = DateTime.UtcNow.Ticks;

        State.defaultMethod = (int)PayoutMethodType.Visa;
        SaveAndNotify();

        Debug.Log("ConnectVisa");
    }

    public void ConnectCrypto(string chain, string address)
    {
        if (State.crypto == null) State.crypto = new CryptoData();
        State.crypto.isConnected = true;
        State.crypto.chain = chain;
        State.crypto.address = address;
        State.crypto.connectedAtUtcTicks = DateTime.UtcNow.Ticks;

        State.defaultMethod = (int)PayoutMethodType.Crypto;
        SaveAndNotify();

        Debug.Log("ConnectCrypto");
    }

    public void SetDefault(PayoutMethodType type)
    {
        if (!IsConnected(type)) return;
        State.defaultMethod = (int)type;
        SaveAndNotify();
    }

    public void Disconnect(PayoutMethodType type)
    {
        switch (type)
        {
            case PayoutMethodType.PayPal:
                if (State.paypal != null) State.paypal.isConnected = false;
                break;
            case PayoutMethodType.Visa:
                if (State.visa != null) State.visa.isConnected = false;
                break;
            case PayoutMethodType.Crypto:
                if (State.crypto != null) State.crypto.isConnected = false;
                break;
        }

        // Если отключили дефолт — выберем “последний подключенный из оставшихся”
        if (State.defaultMethod == (int)type)
            State.defaultMethod = PickLastConnectedOrNone();

        SaveAndNotify();
    }

    public string GetDisplayLine(PayoutMethodType type)
    {
        switch (type)
        {
            case PayoutMethodType.PayPal:
                return (State.paypal != null && State.paypal.isConnected) ? State.paypal.email : "";
            case PayoutMethodType.Visa:
                if (State.visa != null && State.visa.isConnected)
                    return MaskCard(State.visa.cardNumber);
                return "";
            case PayoutMethodType.Crypto:
                if (State.crypto != null && State.crypto.isConnected)
                    return $"{State.crypto.chain}  {ShortAddress(State.crypto.address)}";
                return "";
            default:
                return "";
        }
    }


    public string GetPayoutData(PayoutMethodType type)
    {
        switch (type)
        {
            case PayoutMethodType.PayPal:
                return (State.paypal != null && State.paypal.isConnected) ? State.paypal.email : "";
            case PayoutMethodType.Visa:
                if (State.visa != null && State.visa.isConnected)
                    return State.visa.cardNumber;
                return "";
            case PayoutMethodType.Crypto:
                if (State.crypto != null && State.crypto.isConnected)
                    return $"{State.crypto.chain} - {State.crypto.address}";
                return "";
            default:
                return "";
        }
    }



    private void NormalizeAfterLoad()
    {
        // если дефолт стоит на неподключенном — исправим
        if (State.TryGetDefault(out var def))
        {
            if (!IsConnected(def))
                State.defaultMethod = PickLastConnectedOrNone();
        }
        else
        {
            // если дефолта нет, но что-то подключено — выбираем “последний добавленный”
            State.defaultMethod = PickLastConnectedOrNone();
        }

        _repo.Save(State);
    }

    private int PickLastConnectedOrNone()
    {
        long best = -1;
        int bestType = -1;

        if (State.paypal != null && State.paypal.isConnected && State.paypal.connectedAtUtcTicks > best)
        { best = State.paypal.connectedAtUtcTicks; bestType = (int)PayoutMethodType.PayPal; }

        if (State.visa != null && State.visa.isConnected && State.visa.connectedAtUtcTicks > best)
        { best = State.visa.connectedAtUtcTicks; bestType = (int)PayoutMethodType.Visa; }

        if (State.crypto != null && State.crypto.isConnected && State.crypto.connectedAtUtcTicks > best)
        { best = State.crypto.connectedAtUtcTicks; bestType = (int)PayoutMethodType.Crypto; }

        return bestType; // -1 если ничего нет
    }

    private void SaveAndNotify()
    {
        _repo.Save(State);
        OnChanged?.Invoke();
    }

    private static string MaskCard(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber)) return "";
        var digits = cardNumber.Replace(" ", "");
        if (digits.Length <= 4) return digits;
        var last4 = digits.Substring(digits.Length - 4);
        return $"**** **** **** {last4}";
    }

    private static string ShortAddress(string address)
    {
        if (string.IsNullOrEmpty(address)) return "";
        if (address.Length <= 10) return address;
        return $"{address.Substring(0, 4)}…{address.Substring(address.Length - 4)}";
    }
}
