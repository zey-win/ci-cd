using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OfferPopupActions : MonoBehaviour
{
    [Header("Optional UI")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Buttons")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Open button label (optional)")]
    [SerializeField] private TextMeshProUGUI openButtonText;

    [SerializeField] private string defaultOpenButtonText = "Open";
    [SerializeField] private string openingText = "Opening...";

    private bool _openClicked;

    public void Bind(OfferRemoteConfigData data, Action onOpen, Action onClose)
    {
        _openClicked = false;

        if (titleText != null) titleText.text = data?.title ?? "";
        if (descriptionText != null) descriptionText.text = data?.description ?? "";

        SetOpenButtonLabel(data);

        if (openButton != null)
        {
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(() =>
            {
                if (_openClicked) return;
                _openClicked = true;

                // ✅ UI lock
                SetButtonsInteractable(false);

                // ✅ change text
                if (openButtonText != null)
                    openButtonText.text = openingText;

                onOpen?.Invoke();
            });
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                if (_openClicked) return; // если уже жмём Open — закрывать нельзя
                onClose?.Invoke();
            });
        }
    }

    private void SetOpenButtonLabel(OfferRemoteConfigData data)
    {
        if (openButtonText == null) return;

        string txt = (data != null && !string.IsNullOrEmpty(data.button_text))
            ? data.button_text
            : defaultOpenButtonText;

        openButtonText.text = txt;
    }

    private void SetButtonsInteractable(bool value)
    {
        if (openButton != null) openButton.interactable = value;
        if (closeButton != null) closeButton.interactable = value;
    }
}
