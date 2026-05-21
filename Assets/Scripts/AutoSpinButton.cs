using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class AutoSpinButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private float _holdToEnableSec = 0.35f;

    private bool _pointerDown;
    private Coroutine _holdRoutine;

    private bool _ignoreNextClick;

    private bool _holdTriggeredThisPress;

    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerDown = true;
        _holdTriggeredThisPress = false;

        if (_holdRoutine != null) StopCoroutine(_holdRoutine);
        _holdRoutine = StartCoroutine(HoldCheck());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _pointerDown = false;

        if (_holdRoutine != null)
        {
            StopCoroutine(_holdRoutine);
            _holdRoutine = null;
        }

        if (_holdTriggeredThisPress)
        {
            _holdTriggeredThisPress = false;
            return;
        }

        if (_gameManager != null && !_gameManager.IsAutoSpin)
        {
            _gameManager.TrySpinOnce(fromAuto: false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_gameManager == null) return;

        if (_ignoreNextClick)
        {
            _ignoreNextClick = false;
            return;
        }

        if (_gameManager.IsAutoSpin)
        {
            _gameManager.StopAutoSpin("click");
        }
    }

    private IEnumerator HoldCheck()
    {
        yield return new WaitForSecondsRealtime(_holdToEnableSec);

        if (!_pointerDown) yield break;
        if (_gameManager == null) yield break;

        if (!_gameManager.IsAutoSpin)
        {
            _holdTriggeredThisPress = true;

            _gameManager.StartAutoSpin();
            _ignoreNextClick = true;
        }
    }
}
