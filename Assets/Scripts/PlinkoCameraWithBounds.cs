using UnityEngine;

public class PlinkoCameraWithBounds : MonoBehaviour, IPlinkoCam
{
    [SerializeField] private Camera _cam;
    private IOffset _offset;

    private void Awake()
    {
        _offset = GetComponent<IOffset>();
    }

    public void ViewBoard(PlinkoBoard board)
    {
        var bounds = board.GetBounds();

        var cameraDistance = bounds.size.magnitude / (2f * Mathf.Tan(0.5f * _cam.fieldOfView * Mathf.Deg2Rad));

        var camTransform = _cam.transform;
        var camPosition = bounds.center - cameraDistance * camTransform.forward;
        _cam.transform.rotation = Quaternion.LookRotation(bounds.center - camPosition);

        var height = bounds.size.y;

        _cam.orthographicSize = height * .5f;

        var center = bounds.center;
        var offset = _offset?.GetOffset(board.rows) ?? Vector3.zero;

        center.z = camPosition.z;
        camPosition = center;
        camTransform.position = camPosition + offset;
    }
}