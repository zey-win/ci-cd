using UnityEngine;
using UnityEngine.UI;

namespace Audio
{
    public class VolumeSlider : MonoBehaviour
    {
        public string volumeKey;
        private Slider _slider;

        private void Awake()
        {
            _slider = GetComponent<Slider>();
            _slider.onValueChanged.AddListener(SetVolume);
        }

        private void SetVolume(float volume)
        {
            AudioManager.Instance.SetVolume(volumeKey, volume);
        }
    }
}