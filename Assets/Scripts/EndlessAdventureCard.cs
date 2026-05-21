using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndlessAdventureCard : MonoBehaviour
{
    public int Reward => _reward;
    public bool IsFree => _isFree;

    public RectTransform RectTransform => (RectTransform)transform;
    public CanvasGroup CanvasGroup => _canvasGroup;

    [SerializeField] private int _reward;
    [SerializeField] private bool _isFree;

    [SerializeField] private TextMeshProUGUI _rewardText;

    [Header("Lock (Animator)")]
    [SerializeField] private GameObject _lock;
    [SerializeField] private Animator _lockAnimator;
    private string _unlockTriggerName = "Unlock";
    private string _unlockStateName = "Unlock";
    private string _lockedStateName = "Locked";

    [SerializeField] private bool _hideLockAfterUnlock = true;

    [Header("Optional UI")]
    [SerializeField] private Button _freeButton;
    [SerializeField] private Button _adButton;

    [SerializeField] private Sprite _selectedBackground;
    [SerializeField] private Sprite _baseBackground;
    [SerializeField] private Image _background;

    [Header("Fade")]
    [SerializeField] private CanvasGroup _canvasGroup;

    private bool _isLocked = true;

    private Coroutine _unlockRoutine;
    private Coroutine _unlockEnableRoutine;
    private int _unlockTriggerHash;

    private void Awake()
    {
        if (_lockAnimator == null && _lock != null)
            _lockAnimator = _lock.GetComponentInChildren<Animator>(true);

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        _unlockTriggerHash = Animator.StringToHash(_unlockTriggerName);

        SetReward(_reward);
    }

    public void Init(Action<EndlessAdventureCard> onClick)
    {
        if (_isFree)
        {
            _freeButton.gameObject.SetActive(true);
            _freeButton.onClick.AddListener(() => onClick?.Invoke(this));
        }
        else
        {
            _adButton.gameObject.SetActive(true);
            _adButton.onClick.AddListener(() => onClick?.Invoke(this));
        }
    }


    void OnDisable()
    {
        _freeButton.onClick.RemoveAllListeners();
        _adButton.onClick.RemoveAllListeners();
    }

    private void SetReward(int reward)
    {
        if (_rewardText != null)
            _rewardText.text = reward.ToString();
    }

    public void SetAlpha(float a)
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = a;
    }

    public void SetInteractable(bool interactable)
    {
        _freeButton.interactable = interactable;
        _adButton.interactable = interactable;
    }

    public void SetLocked(bool locked)
    {
        _isLocked = locked;

        _freeButton.interactable = !locked;
        _adButton.interactable = !locked;

        if (_lock != null)
        {
            _lock.SetActive(locked);

            if (locked)
            {
                // ВАЖНО: когда возвращаем замок — сбрасываем анимацию/прозрачность
                ResetLockVisualToLocked();
            }
        }
    }


    public void SetSelected(bool selected)
    {
        if (_background != null && _selectedBackground != null && _baseBackground != null)
            _background.sprite = selected ? _selectedBackground : _baseBackground;
    }

    /// <summary>
    /// Делает карту текущей: показывает замок, проигрывает unlock и только потом включает кнопку.
    /// </summary>
    public void UnlockWithAnimation()
    {
        _isLocked = false;

        _freeButton.interactable = false;
        _adButton.interactable = false;

        // Замок должен быть виден, чтобы было что "открывать"
        if (_lock != null)
            _lock.SetActive(true);

        PlayUnlockFromAnyState();

        if (_unlockEnableRoutine != null)
            StopCoroutine(_unlockEnableRoutine);

        _unlockEnableRoutine = StartCoroutine(EnableAfterUnlockRoutine());
    }

    private IEnumerator EnableAfterUnlockRoutine()
    {
        // Ждём пока закончится unlockRoutine (или пока замок не выключится)
        while (_unlockRoutine != null)
            yield return null;

        // Если в итоге замок прячется, значит unlock прошёл
        if (_lock != null && _hideLockAfterUnlock)
            _lock.SetActive(false);

        _freeButton.interactable = true;
        _adButton.interactable = true;

        _unlockEnableRoutine = null;
    }

    /// <summary>
    /// Запускает анимацию через Any State -> Unlock (Trigger).
    /// </summary>
    public void PlayUnlockFromAnyState()
    {
        if (_unlockRoutine != null)
            StopCoroutine(_unlockRoutine);

        _unlockRoutine = StartCoroutine(UnlockRoutine());
    }

    private IEnumerator UnlockRoutine()
    {
        if (_lock == null)
            yield break;

        if (!_lock.activeSelf)
            _lock.SetActive(true);

        if (_lockAnimator == null || _lockAnimator.runtimeAnimatorController == null)
        {
            if (_hideLockAfterUnlock)
                _lock.SetActive(false);

            _unlockRoutine = null;
            yield break;
        }

        _lockAnimator.ResetTrigger(_unlockTriggerHash);
        _lockAnimator.SetTrigger(_unlockTriggerHash);

        float enterTimeout = 1.0f;
        float t = 0f;

        while (t < enterTimeout && !IsInUnlockState())
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (!IsInUnlockState())
        {
            if (_hideLockAfterUnlock)
                _lock.SetActive(false);

            _unlockRoutine = null;
            yield break;
        }

        float finishTimeout = 5f;
        t = 0f;

        while (t < finishTimeout)
        {
            t += Time.deltaTime;

            var st = _lockAnimator.GetCurrentAnimatorStateInfo(0);
            if (IsUnlockStateInfo(st) && st.normalizedTime >= 1f && !_lockAnimator.IsInTransition(0))
                break;

            yield return null;
        }

        if (_hideLockAfterUnlock)
            _lock.SetActive(false);

        _unlockRoutine = null;
    }

    private bool IsInUnlockState()
    {
        var st = _lockAnimator.GetCurrentAnimatorStateInfo(0);
        return IsUnlockStateInfo(st);
    }

    private bool IsUnlockStateInfo(AnimatorStateInfo st)
    {
        return st.IsName(_unlockStateName) || st.IsName("Base Layer." + _unlockStateName);
    }

    private void ResetLockVisualToLocked()
    {
        if (_lock == null) return;

        _lockAnimator.ResetTrigger(0);
    }

}
