using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TabButton : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Space(10)] public UnityEvent<bool> OnValueChange;

    private Image _image;
    private TabWidget _tabParent;
    public bool State { get; private set; }

    private void Awake()
    {
        _image = GetComponent<Image>();
        _tabParent = GetComponentInParent<TabWidget>();
    }

    public void Select()
    {
        _image.color = _tabParent.selectedColor;
        State = true;
        OnValueChange?.Invoke(State);
    }

    public void Deselect()
    {
        _image.color = _tabParent.deselectedColor;
        State = false;
        OnValueChange?.Invoke(State);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (State)
            return;
            
        _tabParent.Select(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _image.color = _tabParent.highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _image.color = State ? _tabParent.selectedColor : _tabParent.deselectedColor;
    }
}