using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Analytics;

public class EndlessAdventurePopup : Popup
{
    [Header("Prefabs")]
    [SerializeField] private RewardPanel _rewardPanelPrefab;

    [Header("Scene refs (order matters)")]
    [SerializeField] private List<EndlessAdventureCard> _endlessAdventureCards = new List<EndlessAdventureCard>();
    [SerializeField] private List<RectTransform> _arrows = new List<RectTransform>();

    [Header("Points (order matters, same path)")]
    [SerializeField] private List<RectTransform> _slotPoints = new List<RectTransform>();
    [SerializeField] private List<RectTransform> _arrowPoints = new List<RectTransform>();

    [Header("Timings")]
    [SerializeField] private float _fadeDuration = 0.25f;
    [SerializeField] private float _moveDuration = 0.45f;
    [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool _useUnscaledTime = true;

    [Header("Punch (before fade)")]
    [SerializeField] private float _punchScale = 1.07f;      // во сколько раз увеличить
    [SerializeField] private float _punchDuration = 0.12f;   // длительность увеличения
    [SerializeField] private float _punchReturnDuration = 0.10f; // возврат к 1
    [SerializeField] private AnimationCurve _punchCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);


    private const string ProgressOffsetKey = "endless_adventure_offset";
    private int _restoredOffset;


    private bool _busy;
    private RewardPanel _activeRewardPanel;
    private bool _claimInProgress;

    // ===== ANALYTICS =====
    private string _closeReason = "unknown";   // "x" / "system" / "unknown"
    private string _lastRewardSource = "free"; // "free" / "ad"
    private int _lastRewardAmount = 0;

    private void LogEvent(string name, params Parameter[] parameters)
    {
        AnalyticsSafe.LogEvent(name, parameters);
    }

    private static int B(bool v) => v ? 1 : 0;
    // ===== /ANALYTICS =====


    private void Start()
    {
        ValidateSetup();
        RestoreProgressOrder();

        // Инициализация кликов
        foreach (var card in _endlessAdventureCards)
            card.Init(OnClickCardHandler);

        // Позиции
        SnapAllToPoints();

        // Состояния: только первая доступна
        ApplyLocksInitial();


        _closeReason = "unknown";

        int reward = (_endlessAdventureCards.Count > 0) ? _endlessAdventureCards[0].Reward : 0;
        bool isFree = (_endlessAdventureCards.Count > 0) && _endlessAdventureCards[0].IsFree;

        LogEvent("endless_adventure_screen_open",
            new Parameter("current_card_reward", reward),
            new Parameter("current_card_is_free", B(isFree))
        );

    }

    private void ValidateSetup()
    {
        if (_endlessAdventureCards.Count == 0)
        {
            Debug.LogError("[EndlessAdventurePopup] No cards set.");
            return;
        }

        if (_slotPoints.Count != _endlessAdventureCards.Count)
        {
            Debug.LogError($"[EndlessAdventurePopup] slotPoints({_slotPoints.Count}) must match cards({_endlessAdventureCards.Count}).");
        }

        if (_arrows.Count != _arrowPoints.Count)
        {
            Debug.LogWarning($"[EndlessAdventurePopup] arrows({_arrows.Count}) != arrowPoints({_arrowPoints.Count}). Movement will work only for min count.");
        }

        // Убедимся что у стрелок есть CanvasGroup (для fade)
        foreach (var a in _arrows)
            EnsureCanvasGroup(a);
    }

    private void ApplyLocksInitial()
    {
        for (int i = 0; i < _endlessAdventureCards.Count; i++)
        {
            bool isCurrent = (i == 0);
            _endlessAdventureCards[i].SetSelected(isCurrent);

            // первая — unlocked без анимации
            _endlessAdventureCards[i].SetLocked(!isCurrent);
            _endlessAdventureCards[i].SetAlpha(1f);
        }

        foreach (var a in _arrows)
        {
            var cg = EnsureCanvasGroup(a);
            cg.alpha = 1f;
        }
    }



    private void OnClickCardHandler(EndlessAdventureCard card)
    {
        LogEvent("endless_adventure_card_click",
            new Parameter("is_free", B(card.IsFree)),
            new Parameter("reward", card.Reward)
        );


        if (_busy) return;
        if (_activeRewardPanel != null) return;

        if (_endlessAdventureCards.Count == 0 || card != _endlessAdventureCards[0])
            return;

        _claimInProgress = false;


        if (card.IsFree)
        {
            GiveReward(card);
        }
        else
        {
            LogEvent("endless_adventure_rewarded_ad_click",
                new Parameter("reward", card.Reward)
            );


            ShowRewarded(card.Reward, () =>
            {
                GiveReward(card);
            });

        }

    }


    private void RestoreProgressOrder()
    {
        int n = _endlessAdventureCards.Count;
        if (n <= 0) return;

        int offset = PlayerPrefs.GetInt(ProgressOffsetKey, 0);
        offset = ((offset % n) + n) % n;
        _restoredOffset = offset;

        RotateListBy(_endlessAdventureCards, offset);

        if (_arrows != null && _arrows.Count > 0)
        {
            int m = _arrows.Count;
            int offA = ((offset % m) + m) % m;
            RotateListBy(_arrows, offA);
        }
    }

    private static void RotateListBy<T>(List<T> list, int times)
    {
        if (list == null || list.Count <= 1) return;
        times %= list.Count;
        for (int i = 0; i < times; i++)
        {
            var first = list[0];
            list.RemoveAt(0);
            list.Add(first);
        }
    }



    private void GiveReward(EndlessAdventureCard card)
    {
        _lastRewardAmount = card.Reward;
        _lastRewardSource = card.IsFree ? "free" : "ad";

        LogEvent("endless_adventure_reward_panel_open",
            new Parameter("reward", card.Reward),
            new Parameter("source", _lastRewardSource)
        );


        _activeRewardPanel = Instantiate(_rewardPanelPrefab, transform);
        _activeRewardPanel.Init(
            reward: card.Reward,
            onClaim: () =>
            {
                if (_claimInProgress) return;
                _claimInProgress = true;
                LogEvent("endless_adventure_claim_click",
                    new Parameter("reward", card.Reward),
                    new Parameter("source", _lastRewardSource)
                );

                // FindFirstObjectByType<BalanceManager>().AddWinnings(card.Reward, "EndlessAdventureReward");
                SaveProgressAdvance();
                LogEvent("endless_adventure_reward_granted",
                    new Parameter("amount", card.Reward),
                    new Parameter("source", _lastRewardSource)
                );


                if (_activeRewardPanel != null)
                    _activeRewardPanel.Close();

                StartCoroutine(ClaimAndRotateRoutine());
            },
            onClose: () =>
            {
                _activeRewardPanel = null;
                _claimInProgress = false;
            }
        );
    }


    private void SaveProgressAdvance()
    {
        int n = _endlessAdventureCards.Count;
        if (n <= 0) return;

        int offset = PlayerPrefs.GetInt(ProgressOffsetKey, 0);
        offset = (offset + 1) % n;

        PlayerPrefs.SetInt(ProgressOffsetKey, offset);
        PlayerPrefs.Save();
    }



    private void ShowRewarded(int reward, System.Action onSuccess)
    {
        AdManager adManager = FindFirstObjectByType<AdManager>();
        if (adManager == null) return;

        adManager.ShowRewardedAd(() =>
        {
            LogEvent("endless_adventure_rewarded_ad_success",
                new Parameter("reward", reward)
            );

            onSuccess?.Invoke();
        });
    }



    private IEnumerator ClaimAndRotateRoutine()
    {
        if (_endlessAdventureCards.Count == 0) yield break;

        _busy = true;

        // На всякий случай выключим интерактив на время анимаций
        SetCardsInteractable(false);

        var claimedCard = _endlessAdventureCards[0];
        RectTransform claimedArrow = (_arrows.Count > 0) ? _arrows[0] : null;

        yield return PunchScale(claimedCard.RectTransform, _punchScale, _punchDuration, _punchReturnDuration);

        // 1) Fade out текущей карты и стрелки рядом
        yield return FadeCard(claimedCard, 1f, 0f, _fadeDuration);

        if (claimedArrow != null)
            yield return FadeRect(claimedArrow, 1f, 0f, _fadeDuration);

        // 2) Двигаем все элементы к следующим поинтам (по кругу)
        yield return MoveAllToNextPoints(_moveDuration);

        // 3) Ротация списков (первый -> в конец)
        RotateList(_endlessAdventureCards);
        RotateList(_arrows);

        var movedToLast = _endlessAdventureCards[_endlessAdventureCards.Count - 1];
        movedToLast.SetLocked(true);       // включит замок и запретит интерактив
        movedToLast.SetSelected(false);

        // 4) Снэп (на всякий) и логику локов/выделения
        SnapAllToPoints();

        // Все залочить
        for (int i = 0; i < _endlessAdventureCards.Count; i++)
        {
            _endlessAdventureCards[i].SetSelected(i == 0);
            _endlessAdventureCards[i].SetLocked(true);
        }

        // Новый текущий — unlock с анимацией
        var newCurrent = _endlessAdventureCards[0];
        newCurrent.UnlockWithAnimation();

        // claimedCard теперь в конце: должен быть залочен и появиться
        claimedCard.SetLocked(true);

        // 5) Fade in “пропавших” уже в новом месте (в конце)
        yield return FadeCard(claimedCard, 0f, 1f, _fadeDuration);

        if (claimedArrow != null)
            yield return FadeRect(claimedArrow, 0f, 1f, _fadeDuration);

        // Возвращаем интерактив: только текущая
        for (int i = 0; i < _endlessAdventureCards.Count; i++)
            _endlessAdventureCards[i].SetInteractable(i == 0);


        int newReward = (_endlessAdventureCards.Count > 0) ? _endlessAdventureCards[0].Reward : 0;
        bool newIsFree = (_endlessAdventureCards.Count > 0) && _endlessAdventureCards[0].IsFree;

        LogEvent("endless_adventure_rotation_complete",
            new Parameter("new_current_reward", newReward),
            new Parameter("new_current_is_free", B(newIsFree))
        );

        _busy = false;
    }

    private void SetCardsInteractable(bool interactable)
    {
        foreach (var c in _endlessAdventureCards)
            c.SetInteractable(interactable);
    }

    private void SnapAllToPoints()
    {
        int n = Mathf.Min(_endlessAdventureCards.Count, _slotPoints.Count);
        for (int i = 0; i < n; i++)
        {
            var rt = _endlessAdventureCards[i].RectTransform;
            rt.position = _slotPoints[i].position;
        }

        int m = Mathf.Min(_arrows.Count, _arrowPoints.Count);
        for (int i = 0; i < m; i++)
        {
            _arrows[i].SetPositionAndRotation(_arrowPoints[i].position, _arrowPoints[i].rotation);
        }
    }


    /// <summary>
    /// Каждый элемент i едет в точку (i-1), а 0-й — в последнюю. Это даёт эффект:
    /// 2я карта становится на место 1й, 3я -> на место 2й, и т.д.
    /// </summary>
    private IEnumerator MoveAllToNextPoints(float duration)
    {
        float dt() => _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        int n = Mathf.Min(_endlessAdventureCards.Count, _slotPoints.Count);
        var cardRects = new RectTransform[n];
        var cardFrom = new Vector3[n];
        var cardTo = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            cardRects[i] = _endlessAdventureCards[i].RectTransform;
            cardFrom[i] = cardRects[i].position;

            int targetIndex = (i - 1 + n) % n;
            cardTo[i] = _slotPoints[targetIndex].position;
        }

        int m = Mathf.Min(_arrows.Count, _arrowPoints.Count);
        var arrowRects = new RectTransform[m];
        var arrowFromPos = new Vector3[m];
        var arrowToPos = new Vector3[m];
        var arrowFromRot = new Quaternion[m];
        var arrowToRot = new Quaternion[m];

        for (int i = 0; i < m; i++)
        {
            arrowRects[i] = _arrows[i];
            arrowFromPos[i] = arrowRects[i].position;
            arrowFromRot[i] = arrowRects[i].rotation;

            int targetIndex = (i - 1 + m) % m;
            arrowToPos[i] = _arrowPoints[targetIndex].position;
            arrowToRot[i] = _arrowPoints[targetIndex].rotation;
        }

        float t = 0f;
        while (t < duration)
        {
            t += dt();
            float p = Mathf.Clamp01(t / duration);
            float eased = (_moveCurve != null) ? _moveCurve.Evaluate(p) : p;

            for (int i = 0; i < n; i++)
                cardRects[i].position = Vector3.LerpUnclamped(cardFrom[i], cardTo[i], eased);

            for (int i = 0; i < m; i++)
            {
                var pos = Vector3.LerpUnclamped(arrowFromPos[i], arrowToPos[i], eased);
                var rot = Quaternion.SlerpUnclamped(arrowFromRot[i], arrowToRot[i], eased);
                arrowRects[i].SetPositionAndRotation(pos, rot);
            }

            yield return null;
        }

        // финальный snap
        for (int i = 0; i < n; i++)
            cardRects[i].position = cardTo[i];

        for (int i = 0; i < m; i++)
            arrowRects[i].SetPositionAndRotation(arrowToPos[i], arrowToRot[i]);
    }


    private IEnumerator FadeCard(EndlessAdventureCard card, float from, float to, float duration)
    {
        if (card == null) yield break;
        yield return FadeCanvasGroup(card.CanvasGroup, from, to, duration);
    }

    private IEnumerator FadeRect(RectTransform rt, float from, float to, float duration)
    {
        if (rt == null) yield break;
        var cg = EnsureCanvasGroup(rt);
        yield return FadeCanvasGroup(cg, from, to, duration);
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        float dt() => _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        cg.alpha = from;

        float t = 0f;
        while (t < duration)
        {
            t += dt();
            float p = Mathf.Clamp01(t / duration);
            cg.alpha = Mathf.Lerp(from, to, p);
            yield return null;
        }

        cg.alpha = to;
    }

    private static void RotateList<T>(List<T> list)
    {
        if (list == null || list.Count <= 1) return;
        var first = list[0];
        list.RemoveAt(0);
        list.Add(first);
    }

    private static CanvasGroup EnsureCanvasGroup(Component c)
    {
        if (c == null) return null;
        var cg = c.GetComponent<CanvasGroup>();
        if (cg == null) cg = c.gameObject.AddComponent<CanvasGroup>();
        return cg;
    }



    public void OnClose()
    {
        _closeReason = "x";

        LogEvent("endless_adventure_screen_close",
            new Parameter("close_reason", _closeReason)
        );

        Destroy(gameObject);
    }



    private IEnumerator PunchScale(RectTransform rt, float targetScale, float upDuration, float downDuration)
    {
        if (rt == null) yield break;

        float dt() => _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        Vector3 baseScale = Vector3.one;             // если у тебя всегда 1
                                                     // Если хочешь сохранять текущий (на случай нестандарта), используй:
                                                     // Vector3 baseScale = rt.localScale;

        Vector3 upScale = baseScale * targetScale;

        // scale up
        float t = 0f;
        while (t < upDuration)
        {
            t += dt();
            float p = Mathf.Clamp01(t / upDuration);
            float eased = (_punchCurve != null) ? _punchCurve.Evaluate(p) : p;
            rt.localScale = Vector3.LerpUnclamped(baseScale, upScale, eased);
            yield return null;
        }
        rt.localScale = upScale;

        // scale back
        t = 0f;
        while (t < downDuration)
        {
            t += dt();
            float p = Mathf.Clamp01(t / downDuration);
            float eased = (_punchCurve != null) ? _punchCurve.Evaluate(p) : p;
            rt.localScale = Vector3.LerpUnclamped(upScale, baseScale, eased);
            yield return null;
        }
        rt.localScale = baseScale;
    }

}
