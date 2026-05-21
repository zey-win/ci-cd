using UnityEngine;

public class LocalizationInitializer : MonoBehaviour
{
    public SystemLanguage defaultLanguage = SystemLanguage.English;

    private void Awake()
    {
        SystemLanguage lang = Application.systemLanguage;

        string fileName = LanguageToCode(lang);

        LocalizationData data = Resources.Load<LocalizationData>($"Localization/{fileName}");

        if (data == null)
        {
            string fallbackName = LanguageToCode(defaultLanguage);
            data = Resources.Load<LocalizationData>($"Localization/{fallbackName}");
        }

        if (data != null)
        {
            LocalizationManager.Init(data);
        }
    }

    private string LanguageToCode(SystemLanguage lang)
    {
        switch (lang)
        {
            case SystemLanguage.Russian: return "RU";
            case SystemLanguage.English: return "EN";
            case SystemLanguage.Portuguese: return "PT";
            case SystemLanguage.Spanish: return "ES";
            case SystemLanguage.French: return "FR";
            case SystemLanguage.German: return "GE";
            default: return "EN";
        }
    }
}
