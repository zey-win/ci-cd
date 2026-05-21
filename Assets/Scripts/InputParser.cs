using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class InputParser : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;

    public bool Interactable
    {
        get => _inputField.interactable;
        set => _inputField.interactable = value;
    }

    public float Value
    {
        get
        {
            if (_inputField == null)
                return 0;

            return ParseInputField(_inputField.text);
        }
        set
        {
            if (_inputField == null) return;
            _inputField.text = value.ToString(CultureInfo.InvariantCulture);
        }
    }

    private void Awake()
    {
        if (_inputField == null)
            _inputField = GetComponentInChildren<TMP_InputField>();
    }

    private static float ParseInputField(string arg0)
    {
        if (float.TryParse(arg0, out float value))
            return value;
        return 0;
    }
}