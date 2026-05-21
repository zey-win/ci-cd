using TMPro;
using UnityEngine;

public class VisaPopupController : PayoutPopupBase
{
    [Header("Visa UI")]
    public TMP_InputField cardNumber;

    public override PayoutMethodType MethodType => PayoutMethodType.Visa;

    protected override void PopulateFromState()
    {
        deleteButton.gameObject.SetActive(Service.State.visa != null && Service.State.visa.isConnected);

        var mask = cardNumber.GetComponentInChildren<CardNumberInputMask>();

        if (Service.State.visa != null && Service.State.visa.isConnected)
        {
            var raw = Service.State.visa.cardNumber;
            if (mask != null) mask.SetRawDigits(raw);
            else cardNumber.text = raw;
        }
        else
        {
            if (mask != null) mask.SetRawDigits("");
            else cardNumber.text = "";
        }
    }


    protected override bool TrySubmit(out string error)
    {
        var mask = cardNumber.GetComponentInChildren<CardNumberInputMask>();
        var raw = mask != null ? mask.GetRawDigits() : cardNumber.text.Replace(" ", "");


        if (string.IsNullOrEmpty(raw))
        {
            error = "Enter card number";
            return false;
        }

        Service.ConnectVisa(raw);
        error = null;
        return true;
    }

}
