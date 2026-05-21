using System.Collections.Generic;
using Server;
using UnityEngine;
using Firebase.Analytics;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public PlinkoBoard plinkoBoard;

    private IMultiplierStripProvider _multiplierProvider;
    private IPlinkoCam _iPlinkoCam;

    private ConnectionManager _connection;
    public CanvasGroup modesCanvasGroup;

    private readonly List<PlinkoBall> _activeBalls = new();
    private bool _creatingBall;

    private BetController _betController;

    private int _betClickCount = 0;
    private int _createBallRequestCount = 0;

    [SerializeField] private ShopManager _shopManagerPrefab;


    private AdManager _adManager;
    [SerializeField] private float _autoSpinInterval = 0.15f;
    [SerializeField] private int _maxActiveBalls = 30;

    private Coroutine _autoSpinRoutine;
    public bool IsAutoSpin { get; private set; }



    // ===== ANALYTICS =====
    private void LogEvent(string name, params Parameter[] parameters)
    {
        AnalyticsSafe.LogEvent(name, parameters);
    }

    // ===== /ANALYTICS =====




    private void Awake()
    {
        _iPlinkoCam = GetComponent<IPlinkoCam>();
        _multiplierProvider = GetComponent<IMultiplierStripProvider>();
        _connection = GetComponent<ConnectionManager>();
        PlinkoBasket.OnResult += OnResult;
        _betController = FindFirstObjectByType<BetController>();
        _adManager = FindFirstObjectByType<AdManager>();
    }

    private AdManager GetAdManager()
    {
        if (_adManager == null)
            _adManager = FindFirstObjectByType<AdManager>();

        return _adManager;
    }


    private void OnDestroy()
    {
        PlinkoBasket.OnResult -= OnResult;
    }

    private void Start()
    {
        BuildBoard();
        _betController.Bet = 1;

        LogEvent("plinko_game_screen_open",
            new Parameter("rows", plinkoBoard != null ? plinkoBoard.rows : 0),
            new Parameter("max_multiplier", _connection.MaxMultiplier)
        );

        LaunchAnalytics.Instance?.NotifySessionReady();
    }

    public void OnResult(PlinkoResult result)
    {
        var isLastBall = _activeBalls.Count == 1;
        _activeBalls.Remove(result.ball);

        modesCanvasGroup.blocksRaycasts = isLastBall;
    }



    public void StartAutoSpin()
    {
        if (IsAutoSpin) return;

        IsAutoSpin = true;
        _autoSpinRoutine = StartCoroutine(AutoSpinLoop());

        LogEvent("plinko_autospin_on",
            new Parameter("interval", _autoSpinInterval)
        );
    }

    public void StopAutoSpin(string reason = "click")
    {
        if (!IsAutoSpin) return;

        IsAutoSpin = false;
        if (_autoSpinRoutine != null)
        {
            StopCoroutine(_autoSpinRoutine);
            _autoSpinRoutine = null;
        }

        // вернуть интерактивность, если реально можно
        modesCanvasGroup.blocksRaycasts = (_activeBalls.Count == 0 && !_creatingBall);

        LogEvent("plinko_autospin_off",
            new Parameter("reason", reason)
        );
    }

    private IEnumerator AutoSpinLoop()
    {
        while (IsAutoSpin)
        {
            TrySpinOnce(fromAuto: true);
            yield return new WaitForSecondsRealtime(_autoSpinInterval);
        }
    }

    /// <summary>
    /// Один бросок. Если autospin включён и вызывается клик — клик будет выключать автоспин (делай это снаружи).
    /// </summary>
    public void TrySpinOnce(bool fromAuto = false)
    {
        // защита от параллельных запросов
        if (_creatingBall) return;

        // опционально: ограничим кол-во шаров на доске
        if (_maxActiveBalls > 0 && _activeBalls.Count >= _maxActiveBalls) return;

        modesCanvasGroup.blocksRaycasts = false;
        _creatingBall = true;

        ThrowBallOnBoard(fromAuto);
    }

    // public void OnClickBet()
    // {
    //     _betClickCount++;

    //     if (_betClickCount % 200 == 0)
    //     {
    //         LogEvent("plinko_bet_click",
    //             new Parameter("bet", _betController != null ? _betController.Bet : 0),
    //             new Parameter("rows", plinkoBoard != null ? plinkoBoard.rows : 0),
    //             new Parameter("max_multiplier", _connection.MaxMultiplier),
    //             new Parameter("sample_rate", 200)
    //         );
    //     }


    //     modesCanvasGroup.blocksRaycasts = false;
    //     _creatingBall = true;
    //     ThrowBallOnBoard();
    // }

    public void BuildBoard(int rowsValue = 8)
    {
        if (_activeBalls.Count > 0) return;

        var rows = rowsValue;

        _connection.ChangeBoard(rows, (gateValues) =>
        {
            _multiplierProvider.ChangeMultipliers(gateValues);
            plinkoBoard.rows = rows;
            plinkoBoard.GenerateBoard();
            _multiplierProvider.SetMultipliers(plinkoBoard.BasketStrip.Strip.GetBaskets());
            _iPlinkoCam?.ViewBoard(plinkoBoard);
            LogEvent("plinko_board_built",
                new Parameter("rows", rows),
                new Parameter("max_multiplier", _connection.MaxMultiplier)
            );
        });
    }


    private void ThrowBallOnBoard(bool fromAuto)
    {
        var balanceMgr = FindAnyObjectByType<BalanceManager>();
        float balanceF = balanceMgr != null ? balanceMgr.GetBalance() : 0f;

        int bet = _betController != null ? _betController.Bet : 0;

        int balanceInt = Mathf.FloorToInt(balanceF);

        if (bet <= 0 || balanceInt < bet)
        {
            _creatingBall = false;
            if (IsAutoSpin) StopAutoSpin("not_enough_coins");
            string reason = bet <= 0 ? "bet_zero" : "not_enough_coins";
            LogEvent("plinko_bet_blocked_open_shop",
                new Parameter("block_reason", reason),
                new Parameter("bet", bet),
                new Parameter("balance", balanceInt)
            );
            Instantiate(_shopManagerPrefab);
            return;
        }

        _createBallRequestCount++;

        if (ShouldLogCreateBallRequest(_createBallRequestCount, out int sampleRate))
        {
            LogEvent("plinko_ball_create_request",
                new Parameter("bet", _betController != null ? _betController.Bet : 0),
                new Parameter("rows", plinkoBoard != null ? plinkoBoard.rows : 0),
                new Parameter("max_multiplier", _connection.MaxMultiplier),
                new Parameter("sample_rate", sampleRate)
            );
        }


        _connection.CreateBall(_betController.Bet, id =>
        {
            _creatingBall = false;
            modesCanvasGroup.blocksRaycasts = false;

            if (id == "") // OnError
            {
                var areNoBalls = _activeBalls.Count == 0 && !_creatingBall;
                modesCanvasGroup.blocksRaycasts = areNoBalls;
                LogEvent("plinko_ball_create_fail",
                    new Parameter("bet", _betController.Bet),
                    new Parameter("fail_reason", "server_error")
                );

                return;
            }

            var ball = BallPooling.Pool.Get();
            ball.Init(_betController.Bet, id);
            _activeBalls.Add(ball);
            plinkoBoard.ThrowBall(ball);

            if (!fromAuto)
                PayoutProgressManager.Instance?.AddPoint();

            OfferManager.Instance?.AddThrownBall();
            GetAdManager()?.RegisterInterstitialBallThrow();
        });
    }



    private static bool ShouldLogCreateBallRequest(int count, out int sampleRate)
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
