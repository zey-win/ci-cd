using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Firebase.Analytics;
public class RowsController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RectTransform _selector;
    [SerializeField] private float _moveTime = 0.15f;

    [Header("Defaults")]
    [SerializeField] private int _defaultRows = 8;
    [SerializeField] private UnlockRowPopup _unlockRowPopupPrefab;
    private UnlockRowPopup _activeUnlockPopup;

    private readonly List<RowsButton> _buttons = new List<RowsButton>();
    private RowsButton _current;
    private Coroutine _moveCoroutine;

    private bool _adInProgress;

    private const string PREF_UNLOCKED_MAX = "Rows_UnlockedMax";
    private const string PREF_SELECTED = "Rows_Selected";
    private const string PREF_UNLOCK_POPUP_SHOWN = "Rows_UnlockPopupShown";

    private int _unlockedMaxRows;
    private int _selectedRows;

    private void Awake()
    {
        _buttons.Clear();
        _buttons.AddRange(GetComponentsInChildren<RowsButton>(true));

        _buttons.Sort((a, b) => a.RowsCount.CompareTo(b.RowsCount));

        foreach (var b in _buttons)
            b.Init(OnRowsButtonClicked);
    }

    private void Start()
    {
        LoadProgress();
        ApplyStates();
        StartCoroutine(SelectInitialAfterLayout());
    }

    private void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(PREF_UNLOCKED_MAX))
            PlayerPrefs.SetInt(PREF_UNLOCKED_MAX, _defaultRows);

        if (!PlayerPrefs.HasKey(PREF_SELECTED))
            PlayerPrefs.SetInt(PREF_SELECTED, _defaultRows);

        if (!PlayerPrefs.HasKey(PREF_UNLOCK_POPUP_SHOWN))
            PlayerPrefs.SetInt(PREF_UNLOCK_POPUP_SHOWN, 0);

        _unlockedMaxRows = PlayerPrefs.GetInt(PREF_UNLOCKED_MAX, _defaultRows);
        _selectedRows = PlayerPrefs.GetInt(PREF_SELECTED, _defaultRows);

        _unlockedMaxRows = Mathf.Max(_defaultRows, _unlockedMaxRows);

        if (_selectedRows > _unlockedMaxRows)
            _selectedRows = _unlockedMaxRows;
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(PREF_UNLOCKED_MAX, _unlockedMaxRows);
        PlayerPrefs.SetInt(PREF_SELECTED, _selectedRows);
        PlayerPrefs.Save();
    }

    private void ApplyStates()
    {
        int nextAdIndex = -1;
        for (int i = 0; i < _buttons.Count; i++)
        {
            if (_buttons[i].RowsCount > _unlockedMaxRows)
            {
                nextAdIndex = i;
                break;
            }
        }

        int firstLockedAfterAdIndex = (nextAdIndex >= 0 && nextAdIndex + 1 < _buttons.Count)
            ? nextAdIndex + 1
            : -1;

        for (int i = 0; i < _buttons.Count; i++)
        {
            var b = _buttons[i];

            if (b.RowsCount <= _unlockedMaxRows)
                b.SetState(RowsButton.State.Unlocked);
            else if (i == nextAdIndex)
                b.SetState(RowsButton.State.AdUnlock);
            else
                b.SetState(RowsButton.State.Locked);

            bool fullAlpha =
                (b.RowsCount <= _unlockedMaxRows) ||
                (i == nextAdIndex) ||
                (i == firstLockedAfterAdIndex);

            b.SetTextVisibility(fullAlpha);

            b.SetSelected(_current != null && b == _current);
        }
    }



    private void SelectInitial()
    {
        RowsButton btn = _buttons.FirstOrDefault(x => x.RowsCount == _selectedRows && x.RowsCount <= _unlockedMaxRows);
        if (btn == null)
            btn = _buttons.FirstOrDefault(x => x.RowsCount == _defaultRows) ?? _buttons.FirstOrDefault();

        if (btn != null)
            SelectUnlocked(btn, instantSelector: true);
    }

    private void OnRowsButtonClicked(RowsButton b)
    {
        if (_adInProgress) return;

        switch (b.CurrentState)
        {
            case RowsButton.State.Unlocked:
                if (_current == null || b != _current)
                {
                    AnalyticsLogger.Log("plinko_rows_select",
                        new Parameter("new_rows", (long)b.RowsCount)
                    );
                }
                SelectUnlocked(b, instantSelector: false);
                break;

            case RowsButton.State.AdUnlock:
                ShowUnlockPopupThenAd(b);
                break;

            case RowsButton.State.Locked:
                AnalyticsLogger.Log("plinko_rows_locked_click",
                    new Parameter("locked_rows", (long)b.RowsCount)
                );
                break;
        }

    }

    private void ShowUnlockPopupThenAd(RowsButton adButton)
    {
        if (_adInProgress) return;

        if (PlayerPrefs.GetInt(PREF_UNLOCK_POPUP_SHOWN, 0) == 0 && _unlockRowPopupPrefab != null)
        {
            var canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
            _activeUnlockPopup = (canvas != null)
                ? Instantiate(_unlockRowPopupPrefab, canvas.transform)
                : Instantiate(_unlockRowPopupPrefab);

            AnalyticsLogger.Log("plinko_rows_unlock_popup_open",
                new Parameter("target_rows", (long)adButton.RowsCount)
            );


            PlayerPrefs.SetInt(PREF_UNLOCK_POPUP_SHOWN, 1);
            PlayerPrefs.Save();

            _activeUnlockPopup.Init(() =>
            {
                AnalyticsLogger.Log("plinko_rows_unlock_ad_click",
                    new Parameter("target_rows", (long)adButton.RowsCount)
                );

                StartCoroutine(UnlockByAdRoutine(adButton));
            });


            return;
        }


        AnalyticsLogger.Log("plinko_rows_unlock_ad_click",
            new Parameter("target_rows", (long)adButton.RowsCount)
        );


        StartCoroutine(UnlockByAdRoutine(adButton));
    }


    private void SelectUnlocked(RowsButton b, bool instantSelector)
    {
        if (_current != null)
            _current.SetSelected(false);

        _current = b;
        _current.SetSelected(true);

        _selectedRows = _current.RowsCount;
        SaveProgress();

        var target = _current.GetComponent<RectTransform>();
        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);

        if (instantSelector)
        {
            var p = _selector.position;
            p.x = target.position.x;
            _selector.position = p;
        }
        else
        {
            _moveCoroutine = StartCoroutine(MoveSelector(target));
        }

        FindFirstObjectByType<GameManager>().BuildBoard(_current.RowsCount);
    }

    private IEnumerator UnlockByAdRoutine(RowsButton adButton)
    {
        bool hadAdManager = false;
        _adInProgress = true;

        SetAllButtonsInteractable(false);

        bool success = false;

        var adManager = FindFirstObjectByType<AdManager>();
        if (adManager != null)
        {
            hadAdManager = true;
            adManager.ShowRewardedAd(() =>
            {
                success = true;
            });
        }
        else
        {
            Debug.LogWarning("[RowsController] AdManager not found. Unlock denied.");
        }

        float timeout = 30f;
        while (!success && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }



        if (success)
        {
            int prevUnlocked = _unlockedMaxRows;

            _unlockedMaxRows = Mathf.Max(_unlockedMaxRows, adButton.RowsCount);
            _selectedRows = adButton.RowsCount;
            SaveProgress();

            if (_unlockedMaxRows > prevUnlocked)
            {
                AnalyticsLogger.Log("plinko_rows_unlocked",
                    new Parameter("unlocked_max_rows", (long)_unlockedMaxRows)
                );
            }


            ApplyStates();

            SelectUnlocked(adButton, instantSelector: false);

            AnalyticsLogger.Log("plinko_rows_unlock_ad_success",
                new Parameter("target_rows", (long)adButton.RowsCount)
            );
        }
        else
        {
            string failReason = hadAdManager ? "timeout" : "no_ad_manager";

            AnalyticsLogger.Log("plinko_rows_unlock_ad_fail",
                new Parameter("target_rows", (long)adButton.RowsCount),
                new Parameter("fail_reason", failReason)
            );
        }

        SetAllButtonsInteractable(true);
        ApplyStates();
        _adInProgress = false;
    }

    private void SetAllButtonsInteractable(bool value)
    {
        foreach (var b in _buttons)
        {
            var btn = b.GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.interactable = value;
        }
    }

    private IEnumerator MoveSelector(RectTransform target)
    {
        Vector3 start = _selector.position;
        Vector3 end = _selector.position;
        end.x = target.position.x;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / _moveTime;

            var p = _selector.position;
            p.x = Mathf.Lerp(start.x, end.x, t);
            _selector.position = p;

            yield return null;
        }

        var final = _selector.position;
        final.x = end.x;
        _selector.position = final;
    }


    private IEnumerator SelectInitialAfterLayout()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        SelectInitial();
    }
}
