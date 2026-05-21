using System;
using UnityEngine;


public class PushRequestCanvas : MonoBehaviour
{
    private Action _onConfirmCallback;
    private Action _onCancelCallback;




    public void Init(Action onConfirmCallback, Action onCancelCallback)
    {
        _onConfirmCallback = onConfirmCallback;
        _onCancelCallback = onCancelCallback;
    }


    public void OnConfirmHandler()
    {
        if (_onConfirmCallback == null) return;

        _onConfirmCallback?.Invoke();
    }


    public void OnCancelHandler()
    {
        if (_onCancelCallback == null) return;

        _onCancelCallback?.Invoke();
    }
}
