using System;

public interface IPayoutMethodsService
{
    PayoutMethodsState State { get; }
    event Action OnChanged;

    void ConnectPayPal(string email);
    void ConnectVisa(string cardNumber);
    void ConnectCrypto(string chain, string address);

    void Disconnect(PayoutMethodType type);

    bool IsConnected(PayoutMethodType type);
    bool IsDefault(PayoutMethodType type);
    bool TryGetDefault(out PayoutMethodType type);

    void SetDefault(PayoutMethodType type);

    string GetDisplayLine(PayoutMethodType type);
    string GetPayoutData(PayoutMethodType type);

}
