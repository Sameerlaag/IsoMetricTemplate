using System;
using UnityEngine;

/// <summary>
/// First-person controller: mouse look, movement, sprint / aim rules and the "horror game" aim zoom.
///
/// Expected hierarchy:
///   Player (this script + CharacterController)
///     └─ CameraPivot   (at eye height, only rotates on X = pitch)
///          └─ Main Camera
///               └─ Handgun model (+ Animator, HandgunController)
///
/// Rules implemented here:
///  - Aiming stops running, running stops aiming (the most recent action wins).
///  - You can't aim while the weapon is reloading (reloading stops aiming).
///  - Sprinting only works while moving forward.
/// </summary>
[DefaultExecutionOrder(-50)] // run before HandgunController so it reads fresh sprint/aim flags
[RequireComponent(typeof(CharacterController))]
public class PlayerFPSController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _cameraPivot;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private HandgunController _weapon;
    [SerializeField] private FlashlightController _flashlight;

    [Header("Look")]
    [SerializeField] private float _lookSensitivity = 0.12f;
    [SerializeField] private float _aimSensitivityMultiplier = 0.6f;
    [SerializeField] private float _minPitch = -85f;
    [SerializeField] private float _maxPitch = 85f;
    [SerializeField] private bool _lockCursor = true;

    [Header("Movement Speeds")]
    [SerializeField] private float _walkSpeed = 4f;
    [SerializeField] private float _runSpeed = 7.5f;
    [SerializeField] private float _aimSpeed = 2f;
    [SerializeField] private float _acceleration = 40f;
    [SerializeField] private float _deceleration = 50f;

    [Header("Physics")]
    [SerializeField] private float _groundSnapForce = -5f;
    [SerializeField] private float _gravity = -19.62f;

    [Header("Camera - Aim Zoom")]
    [SerializeField] private float _defaultFov = 70f;
    [SerializeField] private float _aimFov = 50f;
    [SerializeField] private float _aimForwardOffset = 0.15f;
    [SerializeField] private float _aimTransitionTime = 0.18f;

    [Header("Camera - Sprint")]
    [SerializeField] private float _sprintFovBoost = 6f;
    [SerializeField] private float _sprintTransitionTime = 0.25f;

    private CharacterController _characterController;
    private Vector3 _horizontalVelocity;
    private float _verticalVelocity;
    private Vector3 _groundNormal = Vector3.up;
    private float _pitch;

    // Sprint / aim arbitration
    private bool _prevSprintWanted;
    private bool _prevAimHeld;
    private bool _sprintSuppressed; // sprint key still held, but aiming took over
    private bool _aimSuppressed;    // aim key still held, but sprinting took over

    // Camera blending
    private float _aimBlend;
    private float _sprintBlend;
    private Vector3 _cameraDefaultLocalPos;

    public bool IsSprinting { get; private set; }
    public bool IsAiming { get; private set; }
    public float CurrentSpeed => _horizontalVelocity.magnitude;

    /// <summary>Raised when the effective aiming state changes (true = started aiming).</summary>
    public event Action<bool> OnAimStateChanged;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_weapon == null) _weapon = GetComponentInChildren<HandgunController>();

        _cameraDefaultLocalPos = _mainCamera.transform.localPosition;
        _mainCamera.fieldOfView = _defaultFov;
    }

    private void OnEnable()
    {
        if (_lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        _inputReader.OnEquipedItemPerformed += HandleEquipment;
    }

    private void OnDisable()
    {
        _inputReader.OnEquipedItemPerformed -= HandleEquipment;
    }

    
    private void HandleEquipment()
    {
        _flashlight.Switch();
    }
    
    private void Update()
    {
        UpdateGroundInfo();
        UpdateSprintAndAimState();
        HandleLook();
        HandleMovement();
        UpdateCameraEffects();
    }

    // ------------------------------------------------------------------
    // Sprint / Aim arbitration
    // ------------------------------------------------------------------
    private void UpdateSprintAndAimState()
    {
        Vector2 move = _inputReader.MoveInput;
        bool aimHeld = _inputReader.IsAiming;
        bool sprintWanted = _inputReader.IsSprinting && move.y > 0.1f && _characterController.isGrounded;
        bool weaponCanAim = _weapon == null || _weapon.CanAim;

        // Running starts -> stops aiming
        if (sprintWanted && !_prevSprintWanted && aimHeld)
            _aimSuppressed = true;

        // Aiming starts -> stops running (only if the weapon is allowed to aim, e.g. not reloading)
        if (aimHeld && !_prevAimHeld && weaponCanAim && sprintWanted)
            _sprintSuppressed = true;

        // Suppression lasts until the suppressed key is released
        if (!sprintWanted) _sprintSuppressed = false;
        if (!aimHeld) _aimSuppressed = false;

        IsSprinting = sprintWanted && !_sprintSuppressed;

        bool aiming = aimHeld && !_aimSuppressed && weaponCanAim && !IsSprinting;
        if (aiming != IsAiming)
        {
            IsAiming = aiming;
            OnAimStateChanged?.Invoke(aiming);
        }

        _prevSprintWanted = sprintWanted;
        _prevAimHeld = aimHeld;
    }

    // ------------------------------------------------------------------
    // Look
    // ------------------------------------------------------------------
    private void HandleLook()
    {
        // NOTE: LookInput is expected to be the raw mouse delta (no Time.deltaTime needed).
        Vector2 look = _inputReader.LookInput * _lookSensitivity;
        if (IsAiming) look *= _aimSensitivityMultiplier;

        transform.Rotate(Vector3.up, look.x, Space.World);

        _pitch = Mathf.Clamp(_pitch - look.y, _minPitch, _maxPitch);
        _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    // ------------------------------------------------------------------
    // Movement
    // ------------------------------------------------------------------
    private void UpdateGroundInfo()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.2f;
        _groundNormal = Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 0.5f)
            ? hit.normal
            : Vector3.up;
    }

    private void HandleMovement()
    {
        Vector2 move = _inputReader.MoveInput;
        Vector3 wishDir = transform.right * move.x + transform.forward * move.y;
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        float targetSpeed = IsAiming ? _aimSpeed : (IsSprinting ? _runSpeed : _walkSpeed);
        Vector3 targetVelocity = wishDir * targetSpeed;

        bool hasInput = wishDir.sqrMagnitude > 0.001f;
        float rate = hasInput ? _acceleration : _deceleration;
        _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, rate * Time.deltaTime);

        // Follow slopes while keeping the same speed
        Vector3 finalHorizontal = _horizontalVelocity;
        if (_horizontalVelocity.sqrMagnitude > 0.0001f)
        {
            finalHorizontal = Vector3.ProjectOnPlane(_horizontalVelocity, _groundNormal).normalized
                              * _horizontalVelocity.magnitude;
        }

        if (_characterController.isGrounded) _verticalVelocity = _groundSnapForce;
        else _verticalVelocity += _gravity * Time.deltaTime;

        Vector3 velocity = finalHorizontal;
        velocity.y += _verticalVelocity;
        _characterController.Move(velocity * Time.deltaTime);
    }

    // ------------------------------------------------------------------
    // Camera: aim zoom (FOV + push forward) and subtle sprint FOV
    // ------------------------------------------------------------------
    private void UpdateCameraEffects()
    {
        _aimBlend = Mathf.MoveTowards(_aimBlend, IsAiming ? 1f : 0f,
            Time.deltaTime / Mathf.Max(0.01f, _aimTransitionTime));
        _sprintBlend = Mathf.MoveTowards(_sprintBlend, IsSprinting ? 1f : 0f,
            Time.deltaTime / Mathf.Max(0.01f, _sprintTransitionTime));

        float aimT = Mathf.SmoothStep(0f, 1f, _aimBlend);
        float sprintT = Mathf.SmoothStep(0f, 1f, _sprintBlend);

        float fov = Mathf.Lerp(_defaultFov, _aimFov, aimT) + _sprintFovBoost * sprintT;
        _mainCamera.fieldOfView = fov;

        _mainCamera.transform.localPosition =
            _cameraDefaultLocalPos + Vector3.forward * (_aimForwardOffset * aimT);
    }
}