using UnityEngine;
using UnityEngine.UI;

namespace Audio
{
    public class VolumeToggle : MonoBehaviour
    {
        public string volumeKey;
        private Toggle _toggle;

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
            _toggle.onValueChanged.AddListener(ToggleVolume);
        }

        private void ToggleVolume(bool isMuted)
        {
            // AudioManager.Instance.ToggleVolume(volumeKey, !isMuted);
        }
    }
}