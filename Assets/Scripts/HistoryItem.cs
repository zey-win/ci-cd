using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HistoryItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _numberField;

    public void Init(string value, Color color)
    {
        _numberField.text = value;
        GetComponent<Image>().color = color;
    }
}
