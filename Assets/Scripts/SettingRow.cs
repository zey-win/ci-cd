using UnityEngine;
using UnityEngine.UI;
using Audio;

public class SettingRow : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private string _mixerKey;
    private string _prefsKey;
    private float _defaultValue = 1f;

    private bool _isBound;

    public void Bind(string mixerKey, string prefsKey, float defaultValue = 1f)
    {
        _mixerKey = mixerKey;
        _prefsKey = prefsKey;
        _defaultValue = defaultValue;
        _isBound = true;
    }

    private void OnEnable()
    {
        if (!_isBound) return;
        if (_slider == null) return;

        // Всегда берём из PlayerPrefs (это источник истины)
        float value = Mathf.Clamp01(PlayerPrefs.GetFloat(_prefsKey, _defaultValue));

        // гарантируем, что ключ существует
        if (!PlayerPrefs.HasKey(_prefsKey))
        {
            PlayerPrefs.SetFloat(_prefsKey, value);
            PlayerPrefs.Save();
        }

        _slider.SetValueWithoutNotify(value);
        _slider.onValueChanged.AddListener(OnSliderChanged);

        Apply(value);
    }

    private void OnDisable()
    {
        if (_slider != null)
            _slider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        value = Mathf.Clamp01(value);

        Apply(value);

        PlayerPrefs.SetFloat(_prefsKey, value);
        PlayerPrefs.Save();
    }

    private void Apply(float value)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetVolume(_mixerKey, value);
    }
}
