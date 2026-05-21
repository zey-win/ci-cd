using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Audio
{
    // Чтобы стартовал позже большинства скриптов и не проигрывал в гонке инициализации
    [DefaultExecutionOrder(10000)]
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioMixer masterMixer;

        [SerializeField] private string sfxKey = "SfxVol";
        [SerializeField] private string musicKey = "MusicVol";

        private const string PREF_SFX = "vol_sfx";
        private const string PREF_MUSIC = "vol_music";

        public static AudioManager Instance { get; private set; }

        private Coroutine _enforceRoutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                ApplySavedVolumes();
                EnforceSavedVolumesForFrames(10); // <-- ключевой момент
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            // На всякий случай ещё раз “дожмём”
            EnforceSavedVolumesForFrames(10);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnforceSavedVolumesForFrames(10);
        }

        private void EnforceSavedVolumesForFrames(int frames)
        {
            if (_enforceRoutine != null) StopCoroutine(_enforceRoutine);
            _enforceRoutine = StartCoroutine(EnforceRoutine(frames));
        }

        private IEnumerator EnforceRoutine(int frames)
        {
            // несколько кадров подряд применяем — переживает snapshot transition / чужие Start()
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                ApplySavedVolumes();
            }
        }

        private void ApplySavedVolumes()
        {
            if (!masterMixer)
            {
                Debug.LogError("AudioManager: masterMixer не назначен!");
                return;
            }

            float sfx = PlayerPrefs.GetFloat(PREF_SFX, 1f);
            float music = PlayerPrefs.GetFloat(PREF_MUSIC, 1f);

            SetVolume(sfxKey, sfx);
            SetVolume(musicKey, music);
        }

        public void SetVolume(string key, float value) => SetVolumeLogarithmic(key, value);

        private void SetVolumeLogarithmic(string key, float linearVolume)
        {
            var dB = Mathf.Log10(Mathf.Clamp(linearVolume, 0.0001f, 1f)) * 20f;

            if (!masterMixer.SetFloat(key, dB))
                Debug.LogError($"AudioManager: параметр '{key}' не найден или не Exposed в AudioMixer!");
        }
    }
}
