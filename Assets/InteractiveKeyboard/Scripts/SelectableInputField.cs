using System;
using TMPro;
using UnityEngine;

public enum InputType
{
    Numeric,
    Alphabetic
}

public class SelectableInputField : MonoBehaviour
{
    public InputType inputType;
    private TMP_InputField _inputField;

    public static Action<TMP_InputField, InputType> OnSelectField;

    private void Awake()
    {
        _inputField = GetComponent<TMP_InputField>();
    }

    private void Start()
    {
        _inputField.onSelect.AddListener(Selected);
    }

    private void Selected(string arg0)
    {
        OnSelectField?.Invoke(_inputField, inputType);
    }
}