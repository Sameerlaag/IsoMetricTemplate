using UnityEngine;

public class IsometricCameraController : MonoBehaviour
{
    [Header("Target Tracking")]
    [SerializeField] private PlayerController _targetPlayer;
    [SerializeField] private Transform _cameraContainer;
    [SerializeField] private Transform _internalCamera;

    [Header("Camera Rig Settings")]
    [Range(0f, 89f)] [SerializeField] private float _pitchAngle = 30f;
    [SerializeField] private float _yawAngle = 45f;
    [SerializeField] private float _distance = 15f;

    [Header("Aim Mode Camera Settings")]
    [SerializeField] private float _aimOffsetDistance = 3f; // Distance camera shifts toward aim sector
    [SerializeField] private float _aimLerpSpeed = 5f;

    [Header("Lag Settings")]
    [SerializeField] private float _baseFollowSpeed = 5f;

    private Vector3 _currentCameraVelocity;
    private Vector3 _aimOffsetTarget;

    private void Start()
    {
        SetupInternalCamera();
    }

    private void LateUpdate()
    {
        FollowPlayer();
    }

    private void SetupInternalCamera()
    {
        if (_cameraContainer == null || _internalCamera == null) return;

        _cameraContainer.rotation = Quaternion.Euler(0f, _yawAngle, 0f);

        float pitchRad = _pitchAngle * Mathf.Deg2Rad;
        float height = Mathf.Sin(pitchRad) * _distance;
        float distanceBack = Mathf.Cos(pitchRad) * _distance;

        _internalCamera.localPosition = new Vector3(0f, height, -distanceBack);
        _internalCamera.localRotation = Quaternion.Euler(_pitchAngle, 0f, 0f);
    }

    private void FollowPlayer()
    {
        if (_targetPlayer == null) return;

        // Determine base position tracking
        Vector3 targetPosition = _targetPlayer.transform.position;

        // Handle Aim 8-Way Offset Calculation
        if (_targetPlayer.CurrentState == PlayerController.MovementState.Aiming)
        {
            Vector3 aimDir = _targetPlayer.AimDirection;
            
            if (aimDir.sqrMagnitude > 0.01f)
            {
                // Angle relative to world space quantised to 8 sectors (45 deg increments)
                float angle = Mathf.Atan2(aimDir.x, aimDir.z) * Mathf.Rad2Deg;
                float snappedAngle = Mathf.Round(angle / 45f) * 45f;
                
                Vector3 snappedDir = new Vector3(
                    Mathf.Sin(snappedAngle * Mathf.Deg2Rad), 
                    0f, 
                    Mathf.Cos(snappedAngle * Mathf.Deg2Rad)
                );

                _aimOffsetTarget = snappedDir * _aimOffsetDistance;
            }
        }
        else
        {
            _aimOffsetTarget = Vector3.zero;
        }

        // Apply 8-way offset with smooth transition
        Vector3 finalTargetPosition = targetPosition + _aimOffsetTarget;

        float targetSmoothTime = 1f / Mathf.Max(_baseFollowSpeed, 0.01f);
        _cameraContainer.position = Vector3.SmoothDamp(
            _cameraContainer.position,
            finalTargetPosition,
            ref _currentCameraVelocity,
            targetSmoothTime,
            Mathf.Infinity,
            Time.deltaTime
        );
    }

    private void OnValidate()
    {
        SetupInternalCamera();
    }
}