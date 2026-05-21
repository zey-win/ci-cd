using System.Collections;
using System.Linq;
using UnityEngine;

public class DifficultyController : MonoBehaviour
{
    [SerializeField] private Difficulty _currentDifficulty = Difficulty.LOW;
    [SerializeField] private DifficultyButton _currentDifficultyButton;

    private DifficultyButton[] _buttons;
    private bool _adInProgress;

    private const string PREF_UNLOCKED_MAX = "Diff_UnlockedMax";
    private const string PREF_SELECTED = "Diff_Selected";

    private int _unlockedMax;
    private int _selected;

    private void Awake()
    {
        _buttons = GetComponentsInChildren<DifficultyButton>(true)
            .OrderBy(b => (int)b.Difficulty)
            .ToArray();

        foreach (var b in _buttons)
            b.Init(OnSelectDifficulty);

        LoadProgress();
        ApplyAdIcons();
        SelectInitial();
    }

    private void LoadProgress()
    {
        // Новый игрок: открыт только LOW
        if (!PlayerPrefs.HasKey(PREF_UNLOCKED_MAX))
            PlayerPrefs.SetInt(PREF_UNLOCKED_MAX, (int)Difficulty.LOW);

        if (!PlayerPrefs.HasKey(PREF_SELECTED))
            PlayerPrefs.SetInt(PREF_SELECTED, (int)Difficulty.LOW);

        _unlockedMax = Mathf.Clamp(PlayerPrefs.GetInt(PREF_UNLOCKED_MAX, (int)Difficulty.LOW), 0, 2);
        _selected = Mathf.Clamp(PlayerPrefs.GetInt(PREF_SELECTED, (int)Difficulty.LOW), 0, _unlockedMax);
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(PREF_UNLOCKED_MAX, _unlockedMax);
        PlayerPrefs.SetInt(PREF_SELECTED, _selected);
        PlayerPrefs.Save();
    }

    private void ApplyAdIcons()
    {
        foreach (var b in _buttons)
        {
            bool unlocked = (int)b.Difficulty <= _unlockedMax;
            b.SetAdIconVisible(!unlocked);
        }
    }

    private void SelectInitial()
    {
        var btn = _buttons.FirstOrDefault(x => (int)x.Difficulty == _selected && (int)x.Difficulty <= _unlockedMax);
        if (btn == null)
            btn = _buttons.FirstOrDefault(x => x.Difficulty == Difficulty.LOW);

        if (btn != null)
            SelectUnlocked(btn);
    }

    private void OnSelectDifficulty(DifficultyButton difficultyButton)
    {
        if (_adInProgress) return;

        int idx = (int)difficultyButton.Difficulty;
        bool unlocked = idx <= _unlockedMax;

        if (unlocked)
        {
            SelectUnlocked(difficultyButton);
            return;
        }

        StartCoroutine(UnlockByAdRoutine(difficultyButton));
    }

    private void SelectUnlocked(DifficultyButton btn)
    {
        if (_currentDifficultyButton != null)
            _currentDifficultyButton.Deactivate();

        _currentDifficultyButton = btn;
        _currentDifficultyButton.Activate();

        _currentDifficulty = btn.Difficulty;
        _selected = (int)_currentDifficulty;
        SaveProgress();

        ApplyAdIcons();

        // FindFirstObjectByType<GameManager>().ChangeRisk(_currentDifficulty);
    }

    private IEnumerator UnlockByAdRoutine(DifficultyButton targetButton)
    {
        _adInProgress = true;

        // отключаем все кнопки, чтобы не спамили
        SetAllInteractable(false);

        bool success = false;
        var adManager = FindFirstObjectByType<AdManager>();
        if (adManager != null)
        {
            adManager.ShowRewardedAd(() =>
            {
                success = true;
            });
        }
        else
        {
            Debug.LogWarning("[DifficultyController] AdManager not found.");
        }

        float timeout = 30f;
        while (!success && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (success)
        {
            int idx = (int)targetButton.Difficulty;
            _unlockedMax = Mathf.Max(_unlockedMax, idx);
            _selected = idx;
            SaveProgress();

            ApplyAdIcons();
            SelectUnlocked(targetButton);
        }

        SetAllInteractable(true);
        _adInProgress = false;
    }

    private void SetAllInteractable(bool value)
    {
        foreach (var b in _buttons)
        {
            var uiBtn = b.GetComponent<UnityEngine.UI.Button>();
            if (uiBtn != null) uiBtn.interactable = value;
        }
    }
}

public enum Difficulty
{
    LOW,
    MEDIUM,
    HIGH
}