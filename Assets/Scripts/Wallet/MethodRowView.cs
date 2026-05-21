using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MethodRowView : MonoBehaviour
{
    [SerializeField] private PayoutMethodType type;

    [Header("UI")]
    [SerializeField] private TMP_Text _paymentName;
    [SerializeField] private GameObject _connectInfoContainer;
    [SerializeField] private TMP_Text _subText;

    [SerializeField] private Button _connectButton;
    [SerializeField] private Button _editButton;

    [SerializeField] private GameObject _defaultCheckmark;


    private Action<PayoutMethodType> _onUseDefaultCallback;



    public void Init(Action<PayoutMethodType> onConnectCallback, Action<PayoutMethodType> onUseDefaultCallback)
    {
        _connectButton.onClick.AddListener(() => onConnectCallback?.Invoke(type));
        _editButton.onClick.AddListener(() => onConnectCallback?.Invoke(type));
        _onUseDefaultCallback = onUseDefaultCallback;
    }


    public void Render(IPayoutMethodsService service)
    {
        var connected = service.IsConnected(type);
        var isDefault = service.IsDefault(type);

        _paymentName.gameObject.SetActive(!connected);
        _connectInfoContainer.SetActive(connected);
        _connectButton.gameObject.SetActive(!connected);
        _defaultCheckmark.SetActive(connected && isDefault);
        _subText.text = connected ? service.GetDisplayLine(type) : "";

        if (connected && !isDefault)
        {
            GetComponent<Button>().interactable = true;
            GetComponent<Button>().onClick.AddListener(() => _onUseDefaultCallback?.Invoke(type));
        }
        else if (connected && isDefault)
        {
            GetComponent<Button>().interactable = false;
            GetComponent<Button>().onClick.RemoveAllListeners();
        }
        else if (!connected)
        {
            GetComponent<Button>().interactable = false;
        }
    }


    void OnDestroy()
    {
        GetComponent<Button>().onClick.RemoveAllListeners();
    }
}
