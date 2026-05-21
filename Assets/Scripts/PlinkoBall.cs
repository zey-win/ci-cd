using UnityEngine;

public class PlinkoBall : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _rb;
    [SerializeField] private SpriteRenderer _sprite;

    [Header("Colliders")]
    [SerializeField] private CircleCollider2D _circleCollider;
    [SerializeField] private EdgeCollider2D _edgeCollider;

    public Rigidbody2D Rb => _rb;
    public SpriteRenderer Sprite => _sprite;

    public float Bet { get; private set; }
    public string ID { get; private set; }

    private bool _cached;
    private float _defaultDrag;
    private float _defaultAngularDrag;
    private float _defaultGravityScale;
    private RigidbodyConstraints2D _defaultConstraints;
    private RigidbodyType2D _defaultBodyType;
    private bool _defaultSimulated;
    private bool _defaultCircleEnabled;
    private bool _defaultEdgeEnabled;

    private void Reset()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        if (_sprite == null) _sprite = GetComponentInChildren<SpriteRenderer>();
        if (_circleCollider == null) _circleCollider = GetComponent<CircleCollider2D>();
        if (_edgeCollider == null) _edgeCollider = GetComponent<EdgeCollider2D>();
    }

    private void Awake()
    {
        CacheDefaults();
    }

    private void CacheDefaults()
    {
        if (_cached) return;

        if (_rb == null) _rb = GetComponent<Rigidbody2D>();

        _defaultDrag = _rb.drag;
        _defaultAngularDrag = _rb.angularDrag;
        _defaultGravityScale = _rb.gravityScale;
        _defaultConstraints = _rb.constraints;
        _defaultBodyType = _rb.bodyType;
        _defaultSimulated = _rb.simulated;

        _defaultCircleEnabled = _circleCollider != null && _circleCollider.enabled;
        _defaultEdgeEnabled = _edgeCollider != null && _edgeCollider.enabled;

        _cached = true;
    }


    public void RestoreDefaults()
    {
        if (!_cached) CacheDefaults();
        if (_rb == null) return;

        _rb.drag = _defaultDrag;
        _rb.angularDrag = _defaultAngularDrag;
        _rb.gravityScale = _defaultGravityScale;
        _rb.constraints = _defaultConstraints;
        _rb.bodyType = _defaultBodyType;
        _rb.simulated = _defaultSimulated;

        _rb.velocity = Vector2.zero;
        _rb.angularVelocity = 0f;

        if (_circleCollider != null) _circleCollider.enabled = _defaultCircleEnabled;
        if (_edgeCollider != null) _edgeCollider.enabled = _defaultEdgeEnabled;

        _rb.WakeUp();
    }

    public void SetColliderType(BallColliderType type)
    {
        if (_circleCollider != null) _circleCollider.enabled = (type == BallColliderType.Circle);
        if (_edgeCollider != null) _edgeCollider.enabled = (type == BallColliderType.Edge);
    }


    public void Deactivate()
    {
        if (_rb != null)
        {
            _rb.velocity = Vector2.zero;
            _rb.angularVelocity = 0f;

            _rb.simulated = false;
            _rb.Sleep();
        }

        gameObject.SetActive(false);
    }

    public void Activate()
    {
        gameObject.SetActive(true);

        if (_rb != null)
        {
            _rb.simulated = true;
            RestoreDefaults();
        }
    }

    public void Init(float bet, string id)
    {
        Bet = bet;
        ID = id;
    }
}
