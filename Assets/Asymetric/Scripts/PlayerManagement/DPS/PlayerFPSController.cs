using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// First-person controller: mouse look, movement, sprint/aim rules, 
/// directional knockback, dynamic low-health camera shake, URP post-processing (Bloom, Lens Distortion, Vignette),
/// eye-opening intro, death sequence, hit sounds, and breathing loop.
/// </summary>
[DefaultExecutionOrder(-50)] // run before HandgunController so it reads fresh sprint/aim flags
[RequireComponent(typeof(CharacterController))]
public class PlayerFPSController : MonoBehaviour, IDamageable
{
    [Header("References")] 
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private Transform _cameraPivot;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private HandgunController _weapon;
    [SerializeField] private FlashlightController _flashlight;
    [SerializeField] private FeetAudio _feetAudioManager;
    [SerializeField] private PlayerHealth _playerHealth;

    [Header("Audio")]
    [SerializeField] private AudioSource _sfxAudioSource;
    [SerializeField] private AudioSource _breathingAudioSource;
    [SerializeField] private AudioClip[] _hitAudioClips;
    [SerializeField] private AudioClip _deathAudioClip;
    [SerializeField] private AudioClip _introBreathClip;

    [Header("Post-Processing / Juice")]
    [SerializeField] private Volume _postProcessingVolume;
    [SerializeField] private float _damageShakeBaseIntensity = 0.15f;
    [SerializeField] private float _damageShakeDuration = 0.35f;
    [SerializeField] private float _introDuration = 1.0f;
    [SerializeField] private float _deathSequenceDuration = 1.5f;
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

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
    private bool _sprintSuppressed;
    private bool _aimSuppressed;

    // State flags
    private bool _isIntroActive = true;
    private bool _isDead = false;

    // Camera blending & juice variables
    private float _aimBlend;
    private float _sprintBlend;
    private Vector3 _cameraDefaultLocalPos;
    
    // Knockback and Shake runtime state
    private Vector3 _knockbackVelocity;
    private float _shakeTimer;
    private float _currentShakeIntensity;
    
    // Post-processing profile runtime cached references
    private Bloom _bloomComponent;
    private LensDistortion _lensDistortionComponent;
    private Vignette _vignetteComponent;
    private float _defaultVignetteIntensity = 0.35f;

    public bool IsSprinting { get; private set; }
    public bool IsAiming { get; private set; }
    public float CurrentSpeed => _horizontalVelocity.magnitude;

    public event Action<bool> OnAimStateChanged;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_weapon == null) _weapon = GetComponentInChildren<HandgunController>();

        _cameraDefaultLocalPos = _mainCamera.transform.localPosition;
        _mainCamera.fieldOfView = _defaultFov;

        CachePostProcessingComponents();
    }

    private void CachePostProcessingComponents()
    {
        if (_postProcessingVolume != null && _postProcessingVolume.profile != null)
        {
            _postProcessingVolume.profile.TryGet(out _bloomComponent);
            _postProcessingVolume.profile.TryGet(out _lensDistortionComponent);
            
            if (_postProcessingVolume.profile.TryGet(out _vignetteComponent))
            {
                _defaultVignetteIntensity = _vignetteComponent.intensity.value;
            }
        }
    }

    private void Start()
    {
        StartCoroutine(PlayIntroSequence());
        StartBreathingLoop();
    }

    private IEnumerator PlayIntroSequence()
    {
        _isIntroActive = true;

        // Start eyes fully closed (maximum vignette)
        if (_vignetteComponent != null)
        {
            _vignetteComponent.intensity.value = 1.0f;
        }

        float elapsed = 0f;
        while (elapsed < _introDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _introDuration;

            // Smoothly open eyes (lerp vignette back to default)
            if (_vignetteComponent != null)
            {
                _vignetteComponent.intensity.value = Mathf.Lerp(1.0f, _defaultVignetteIntensity, Mathf.SmoothStep(0f, 1f, t));
            }
            yield return null;
        }

        if (_vignetteComponent != null)
        {
            _vignetteComponent.intensity.value = _defaultVignetteIntensity;
        }

        _isIntroActive = false;
    }

    private void StartBreathingLoop()
    {
        if (_breathingAudioSource != null)
        {
            if (_introBreathClip != null) _breathingAudioSource.clip = _introBreathClip;
            _breathingAudioSource.loop = true;
            _breathingAudioSource.Play();
        }
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
        if (_isDead || _isIntroActive) return;

        UpdateGroundInfo();
        UpdateSprintAndAimState();
        HandleLook();
        HandleMovement();
        UpdateCameraEffects();
        UpdateJuiceEffects();

        // Handle footstep audio based on current movement states
        if (_feetAudioManager != null)
        {
            _feetAudioManager.PlayFootsteps(
                _characterController.isGrounded,
                IsSprinting,
                IsAiming,
                CurrentSpeed
            );
        }
    }

    private void UpdateSprintAndAimState()
    {
        Vector2 move = _inputReader.MoveInput;
        bool aimHeld = _inputReader.IsAiming;
        bool sprintWanted = _inputReader.IsSprinting && move.y > 0.1f && _characterController.isGrounded;
        bool weaponCanAim = _weapon == null || _weapon.CanAim;

        if (sprintWanted && !_prevSprintWanted && aimHeld)
            _aimSuppressed = true;

        if (aimHeld && !_prevAimHeld && weaponCanAim && sprintWanted)
            _sprintSuppressed = true;

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

    private void HandleLook()
    {
        Vector2 look = _inputReader.LookInput * _lookSensitivity;
        if (IsAiming) look *= _aimSensitivityMultiplier;

        transform.Rotate(Vector3.up, look.x, Space.World);

        _pitch = Mathf.Clamp(_pitch - look.y, _minPitch, _maxPitch);
        _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

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

        Vector3 finalHorizontal = _horizontalVelocity;
        if (_horizontalVelocity.sqrMagnitude > 0.0001f)
        {
            finalHorizontal = Vector3.ProjectOnPlane(_horizontalVelocity, _groundNormal).normalized *
                              _horizontalVelocity.magnitude;
        }

        if (_characterController.isGrounded) _verticalVelocity = _groundSnapForce;
        else _verticalVelocity += _gravity * Time.deltaTime;

        // Apply external knockback decay over time
        _knockbackVelocity = Vector3.Lerp(_knockbackVelocity, Vector3.zero, 10f * Time.deltaTime);

        Vector3 velocity = finalHorizontal + _knockbackVelocity;
        velocity.y += _verticalVelocity;
        _characterController.Move(velocity * Time.deltaTime);
    }

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

        Vector3 targetLocalPos = _cameraDefaultLocalPos + Vector3.forward * (_aimForwardOffset * aimT);
        
        if (_shakeTimer > 0f)
        {
            Vector3 randomShake = UnityEngine.Random.insideUnitSphere * _currentShakeIntensity;
            targetLocalPos += randomShake;
            _shakeTimer -= Time.deltaTime;
        }

        _mainCamera.transform.localPosition = targetLocalPos;
    }

    private void UpdateJuiceEffects()
    {
        if (_playerHealth == null) return;

        float healthNormalized = Mathf.Clamp01(_playerHealth.Current / _playerHealth.Max);
        float missingHealthFactor = 1f - healthNormalized;

        if (_bloomComponent != null)
        {
            float targetBloom = Mathf.Lerp(1f, 8f, missingHealthFactor) + (missingHealthFactor * Mathf.Sin(Time.time * 6f) * 2f);
            _bloomComponent.intensity.value = Mathf.Lerp(_bloomComponent.intensity.value, targetBloom, Time.deltaTime * 4f);
        }

        if (_lensDistortionComponent != null)
        {
            float targetDistortion = Mathf.Lerp(0f, -0.35f, missingHealthFactor);
            _lensDistortionComponent.intensity.value = Mathf.Lerp(_lensDistortionComponent.intensity.value, targetDistortion, Time.deltaTime * 4f);
        }

        // Return to standard default vignette when alive, scale slightly with low health panic
        if (_vignetteComponent != null && !_isIntroActive)
        {
            float targetVignette = Mathf.Lerp(_defaultVignetteIntensity, 0.65f, missingHealthFactor);
            _vignetteComponent.intensity.value = Mathf.Lerp(_vignetteComponent.intensity.value, targetVignette, Time.deltaTime * 4f);
        }
    }

    public void TakeDamage(float amount, Transform damageSourceTransform)
    {
        if (_isDead) return;

        if (_playerHealth != null)
        {
            _playerHealth.TakeDamage(amount);
        }

        // Play random hit audio clip
        if (_sfxAudioSource != null && _hitAudioClips != null && _hitAudioClips.Length > 0)
        {
            AudioClip clip = _hitAudioClips[UnityEngine.Random.Range(0, _hitAudioClips.Length)];
            _sfxAudioSource.PlayOneShot(clip);
        }

        // Directional Knockback
        if (damageSourceTransform != null)
        {
            Vector3 pushDirection = (transform.position - damageSourceTransform.position).normalized;
            pushDirection.y = 0f;
            _knockbackVelocity = pushDirection * Mathf.Lerp(4f, 10f, amount / 50f);
        }

        // Camera Shake
        float healthNormalized = _playerHealth != null ? Mathf.Clamp01(_playerHealth.Current / _playerHealth.Max) : 1f;
        float lowHealthMultiplier = Mathf.Lerp(2.5f, 0.8f, healthNormalized);
        
        _currentShakeIntensity = _damageShakeBaseIntensity * lowHealthMultiplier;
        _shakeTimer = _damageShakeDuration;

        // Check for Death
        if (_playerHealth != null && _playerHealth.Current <= 0f)
        {
            StartCoroutine(TriggerDeathSequence());
        }
    }

    private IEnumerator TriggerDeathSequence()
    {
        _isDead = true;

        // Stop breathing and play death sound
        if (_breathingAudioSource != null) _breathingAudioSource.Stop();
        if (_sfxAudioSource != null && _deathAudioClip != null)
        {
            _sfxAudioSource.PlayOneShot(_deathAudioClip);
        }

        // Unlock cursor for UI transition
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        float elapsed = 0f;
        Quaternion initialPivotRotation = _cameraPivot.localRotation;
        Quaternion targetPivotRotation = initialPivotRotation * Quaternion.Euler(45f, 0f, 25f); // Fall / tilt over

        float startVignette = _vignetteComponent != null ? _vignetteComponent.intensity.value : _defaultVignetteIntensity;

        while (elapsed < _deathSequenceDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _deathSequenceDuration;

            // Tilt camera/eyes down (falling effect)
            _cameraPivot.localRotation = Quaternion.Slerp(initialPivotRotation, targetPivotRotation, Mathf.SmoothStep(0f, 1f, t));

            // Close eyes completely (vignette to 1.0)
            if (_vignetteComponent != null)
            {
                _vignetteComponent.intensity.value = Mathf.Lerp(startVignette, 1.0f, Mathf.SmoothStep(0f, 1f, t));
            }

            yield return null;
        }

        // Return to main menu via GameManager
        if (DPSGameManager.Instance != null)
        {
            DPSGameManager.Instance.ReturnToMainMenu();
        }
        else
        {
            SceneManager.LoadScene(_mainMenuSceneName);
        }
    }
}