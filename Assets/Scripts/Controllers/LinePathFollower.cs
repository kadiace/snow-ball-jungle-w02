
using UnityEngine;

public class LinePathFollower : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 200f;

    private Vector3[] _points;
    private float[] _cumulativeLengths;
    private float _totalLength;
    private float _distance;
    private bool _isArrived;

    public LineRenderer LineRenderer;

    private void Start()
    {
        int count = LineRenderer.positionCount;

        if (count < 2)
        {
            enabled = false;
            return;
        }

        _points = new Vector3[count];
        _cumulativeLengths = new float[count];

        LineRenderer.GetPositions(_points);

        _cumulativeLengths[0] = 0f;

        for (int i = 1; i < count; i++)
        {
            _cumulativeLengths[i] =
                _cumulativeLengths[i - 1] +
                Vector3.Distance(_points[i - 1], _points[i]);
        }

        _totalLength = _cumulativeLengths[count - 1];
    }

    private void Update()
    {
        if (_isArrived)
            return;

        if (_distance >= _totalLength || new Vector2(transform.position.x, transform.position.z).magnitude <= 55f)
        {
            OnArrived();
            return;
        }

        _distance += _moveSpeed * Time.deltaTime;
        _distance = Mathf.Min(_distance, _totalLength);

        for (int i = 1; i < _points.Length; i++)
        {
            if (_distance > _cumulativeLengths[i])
                continue;

            float segmentLength =
                _cumulativeLengths[i] -
                _cumulativeLengths[i - 1];

            float t = segmentLength > 0f
                ? (_distance - _cumulativeLengths[i - 1])
                    / segmentLength
                : 0f;

            Vector3 localPosition =
                Vector3.Lerp(_points[i - 1], _points[i], t);

            transform.position = LineRenderer.useWorldSpace
                ? localPosition
                : LineRenderer.transform.TransformPoint(localPosition);

            break;
        }
    }

    private void OnArrived()
    {
        if (_isArrived)
            return;

        _isArrived = true;

        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        rb.isKinematic = false;

        PlayerController player = gameObject.GetComponent<PlayerController>();
        player.enabled = true;

        enabled = false;
    }
}