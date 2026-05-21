using UnityEngine;

public class SettingsPopup : MonoBehaviour
{
    [SerializeField] private SettingRow _soundSlider; // SFX
    [SerializeField] private SettingRow _musicSlider; // Music

    private const string MIX_SFX = "SfxVol";
    private const string MIX_MUSIC = "MusicVol";

    private const string PREF_SFX = "vol_sfx";
    private const string PREF_MUSIC = "vol_music";

    private void Awake()
    {
        _soundSlider.Bind(MIX_SFX, PREF_SFX, 1f);
        _musicSlider.Bind(MIX_MUSIC, PREF_MUSIC, 1f);
    }



    public void OnClose()
    {
        Destroy(gameObject);
    }
}
