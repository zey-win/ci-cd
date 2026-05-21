using System;
using UnityEngine;
using UnityEngine.UI;

public class DeleteConfirmPopup : MonoBehaviour
{
    [SerializeField] private Button _noButton;
    [SerializeField] private Button _yesButton;


    public void Init(Action onNoCallback, Action onYesCallback)
    {
        _noButton.onClick.AddListener(() => onNoCallback?.Invoke());
        _yesButton.onClick.AddListener(() => onYesCallback?.Invoke());
    }


    private void OnDestroy()
    {
        _noButton.onClick.RemoveAllListeners();
        _yesButton.onClick.RemoveAllListeners();
    }


    public void OnClose()
    {
        Destroy(gameObject);
    }
}
