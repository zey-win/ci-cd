using UnityEngine;

public class PlinkoCameraHard : MonoBehaviour, IPlinkoCam
{
    [SerializeField] private Camera _cam;
    private IOffset _offset;

    public float zoomMultiplier = 4.5f;

    [Header("Extra camera offset")]
    [SerializeField] private float extraYOffsetWithAds = 2.5f;
    [SerializeField] private float extraYOffsetNoAds = 3.5f;

    private float _extraYOffsetCurrent;

    private PlinkoBoard _lastBoard;


    private void Awake()
    {
        _offset = GetComponent<IOffset>();
        ApplyNoAdsState(NoAdsManager.IsOwned);
    }

    private void OnEnable()
    {
        NoAdsManager.OnChanged += ApplyNoAdsState;
    }

    private void OnDisable()
    {
        NoAdsManager.OnChanged -= ApplyNoAdsState;
    }

    private void ApplyNoAdsState(bool noAdsOwned)
    {
        _extraYOffsetCurrent = noAdsOwned ? extraYOffsetNoAds : extraYOffsetWithAds;

        if (_lastBoard != null)
            ViewBoard(_lastBoard);
    }

    public void ViewBoard(PlinkoBoard board)
    {
        _lastBoard = board;

        var bounds = board.GetBounds();

        var cameraDistance = bounds.size.magnitude / (2f * Mathf.Tan(0.5f * _cam.fieldOfView * Mathf.Deg2Rad));

        var camTransform = _cam.transform;
        var camPosition = bounds.center - cameraDistance * camTransform.forward;

        _cam.transform.rotation = Quaternion.LookRotation(bounds.center - camPosition);

        var height = bounds.size.y;
        _cam.orthographicSize = height * .5f * zoomMultiplier;

        var center = bounds.center;
        var offset = _offset?.GetOffset(board.rows) ?? Vector3.zero;

        offset.y += _extraYOffsetCurrent * height * 0.1f;

        center.z = camPosition.z;
        camPosition = center;

        camTransform.position = camPosition + offset;
    }
}
