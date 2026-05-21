using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupButton : MonoBehaviour
{
    [SerializeField] private Popup _popupPrefab;
    [SerializeField] private Canvas _canvas;



    public void OnClick()
    {
        Instantiate(_popupPrefab, _canvas.transform);
    }
}
