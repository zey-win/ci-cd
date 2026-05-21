using System;
using System.Net.Mail;
using TMPro;
using UnityEngine;

public class PayPalPopupController : PayoutPopupBase
{
    [Header("PayPal UI")]
    public TMP_InputField email;
    public TMP_InputField confirmEmail;

    [Header("Validation colors")]
    [SerializeField] private Color invalidTextColor = Color.red;

    // Поле красим ТОЛЬКО если пустое
    [SerializeField] private Color emptyFieldColor = new Color(1f, 0.85f, 0.85f);

    private Color _emailNormalTextColor;
    private Color _confirmNormalTextColor;

    private Color _emailNormalFieldColor;
    private Color _confirmNormalFieldColor;

    public override PayoutMethodType MethodType => PayoutMethodType.PayPal;

    protected override void Awake()
    {
        base.Awake();

        // запомним исходные цвета текста
        if (email != null && email.textComponent != null)
            _emailNormalTextColor = email.textComponent.color;

        if (confirmEmail != null && confirmEmail.textComponent != null)
            _confirmNormalTextColor = confirmEmail.textComponent.color;

        // запомним исходные цвета поля (фон инпута)
        if (email != null && email.targetGraphic != null)
            _emailNormalFieldColor = email.targetGraphic.color;

        if (confirmEmail != null && confirmEmail.targetGraphic != null)
            _confirmNormalFieldColor = confirmEmail.targetGraphic.color;

        // при вводе — сбрасываем подсветку именно этого поля
        if (email != null) email.onValueChanged.AddListener(_ =>
        {
            SetEmailTextValid(true);
            SetEmailFieldEmpty(false);
        });

        if (confirmEmail != null) confirmEmail.onValueChanged.AddListener(_ =>
        {
            SetConfirmTextValid(true);
            SetConfirmFieldEmpty(false);
        });
    }

    private void SetEmailTextValid(bool valid)
    {
        if (email?.textComponent == null) return;
        email.textComponent.color = valid ? _emailNormalTextColor : invalidTextColor;
    }

    private void SetConfirmTextValid(bool valid)
    {
        if (confirmEmail?.textComponent == null) return;
        confirmEmail.textComponent.color = valid ? _confirmNormalTextColor : invalidTextColor;
    }

    // ПОЛЕ красим только при пустом значении
    private void SetEmailFieldEmpty(bool isEmptyError)
    {
        if (email?.targetGraphic == null) return;
        email.targetGraphic.color = isEmptyError ? emptyFieldColor : _emailNormalFieldColor;
    }

    private void SetConfirmFieldEmpty(bool isEmptyError)
    {
        if (confirmEmail?.targetGraphic == null) return;
        confirmEmail.targetGraphic.color = isEmptyError ? emptyFieldColor : _confirmNormalFieldColor;
    }

    private void ResetAllVisual()
    {
        SetEmailTextValid(true);
        SetConfirmTextValid(true);
        SetEmailFieldEmpty(false);
        SetConfirmFieldEmpty(false);
    }

    protected override void PopulateFromState()
    {
        deleteButton.gameObject.SetActive(Service.State.paypal != null && Service.State.paypal.isConnected);

        if (Service.State.paypal != null && Service.State.paypal.isConnected)
        {
            email.text = Service.State.paypal.email;
            confirmEmail.text = Service.State.paypal.email;
        }
        else
        {
            email.text = "";
            confirmEmail.text = "";
        }

        ResetAllVisual();
    }

    protected override bool TrySubmit(out string error)
    {
        var e1 = (email != null ? email.text : "").Trim();
        var e2 = (confirmEmail != null ? confirmEmail.text : "").Trim();

        ResetAllVisual();

        // 1) Email пустой -> красим текст + поле
        if (string.IsNullOrEmpty(e1))
        {
            error = "Enter email";
            SetEmailTextValid(false);
            SetEmailFieldEmpty(true);
            return false;
        }

        // 2) Email невалидный -> красим только текст (поле НЕ красим)
        if (!IsValidEmail(e1))
        {
            error = "Email is not valid";
            SetEmailTextValid(false);
            SetEmailFieldEmpty(false);
            return false;
        }

        // 3) Confirm пустой -> красим текст + поле (потому что пустой)
        if (string.IsNullOrEmpty(e2))
        {
            error = "Confirm email";
            SetConfirmTextValid(false);
            SetConfirmFieldEmpty(true);
            return false;
        }

        // 4) Не совпадают -> красим только текст confirm (поле НЕ красим)
        if (!string.Equals(e1, e2, StringComparison.OrdinalIgnoreCase))
        {
            error = "Emails do not match";
            SetConfirmTextValid(false);
            SetConfirmFieldEmpty(false);
            return false;
        }

        Service.ConnectPayPal(e1);
        error = null;
        return true;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return string.Equals(addr.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
