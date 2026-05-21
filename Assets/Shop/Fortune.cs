using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using Firebase.Analytics;

public class Fortune : Popup
{
    private enum WheelState
    {
        Cooldown,
        ReadyToSpin,
        Spinning,
        ReadyToClaim
    }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _actionTextField;

    [Header("Cooldown UI")]
    [SerializeField] private GameObject _actionButtonRoot;            // root кнопки Spin/Skip/Claim
    [SerializeField] private TextMeshProUGUI _cooldownText;      // текст таймера
    [SerializeField] private TextMeshProUGUI _cooldownTextField;      // текст таймера

    [SerializeField] private Button _actionButton;
    // [SerializeField] private GameObject _spinForAdButton;

    [SerializeField] private GameObject _adSpinButtonRoot;            // Rewardx2

    [Header("Wheel")]
    [SerializeField] private Transform _wheelTransform;
    [SerializeField] private List<Sector> _sectors = new List<Sector>();

    [Header("Cover highlight")]
    [SerializeField] private float _coverMinAlpha = 0.75f;
    [SerializeField] private float _coverMaxAlpha = 1f;
    [SerializeField] private float _coverPingPongSpeed = 3f;
    [SerializeField] private float _coverHighlightDuration = 2.5f;

    [Header("Spin tuning")]
    [SerializeField] private float _minStartSpeed = 600f;
    [SerializeField] private float _maxStartSpeed = 1500f;
    [SerializeField] private float _deceleration = 200f;

    private WheelState _state = WheelState.ReadyToSpin;

    private bool _isSkipping;
    private float _currentSpeed;
    private float _angle;

    private int _reward;

    private Coroutine _spinRoutine;
    private Coroutine _coverRoutine;
    private Coroutine _cooldownRoutine;

    private int _spinsCompleted;
    private bool _extraSpinUsed;
    private int _rewardedAdsWatchedThisSession = 0;
    private bool _autoClaimThisSpin = false;


    [Header("Reward FX")]
    [SerializeField] private RectTransform _rewardFxRoot;
    [SerializeField] private CanvasGroup _rewardFxCanvas;
    [SerializeField] private TextMeshProUGUI _rewardFxText;

    [SerializeField] private float _rewardFxMoveUp = 90f;
    [SerializeField] private float _rewardFxFadeIn = 0.15f;
    [SerializeField] private float _rewardFxHold = 0.45f;
    [SerializeField] private float _rewardFxFadeOut = 0.25f;
    [SerializeField] private AnimationCurve _rewardFxMoveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine _rewardFxRoutine;
    private Vector2 _rewardFxStartPos;


    [Header("Spin SFX")]
    [SerializeField] private AudioSource _spinSfxSource;   // отдельный AudioSource (лучше на этом же объекте/дочернем)
    [SerializeField] private AudioClip _spinLoopClip;      // loop клип "кручение"
    [SerializeField, Range(0f, 1f)] private float _spinSfxVolume = 0.5f;
    [SerializeField] private bool _spinPitchBySpeed = true;
    [SerializeField] private Vector2 _spinPitchRange = new Vector2(0.9f, 1.15f);


    // ===== ANALYTICS =====
    private string _closeReason = "unknown";
    private int _currentSpinNumber = 0;
    private string _nextSpinSource = "free";

    private static int B(bool v) => v ? 1 : 0;

    private void LogEvent(string eventName, params Parameter[] parameters)
    {
        AnalyticsSafe.LogEvent(eventName, parameters);
    }

    // Парсим "HH:MM:SS" -> секунды. Если не получилось — 0.
    private int GetCooldownRemainingSec()
    {
        var cm = FortuneCooldownManager.Instance;
        if (cm == null) return 0;

        string txt = cm.GetRemainingText();
        if (string.IsNullOrEmpty(txt)) return 0;

        var parts = txt.Split(':');
        if (parts.Length != 3) return 0;

        if (int.TryParse(parts[0], out int h) &&
            int.TryParse(parts[1], out int m) &&
            int.TryParse(parts[2], out int s))
        {
            return h * 3600 + m * 60 + s;
        }

        return 0;
    }
    // ===== /ANALYTICS =====


#if UNITY_EDITOR
    private void OnValidate()
    {
        SyncSectorTexts();
    }
#endif


    private void OnEnable()
    {
        SyncSectorTexts();

        ResetWheelVisual();
        _reward = 0;
        _spinsCompleted = 0;
        _extraSpinUsed = false;
        _rewardedAdsWatchedThisSession = 0;
        _autoClaimThisSpin = false;

        SetAdButtonVisible(false);

        if (FortuneCooldownManager.Instance != null && FortuneCooldownManager.Instance.IsOnCooldown())
        {
            EnterCooldownMode();
            SetState(WheelState.Cooldown);
        }
        else
        {
            ExitCooldownMode();
            SetState(WheelState.ReadyToSpin);
        }

        InitRewardFx();

        _closeReason = "unknown";

        bool cooldownActive = FortuneCooldownManager.Instance != null && FortuneCooldownManager.Instance.IsOnCooldown();
        string state = cooldownActive ? "cooldown" : "ready";
        int rem = cooldownActive ? GetCooldownRemainingSec() : 0;

        LogEvent("fortune_wheel_screen_open",
            new Parameter("state", state),
            new Parameter("cooldown_active", B(cooldownActive)),
            new Parameter("cooldown_remaining_sec", rem)
        );

    }

    private void OnDisable()
    {
        if (_rewardedAdsWatchedThisSession > 0 || _spinsCompleted > 0)
            StartCooldownIfNeeded("on_close");

        if (_cooldownRoutine != null) StopCoroutine(_cooldownRoutine);
        _cooldownRoutine = null;

        int unclaimed = (_state == WheelState.ReadyToClaim && _reward > 0) ? _reward : 0;

        LogEvent("fortune_wheel_screen_close",
            new Parameter("close_reason", _closeReason),
            new Parameter("spins_completed", _spinsCompleted),
            new Parameter("ads_watched", _rewardedAdsWatchedThisSession),
            new Parameter("unclaimed_reward", unclaimed)
        );

        StopSpinSfx();
    }

    public void OnActionButtonClick()
    {
        if (_state == WheelState.ReadyToSpin &&
            FortuneCooldownManager.Instance != null &&
            FortuneCooldownManager.Instance.IsOnCooldown())
        {
            EnterCooldownMode();
            SetState(WheelState.Cooldown);
            return;
        }

        if (_state == WheelState.Cooldown)
            return;

        switch (_state)
        {
            case WheelState.ReadyToSpin:
                Spin();
                break;

            case WheelState.Spinning:
                if (!_isSkipping)
                {
                    _isSkipping = true;
                    _currentSpeed = 0;
                    LogEvent("fortune_wheel_spin_skip",
                        new Parameter("spin_number", _currentSpinNumber)
                    );
                }
                break;

            case WheelState.ReadyToClaim:
                ClaimAndClose();
                LogEvent("fortune_wheel_claim",
                    new Parameter("spin_number", 1),
                    new Parameter("reward", _reward)
                );

                _closeReason = "claim";
                break;
        }
    }

    public void OnAdSpinButtonClick()
    {
        if (_state != WheelState.ReadyToClaim) return;
        if (_spinsCompleted != 1) return;   // только после первого бесплатного спина
        if (_extraSpinUsed) return;

        LogEvent("fortune_wheel_rewardx2_ad_click",
            new Parameter("ad_purpose", "second_spin")
        );

        // следующий спин будет из этого источника
        _nextSpinSource = "ad_second_spin";


        // Забираем награду за первый спин
        GiveRewardToPlayer("pre_ad_claim", 1);

        _extraSpinUsed = true;
        SetAdButtonVisible(false);

        // Кулдаун должен начаться СЕЙЧАС (попап не закрываем)
        StartCooldownIfNeeded("after_rewardx2");

        ShowRewardedAd("second_spin", () =>
        {
            _rewardedAdsWatchedThisSession++;

            ResetWheelForNextSpin();
            SetState(WheelState.ReadyToSpin);

            // Второй спин: игнор кулдауна + авто-клейм в конце
            StartSpinInternal(ignoreCooldown: true, autoClaim: true);
        });
    }

    // Кнопка Spin for ad в режиме кулдауна: реклама -> спин -> авто-клейм -> обратно в кулдаун UI
    public void OnSpinForAdButtonClick()
    {
        if (FortuneCooldownManager.Instance == null || !FortuneCooldownManager.Instance.IsOnCooldown())
            return;

        if (_state == WheelState.Spinning) return;

        LogEvent("fortune_wheel_spin_for_ad_click",
            new Parameter("ad_purpose", "cooldown_spin"),
            new Parameter("cooldown_remaining_sec", GetCooldownRemainingSec())
        );

        _nextSpinSource = "ad_cooldown";


        ShowRewardedAd("cooldown_spin", () =>
        {
            _rewardedAdsWatchedThisSession++;

            ResetWheelForNextSpin();
            SetState(WheelState.ReadyToSpin);

            StartSpinInternal(ignoreCooldown: true, autoClaim: true);
        });
    }

    public void Spin()
    {
        StartSpinInternal(ignoreCooldown: false, autoClaim: false);
    }

    private void StartSpinInternal(bool ignoreCooldown, bool autoClaim)
    {
        if (_state != WheelState.ReadyToSpin && _state != WheelState.Cooldown) return;

        _autoClaimThisSpin = autoClaim;

        if (!ignoreCooldown &&
            FortuneCooldownManager.Instance != null &&
            FortuneCooldownManager.Instance.IsOnCooldown())
        {
            EnterCooldownMode();
            SetState(WheelState.Cooldown);
            return;
        }

        // На время спина прячем кулдаун UI
        ExitCooldownMode();
        _currentSpinNumber = _spinsCompleted + 1;

        string spinSource = _nextSpinSource;
        if (string.IsNullOrEmpty(spinSource))
            spinSource = (ignoreCooldown ? "ad_cooldown" : "free");

        LogEvent("fortune_wheel_spin_started",
            new Parameter("spin_number", _currentSpinNumber),
            new Parameter("spin_source", spinSource)
        );


        ResetCovers();
        if (_spinRoutine != null) StopCoroutine(_spinRoutine);
        _spinRoutine = StartCoroutine(SpinCoroutine());
    }

    private IEnumerator SpinCoroutine()
    {
        _actionButton.gameObject.SetActive(false);
        SetState(WheelState.Spinning);
        _isSkipping = false;

        float startSpeed = UnityEngine.Random.Range(_minStartSpeed, _maxStartSpeed);
        _currentSpeed = startSpeed;

        StartSpinSfx(startSpeed);

        while (_currentSpeed > 0f)
        {
            _angle += _currentSpeed * Time.deltaTime;
            _wheelTransform.eulerAngles = new Vector3(0, 0, _angle);

            _currentSpeed -= _deceleration * Time.deltaTime;

            if (_spinSfxSource != null && _spinSfxSource.isPlaying && _spinPitchBySpeed)
            {
                float t = Mathf.InverseLerp(0f, startSpeed, _currentSpeed);
                _spinSfxSource.pitch = Mathf.Lerp(_spinPitchRange.x, _spinPitchRange.y, t);
            }

            yield return null;
        }

        float finalAngle = _angle % 360f;

        if (_isSkipping)
        {
            finalAngle = UnityEngine.Random.Range(0f, 360f);
            _wheelTransform.eulerAngles = new Vector3(0, 0, finalAngle);
        }

        _angle = finalAngle;

        Sector sector = GetSectorByAngle(finalAngle);
        _reward = sector.Value;
        _spinsCompleted++;

        int sectorIndex = _sectors != null ? _sectors.IndexOf(sector) : -1;

        LogEvent("fortune_wheel_spin_result",
            new Parameter("spin_number", _currentSpinNumber),
            new Parameter("reward", _reward),
            new Parameter("sector_index", sectorIndex)
        );

        HighlightSectorCover(sector);

        // === ВАЖНОЕ РАЗВЕТВЛЕНИЕ ===
        if (_autoClaimThisSpin)
        {
            // После ad-спинов: НЕ показываем Claim, выдаём сразу
            GiveRewardToPlayer("auto_claim", _currentSpinNumber);

            // И показываем только таймер + Spin for ad до конца кулдауна
            EnterCooldownMode();
            SetState(WheelState.Cooldown);

            // На всякий случай прячем кнопку
            _actionButton.gameObject.SetActive(false);
        }
        else
        {
            // После бесплатного спина: показываем Claim + Rewardx2
            SetState(WheelState.ReadyToClaim);

            bool canShowAd = (_spinsCompleted == 1 && !_extraSpinUsed);
            SetAdButtonVisible(canShowAd);

            _actionButton.gameObject.SetActive(true);
        }

        Debug.Log($"🎯 Angle {finalAngle:F1}°, reward: {sector.Value}, spins: {_spinsCompleted}");
        StopSpinSfx();
    }

    private Sector GetSectorByAngle(float angle)
    {
        float sectorAngle = 360f / _sectors.Count;
        int index = Mathf.FloorToInt(angle / sectorAngle);
        index = _sectors.Count - 1 - index;
        return _sectors[Mathf.Clamp(index, 0, _sectors.Count - 1)];
    }

    // Claim только для бесплатного спина: выдаём и закрываем попап + запускаем кулдаун
    public void ClaimAndClose()
    {
        GiveRewardToPlayer("claim", 1);
        if (_spinsCompleted > 0 || _rewardedAdsWatchedThisSession > 0)
            StartCooldownIfNeeded("after_claim");

        _closeReason = "claim";
        Destroy(gameObject);
    }


    private void GiveRewardToPlayer(string grantReason, int spinNumber)
    {
        if (_reward <= 0) return;

        if (_coverRoutine != null) StopCoroutine(_coverRoutine);

        int amount = _reward;
        _reward = 0;

        FindFirstObjectByType<BalanceManager>().AddWinnings(amount, "WheelOfFortune");

        LogEvent("fortune_wheel_reward_granted",
            new Parameter("spin_number", spinNumber),
            new Parameter("amount", amount),
            new Parameter("grant_reason", grantReason)
        );

        PlayRewardFx(amount);
    }


    private void ScheduleWheelReadyPushFromNow()
    {
        var cm = FortuneCooldownManager.Instance;
        if (cm == null) return;
        if (GameLocalNotifications.Instance == null) return;

        int hours = cm.CooldownHours;
        if (hours <= 0) return;

        GameLocalNotifications.Instance.ScheduleAfter(
            GameNotificationType.WheelReady,
            TimeSpan.FromHours(hours),
            replace: true
        );
    }


    private void SetState(WheelState newState)
    {
        _state = newState;

        switch (_state)
        {
            case WheelState.Cooldown:
                SetAdButtonVisible(false);
                UpdateCooldownText();
                if (_actionTextField != null) _actionTextField.text = "";
                break;

            case WheelState.ReadyToSpin:
                if (_actionTextField != null) _actionTextField.text = "Spin";
                break;

            case WheelState.Spinning:
                if (_actionTextField != null) _actionTextField.text = "Tap to skip";
                break;

            case WheelState.ReadyToClaim:
                if (_actionTextField != null) _actionTextField.text = "Claim";
                break;
        }
    }

    private void EnterCooldownMode()
    {
        if (_actionButtonRoot != null) _actionButtonRoot.SetActive(false);
        SetAdButtonVisible(false);

        // if (_spinForAdButton != null) _spinForAdButton.SetActive(true);
        if (_cooldownText != null) _cooldownText.gameObject.SetActive(true);
        if (_cooldownTextField != null) _cooldownTextField.gameObject.SetActive(true);

        StartCooldownTicker();
    }

    private void ExitCooldownMode()
    {
        if (_cooldownRoutine != null) StopCoroutine(_cooldownRoutine);
        _cooldownRoutine = null;

        if (_cooldownTextField != null) _cooldownTextField.gameObject.SetActive(false);
        if (_cooldownText != null) _cooldownText.gameObject.SetActive(false);

        if (_actionButtonRoot != null) _actionButtonRoot.SetActive(true);
    }

    private void StartCooldownTicker()
    {
        if (_cooldownRoutine != null) StopCoroutine(_cooldownRoutine);
        _cooldownRoutine = StartCoroutine(CooldownTick());
    }

    private IEnumerator CooldownTick()
    {
        while (FortuneCooldownManager.Instance != null && FortuneCooldownManager.Instance.IsOnCooldown())
        {
            UpdateCooldownText();
            yield return new WaitForSeconds(1f);
        }

        ExitCooldownMode();
        SetState(WheelState.ReadyToSpin);
    }

    private void UpdateCooldownText()
    {
        if (_cooldownTextField == null) return;

        var cm = FortuneCooldownManager.Instance;
        if (cm == null)
        {
            _cooldownTextField.text = "00:00:00";
            return;
        }

        _cooldownTextField.text = cm.GetRemainingText();
    }

    private void StartCooldownIfNeeded(string reason)
    {
        var cm = FortuneCooldownManager.Instance;
        if (cm == null) return;

        if (!cm.IsOnCooldown())
        {
            cm.StartCooldown();

            ScheduleWheelReadyPushFromNow();

            LogEvent("fortune_wheel_cooldown_started",
                new Parameter("reason", reason)
            );
        }
    }


    private void SetAdButtonVisible(bool visible)
    {
        if (_adSpinButtonRoot != null)
            _adSpinButtonRoot.SetActive(visible);
    }

    private void ResetWheelForNextSpin()
    {
        _reward = 0;
        ResetWheelVisual();
    }

    private void ResetWheelVisual()
    {
        ResetCovers();
        _isSkipping = false;
        _currentSpeed = 0f;
        _angle = 0f;
        if (_wheelTransform != null)
            _wheelTransform.eulerAngles = Vector3.zero;
    }

    private void ResetCovers()
    {
        if (_coverRoutine != null) StopCoroutine(_coverRoutine);
        _coverRoutine = null;

        foreach (var s in _sectors)
        {
            if (s != null && s.Cover != null)
            {
                s.Cover.gameObject.SetActive(false);
                SetImageAlpha(s.Cover, _coverMaxAlpha);
            }
        }
    }

    private void HighlightSectorCover(Sector sector)
    {
        ResetCovers();

        if (sector == null || sector.Cover == null) return;

        sector.Cover.gameObject.SetActive(true);
        _coverRoutine = StartCoroutine(CoverPingPongAlpha(sector.Cover, _coverHighlightDuration));
    }

    private IEnumerator CoverPingPongAlpha(Image img, float duration)
    {
        float t = 0f;
        SetImageAlpha(img, _coverMinAlpha);

        while (t < duration)
        {
            t += Time.deltaTime;
            float ping = Mathf.PingPong(Time.time * _coverPingPongSpeed, 1f);
            float a = Mathf.Lerp(_coverMinAlpha, _coverMaxAlpha, ping);
            SetImageAlpha(img, a);
            yield return null;
        }

        SetImageAlpha(img, _coverMaxAlpha);
    }

    private void SetImageAlpha(Image img, float alpha)
    {
        var c = img.color;
        c.a = Mathf.Clamp01(alpha);
        img.color = c;
    }

    private void ShowRewardedAd(string adPurpose, Action onFinished)
    {
        AdManager adManager = FindFirstObjectByType<AdManager>();
        if (adManager == null) return;

        adManager.ShowRewardedAd(() =>
        {
            LogEvent("fortune_wheel_rewarded_ad_success",
                new Parameter("ad_purpose", adPurpose)
            );

            onFinished?.Invoke();
        });
    }


    private void InitRewardFx()
    {
        if (_rewardFxRoot == null) return;

        if (_rewardFxCanvas == null)
            _rewardFxCanvas = _rewardFxRoot.GetComponent<CanvasGroup>();

        if (_rewardFxCanvas == null)
            _rewardFxCanvas = _rewardFxRoot.gameObject.AddComponent<CanvasGroup>();

        _rewardFxStartPos = _rewardFxRoot.anchoredPosition;

        _rewardFxCanvas.alpha = 0f;
        _rewardFxRoot.gameObject.SetActive(false);
    }

    private void PlayRewardFx(int amount)
    {
        if (_rewardFxRoot == null || _rewardFxCanvas == null || _rewardFxText == null) return;

        if (_rewardFxRoutine != null) StopCoroutine(_rewardFxRoutine);
        _rewardFxRoutine = StartCoroutine(RewardFxCoroutine(amount));
    }

    private IEnumerator RewardFxCoroutine(int amount)
    {
        _rewardFxRoot.gameObject.SetActive(true);
        _rewardFxRoot.anchoredPosition = _rewardFxStartPos;

        _rewardFxText.text = $"+{amount:N0}";
        _rewardFxCanvas.alpha = 0f;

        float total = _rewardFxFadeIn + _rewardFxHold + _rewardFxFadeOut;
        float t = 0f;

        while (t < total)
        {
            t += Time.deltaTime;

            // Move up
            float p = Mathf.Clamp01(t / total);
            float y = _rewardFxMoveCurve.Evaluate(p) * _rewardFxMoveUp;
            _rewardFxRoot.anchoredPosition = _rewardFxStartPos + Vector2.up * y;

            // Alpha
            float a;
            if (t <= _rewardFxFadeIn)
                a = Mathf.Clamp01(t / _rewardFxFadeIn);
            else if (t <= _rewardFxFadeIn + _rewardFxHold)
                a = 1f;
            else
                a = 1f - Mathf.Clamp01((t - _rewardFxFadeIn - _rewardFxHold) / _rewardFxFadeOut);

            _rewardFxCanvas.alpha = a;

            yield return null;
        }

        _rewardFxCanvas.alpha = 0f;
        _rewardFxRoot.anchoredPosition = _rewardFxStartPos;
        _rewardFxRoot.gameObject.SetActive(false);
        _rewardFxRoutine = null;
    }



    private void SyncSectorTexts()
    {
        if (_sectors == null) return;

        foreach (var s in _sectors)
        {
            if (s == null) continue;

            if (s._valueText != null)
                s._valueText.text = s.Value.ToString();
        }
    }



    private void StartSpinSfx(float startSpeed)
    {
        if (_spinSfxSource == null || _spinLoopClip == null) return;

        _spinSfxSource.playOnAwake = false;
        _spinSfxSource.loop = true;
        _spinSfxSource.clip = _spinLoopClip;
        _spinSfxSource.volume = _spinSfxVolume;

        if (_spinPitchBySpeed)
            _spinSfxSource.pitch = _spinPitchRange.y; // на старте обычно быстрее

        if (!_spinSfxSource.isPlaying)
            _spinSfxSource.Play();
    }

    private void StopSpinSfx()
    {
        if (_spinSfxSource == null) return;
        if (_spinSfxSource.isPlaying)
            _spinSfxSource.Stop();
    }

}

[Serializable]
public class Sector
{
    public int Value;
    public TextMeshProUGUI _valueText;
    public Image Cover;
}
