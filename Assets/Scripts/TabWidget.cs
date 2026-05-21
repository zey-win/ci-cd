using UnityEngine;

public class TabWidget : MonoBehaviour
{
    [SerializeField] private TabButton _selectedTab;

    [Space(10)] public Color selectedColor = Color.white;
    public Color deselectedColor = Color.white;
    public Color highlightColor = Color.white;

    private void Start()
    {
        if (_selectedTab)
            _selectedTab.Select();
    }

    public void Select(TabButton tabButton)
    {
        _selectedTab.Deselect();
        _selectedTab = tabButton;
        _selectedTab.Select();
    }
}