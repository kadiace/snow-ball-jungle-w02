using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Wheel : MonoBehaviour
{
    private List<Rigidbody> _wings;
    private float _prevYAngle;
    private float _lastRotateTime;
    private bool _isRotating;

    [SerializeField] private float _stopDelay = 0.2f;

    void Awake()
    {
        _wings = new List<Rigidbody>();
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
        {
            if (rb.gameObject.GetComponent<Wheel>() == null)
            {
                _wings.Add(rb);
            }
        }
        _prevYAngle = transform.eulerAngles.y;
    }

    void Update()
    {
        float currentYAngle = transform.eulerAngles.y;
        float deltaAngle = Mathf.Abs(Mathf.DeltaAngle(_prevYAngle, currentYAngle));

        if (deltaAngle > 0.05f)
        {
            _lastRotateTime = Time.time;
            if (!_isRotating)
            {
                _isRotating = true;
                Managers.Game.EnergyGain += 10;
            }

        }
        else if (_isRotating && Time.time - _lastRotateTime >= _stopDelay)
        {
            _isRotating = false;
            Managers.Game.EnergyGain -= 10;
        }

        _prevYAngle = currentYAngle;
    }

    public void SetRigidbodyConstraintsAllWings(bool collisionStay)
    {
        if (collisionStay)
        {
            foreach (Rigidbody rb in _wings)
            {
                rb.constraints = RigidbodyConstraints.None;
            }
        }
        else
        {
            foreach (Rigidbody rb in _wings)
            {
                rb.constraints = RigidbodyConstraints.FreezePosition;
            }
        }
    }
}
