using TMPro;
using UnityEngine;

public class LocalizedText : MonoBehaviour
{
    public string key;

    void Start()
    {
        var textComp = GetComponent<TextMeshProUGUI>();
        textComp.text = LocalizationManager.Get(key);
    }
}
