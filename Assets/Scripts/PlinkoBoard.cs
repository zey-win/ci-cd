using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Firebase.Analytics;

public class PlinkoBoard : MonoBehaviour
{
    [Header("Skillfactor UI")]
    [SerializeField] private GameObject _skillfactorSwitchRoot;
    [SerializeField] private RectTransform _skillfactorLeverImage;

    [SerializeField] private bool _defaultArcPath = true;

    private bool _skillfactorAvailable;
    private bool _useArcPath;

    private const string PrefPathKey = "plinko_carriage_path_arc";



    [SerializeField] private float _inheritVelocityMultiplier = 1.0f;
    [SerializeField] private float _releaseDownSpeed = 0.5f;
    [SerializeField] private bool _inheritOnlyX = true;


    [SerializeField] private CarriageMover _carriage;
    [SerializeField] private Transform _carriageSpawnPoint;
    [SerializeField] private Vector3 _ballOffset = Vector3.zero;

    [Range(2, 20)] public int rows = 5;
    [Range(.1f, 2f)] public float scale = 1;
    [Range(.1f, 2f)] public float rowSpacing = 1.5f;

    [SerializeField] private GameObject _obstaclePrefab;
    [SerializeField] private PlinkoBasket _basketPrefab;

    private const float ObstacleSpacing = 1.0f;
    private const float StartX = -2.0f;
    private const float StartY = 2.0f;

    private Vector3[] _ballPositions;
    public BasketStrip BasketStrip { get; private set; }

    private Coroutine _spawnRoutine;
    private bool _isSpawning;
    private float _minSpawnX;
    private float _maxSpawnX;


    [SerializeField] private float _ballSpawnYOffset = 1.0f;

    [SerializeField] private float _basketSpawnYOffset = 1.0f;
    [SerializeField] private float _basketSpawnMoveTime = 0.25f;
    [SerializeField] private float _basketSpawnFadeTime = 0.25f;
    [SerializeField] private float _basketSpawnDelayBetween = 0.05f;




    [SerializeField] private float _boardYOffsetWithAds = -1.0f;
    [SerializeField] private float _boardYOffsetNoAds = 0.0f;
    private float _boardYOffset;



    private readonly List<BasketSpawnInfo> _spawnList = new();



    private void LogEvent(string name, params Parameter[] parameters)
    {
        AnalyticsSafe.LogEvent(name, parameters);
    }


    private void LogCarriageMode(string eventName, bool isArc, string reason, bool? fromArc = null)
    {
        var list = new List<Parameter>(8)
    {
        new Parameter("mode", isArc ? "arc" : "line"),
        new Parameter("reason", reason),
        new Parameter("rows", rows),
        new Parameter("skillfactor_available", _skillfactorAvailable ? 1 : 0),
        new Parameter("no_ads", NoAdsManager.IsOwned ? 1 : 0),
    };

        if (fromArc.HasValue)
            list.Add(new Parameter("from_mode", fromArc.Value ? "arc" : "line"));

        LogEvent(eventName, list.ToArray());
    }


    private void Awake()
    {
        BasketStrip = GetComponent<BasketStrip>();
    }


    private void OnEnable()
    {
        if (_carriage != null)
            _carriage.ForceHidePreview(true);

        StartCoroutine(InitSkillfactorCoroutine());
    }


    private IEnumerator InitSkillfactorCoroutine()
    {
        yield return RemoteConfigManager.EnsureReadyCoroutine();

        _skillfactorAvailable = RemoteConfigManager.SkillfactorEnabled;

        if (!_skillfactorAvailable)
        {
            if (_skillfactorSwitchRoot != null) _skillfactorSwitchRoot.SetActive(false);

            if (_carriage != null)
            {
                _carriage.gameObject.SetActive(true);                 // каретка видна
                _carriage.SetPathMode(CarriageMover.PathMode.Line);   // любой “базовый” вид (тарелка)
                _carriage.ForceHidePreview(true);                     // шарик не показываем
                _carriage.StopMove();                                 // чтобы вообще не гонять логику движения
            }

            yield break;
        }

        if (_skillfactorSwitchRoot != null) _skillfactorSwitchRoot.SetActive(true);
        if (_carriage != null)
        {
            _carriage.ForceHidePreview(false); // снова разрешаем шарик-превью
            _carriage.StartMove();             // на случай, если он был остановлен раньше
        }

        _useArcPath = PlayerPrefs.GetInt(PrefPathKey, _defaultArcPath ? 1 : 0) == 1;

        ApplyLeverVisual();
        ApplyCarriagePath();

        LogCarriageMode("plinko_carriage_mode_init", _useArcPath, "init");
    }


    private void ApplyCarriagePath()
    {
        if (_carriage == null) return;
        _carriage.SetPathMode(_useArcPath ? CarriageMover.PathMode.Arc : CarriageMover.PathMode.Line);
    }

    private void ApplyLeverVisual()
    {
        if (_skillfactorLeverImage == null) return;

        var s = _skillfactorLeverImage.localScale;
        s.x = Mathf.Abs(s.x) * (_useArcPath ? 1f : -1f);
        _skillfactorLeverImage.localScale = s;
    }


    public void OnLeverClicked()
    {
        if (!_skillfactorAvailable) return;
        bool fromArc = _useArcPath;

        _useArcPath = !_useArcPath;
        PlayerPrefs.SetInt(PrefPathKey, _useArcPath ? 1 : 0);
        PlayerPrefs.Save();

        ApplyLeverVisual();
        ApplyCarriagePath();

        LogCarriageMode("plinko_carriage_mode_toggle", _useArcPath, "lever", fromArc);
    }








    public void GenerateBoard()
    {
        _boardYOffset = (NoAdsManager.IsOwned && _skillfactorAvailable) ? _boardYOffsetNoAds : _boardYOffsetWithAds;

        _basketSpawnYOffset = _skillfactorAvailable == false ? 0.3f : 1.6f;

        CancelBasketSpawn();

        DestroyBoard();
        GenerateBallPositions(_boardYOffset);
        BasketStrip.Strip.Clear();
        _spawnList.Clear();

        var currentY = StartY + _boardYOffset;

        for (var i = 0; i <= rows; i++)
        {
            var numObstacles = i + 3;
            var rowWidth = ObstacleSpacing * (numObstacles - 1);
            var rowStartX =
                StartX - (rowWidth / 2.0f) +
                (ObstacleSpacing / 2.0f);

            currentY -= rowSpacing;

            if (i != rows) DrawObstacleRow(numObstacles, rowStartX, currentY);
            else DrawBasketRow(numObstacles, rowStartX, currentY);
        }

        BasketStrip.ColorizeBasket();
        _spawnRoutine = StartCoroutine(AnimateBasketsSpawn());
        CacheSpawnXRange();
    }



    private void CancelBasketSpawn()
    {
        _isSpawning = false;

        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }

        foreach (Transform child in transform)
        {
            child.DOKill(true);
        }
    }



    private IEnumerator AnimateBasketsSpawn()
    {
        _isSpawning = true;

        for (int i = 0; i < _spawnList.Count; i++)
        {
            if (!_isSpawning) yield break;

            var info = _spawnList[i];
            if (info.basket == null) continue;

            var t = info.basket.PlaySpawnTo(info.targetPos, _basketSpawnMoveTime, _basketSpawnFadeTime);
            yield return t.WaitForCompletion();

            if (_basketSpawnDelayBetween > 0f)
                yield return new WaitForSeconds(_basketSpawnDelayBetween);
        }

        _isSpawning = false;
        _spawnRoutine = null;
    }



    public void ThrowBall(PlinkoBall ball)
    {
        if (!_skillfactorAvailable)
        {
            ThrowBall_Classic(ball);
            return;
        }

        if (!_useArcPath)
        {
            ThrowBall_RandomStraight(ball);
            return;
        }

        ThrowBall_Skillfactor(ball);
    }



    private void CacheSpawnXRange()
    {
        _minSpawnX = float.PositiveInfinity;
        _maxSpawnX = float.NegativeInfinity;

        for (int i = 0; i < _ballPositions.Length; i++)
        {
            float x = _ballPositions[i].x;
            if (x < _minSpawnX) _minSpawnX = x;
            if (x > _maxSpawnX) _maxSpawnX = x;
        }
    }



    private void ThrowBall_Skillfactor(PlinkoBall ball)
    {
        Vector3 spawnPos = _carriage != null ? _carriage.CurrentWorldPos + _ballOffset : _ballPositions[0];
        ball.transform.position = spawnPos;
        ball.Rb.velocity = Vector2.zero;
        ball.Rb.angularVelocity = 0f;

        ball.Rb.position = spawnPos;
        ball.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        Vector2 v = Vector2.zero;

        if (_carriage != null)
        {
            var cv = _carriage.CurrentWorldVelocity2D * _inheritVelocityMultiplier;
            v = _inheritOnlyX ? new Vector2(cv.x, 0f) : cv;
        }

        v += Vector2.down * _releaseDownSpeed;

        ball.Rb.velocity = v;

        ball.Rb.angularVelocity = Random.Range(0.01f, 0.1f);
        ball.gameObject.SetActive(true);
    }



    private void ThrowBall_Classic(PlinkoBall ball)
    {
        if (ball == null)
            return;

        if (_ballPositions == null || _ballPositions.Length == 0) return;

        float x = Random.Range(_minSpawnX, _maxSpawnX);

        Vector3 pos = _ballPositions[0];
        pos.x = x;

        ball.transform.position = pos;

        ball.Rb.velocity = Vector2.zero;
        ball.Rb.angularVelocity = 0f;

        ball.transform.position = pos;
        ball.Rb.angularVelocity = Random.Range(0.01f, 0.1f);


        ball.gameObject.SetActive(true);
    }


    private void ThrowBall_RandomStraight(PlinkoBall ball)
    {
        if (_ballPositions == null || _ballPositions.Length == 0) return;

        float x = Random.Range(_minSpawnX, _maxSpawnX);

        float y = (_carriage != null) ? _carriage.CurrentWorldPos.y - _ballSpawnYOffset : _ballPositions[0].y - _ballSpawnYOffset;

        Vector3 spawnPos = new Vector3(x, y, _ballPositions[0].z) + _ballOffset;

        ball.transform.position = spawnPos;
        ball.Rb.position = spawnPos;

        ball.Rb.velocity = Vector2.down * _releaseDownSpeed;
        ball.Rb.angularVelocity = Random.Range(0.01f, 0.1f);

        ball.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        ball.gameObject.SetActive(true);
    }




    private void GenerateBallPositions(float yOffset)
    {
        var currentY = StartY + yOffset;

        const int positions = 2;
        const float rowWidth = ObstacleSpacing * (positions - 1);
        const float rowStartX = StartX - (rowWidth / 2.0f) + (ObstacleSpacing / 2.0f);

        currentY += rowSpacing;

        SetBallPositions(positions, rowStartX, currentY);
    }


    private void SetBallPositions(int numberOfPositions, float currentX, float currentY)
    {
        _ballPositions = new Vector3[numberOfPositions];

        for (var j = 0; j < numberOfPositions; j++)
        {
            var pos = transform.position + new Vector3(currentX, currentY, 0);
            _ballPositions[j] = pos;
            currentX += ObstacleSpacing;
        }
    }

    private void DrawObstacleRow(int numObstacles, float currentX, float currentY)
    {
        for (var j = 0; j < numObstacles; j++)
        {
            var obstaclePosition = transform.position + new Vector3(currentX, currentY, 0);
            var obs = Instantiate(_obstaclePrefab, obstaclePosition, Quaternion.identity);
            obs.transform.SetParent(transform);
            obs.transform.localScale = Vector3.one * scale;
            currentX += ObstacleSpacing;
        }
    }

    private void DrawBasketRow(int numObstacles, float currentX, float currentY)
    {
        currentX += ObstacleSpacing;

        for (var j = 1; j < numObstacles - 1; j++)
        {
            var targetPos = transform.position + new Vector3(currentX, currentY, 0);
            var basket = Instantiate(_basketPrefab, targetPos, Quaternion.identity);

            Transform basketTransform;
            (basketTransform = basket.transform).SetParent(transform);
            basketTransform.localScale *= scale;

            basket.audioSource.pitch = .7f + (j * 0.1f);

            basket.SetAlphaInstant(0f);
            basket.transform.position = targetPos + Vector3.down * _basketSpawnYOffset;

            _spawnList.Add(new BasketSpawnInfo { basket = basket, targetPos = targetPos });

            currentX += ObstacleSpacing;
            BasketStrip.Strip.AddBasket(basket);
        }
    }






    private void DestroyBoard()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
    }

    public Bounds GetBounds()
    {
        var bounds = new Bounds();
        for (var j = 0; j < transform.childCount; j++)
        {
            var obs = transform.GetChild(j);
            bounds.Encapsulate(obs.position);
        }

        bounds.Expand(3);
        return bounds;
    }





}

public class BasketSpawnInfo
{
    public PlinkoBasket basket;
    public Vector3 targetPos;
}