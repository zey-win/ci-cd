using UnityEngine;

public class CarriageMover : MonoBehaviour
{
    [Header("Carriage visuals by mode")]
    [SerializeField] private Sprite _lineCarriageSprite; // объект/спрайт для Line
    [SerializeField] private Sprite _arcCarriageSprite;  // объект/спрайт для Arc
    [SerializeField] private SpriteRenderer _carriageField;

    public enum PathMode { Line, Arc }

    [Header("Path points (world space)")]
    [SerializeField] private Transform _left;
    [SerializeField] private Transform _bottom; // нужен только для Arc
    [SerializeField] private Transform _right;

    [Header("Path mode")]
    [SerializeField] private PathMode _mode = PathMode.Line;

    [Header("Visual that moves along path")]
    [SerializeField] private Transform _previewBall;

    [Header("Move")]
    [SerializeField] private float _speed = 0.8f;
    [SerializeField] private bool _playOnStart = true;

    [Header("Optional rolling")]
    [SerializeField] private float _ballRadius = 0.18f;
    [SerializeField] private bool _rollVisual = true;

    [Header("Preview visibility")]
    [SerializeField] private bool _hidePreviewInLine = true;
    private bool _forceHidePreview;

    private float _t;
    private int _dir = 1;
    private bool _running;

    private Vector3 _prevPos;

    public Vector3 CurrentWorldPos => (_previewBall != null) ? _previewBall.position : transform.position;
    public Vector3 CurrentWorldVelocity { get; private set; }
    public Vector2 CurrentWorldVelocity2D => new(CurrentWorldVelocity.x, CurrentWorldVelocity.y);

    public PathMode Mode => _mode;

    private void Start()
    {
        _running = _playOnStart;
        ApplyVisualByMode();
        ApplyPosition(teleport: true);
        OnChangeBallSkin();
    }

    public void ForceHidePreview(bool hide)
    {
        _forceHidePreview = hide;
        ApplyVisualByMode();
    }

    private void OnEnable()
    {
        if (BallSkinsManager.Instance != null)
            BallSkinsManager.Instance.OnActiveSkinChanged += OnChangeBallSkin;

        ApplyVisualByMode();
        ApplyPosition(teleport: true);
    }

    private void OnDisable()
    {
        if (BallSkinsManager.Instance != null)
            BallSkinsManager.Instance.OnActiveSkinChanged -= OnChangeBallSkin;
    }

    private void OnChangeBallSkin(string name = "")
    {
        if (_previewBall == null) return;
        if (BallSkinsManager.Instance == null) return;

        var skin = BallSkinsManager.Instance.GetActiveBall();
        if (skin == null) return;

        var sr = _previewBall.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = skin.Preview;
    }

    private void Update()
    {
        if (!_running)
            return;

        _t += _dir * _speed * Time.deltaTime;
        if (_t >= 1f) { _t = 1f; _dir = -1; }
        if (_t <= 0f) { _t = 0f; _dir = 1; }

        ApplyPosition();
    }

    public void SetPathMode(PathMode mode)
    {
        if (_mode == mode) return;
        _mode = mode;

        ApplyPosition(teleport: true);
        ApplyVisualByMode();
    }

    public void TogglePathMode()
    {
        SetPathMode(_mode == PathMode.Line ? PathMode.Arc : PathMode.Line);
    }

    private void ApplyPosition(bool teleport = false)
    {
        if (_left == null || _right == null) return;

        Vector3 a = _left.position;
        Vector3 c = _right.position;

        bool useArc = (_mode == PathMode.Arc) && (_bottom != null);

        Vector3 pos;
        Vector3 tangent;

        if (useArc)
        {
            Vector3 b = _bottom.position;
            pos = Bezier(a, b, c, _t);
            tangent = BezierDerivative(a, b, c, _t);
        }
        else
        {
            pos = Vector3.Lerp(a, c, _t);
            tangent = (c - a);
        }

        var target = (_previewBall != null) ? _previewBall : transform;
        target.position = pos;

        if (teleport)
        {
            _prevPos = pos;
            CurrentWorldVelocity = Vector3.zero;
            return;
        }

        var delta = pos - _prevPos;
        CurrentWorldVelocity = delta / Mathf.Max(Time.deltaTime, 0.000001f);

        if (_rollVisual && _previewBall != null && _ballRadius > 0.0001f)
        {
            var tNorm = tangent.sqrMagnitude > 0.000001f ? tangent.normalized : Vector3.right;

            float signedDist = Vector3.Dot(delta, tNorm);
            float angleDeg = (signedDist / _ballRadius) * Mathf.Rad2Deg;

            _previewBall.Rotate(0f, 0f, -angleDeg);
        }

        _prevPos = pos;
    }

    private void ApplyVisualByMode()
    {
        if (_carriageField != null)
            _carriageField.sprite = _mode == PathMode.Line ? _lineCarriageSprite : _arcCarriageSprite;

        if (_previewBall != null)
        {
            bool showPreview;

            if (_forceHidePreview)
                showPreview = false;
            else if (_hidePreviewInLine)
                showPreview = (_mode == PathMode.Arc);
            else
                showPreview = true;

            _previewBall.gameObject.SetActive(showPreview);
        }
    }



    private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        var ab = Vector3.Lerp(a, b, t);
        var bc = Vector3.Lerp(b, c, t);
        return Vector3.Lerp(ab, bc, t);
    }

    private static Vector3 BezierDerivative(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        return 2f * (1f - t) * (b - a) + 2f * t * (c - b);
    }

    public void StopMove() => _running = false;
    public void StartMove() => _running = true;




    public float SideFactor01
    {
        get
        {
            if (_left == null || _right == null) return 0f;

            float leftX = _left.position.x;
            float rightX = _right.position.x;
            float half = Mathf.Abs(rightX - leftX) * 0.5f;
            if (half < 0.0001f) return 0f;

            float centerX = (_bottom != null) ? _bottom.position.x : (leftX + rightX) * 0.5f;

            float x = CurrentWorldPos.x;
            return Mathf.Clamp01(Mathf.Abs(x - centerX) / half);
        }
    }

    public float MoveSignX
    {
        get
        {
            float s = Mathf.Sign(CurrentWorldVelocity.x);
            return s == 0f ? 1f : s;
        }
    }

}
