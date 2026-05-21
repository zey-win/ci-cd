using UnityEngine;

public class PaymentInProgressPopup : MonoBehaviour
{
    public void OnClose()
    {
        Destroy(gameObject);
    }
}
