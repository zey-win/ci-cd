using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DifficultyButton : MonoBehaviour
{
    public Difficulty Difficulty => _currentDifficulty;
    [SerializeField] private Difficulty _currentDifficulty = Difficulty.LOW;

    [SerializeField] private Sprite _inactiveState;
    [SerializeField] private Sprite _activeState;

    [Header("Ad icon")]
    [SerializeField] private GameObject _adIcon;

    private Action<DifficultyButton> _clickCallback;




    public void Init(Action<DifficultyButton> clickCallback)
    {
        _clickCallback = clickCallback;
        GetComponent<Button>().onClick.AddListener(OnClickHandler);
    }



    public void Deactivate()
    {
        GetComponent<Image>().sprite = _inactiveState;
    }


    public void Activate()
    {
        GetComponent<Image>().sprite = _activeState;
    }


    private void OnClickHandler()
    {
        _clickCallback?.Invoke(this);
    }


    public void SetAdIconVisible(bool visible)
    {
        if (_adIcon != null)
            _adIcon.SetActive(visible);
    }
}
