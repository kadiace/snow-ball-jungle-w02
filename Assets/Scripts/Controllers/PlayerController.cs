using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    static string INTERACT_GUIDE = "{Interact} 를 눌러 상호작용";
    static string RETURN_GUIDE = "{Return} 를 눌러 빠르게 복귀";

    [Header("Ground")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckDistance = 0.1f;
    [SerializeField, Range(0.5f, 1f)] private float _groundCheckRadiusRatio = 0.9f;
    [SerializeField, Range(0f, 90f)] private float _maxGroundAngle = 50f;

    [Header("Ball Stat")]
    [SerializeField] private BallStat _ballStat;

    [Header("Boundary")]
    [SerializeField] private float _boundaryRadius = 2500f;

    [Header("Jump")]
    [SerializeField] private float _coyoteTime = 0.1f;
    [SerializeField] private float _jumpBufferTime = 0.15f;
    [SerializeField] private float _jumpGroundedCheckLockTime = 0.15f;
    [SerializeField] private int _maxJumpCount;
    [SerializeField] private int _currentJumpCount;
    [SerializeField] private bool _canJump;

    [Header("Resize")]
    [SerializeField] private float _resizeSpeed = 2f;

    private Rigidbody _rb;
    private SphereCollider _collider;
    private PhysicsMaterial _physicsMaterial;
    private Transform _cameraTransform;
    private HapticManager _hapticManager;
    private JumpPanelController _jumpPanelController;

    private Vector2 _moveInput;
    private float _jumpBufferTimer;
    private float _jumpGroundedCheckLockTimer;
    private float _coyoteTimer;
    private GameObject _interactGuide;
    private GameObject _returnGuide;

    private Vector3 _gravityDir = Vector3.down;
    private Vector3 _groundNormal = Vector3.up;
    private bool _hasGroundContact;
    private Vector3 _contactGroundNormal = Vector3.up;

    public bool IsGrounded { get; private set; }
    public float MaxJumpCount => _maxJumpCount;
    public BallStat BallStat => _ballStat;

    public string CurrentTag { get; set; }
    public List<FacilityInteractionController> Facilities { get; private set; }

    private void Awake()
    {
        GameObject jumpCountCanvas = Instantiate(Resources.Load<GameObject>("Prefabs/UIs/JumpCountCanvas"));
        _jumpPanelController = jumpCountCanvas.GetComponent<JumpPanelController>();

        _interactGuide = Instantiate(Resources.Load<GameObject>("Prefabs/UIs/InteractGuide"));
        _interactGuide.SetActive(false);

        _returnGuide = Instantiate(Resources.Load<GameObject>("Prefabs/UIs/ReturnGuide"));
        _returnGuide.SetActive(false);

        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = true;

        _collider = GetComponent<SphereCollider>();
        _physicsMaterial = _collider.material;
        _hapticManager = GetComponent<HapticManager>();

        _cameraTransform = Camera.main.transform;

        ApplyCurrentSizeStat();

        _jumpPanelController.SetMaxJumps(_maxJumpCount);
        SetCurrentJumpCount(_maxJumpCount);

        Facilities = new();
    }

    private void Update()
    {
        ProcessMoveInput();
        ProcessJumpInput();
        ProcessInteractInput();
        ProcessReturnInput();
    }

    private void FixedUpdate()
    {
        CheckGround();
        Resize();

        ProcessJump();
        ApplyMovement();
        ClampGravityVelocity();

        RestrictPosition();
    }

    private void OnDisable()
    {
        _hapticManager.StopHaptic();
    }

    private void ProcessMoveInput()
    {
        _moveInput = GameInputController.Instance.MoveInput;
    }

    private void ProcessJumpInput()
    {
        if (GameInputController.Instance.JumpPressed)
            _jumpBufferTimer = _jumpBufferTime;
        else
            _jumpBufferTimer = Mathf.Max(0f, _jumpBufferTimer - Time.deltaTime);
    }

    private void ProcessInteractInput()
    {
        if (Facilities.Count <= 0)
        {
            _interactGuide.SetActive(false);
            return;
        }
        _interactGuide.SetActive(true);
        Text text = _interactGuide.transform.Find("Panel/Guide").GetComponent<Text>();
        string bindingName = Util.GetBindingName("Interact");
        string message = INTERACT_GUIDE.Replace("{Interact}", bindingName);
        text.text = message;

        if (!GameInputController.Instance.InteractPressed)
            return;

        FacilityInteractionController nearestFacility = NearestFacility();
        if (nearestFacility == null)
            return;

        nearestFacility.OpenCanvas();
    }

    private void ProcessReturnInput()
    {
        FacilityInteractionController nearestFacility = NearestFacility();
        if (nearestFacility == null || !Managers.Game.Researches.Contains(ResearchType.TowerWire) || nearestFacility is not TowerController tower || !tower.IsActivated)
            return;

        _returnGuide.SetActive(true);

        Text text = _returnGuide.transform.Find("Panel/Guide").GetComponent<Text>();
        string bindingName = Util.GetBindingName("Return");
        string message = RETURN_GUIDE.Replace("{Return}", bindingName);
        text.text = message;

        if (!GameInputController.Instance.ReturnPressed)
            return;

        tower.Return(this);
    }

    private FacilityInteractionController NearestFacility()
    {
        float minDistance = float.MaxValue;
        FacilityInteractionController nearestFacility = null;
        Facilities.ForEach(facility =>
        {
            float distance = Vector3.Distance(transform.position, facility.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestFacility = facility;
            }
        });
        return nearestFacility;
    }

    private void ApplyCurrentSizeStat()
    {
        transform.localScale = Vector3.one * Managers.Game.ResourcesData.Scale;
        _rb.mass = Managers.Game.ResourcesData.Mass;
        _physicsMaterial.bounciness = _ballStat.Bounciness;
    }

    private void CheckGround()
    {
        if (_jumpGroundedCheckLockTimer > 0f)
        {
            _jumpGroundedCheckLockTimer = Mathf.Max(0f, _jumpGroundedCheckLockTimer - Time.fixedDeltaTime);
            _coyoteTimer = 0f;
            _hasGroundContact = false;
            IsGrounded = false;
            _groundNormal = -_gravityDir;
            return;
        }

        float sphereRadius = _collider.radius * transform.lossyScale.x;
        float checkRadius = sphereRadius * _groundCheckRadiusRatio;
        float castDistance = sphereRadius - checkRadius + _groundCheckDistance;

        if (Physics.SphereCast(
            transform.position,
            checkRadius,
            _gravityDir,
            out RaycastHit hit,
            castDistance,
            _groundLayer,
            QueryTriggerInteraction.Ignore))
        {
            IsGrounded = true;
            _groundNormal = hit.normal;

            SetCurrentJumpCount(_maxJumpCount);
            _coyoteTimer = _coyoteTime;
        }
        else if (_hasGroundContact)
        {
            IsGrounded = true;
            _groundNormal = _contactGroundNormal;

            SetCurrentJumpCount(_maxJumpCount);
            _coyoteTimer = _coyoteTime;
        }
        else
        {
            IsGrounded = false;
            _groundNormal = -_gravityDir;
            _coyoteTimer = Mathf.Max(0f, _coyoteTimer - Time.fixedDeltaTime);
        }

        _hasGroundContact = false;
    }

    private void Resize()
    {
        int resourceCount = Managers.Game.Woods.Count + Managers.Game.Irons.Count;

        float targetMass = 3f + 0.5f * resourceCount;
        float targetScale = 6f + resourceCount;

        _rb.mass = targetMass;
        Managers.Game.ResourcesData.Mass = targetMass;

        float currentScale = transform.localScale.x;

        float nextScale = Mathf.MoveTowards(currentScale, targetScale,
            _resizeSpeed * Time.fixedDeltaTime);

        float changeScale = nextScale - currentScale;

        transform.localScale = Vector3.one * nextScale;
        Managers.Game.ResourcesData.Scale = nextScale;

        transform.position += Vector3.up * (0.5f * changeScale);

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            child.localScale = Vector3.one / nextScale;
        }
    }

    private void ProcessJump()
    {
        if (!_canJump)
            return;

        if (_jumpBufferTimer <= 0f)
            return;

        bool canCoyote = _coyoteTimer > 0f;

        if (!IsGrounded && !canCoyote && _currentJumpCount <= 0)
            return;

        _jumpBufferTimer = 0f;

        bool groundJump = IsGrounded || canCoyote;

        float jumpForce = _ballStat.JumpForce;

        if (groundJump)
        {
            Vector3 velocity = _rb.linearVelocity;
            velocity.y = 0f;
            _rb.linearVelocity = velocity;
            _rb.AddForce(-_gravityDir * jumpForce * _rb.mass, ForceMode.Impulse);
        }
        else
        {
            SetCurrentJumpCount(_currentJumpCount - 1);
            _rb.AddForce(-_gravityDir * jumpForce, ForceMode.Impulse);
        }

        _jumpGroundedCheckLockTimer = _jumpGroundedCheckLockTime;
    }

    private void ApplyMovement()
    {
        Vector3 worldMoveInput = GetWorldMoveInput(_moveInput);

        if (IsGrounded)
            Roll(worldMoveInput);
        else
            AirMove(worldMoveInput);
    }

    private Vector3 GetWorldMoveInput(Vector2 input)
    {
        Vector3 upDirection = -_gravityDir;
        Vector3 cameraForward = Vector3.ProjectOnPlane(_cameraTransform.forward, upDirection);

        if (cameraForward.sqrMagnitude < 0.001f)
            return Vector3.zero;

        cameraForward.Normalize();

        Vector3 cameraRight = Vector3.Cross(upDirection, cameraForward).normalized;
        Vector3 worldMoveInput = cameraForward * input.y + cameraRight * input.x;

        return Vector3.ClampMagnitude(worldMoveInput, 1f);
    }

    private void Roll(Vector3 worldMoveInput)
    {
        Vector3 groundMoveDirection = GetGroundMoveDirection(worldMoveInput);
        UpdateGroundMoveVelocity(groundMoveDirection, worldMoveInput.magnitude);
    }

    private Vector3 GetGroundMoveDirection(Vector3 worldMoveInput)
    {
        Vector3 groundMoveDirection = Vector3.ProjectOnPlane(worldMoveInput, _groundNormal);

        if (groundMoveDirection.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return groundMoveDirection.normalized;
    }

    private void UpdateGroundMoveVelocity(Vector3 groundMoveDirection, float inputMagnitude)
    {
        inputMagnitude = Mathf.Clamp01(inputMagnitude);

        if (inputMagnitude <= 0.001f)
            return;

        float moveSpeed = Managers.Game.Researches.Contains(ResearchType.MoveFast) ?
             _ballStat.MoveSpeed * 1.5f : _ballStat.MoveSpeed;
        float moveResponseTime = _ballStat.MoveResponseTime;

        Vector3 targetVelocity = groundMoveDirection * moveSpeed * inputMagnitude;
        Vector3 currentVelocity = Vector3.ProjectOnPlane(_rb.linearVelocity, _groundNormal);
        Vector3 velocityDelta = targetVelocity - currentVelocity;

        Vector3 responseAcceleration = velocityDelta / moveResponseTime;
        Vector3 velocityChange = responseAcceleration * Time.fixedDeltaTime;
        velocityChange = Vector3.ClampMagnitude(velocityChange, velocityDelta.magnitude);

        Vector3 acceleration = velocityChange / Time.fixedDeltaTime;
        _rb.AddForce(acceleration, ForceMode.Acceleration);
    }

    private void AirMove(Vector3 worldMoveInput)
    {
        float inputMagnitude = Mathf.Clamp01(worldMoveInput.magnitude);

        if (inputMagnitude <= 0.001f)
            return;

        float moveSpeed = Managers.Game.Researches.Contains(ResearchType.MoveFast) ?
             _ballStat.MoveSpeed * 1.5f : _ballStat.MoveSpeed;
        float moveAcceleration = _ballStat.MoveAcceleration;

        Vector3 moveDirection = worldMoveInput.normalized;
        float targetSpeed = moveSpeed * inputMagnitude;

        Vector3 planarVelocity = Vector3.ProjectOnPlane(_rb.linearVelocity, _gravityDir);
        float currentSpeed = Vector3.Dot(planarVelocity, moveDirection);

        if (currentSpeed >= targetSpeed)
            return;

        float remainingSpeed = targetSpeed - currentSpeed;
        float acceleration = moveAcceleration * inputMagnitude;
        float velocityChange = Mathf.Min(acceleration * Time.fixedDeltaTime, remainingSpeed);

        acceleration = velocityChange / Time.fixedDeltaTime;
        _rb.AddForce(moveDirection * acceleration, ForceMode.Acceleration);
    }

    private void ClampGravityVelocity()
    {
        float maxGravityVelocity = _ballStat.MaxGravityVelocity;
        float gravitySpeed = Vector3.Dot(_rb.linearVelocity, _gravityDir);

        if (gravitySpeed <= maxGravityVelocity)
            return;

        Vector3 excessVelocity = _gravityDir * (gravitySpeed - maxGravityVelocity);
        _rb.linearVelocity -= excessVelocity;
    }

    private void RestrictPosition()
    {
        Vector3 position = _rb.position;
        position.x = MathF.Min(_rb.position.x, _boundaryRadius);
        position.z = MathF.Min(_rb.position.z, _boundaryRadius);
        _rb.position = position;
    }

    private void OnCollisionEnter(Collision collision)
    {
        float clampedImpulseData = Mathf.Clamp(collision.impulse.magnitude, 70, 100);
        float intensity = Mathf.InverseLerp(0f, 100f, clampedImpulseData);
        _hapticManager.HapticControl(intensity * 3, 0f, 0.15f);
    }

    private void OnCollisionStay(Collision collision)
    {
        if ((_groundLayer.value & (1 << collision.gameObject.layer)) == 0)
            return;

        float bestGroundDot = Mathf.Cos(_maxGroundAngle * Mathf.Deg2Rad);
        Vector3 bestGroundNormal = Vector3.zero;
        bool hasGroundContact = false;

        foreach (ContactPoint contact in collision.contacts)
        {
            float groundDot = Vector3.Dot(contact.normal, Vector3.up);

            if (groundDot < bestGroundDot)
                continue;

            bestGroundDot = groundDot;
            bestGroundNormal = contact.normal;
            hasGroundContact = true;
        }

        if (!hasGroundContact)
            return;

        _hasGroundContact = true;
        _contactGroundNormal = bestGroundNormal;
    }

    private void SetCurrentJumpCount(int currentJumpCount)
    {
        _jumpPanelController.SetCurrentJumps(currentJumpCount);
        _currentJumpCount = currentJumpCount;
    }
}
