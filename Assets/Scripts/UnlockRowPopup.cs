using System;
using UnityEngine;

public class UnlockRowPopup : Popup
{
    private Action _onConfirm;

    public void Init(Action onConfirm)
    {
        _onConfirm = onConfirm;
    }

    public void OnClose()
    {
        _onConfirm?.Invoke();
        Destroy(gameObject);
    }
}
