using System;
using TMPro;
using UnityEngine;
using Firebase.Analytics;

public class BetController : MonoBehaviour
{
    [SerializeField] private HoldRepeatButton _plus;
    [SerializeField] private HoldRepeatButton _minus;
    [SerializeField] private TMP_Text _betText;

    [Header("Rules")]
    [SerializeField] private int _minBet = 1;
    [SerializeField] private int _maxBet = 999999;

    [SerializeField] private int _threshold = 100; // после 100
    [SerializeField] private int _bigStep = 10;    // шаг на удержании
    private int _holdChangeTickCounter = 0;
    private float _lastLimitLogTime = -999f;


    public int Bet { get; set; } = 0;

    private void Awake()
    {
        _plus.OnStep += HandleStep;
        _minus.OnStep += HandleStep;
        Refresh();
    }

    void Start()
    {
        int balance = (int)FindAnyObjectByType<BalanceManager>().GetBalance();

        Bet = Mathf.CeilToInt(balance / 12f);

        Bet = Mathf.Clamp(Bet, _minBet, Mathf.Min(_maxBet, balance));

        Refresh();

        AnalyticsLogger.Log("plinko_bet_auto_set",
            new Parameter("bet", (long)Bet)
        );

    }


    private void OnDestroy()
    {
        if (_plus != null) _plus.OnStep -= HandleStep;
        if (_minus != null) _minus.OnStep -= HandleStep;
    }


    private void HandleStep(int direction, bool isHoldTick)
    {
        int before = Bet;

        int step = 1;
        if (isHoldTick && Bet >= _threshold)
            step = _bigStep;

        int balance = Convert.ToInt32(FindAnyObjectByType<BalanceManager>().GetBalance());
        int desired = before + direction * step;

        int after = Mathf.Clamp(desired, _minBet, balance);

        if (after == before)
        {
            if (Time.unscaledTime - _lastLimitLogTime > 1f)
            {
                _lastLimitLogTime = Time.unscaledTime;

                string limitType = (before <= _minBet) ? "min" : "max_by_balance";

                AnalyticsLogger.Log("plinko_bet_change_limit_reached",
                    new Parameter("limit_type", limitType),
                    new Parameter("bet", (long)before)
                );
            }

            Refresh();
            return;
        }

        Bet = after;
        int actualChange = after - before;

        bool shouldLog = true;
        if (isHoldTick)
        {
            _holdChangeTickCounter++;
            shouldLog = (_holdChangeTickCounter % 5 == 0);
        }

        if (shouldLog)
        {
            AnalyticsLogger.Log("plinko_bet_change",
                new Parameter("bet", (long)Bet),
                new Parameter("change", (long)actualChange),
                new Parameter("is_hold", isHoldTick ? 1L : 0L),
                new Parameter("sample_rate", isHoldTick ? 5L : 1L)
            );
        }

        Refresh();
    }


    private void Refresh()
    {
        if (_betText != null)
            _betText.text = Bet.ToString();
    }
}
