using System;
using System.Collections.Generic;
using Server;
using UnityEngine;
using Firebase.Analytics;

public class BalanceManager : MonoBehaviour
{
    private float _balance;
    private float _totalBet;
    public Action<float> OnBalanceChanged;
    public Action<float> OnBetChanged;
    public float GetBalance() => _balance;
    public float GetTotalBet() => _totalBet;
    private const float BalanceResetTrigger = 10_000_000f;
    private const float BalanceResetValue = 1_000_000f;
    private readonly Stack<IBetCommand> _betCommands = new Stack<IBetCommand>();
    private bool _isFirstBet = true;
    private int _coinsAddedLogCount = 0;
    [SerializeField] private ShopManager _shopManagerPrefab;


    private void Start()
    {
        SetBalance(PlayerPrefs.GetFloat("Balance", 1));

        if (_balance == 0)
        {
            AnalyticsLogger.Log("balance_zero_shop_auto_open");
            Instantiate(_shopManagerPrefab);
        }

    }

    public bool AddBet(float value)
    {
        if (value <= 0 || _balance - value < 0)
            return false;


        _balance -= value;
        _totalBet += value;

        ClampValues();
        OnBalanceChanged?.Invoke(_balance);
        OnBetChanged?.Invoke(_totalBet);
        return true;
    }

    public bool CheckBet(float value)
    {
        return !(value <= 0) && !(_balance - value < 0);
    }

    public void AddBetCommand(IBetCommand command)
    {
        if (_isFirstBet)
        {
            _betCommands.Clear();
            _isFirstBet = false;
        }

        _betCommands.Push(command);
        command.Execute();
    }

    public void UndoBetCommand()
    {
        if (_betCommands.Count == 0)
            return;
        _betCommands.Pop().Undo();
    }

    public void ClearBetCommand()
    {
        while (_betCommands.Count > 0)
            UndoBetCommand();
    }

    public void RebetBetCommand()
    {
        if (!_isFirstBet)
            return;
        var betCommands = new List<IBetCommand>(_betCommands.ToArray());
        foreach (var betCommand in betCommands)
        {
            betCommand.Execute();
        }
    }

    public void RemoveBet(float value)
    {
        if (value <= 0)
            return;

        _balance += value;
        _totalBet -= value;

        ClampValues();
        OnBalanceChanged?.Invoke(_balance);
        OnBetChanged?.Invoke(_totalBet);
    }

    public void AddWinnings(float value, string reason = "unknown")
    {
        if (value < 0) return;

        _balance += value;

        _coinsAddedLogCount++;

        if (ShouldLogCoinsAdded(_coinsAddedLogCount, out int sampleRate))
        {
            AnalyticsLogger.Log("coins_added",
                new Parameter("amount", (long)Mathf.RoundToInt(value)),
                new Parameter("reason", reason),
                new Parameter("sample_rate", sampleRate)
            );
        }


        ClampValues();
        OnBalanceChanged?.Invoke(_balance);
    }


    public void ResetBet()
    {
        _totalBet = 0;
        _isFirstBet = true;
        OnBetChanged?.Invoke(_totalBet);
    }

    public void SetBalance(float value)
    {
        if (value < 0)
        {
            _balance = 0;
            return;
        }

        _balance = value;
        _totalBet = 0;
        ClampValues();
        OnBalanceChanged?.Invoke(_balance);
        OnBetChanged?.Invoke(_totalBet);
    }


    public bool TrySpend(int amount, string reason = "unknown")
    {
        if (amount <= 0) return true;
        if (_balance < amount) return false;

        _balance -= amount;
        ClampValues();

        AnalyticsLogger.Log("coins_spent",
            new Parameter("amount", (long)amount),
            new Parameter("reason", reason)
        );

        OnBalanceChanged?.Invoke(_balance);

        return true;
    }


    private void ClampValues()
    {
        if (_balance < 0)
            _balance = 0;
        if (_totalBet < 0)
            _totalBet = 0;

        // === RESET LOGIC ===
        if (_balance >= BalanceResetTrigger)
        {
            float old = _balance;
            _balance = BalanceResetValue;

            AnalyticsLogger.Log("balance_reset_to_1m",
                new Parameter("old_balance", (long)Mathf.RoundToInt(old)),
                new Parameter("new_balance", (long)BalanceResetValue),
                new Parameter("trigger", (long)BalanceResetTrigger)
            );
        }

        PlayerPrefs.SetFloat("Balance", _balance);
    }


    private static bool ShouldLogCoinsAdded(int count, out int sampleRate)
    {
        if (count <= 50)
        {
            sampleRate = 1;
            return true;
        }

        if (count <= 500)
        {
            sampleRate = 10;
            return count % 10 == 0;
        }

        if (count <= 10_000)
        {
            sampleRate = 100;
            return count % 100 == 0;
        }

        sampleRate = 1000;
        return count % 1000 == 0;
    }
}