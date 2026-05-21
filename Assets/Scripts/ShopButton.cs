using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ShopButton : MonoBehaviour
{
    [SerializeField] private ShopManager _shopManagerPrefab;

    private void OnEnable()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            Instantiate(_shopManagerPrefab);
        });
    }


    private void OnDisable()
    {
        GetComponent<Button>().onClick.RemoveAllListeners();
    }
}
