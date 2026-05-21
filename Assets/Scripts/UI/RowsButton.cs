using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RowsButton : MonoBehaviour
{
    public enum State
    {
        Unlocked,
        AdUnlock,
        Locked
    }

    public int RowsCount => _rowsCount;
    public State CurrentState => _state;

    [Header("Data")]
    [SerializeField] private int _rowsCount = 8;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _numberField;
    [SerializeField] private GameObject _adIcon;
    [SerializeField] private GameObject _lockIcon;
    [SerializeField] private Button _button;

    [Header("Colors")]
    [SerializeField] private Color _selectedColor = Color.white;
    [SerializeField] private Color _normalColor = new Color(0.3607843f, 0.4117647f, 0.7450981f);

    [Header("Alpha")]
    [Range(0f, 1f)][SerializeField] private float _alphaFull = 1f;
    [Range(0f, 1f)][SerializeField] private float _alphaDim = 0.35f;

    private Action<RowsButton> _clickCallback;
    private State _state = State.Locked;
    private bool _selected;

    // управляется контроллером
    private float _textAlpha = 1f;

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();
    }

    public void Init(Action<RowsButton> clickCallback)
    {
        _clickCallback = clickCallback;

        if (_button == null)
            _button = GetComponent<Button>();

        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClicked);
    }

    private void OnDisable()
    {
        if (_button != null)
            _button.onClick.RemoveAllListeners();
    }

    private void OnClicked()
    {
        _clickCallback?.Invoke(this);
    }

    public void SetState(State state)
    {
        _state = state;

        if (_adIcon != null)
            _adIcon.SetActive(state == State.AdUnlock);

        if (_lockIcon != null)
            _lockIcon.SetActive(state == State.Locked);

        if (_button != null)
            _button.interactable = (state != State.Locked);

        ApplyTextStyle();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        ApplyTextStyle();
    }

    /// <summary>
    /// true = полный альфа, false = приглушённый (для дальних locked)
    /// </summary>
    public void SetTextVisibility(bool full)
    {
        _textAlpha = full ? _alphaFull : _alphaDim;
        ApplyTextStyle();
    }

    private void ApplyTextStyle()
    {
        if (_numberField == null) return;

        Color baseColor = _selected ? _selectedColor : _normalColor;
        baseColor.a = _textAlpha;
        _numberField.color = baseColor;
    }
}
