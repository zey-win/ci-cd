using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoldRepeatButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private int _direction = 1; // +1 для плюса, -1 для минуса

    [Header("Hold repeat")]
    [SerializeField] private float _initialDelay = 0.35f; // пауза перед автоповтором
    [SerializeField] private float _startInterval = 0.18f; // начальная частота тиков
    [SerializeField] private float _minInterval = 0.05f;   // максимальная скорость (минимальный интервал)
    [SerializeField] private float _accelTime = 1.2f;      // за сколько секунд разгоняемся до minInterval

    public event Action<int, bool> OnStep; // (direction, isHoldTick)

    private bool _pressed;
    private float _pressedAt;
    private Coroutine _co;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_pressed) return;
        _pressed = true;
        _pressedAt = Time.unscaledTime;

        OnStep?.Invoke(_direction, false);

        _co = StartCoroutine(Repeat());
    }

    public void OnPointerUp(PointerEventData eventData) => StopRepeat();
    public void OnPointerExit(PointerEventData eventData) => StopRepeat();

    private void StopRepeat()
    {
        _pressed = false;
        if (_co != null)
        {
            StopCoroutine(_co);
            _co = null;
        }
    }

    private IEnumerator Repeat()
    {
        yield return new WaitForSecondsRealtime(_initialDelay);

        while (_pressed)
        {
            float held = Mathf.Max(0f, Time.unscaledTime - _pressedAt - _initialDelay);
            float t = (_accelTime <= 0f) ? 1f : Mathf.Clamp01(held / _accelTime);
            float interval = Mathf.Lerp(_startInterval, _minInterval, t);

            OnStep?.Invoke(_direction, true);

            yield return new WaitForSecondsRealtime(interval);
        }
    }
}
