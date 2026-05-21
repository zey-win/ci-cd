using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SettingsButton : MonoBehaviour
{
    [SerializeField] private SettingsPopup _settingsPopupPrefab;

    private void OnEnable()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            Instantiate(_settingsPopupPrefab, transform.parent);
        });
    }


    private void OnDisable()
    {
        GetComponent<Button>().onClick.RemoveAllListeners();
    }
}
