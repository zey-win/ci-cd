using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BallSkinsManager : MonoBehaviour
{
    public static BallSkinsManager Instance { get; private set; }

    public event Action<string> OnActiveSkinChanged;
    public event Action<string> OnSkinUnlocked;

    public string ActiveId => _activeId;

    [Header("All skins (BallData)")]
    [SerializeField] private BallData[] _allBalls;
    [SerializeField] private string _defaultBallId = "default";

    private readonly Dictionary<string, BallData> _byId = new();
    private readonly HashSet<string> _unlocked = new();
    private string _activeId;

    private const string PREF_UNLOCKED = "BallSkins_Unlocked";
    private const string PREF_ACTIVE = "BallSkins_Active";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        _byId.Clear();
        foreach (var b in _allBalls)
        {
            if (b == null || string.IsNullOrEmpty(b.Key)) continue;
            _byId[b.Key] = b;
        }

        Load();
        EnsureDefaultUnlocked();
        EnsureActiveValid();
    }

    public BallData GetActiveBall()
    {
        if (!string.IsNullOrEmpty(_activeId) && _byId.TryGetValue(_activeId, out var ball))
            return ball;

        if (_byId.TryGetValue(_defaultBallId, out var def))
            return def;

        return _byId.Values.FirstOrDefault();
    }

    public bool IsUnlocked(string id) => !string.IsNullOrEmpty(id) && _unlocked.Contains(id);
    public bool IsActive(string id) => !string.IsNullOrEmpty(id) && _activeId == id;

    public void Unlock(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (!_byId.ContainsKey(id)) return;

        if (_unlocked.Add(id))
        {
            Save();
            OnSkinUnlocked?.Invoke(id);
        }
    }

    public void SetActive(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (!IsUnlocked(id)) return;

        if (_activeId == id) return;
        _activeId = id;

        PlayerPrefs.SetString(PREF_ACTIVE, _activeId);
        PlayerPrefs.Save();

        OnActiveSkinChanged?.Invoke(_activeId);
    }

    private void EnsureDefaultUnlocked()
    {
        if (string.IsNullOrEmpty(_defaultBallId)) return;

        if (_byId.ContainsKey(_defaultBallId))
        {
            _unlocked.Add(_defaultBallId);
            Save();
        }
    }

    private void EnsureActiveValid()
    {
        if (string.IsNullOrEmpty(_activeId) || !IsUnlocked(_activeId))
            _activeId = _defaultBallId;

        PlayerPrefs.SetString(PREF_ACTIVE, _activeId);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        _unlocked.Clear();

        string raw = PlayerPrefs.GetString(PREF_UNLOCKED, "");
        if (!string.IsNullOrEmpty(raw))
        {
            foreach (var id in raw.Split('|'))
            {
                if (!string.IsNullOrEmpty(id) && _byId.ContainsKey(id))
                    _unlocked.Add(id);
            }
        }

        _activeId = PlayerPrefs.GetString(PREF_ACTIVE, _defaultBallId);
    }

    private void Save()
    {
        string raw = string.Join("|", _unlocked);
        PlayerPrefs.SetString(PREF_UNLOCKED, raw);
        PlayerPrefs.SetString(PREF_ACTIVE, _activeId);
        PlayerPrefs.Save();
    }
}
