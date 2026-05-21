using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationData", menuName = "Localization/Data")]
public class LocalizationData : ScriptableObject
{
    public List<LocalizedEntry> entries;
}


[System.Serializable]
public class LocalizedEntry
{
    public string key;
    [TextArea] public string value;
}
