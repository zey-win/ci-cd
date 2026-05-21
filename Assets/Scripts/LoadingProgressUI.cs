using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingProgressUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image _fillImage;   // Image Type = Filled
    [SerializeField] private TextMeshProUGUI _progressText;

    [Header("Money marker")]
    [SerializeField] private RectTransform _moneyRect; // money (иконка)
    [SerializeField] private RectTransform _trackRect; // bar (полная ширина прогресс-бара)
    [SerializeField] private float _moneyPaddingPx = 0f; // отступ от краёв трека

    [Tooltip("Ручной X-offset для калибровки (в UI-пикселях/локальных единицах канваса).")]
    [SerializeField] private float _moneyExtraOffsetX = 0f;

    [Header("Smoothing")]
    [SerializeField] private float minSpeed = 0.15f;
    [SerializeField] private float maxSpeed = 2.0f;
    [SerializeField] private float diffForMaxSpeed = 0.35f;

    [Header("Anti-freeze (idle creep)")]
    [SerializeField] private float idleDelay = 0.8f;
    [SerializeField] private float idleExtraCap = 0.05f;
    [SerializeField] private float hardCap = 0.95f;

    [Header("Debug / Test mode")]
    [SerializeField] private bool testMode = false;

    [Tooltip("Если true — прогресс будет бегать 0→1→0 по кругу.")]
    [SerializeField] private bool testAnimate = true;

    [Range(0f, 1f)]
    [SerializeField] private float testProgress01 = 0.25f;

    [Tooltip("За сколько секунд сделать полный цикл 0→1→0.")]
    [SerializeField] private float testCycleSeconds = 6f;

    [Tooltip("Если true — в тест-режиме игнорируем smoothing и сразу ставим display = target.")]
    [SerializeField] private bool testBypassSmoothing = false;

    [SerializeField] private TextMeshProUGUI _debugText;       // optional
    [SerializeField] private RectTransform _debugMarkerRect;   // optional (маленькая точка/иконка)

    private float _display;
    private float _target;
    private float _lastEventTime;

    private void OnEnable()
    {
        _display = InitProgress.Last;
        _target = InitProgress.Last;
        _lastEventTime = Time.unscaledTime;

        ApplyUI(_display);

        InitProgress.OnProgress += HandleProgress;
    }

    private void OnDisable()
    {
        InitProgress.OnProgress -= HandleProgress;
    }

    private void HandleProgress(float p, string stage)
    {
        if (testMode) return; // в тест-режиме игнорим реальные события
        if (p < _target) return;
        _target = p;
        _lastEventTime = Time.unscaledTime;
    }

    private void Update()
    {
        // ---------- TEST MODE ----------
        if (testMode)
        {
            float p;
            if (testAnimate)
            {
                float cycle = Mathf.Max(0.01f, testCycleSeconds);
                // PingPong длиной 1 имеет период 2, поэтому умножаем скорость на 2/cycle
                p = Mathf.PingPong(Time.unscaledTime * (2f / cycle), 1f);
            }
            else
            {
                p = Mathf.Clamp01(testProgress01);
            }

            _target = p;
            _lastEventTime = Time.unscaledTime; // чтобы idle creep не вмешивался

            if (testBypassSmoothing)
            {
                _display = _target;
                ApplyUI(_display);
                return;
            }
            // иначе — пойдём дальше обычным smoothing
        }

        float effectiveTarget = _target;

        float idleTime = Time.unscaledTime - _lastEventTime;
        if (idleTime >= idleDelay && _display >= _target - 0.002f)
        {
            effectiveTarget = Mathf.Min(_target + idleExtraCap, hardCap);
        }

        if (_target >= 0.999f)
            effectiveTarget = 1f;

        float diff = Mathf.Clamp01(effectiveTarget - _display);
        if (diff <= 0f)
        {
            ApplyUI(_display); // важно: чтобы money/debug обновлялись даже когда diff=0
            return;
        }

        float t = Mathf.Clamp01(diff / diffForMaxSpeed);
        float speed = Mathf.Lerp(minSpeed, maxSpeed, t);

        _display = Mathf.MoveTowards(_display, effectiveTarget, speed * Time.unscaledDeltaTime);

        ApplyUI(_display);
    }

    private void ApplyUI(float value01)
    {
        if (_fillImage != null)
            _fillImage.fillAmount = value01;

        if (_progressText != null)
        {
            int pct = Mathf.Clamp(Mathf.RoundToInt(value01 * 100f), 0, 100);
            _progressText.text = $"Loading {pct}%";
        }

        UpdateMoneyAndDebug(value01);
    }

    private void UpdateMoneyAndDebug(float value01)
    {
        if (_moneyRect == null) return;

        var track = _trackRect != null
            ? _trackRect
            : (_fillImage != null ? _fillImage.rectTransform : null);

        if (track == null) return;

        var parent = _moneyRect.parent as RectTransform;
        if (parent == null) return;

        value01 = Mathf.Clamp01(value01);

        // --- 1) Получаем левый/правый край трека в локальных координатах PARENT ---
        Vector3 leftWorld = track.TransformPoint(new Vector3(track.rect.xMin, 0f, 0f));
        Vector3 rightWorld = track.TransformPoint(new Vector3(track.rect.xMax, 0f, 0f));

        float leftX = parent.InverseTransformPoint(leftWorld).x;
        float rightX = parent.InverseTransformPoint(rightWorld).x;

        // --- 2) “идеальная” X точка конца прогресса (pivot money будет поставлен сюда) ---
        float rawX = Mathf.Lerp(leftX, rightX, value01);

        // добавляем ручную калибровку
        rawX += _moneyExtraOffsetX;

        // --- 3) Кламп по краям с учётом pivot money ---
        float scaleRatioX = parent.lossyScale.x == 0f ? 1f : (_moneyRect.lossyScale.x / parent.lossyScale.x);
        float widthLocal = _moneyRect.rect.width * scaleRatioX;

        float leftExtent = widthLocal * _moneyRect.pivot.x;            // сколько занимает влево от pivot
        float rightExtent = widthLocal * (1f - _moneyRect.pivot.x);    // сколько вправо от pivot

        float minX = leftX + leftExtent + _moneyPaddingPx;
        float maxX = rightX - rightExtent - _moneyPaddingPx;

        float clampedX = Mathf.Clamp(rawX, minX, maxX);

        // --- 4) Ставим money по X (localPosition — стабильно, не зависит от anchors) ---
        var lp = _moneyRect.localPosition;
        _moneyRect.localPosition = new Vector3(clampedX, lp.y, lp.z);

        // --- 5) Debug marker (показывает rawX ДО клампа) ---
        if (_debugMarkerRect != null)
        {
            var mlp = _debugMarkerRect.localPosition;
            _debugMarkerRect.localPosition = new Vector3(rawX, mlp.y, mlp.z);
        }

        // --- 6) Debug text ---
        if (_debugText != null)
        {
            // Полезные “рекомендованные” оффсеты:
            // если хочешь, чтобы ПРАВЫЙ край money совпадал с концом — offset = -rightExtent
            // если ЛЕВЫЙ край совпадал — offset = +leftExtent
            float offsetToRightEdgeAlign = -rightExtent;
            float offsetToLeftEdgeAlign = +leftExtent;

            _debugText.text =
                $"[TEST={testMode}] p={value01:0.000}\n" +
                $"trackX: L={leftX:0.0} R={rightX:0.0}\n" +
                $"rawX={rawX:0.0} clampX={clampedX:0.0}\n" +
                $"moneyPivot={_moneyRect.pivot.x:0.00} width={widthLocal:0.0}\n" +
                $"extents: L={leftExtent:0.0} R={rightExtent:0.0}\n" +
                $"extraOffsetX(now)={_moneyExtraOffsetX:0.0}\n" +
                $"suggest: alignRightEdge={offsetToRightEdgeAlign:0.0} | alignLeftEdge={offsetToLeftEdgeAlign:0.0}";
        }
    }
}