using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CollisionScaleAnimation : MonoBehaviour, ICollisionAnimation
{
    public float scaleFactor = .9f;

    private Vector3 _startScale;
    private SpriteRenderer _visual;
    private Sequence _sq;

    [SerializeField] private ParticleSystem _particaleSystem;

    [Header("Audio limit (global)")]
    [SerializeField] private int maxSimultaneousHitSounds = 10;
    [SerializeField] private AudioClip hitClipOverride;   // если null — возьмем AudioSource.clip
    [SerializeField] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    private AudioSource _templateSource;

    // ---- Global pool ----
    private static AudioSource[] s_pool;
    private static Transform s_poolRoot;
    private static int s_poolSize;
    private static int s_nextIdx;

    // чтобы один и тот же пег не пытался проиграть 2 раза в один кадр
    private int _lastSfxFrame = -1;

    // ВАЖНО: сброс static даже при выключенном Domain Reload
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_pool = null;
        s_poolRoot = null;
        s_poolSize = 0;
        s_nextIdx = 0;
    }

    private void Awake()
    {
        _visual = GetComponentInChildren<SpriteRenderer>();
        if (_visual != null)
            _startScale = _visual.transform.localScale;

        _templateSource = GetComponent<AudioSource>();
        _templateSource.playOnAwake = false;
        _templateSource.loop = false;
    }

    public void PlayAnimation()
    {
        if (_visual == null) return;

        _sq?.Kill();
        _sq = DOTween.Sequence();
        _sq
            .Append(_visual.transform.DOScale(_startScale, 0))
            .Join(_visual.DOFade(.6f, 0))
            .Append(_visual.transform.DOScale(_startScale * scaleFactor, .2f).SetEase(Ease.OutElastic))
            .Append(_visual.transform.DOScale(_startScale, .1f).SetEase(Ease.OutElastic))
            .Join(_visual.DOFade(1f, .6f));
    }

    private void OnCollisionEnter2D(Collision2D c)
    {
        TryPlayHitLimited();
        PlayAnimation();
    }

    private void TryPlayHitLimited()
    {
        // объект/компонент уже уничтожается — не трогаем
        if (!this || _templateSource == null) return;

        // защита от дублей в один кадр
        if (_lastSfxFrame == Time.frameCount) return;
        _lastSfxFrame = Time.frameCount;

        var clip = hitClipOverride != null ? hitClipOverride : _templateSource.clip;
        if (clip == null) return;

        EnsurePoolSafe(_templateSource, Mathf.Max(1, maxSimultaneousHitSounds));

        if (s_pool == null || s_poolSize <= 0) return;

        // ищем свободный голос
        AudioSource free = null;
        for (int i = 0; i < s_poolSize; i++)
        {
            var src = s_pool[i];
            if (src == null) continue; // на всякий случай
            if (!src.isPlaying)
            {
                free = src;
                break;
            }
        }

        // если все заняты — НЕ молчим, а перезапускаем голос по кругу
        if (free == null)
        {
            free = s_pool[s_nextIdx];
            s_nextIdx = (s_nextIdx + 1) % s_poolSize;
            if (free == null) return;
            free.Stop();
        }

        free.pitch = Random.Range(pitchRange.x, pitchRange.y);
        free.PlayOneShot(clip, volume);

        if (_particaleSystem != null)
            _particaleSystem.Play();
    }

    private static void EnsurePoolSafe(AudioSource template, int wantedSize)
    {
        // template мог быть уничтожен/отключен
        if (template == null) return;

        bool needRebuild = false;

        // если корень уничтожен или отсутствует
        if (s_poolRoot == null) needRebuild = true;

        // если пула нет/размер не тот
        if (s_pool == null || s_poolSize != wantedSize) needRebuild = true;

        // если в пуле есть уничтоженные источники (MissingReference = null по Unity-логике)
        if (!needRebuild && s_pool != null)
        {
            for (int i = 0; i < s_pool.Length; i++)
            {
                if (s_pool[i] == null)
                {
                    needRebuild = true;
                    break;
                }
            }
        }

        if (!needRebuild) return;

        s_poolSize = wantedSize;

        if (s_poolRoot == null)
        {
            var go = new GameObject("HitSfxPool");
            Object.DontDestroyOnLoad(go);
            s_poolRoot = go.transform;
        }
        else
        {
            // чистим старые
            for (int i = s_poolRoot.childCount - 1; i >= 0; i--)
                Object.Destroy(s_poolRoot.GetChild(i).gameObject);
        }

        s_pool = new AudioSource[s_poolSize];

        for (int i = 0; i < s_poolSize; i++)
        {
            var child = new GameObject($"HitVoice_{i}");
            child.transform.SetParent(s_poolRoot);

            var src = child.AddComponent<AudioSource>();

            // копируем ключевые настройки из “шаблона” на пеге
            src.outputAudioMixerGroup = template.outputAudioMixerGroup;
            src.spatialBlend = template.spatialBlend;
            src.rolloffMode = template.rolloffMode;
            src.minDistance = template.minDistance;
            src.maxDistance = template.maxDistance;
            src.dopplerLevel = template.dopplerLevel;

            src.playOnAwake = false;
            src.loop = false;

            s_pool[i] = src;
        }

        s_nextIdx = 0;
    }
}