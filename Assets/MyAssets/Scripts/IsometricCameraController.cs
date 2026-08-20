using UnityEngine;

public class IsometricCameraController : MonoBehaviour
{
    [Header("Target Tracking")]
    [SerializeField] private PlayerController _targetPlayer;
    [SerializeField] private Transform _cameraContainer;
    [SerializeField] private Transform _internalCamera;

    [Header("Camera Rig Settings")]
    [Range(0f, 89f)]
    [SerializeField] private float _pitchAngle = 30f; // Tilt downwards (X-axis rotation)
    [SerializeField] private float _yawAngle = 45f;   // Isometric rotation (Y-axis rotation)
    [SerializeField] private float _distance = 15f;   // Distance away from target

    [Header("Lag & Delay Settings")]
    [SerializeField] private float _baseFollowSpeed = 5f;
    [SerializeField] private float _fullSpeedFollowDelay = 0.25f;

    private Vector3 _currentCameraVelocity;
    private float _delayTimer;

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

        // 1. Rotate the container around Y (Yaw) to establish the isometric angle
        _cameraContainer.rotation = Quaternion.Euler(0f, _yawAngle, 0f);

        // 2. Compute exact local offset using Trigonometry to keep camera elevated above ground
        float pitchRad = _pitchAngle * Mathf.Deg2Rad;
        float height = Mathf.Sin(pitchRad) * _distance;
        float distanceBack = Mathf.Cos(pitchRad) * _distance;

        // Position camera relative to container (Up and Back)
        _internalCamera.localPosition = new Vector3(0f, height, -distanceBack);

        // 3. Pitch camera down to look at container origin
        _internalCamera.localRotation = Quaternion.Euler(_pitchAngle, 0f, 0f);
    }

    private void FollowPlayer()
    {
        if (_targetPlayer == null) return;

        Vector3 targetPosition = _targetPlayer.transform.position;
        float currentSpeed = _targetPlayer.CurrentSpeed;

        float targetSmoothTime = 1f / Mathf.Max(_baseFollowSpeed, 0.01f);
        if (currentSpeed > 0.1f)
        {
            _delayTimer = Mathf.MoveTowards(_delayTimer, _fullSpeedFollowDelay, Time.deltaTime);
            targetSmoothTime += _delayTimer;
        }
        else
        {
            _delayTimer = 0f;
        }

        _cameraContainer.position = Vector3.SmoothDamp(
            _cameraContainer.position,
            targetPosition,
            ref _currentCameraVelocity,
            targetSmoothTime
        );
    }

    private void OnValidate()
    {
        SetupInternalCamera();
    }
}