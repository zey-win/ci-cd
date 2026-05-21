using System.Collections.Generic;
using UnityEngine;

public static class LocalizationManager
{
    private static Dictionary<string, string> localizedTexts;
    private static bool isInitialized = false;

    public static void Init(LocalizationData data)
    {
        localizedTexts = new Dictionary<string, string>();

        foreach (var entry in data.entries)
        {
            // Если вдруг внутри одного языка есть дубли — перезапишем, но не падаем
            localizedTexts[entry.key] = entry.value;
        }

        isInitialized = true;
    }

    public static string Get(string key)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("LocalizationManager not initialized!");
            return key;
        }

        if (localizedTexts.TryGetValue(key, out string value))
            return value;

        Debug.LogWarning($"No translation for key: {key}");
        return key;
    }
}
