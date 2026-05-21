using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class PlinkoBasket : MonoBehaviour
{
    public AudioSource audioSource;

    [Header("Big Catch Animation")]
    [SerializeField] private CanvasGroup bigCatchCanvasGroup;
    [SerializeField] private RectTransform bigCatchRectTransform;
    [SerializeField] private float bigCatchThreshold = 3f;
    [SerializeField] private float bigCatchMoveUp = 80f;
    [SerializeField] private float bigCatchDuration = 0.55f;
    [SerializeField] private Ease bigCatchMoveEase = Ease.OutQuad;
    [SerializeField] private Ease bigCatchFadeEase = Ease.OutQuad;

    public float Multiplier { get; private set; } = 1;
    public SpriteRenderer SpriteRenderer { get; private set; }

    public static Action<PlinkoBasket> OnBallHit;
    public static Action<PlinkoResult> OnPlinkoResult;
    public static Action<PlinkoResult> OnResult;

    private TMP_Text _text;
    private Vector2 _bigCatchStartAnchoredPosition;
    private Sequence _bigCatchSequence;

    public void SetMultiplier(float value)
    {
        Multiplier = value;

        _text.text = $"x{value:F2}";
    }

    private void Awake()
    {
        SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        _text = GetComponentInChildren<TMP_Text>();

        if (bigCatchCanvasGroup != null)
        {
            bigCatchCanvasGroup.alpha = 0f;
            bigCatchCanvasGroup.interactable = false;
            bigCatchCanvasGroup.blocksRaycasts = false;
        }

        if (bigCatchRectTransform != null)
        {
            _bigCatchStartAnchoredPosition = bigCatchRectTransform.anchoredPosition;
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Ball")) return;

        if (audioSource != null)
            audioSource.Play();

        var ball = col.GetComponent<PlinkoBall>();
        if (ball == null) return;

        OnBallHit?.Invoke(this);

        var pinkoResult = new PlinkoResult(ball, ball.Bet, Multiplier);
        OnPlinkoResult?.Invoke(pinkoResult);
        OnResult?.Invoke(pinkoResult);

        if (Multiplier > bigCatchThreshold)
        {
            PlayBigCatchAnimation();
        }

        var balanceManager = FindFirstObjectByType<BalanceManager>();
        if (balanceManager != null)
        {
            balanceManager.AddWinnings(ball.Bet * Multiplier, "BallBasketMultiplier");
        }

        var luckyChestManager = FindAnyObjectByType<LuckyChestManager>();
        if (luckyChestManager != null)
        {
            luckyChestManager.AddScore(Multiplier);
        }

        BallPooling.Pool.Release(ball);
    }

    public void SetAlphaInstant(float a)
    {
        if (SpriteRenderer != null)
        {
            var c = SpriteRenderer.color;
            c.a = a;
            SpriteRenderer.color = c;
        }

        if (_text != null)
            _text.alpha = a;
    }

    public Tween PlaySpawnTo(Vector3 targetPos, float moveTime, float fadeTime)
    {
        if (SpriteRenderer == null) SpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (_text == null) _text = GetComponentInChildren<TMP_Text>();

        transform.DOKill();
        if (SpriteRenderer != null) SpriteRenderer.DOKill();
        if (_text != null) _text.DOKill();

        var seq = DOTween.Sequence();
        seq.Join(transform.DOMove(targetPos, moveTime).SetEase(Ease.OutCubic));

        if (SpriteRenderer != null)
            seq.Join(SpriteRenderer.DOFade(1f, fadeTime));

        if (_text != null)
        {
            var textTween = DOTween.To(() => _text.alpha, x => _text.alpha = x, 1f, fadeTime);
            textTween.SetTarget(_text);
            seq.Join(textTween);
        }

        return seq;
    }

    private void PlayBigCatchAnimation()
    {
        if (bigCatchCanvasGroup == null || bigCatchRectTransform == null)
            return;

        _bigCatchSequence?.Kill();

        bigCatchRectTransform.anchoredPosition = _bigCatchStartAnchoredPosition;
        bigCatchCanvasGroup.alpha = 1f;

        _bigCatchSequence = DOTween.Sequence();
        _bigCatchSequence.Join(
            bigCatchRectTransform
                .DOAnchorPosY(_bigCatchStartAnchoredPosition.y + bigCatchMoveUp, bigCatchDuration)
                .SetEase(bigCatchMoveEase)
        );
        _bigCatchSequence.Join(
            bigCatchCanvasGroup
                .DOFade(0f, bigCatchDuration)
                .SetEase(bigCatchFadeEase)
        );
    }

    private void OnDisable()
    {
        _bigCatchSequence?.Kill();

        if (bigCatchRectTransform != null)
            bigCatchRectTransform.anchoredPosition = _bigCatchStartAnchoredPosition;

        if (bigCatchCanvasGroup != null)
            bigCatchCanvasGroup.alpha = 0f;
    }
}

public class PlinkoResult
{
    public readonly PlinkoBall ball;
    public readonly double bet;
    public readonly double multiplier;

    public override string ToString() => $"Ball: {ball}, Bet: {bet}, Multiplier: {multiplier}";

    public PlinkoResult(PlinkoBall ball, double bet, double multiplier)
    {
        this.ball = ball;
        this.bet = bet;
        this.multiplier = multiplier;
    }
}