using System.Text;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class CardNumberInputMask : MonoBehaviour
{
    [SerializeField] private TMP_InputField input;
    [SerializeField] private int maxDigits = 16;

    private bool _updating;

    private void Reset() => input = GetComponent<TMP_InputField>();

    private void Awake()
    {
        if (input == null) input = GetComponent<TMP_InputField>();
        input.onValueChanged.AddListener(OnChanged);
    }

    private void OnDestroy()
    {
        if (input != null) input.onValueChanged.RemoveListener(OnChanged);
    }

    private void OnChanged(string value)
    {
        if (_updating) return;
        _updating = true;

        // 1) Сколько цифр было слева от курсора в текущем (возможно уже форматированном) тексте
        int caretIndex = input.stringPosition;
        int digitsBeforeCaret = CountDigitsBeforeIndex(value, caretIndex);

        // 2) Оставляем только цифры и ограничиваем длину
        string digits = ExtractDigits(value);
        if (digits.Length > maxDigits)
            digits = digits.Substring(0, maxDigits);

        // 3) Форматируем
        string formatted = FormatGroupsOf4(digits);

        // 4) Ставим текст без рекурсии
        input.SetTextWithoutNotify(formatted);

        // 5) Восстанавливаем каретку по "количеству цифр слева"
        int newCaret = IndexAfterNDigits(formatted, digitsBeforeCaret);
        input.stringPosition = newCaret;
        input.caretPosition = newCaret;

        _updating = false;
    }

    // --- Public helpers ---

    public string GetRawDigits() => ExtractDigits(input.text);

    /// <summary>Установить сырые цифры программно (например, при открытии попапа)</summary>
    public void SetRawDigits(string rawDigits)
    {
        if (input == null) input = GetComponent<TMP_InputField>();

        _updating = true;

        string digits = ExtractDigits(rawDigits);
        if (digits.Length > maxDigits)
            digits = digits.Substring(0, maxDigits);

        string formatted = FormatGroupsOf4(digits);
        input.SetTextWithoutNotify(formatted);

        // курсор в конец
        input.stringPosition = formatted.Length;
        input.caretPosition = formatted.Length;

        _updating = false;
    }

    // --- Utilities ---

    private static int CountDigitsBeforeIndex(string text, int index)
    {
        int count = 0;
        int safe = Mathf.Clamp(index, 0, text.Length);
        for (int i = 0; i < safe; i++)
        {
            char c = text[i];
            if (c >= '0' && c <= '9') count++;
        }
        return count;
    }

    /// <summary>Вернуть позицию в строке после N цифр (учитывая пробелы)</summary>
    private static int IndexAfterNDigits(string formatted, int nDigits)
    {
        if (nDigits <= 0) return 0;

        int count = 0;
        for (int i = 0; i < formatted.Length; i++)
        {
            char c = formatted[i];
            if (c >= '0' && c <= '9')
            {
                count++;
                if (count == nDigits)
                    return i + 1; // позиция ПОСЛЕ этой цифры
            }
        }
        return formatted.Length;
    }

    private static string ExtractDigits(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
            if (c >= '0' && c <= '9')
                sb.Append(c);
        return sb.ToString();
    }

    private static string FormatGroupsOf4(string digits)
    {
        var sb = new StringBuilder(digits.Length + digits.Length / 4);
        for (int i = 0; i < digits.Length; i++)
        {
            if (i > 0 && i % 4 == 0) sb.Append(' ');
            sb.Append(digits[i]);
        }
        return sb.ToString();
    }
}
