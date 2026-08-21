using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public enum MovementState { Normal, Aiming }

    [Header("Input & Camera Reference")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _rootTransform;
    [SerializeField] private Camera _mainCamera;

    [Header("Movement Speeds")]
    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _runSpeed = 9f;
    [SerializeField] private float _aimSpeed = 2.5f;
    [SerializeField] private float _rotationSpeed = 720f;
    [SerializeField] private float _aimRotationSpeed = 1200f;

    [Header("Deceleration & Physics")]
    [SerializeField] private float _maxSlopeAngle = 45f;

    [SerializeField] private float _stopDecelerationTime = 0.5f;
    [SerializeField] private float _sprintToWalkDeceleration = 8f;
    [SerializeField] private float _cardinalTurnDeceleration = 25f;
    [SerializeField] private float _groundSnapForce = -5f;
    [SerializeField] private float _gravity = -19.62f;
    [SerializeField] private LayerMask _aimRaycastMask = ~0;

    private CharacterController _characterController;
    private Vector3 _currentVelocity;
    private Vector3 _lastDirection;
    private float _currentSpeed;
    private float _stopTimer;
    private bool _isHardStopping;
    private float _verticalVelocity;
    private Vector3 _groundNormal = Vector3.up;

    public MovementState CurrentState { get; private set; } = MovementState.Normal;
    public Vector3 AimTargetPoint { get; private set; }
    public Vector3 AimDirection { get; private set; }
    public float CurrentSpeed => _currentSpeed;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        if (_mainCamera == null) _mainCamera = Camera.main;
    }

    private void Update()
    {
        UpdateGroundInfo();
        UpdateState();
        
        if (CurrentState == MovementState.Normal)
        {
            HandleNormalMovement();
        }
        else
        {
            HandleAimMovement();
        }
    }

    private void UpdateState()
    {
        CurrentState = _inputReader.IsAiming ? MovementState.Aiming : MovementState.Normal;
    }

    private void UpdateGroundInfo()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.2f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 0.5f))
        {
            _groundNormal = hit.normal;
        }
        else
        {
            _groundNormal = Vector3.up;
        }
    }

    private void HandleNormalMovement()
    {
        Vector2 rawInput = _inputReader.MoveInput;
        Vector3 cameraForward = Vector3.Scale(_rootTransform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 cameraRight = Vector3.Scale(_rootTransform.right, new Vector3(1, 0, 1)).normalized;
        
        Vector3 worldInputDirection = (cameraForward * rawInput.y + cameraRight * rawInput.x).normalized;
        Vector3 targetDirection = SnapTo8Directions(worldInputDirection);

        bool hasInput = targetDirection.sqrMagnitude > 0.01f;
        float targetMaxSpeed = _inputReader.IsSprinting ? _runSpeed : _walkSpeed;

        // Check slope steepness limit
        float slopeAngle = Vector3.Angle(_groundNormal, Vector3.up);
        bool isSlopeTooSteep = slopeAngle > _maxSlopeAngle;

        if (hasInput && !isSlopeTooSteep)
        {
            _stopTimer = 0f;
            float angleDifference = Vector3.Angle(_lastDirection, targetDirection);
            if (_lastDirection != Vector3.zero && angleDifference > 135f)
            {
                _isHardStopping = true;
            }

            if (_isHardStopping)
            {
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, _cardinalTurnDeceleration * Time.deltaTime);
                if (_currentSpeed <= 0.1f)
                {
                    _isHardStopping = false;
                    _lastDirection = targetDirection;
                }
            }
            else
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);

                float accelRate = (_currentSpeed > targetMaxSpeed) ? _sprintToWalkDeceleration : 12f;
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetMaxSpeed, accelRate * Time.deltaTime);
                _lastDirection = transform.forward;
            }
        }
        else
        {
            _isHardStopping = false;
            ApplyDeceleration();
        }

        ApplyMovementVelocity(_lastDirection != Vector3.zero ? _lastDirection : transform.forward);
    }

    private void HandleAimMovement()
    {
        // 1. Raycast from cursor to ground to determine Aim Point & Orientation
        Ray ray = _mainCamera.ScreenPointToRay(_inputReader.MousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, _aimRaycastMask))
        {
            AimTargetPoint = hit.point;
        }
        else
        {
            Plane groundPlane = new Plane(Vector3.up, transform.position);
            if (groundPlane.Raycast(ray, out float enter))
            {
                AimTargetPoint = ray.GetPoint(enter);
            }
        }

        Vector3 aimDir = (AimTargetPoint - transform.position);
        aimDir.y = 0;
        if (aimDir.sqrMagnitude > 0.01f)
        {
            AimDirection = aimDir.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(AimDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _aimRotationSpeed * Time.deltaTime);
        }

        // 2. Strafe Movement (Relative to Camera)
        Vector2 rawInput = _inputReader.MoveInput;
        Vector3 cameraForward = Vector3.Scale(_rootTransform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 cameraRight = Vector3.Scale(_rootTransform.right, new Vector3(1, 0, 1)).normalized;
        
        Vector3 moveDir = (cameraForward * rawInput.y + cameraRight * rawInput.x).normalized;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, _aimSpeed, 12f * Time.deltaTime);
            _lastDirection = moveDir;
        }
        else
        {
            ApplyDeceleration();
        }

        ApplyMovementVelocity(moveDir.sqrMagnitude > 0.01f ? moveDir : _lastDirection);
    }

    private void ApplyDeceleration()
    {
        if (_stopTimer < _stopDecelerationTime)
        {
            _stopTimer += Time.deltaTime;
            float progress = _stopTimer / _stopDecelerationTime;
            _currentSpeed = Mathf.Lerp(_currentSpeed, 0f, progress);
        }
        else
        {
            _currentSpeed = 0f;
        }
    }

    private void ApplyMovementVelocity(Vector3 direction)
    {
        Vector3 slopeProjectedDirection = Vector3.ProjectOnPlane(direction, _groundNormal).normalized;
        Vector3 horizontalVelocity = slopeProjectedDirection * _currentSpeed;

        if (_characterController.isGrounded)
        {
            _verticalVelocity = _groundSnapForce;
        }
        else
        {
            _verticalVelocity += _gravity * Time.deltaTime;
        }

        _currentVelocity = horizontalVelocity;
        _currentVelocity.y += _verticalVelocity;

        _characterController.Move(_currentVelocity * Time.deltaTime);
    }

    private Vector3 SnapTo8Directions(Vector3 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return Vector3.zero;

        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        float snappedAngle = Mathf.Round(angle / 45f) * 45f;
        
        return new Vector3(Mathf.Sin(snappedAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(snappedAngle * Mathf.Deg2Rad));
    }
}