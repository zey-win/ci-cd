using System;
using InteractiveKeyboard.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyboardManager : MonoBehaviour
{
    [SerializeField] private KeyboardFunction _standard;
    [SerializeField] private KeyboardFunction _numeric;
    [SerializeField] private Button _closeButton;

    private void OnEnable()
    {
        SelectableInputField.OnSelectField += SelectInputField;
    }

    private void OnDisable()
    {
        SelectableInputField.OnSelectField -= SelectInputField;
    }

    private void SelectInputField(TMP_InputField field, InputType type)
    {
        _closeButton.gameObject.SetActive(true);
        switch (type)
        {
            case InputType.Numeric:
                _standard.gameObject.SetActive(false);

                _numeric.gameObject.SetActive(true);
                _numeric.SetInputField(field);
                break;
            case InputType.Alphabetic:
                _numeric.gameObject.SetActive(false);

                _standard.gameObject.SetActive(true);
                _standard.SetInputField(field);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}