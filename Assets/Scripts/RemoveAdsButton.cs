using UnityEngine;

public class RemoveAdsButton : MonoBehaviour
{
    [SerializeField] private RemoveAdsPopup _removeAdsPopup;
    [SerializeField] private Canvas _canvas;



    private void OnEnable()
    {
        NoAdsManager.OnChanged += OnChangeNoAdsState;
        OnChangeNoAdsState(NoAdsManager.IsOwned);
    }


    private void OnDisable()
    {
        NoAdsManager.OnChanged -= OnChangeNoAdsState;
    }


    private void OnChangeNoAdsState(bool isState)
    {
        gameObject.SetActive(!isState);
    }


    public void OnOpenClick()
    {
        Instantiate(_removeAdsPopup, _canvas.transform);
    }
}
