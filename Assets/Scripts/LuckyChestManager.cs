using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using Firebase;
using Firebase.RemoteConfig;

public class LuckyChestManager : MonoBehaviour
{
    public float Score => _score;

    [Header("Score")]
    [Range(0, 200)]
    [SerializeField] private float _score = 0;

    [Header("UI")]
    [SerializeField] private Image _progressBar;
    [SerializeField] private LuckyChestPopup _popupRoot;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private GameObject _lighting;
    [SerializeField] private CanvasGroup _tooltipPopup;
    [SerializeField] private TextMeshProUGUI _tooltipText;
    [SerializeField] private TextMeshProUGUI _progressPercentField;


    [Header("Tooltip Animation")]
    [SerializeField] private float _fadeDuration = 0.25f;
    [SerializeField] private float _showDuration = 3f;

    [Header("Rewards (used for tooltip + passed into popup)")]
    [SerializeField] private int _rewardForFirstAd = 1000;
    [SerializeField] private int _rewardMultiplierSecondAd = 2;

    private Coroutine _tooltipRoutine;

    private const float MinScore = 0f;
    private const float MaxScore = 200f;

    private const string PrefKeyScore = "LuckyChest_Score";

    // Remote Config
    private const string LuckyRewardFirstKey = "lucky_chest_reward";
    private bool _rcReady;
    private bool _rcInitRunning;

    private void Awake()
    {
        LoadScore();
        ClampScore();
        UpdateUI();

        if (_tooltipPopup != null)
        {
            _tooltipPopup.alpha = 0f;
            _tooltipPopup.interactable = false;
            _tooltipPopup.blocksRaycasts = false;
        }
    }

    private IEnumerator Start()
    {
        yield return RemoteConfigManager.EnsureReadyCoroutine();
        _rewardForFirstAd = RemoteConfigManager.LuckyChestReward;
    }


    public void AddScore(float amount)
    {
        _score = Mathf.Clamp(_score + amount, MinScore, MaxScore);
        SaveScore();
        UpdateUI();
    }

    public void OpenPopup()
    {
        if (_score < MaxScore)
        {
            ShowTooltip();
            return;
        }

        _score = 0f;
        SaveScore();
        UpdateUI();

        if (_popupRoot != null && _canvas != null)
        {
            var popup = Instantiate(_popupRoot, _canvas.transform);

            // ВАЖНО: в LuckyChestPopup должен быть метод Init(firstReward, x2Multiplier)
            popup.Init(_rewardForFirstAd, _rewardMultiplierSecondAd);
        }

        // ✅ Сообщаем OfferManager: сундук открыт
        if (OfferManager.Instance != null)
            OfferManager.Instance.NotifyLuckyChestOpened();
    }

    private void ShowTooltip()
    {
        if (_tooltipPopup == null) return;

        if (_tooltipText != null)
        {
            int baseReward = Mathf.Max(0, _rewardForFirstAd);
            _tooltipText.text = baseReward.ToString();
        }

        if (_tooltipRoutine != null)
            StopCoroutine(_tooltipRoutine);

        _tooltipRoutine = StartCoroutine(TooltipSequence());
    }

    private IEnumerator TooltipSequence()
    {
        // fade in
        yield return FadeCanvasGroup(_tooltipPopup, 1f, _fadeDuration);

        // hold
        yield return new WaitForSecondsRealtime(_showDuration);

        // fade out
        yield return FadeCanvasGroup(_tooltipPopup, 0f, _fadeDuration);

        _tooltipRoutine = null;
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup cg, float target, float duration)
    {
        if (cg == null) yield break;

        float start = cg.alpha;

        cg.interactable = target > 0.99f;
        cg.blocksRaycasts = target > 0.99f;

        if (duration <= 0f)
        {
            cg.alpha = target;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // чтобы работало даже при паузе
            float k = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(start, target, k);
            yield return null;
        }

        cg.alpha = target;
        cg.interactable = target > 0.99f;
        cg.blocksRaycasts = target > 0.99f;
    }

    private void LoadScore()
    {
        _score = PlayerPrefs.GetFloat(PrefKeyScore, 0f);
    }

    private void SaveScore()
    {
        PlayerPrefs.SetFloat(PrefKeyScore, _score);
        PlayerPrefs.Save();
    }

    private void ClampScore()
    {
        _score = Mathf.Clamp(_score, MinScore, MaxScore);
    }

    private void UpdateUI()
    {
        float fill = 0f;

        if (_progressBar != null)
        {
            fill = Mathf.Clamp01(_score / MaxScore);
            _progressBar.fillAmount = fill;

            _progressPercentField.text = $"{(int)(fill * 100)}%";
        }

        if (_lighting != null)
            _lighting.SetActive(fill >= 0.999f);
    }
}
