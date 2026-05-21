using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaymentMethodErrorPopup : MonoBehaviour
{
    public void OnClose()
    {
        Destroy(gameObject);
    }
}
