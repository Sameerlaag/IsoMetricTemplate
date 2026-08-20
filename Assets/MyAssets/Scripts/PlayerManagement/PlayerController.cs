using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _rootTransform;

    [Header("Movement Speeds")]
    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _runSpeed = 9f;
    [SerializeField] private float _rotationSpeed = 720f;

    [Header("Deceleration Settings")]
    [SerializeField] private float _stopDecelerationTime = 0.5f;
    [SerializeField] private float _sprintToWalkDeceleration = 8f;
    [SerializeField] private float _cardinalTurnDeceleration = 25f;

    [Header("Verticality & Slope Settings")]
    [SerializeField] private float _maxSlopeAngle = 45f;
    [SerializeField] private float _groundSnapForce = -5f;
    [SerializeField] private float _gravity = -19.62f; // Elevated gravity for tighter grounding feeling
    [SerializeField] private LayerMask _groundLayer = ~0; // Default to Everything

    // Component References
    private CharacterController _characterController;

    // State Variables
    private Vector3 _currentVelocity;
    private Vector3 _lastDirection;
    private float _currentSpeed;
    private float _stopTimer;
    private bool _isHardStopping;
    private float _verticalVelocity;
    private Vector3 _groundNormal = Vector3.up;

    // Properties exposed for future Animation Controllers (Mixamo / Animator)
    public float CurrentSpeed => _currentSpeed;
    public bool IsGrounded => _characterController.isGrounded;
    public Vector3 GroundNormal => _groundNormal;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        UpdateGroundInfo();
        HandleMovement();
    }

    private void UpdateGroundInfo()
    {
        // Sphere/Ray check below character feet to get precise surface normal
        Vector3 rayStart = transform.position + Vector3.up * 0.2f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 0.5f, _groundLayer))
        {
            _groundNormal = hit.normal;
        }
        else
        {
            _groundNormal = Vector3.up;
        }
    }

    private void HandleMovement()
    {
        Vector2 rawInput = _inputReader.MoveInput;
        
        // Convert input direction to camera-relative orientation
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

        // Horizontal velocity calculation
        Vector3 moveDirection = (_lastDirection != Vector3.zero ? _lastDirection : transform.forward);
        
        // Project movement direction onto slope surface to maintain speed up/down inclines
        Vector3 slopeProjectedDirection = Vector3.ProjectOnPlane(moveDirection, _groundNormal).normalized;
        Vector3 horizontalVelocity = slopeProjectedDirection * _currentSpeed;

        // Apply ground snapping or falling gravity
        if (_characterController.isGrounded)
        {
            _verticalVelocity = _groundSnapForce; // Keeps player stuck to downward steps/slopes
        }
        else
        {
            _verticalVelocity += _gravity * Time.deltaTime; // Standard falling velocity
        }

        // Final velocity combining ground projected motion + vertical force
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