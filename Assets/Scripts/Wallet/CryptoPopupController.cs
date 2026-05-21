using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CryptoPopupController : PayoutPopupBase
{
    [Header("Crypto UI")]
    public TMP_Dropdown chainDropdown;
    public TMP_InputField address;

    [SerializeField] private Color invalidTextColor = Color.red;

    public override PayoutMethodType MethodType => PayoutMethodType.Crypto;

    private static readonly Dictionary<string, AddressRule> Rules = new()
    {
        { "ETH", new AddressRule(minLen: 42, maxLen: 42, prefix: "0x", charset: AddressCharset.HexLowerUpper) },
        { "TRON", new AddressRule(minLen: 34, maxLen: 34, prefix: "T", charset: AddressCharset.Base58) },
        // { "BTC", new AddressRule(minLen: 26, maxLen: 62, prefix: null, charset: AddressCharset.Base58OrBech32) },
        // { "SOL", new AddressRule(minLen: 32, maxLen: 44, prefix: null, charset: AddressCharset.Base58) },
        // { "BEP20", new AddressRule(minLen: 42, maxLen: 42, prefix: "0x", charset: AddressCharset.HexLowerUpper) },
    };


    protected override void PopulateFromState()
    {
        deleteButton.gameObject.SetActive(Service.State.crypto != null && Service.State.crypto.isConnected);

        if (Service.State.crypto != null && Service.State.crypto.isConnected)
        {
            address.text = Service.State.crypto.address;

            var chain = Service.State.crypto.chain;
            for (int i = 0; i < chainDropdown.options.Count; i++)
            {
                if (chainDropdown.options[i].text == chain)
                {
                    chainDropdown.value = i;
                    break;
                }
            }
        }
        else
        {
            address.text = "";
            if (chainDropdown.options.Count > 0) chainDropdown.value = 0;
        }
    }

    protected override bool TrySubmit(out string error)
    {
        if (chainDropdown.options.Count == 0)
        {
            error = "Select chain";
            return false;
        }

        var chain = chainDropdown.options[chainDropdown.value].text;
        var addr = address.text.Trim();

        if (string.IsNullOrEmpty(addr))
        {
            error = "Enter address";
            return false;
        }

        if (!ValidateAddress(chain, addr, out error))
        {
            address.textComponent.color = invalidTextColor;
            return false;
        }

        Service.ConnectCrypto(chain, addr);
        error = null;
        return true;
    }

    private static bool ValidateAddress(string chain, string address, out string error)
    {
        if (!Rules.TryGetValue(chain, out var rule))
        {
            if (address.Length < 10)
            {
                error = "Address is too short";
                return false;
            }

            error = null;
            return true;
        }

        if (address.Length < rule.MinLen || address.Length > rule.MaxLen)
        {
            error = $"Address length must be {rule.MinLen}-{rule.MaxLen} chars";
            return false;
        }

        if (!string.IsNullOrEmpty(rule.Prefix) && !address.StartsWith(rule.Prefix, StringComparison.Ordinal))
        {
            error = $"Address must start with '{rule.Prefix}'";
            return false;
        }

        if (!IsAllowedCharset(address, rule.Charset))
        {
            error = "Address contains invalid characters";
            return false;
        }

        error = null;
        return true;
    }

    private static bool IsAllowedCharset(string s, AddressCharset charset)
    {
        switch (charset)
        {
            case AddressCharset.HexLowerUpper:
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    bool ok =
                        (c >= '0' && c <= '9') ||
                        (c >= 'a' && c <= 'f') ||
                        (c >= 'A' && c <= 'F') ||
                        c == 'x' || c == 'X';
                    if (!ok) return false;
                }
                return true;

            case AddressCharset.Base58:
                const string base58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
                for (int i = 0; i < s.Length; i++)
                    if (base58.IndexOf(s[i]) < 0) return false;
                return true;

            case AddressCharset.Base58OrBech32:
                if (s.StartsWith("bc1", StringComparison.OrdinalIgnoreCase) ||
                    s.StartsWith("tb1", StringComparison.OrdinalIgnoreCase))
                {
                    for (int i = 0; i < s.Length; i++)
                    {
                        char c = s[i];
                        bool ok = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z');
                        if (!ok) return false;
                    }
                    return true;
                }
                else
                {
                    const string b58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
                    for (int i = 0; i < s.Length; i++)
                        if (b58.IndexOf(s[i]) < 0) return false;
                    return true;
                }

            default:
                return true;
        }
    }

    private readonly struct AddressRule
    {
        public readonly int MinLen;
        public readonly int MaxLen;
        public readonly string Prefix;
        public readonly AddressCharset Charset;

        public AddressRule(int minLen, int maxLen, string prefix, AddressCharset charset)
        {
            MinLen = minLen;
            MaxLen = maxLen;
            Prefix = prefix;
            Charset = charset;
        }
    }

    private enum AddressCharset
    {
        Any,
        HexLowerUpper,
        Base58,
        Base58OrBech32
    }
}
