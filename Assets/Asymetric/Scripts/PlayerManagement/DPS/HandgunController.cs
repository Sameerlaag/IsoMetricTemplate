using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handgun state machine + animator driver.
///
/// States:  Drawing -> Idle <-> Shooting
///                      Idle -> Reloading -> Idle
///          (any)  -> Hiding -> Hidden  (when sprinting)   Hidden -> Drawing (when sprint stops)
///
/// Fire timing (per shot):
///   [ mandatory recovery ][ recovery window ]
///   - mandatory recovery: input is ignored (no spam, animation + audio can finish their kick)
///   - recovery window:    pressing fire retriggers animation / audio / blowback immediately
///   - after both:         back to Idle
///
/// Animator: fires the triggers "Draw", "Idle", "Shoot", "Hide" (+ optional "Reload").
/// </summary>
public class HandgunController : MonoBehaviour
{
    public enum GunState
    {
        Drawing,
        Idle,
        Shooting,
        Reloading,
        Hiding,
        Hidden
    }

    [Header("References")]
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private PlayerFPSController _player;
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _fireOrigin; // usually the camera

    [Header("Ammo")]
    [SerializeField] private int _magazineSize = 12;
    [SerializeField] private int _startingReserve = 48;
    [SerializeField] private bool _infiniteReserve = false;

    [Header("Timings (seconds)")]
    [SerializeField] private float _drawDuration = 0.6f;
    [SerializeField] private float _hideDuration = 0.35f;
    [SerializeField] private float _reloadDuration = 1.8f;

    [Header("Fire Timing")]
    [Tooltip("Right after a shot, fire input is ignored for this long.")]
    [SerializeField] private float _mandatoryRecovery = 0.10f;
    [Tooltip("After the mandatory recovery, firing again retriggers the shot immediately. " +
             "When this ends the gun returns to Idle.")]
    [SerializeField] private float _recoveryWindow = 0.15f;

    [Header("Blowback (move the hands container)")]
    [Tooltip("Container holding the hands + gun. Use an object the Animator does NOT animate " +
             "(e.g. a parent 'HandsRecoil'), otherwise the Animator will overwrite the movement.")]
    [SerializeField] private Transform _handsContainer;
    [Tooltip("Local-space offset at the peak of the recoil. -Z pushes the gun back toward the player.")]
    [SerializeField] private Vector3 _blowbackOffset = new Vector3(0f, 0.01f, -0.07f);
    [SerializeField] private float _blowbackKickTime = 0.04f;
    [SerializeField] private float _blowbackReturnTime = 0.14f;

    [Header("Shooting")]
    [SerializeField] private float _range = 100f;
    [SerializeField] private LayerMask _hitMask = ~0;

    [Header("Animator Triggers (leave Reload empty to skip)")]
    [SerializeField] private string _drawTrigger = "Draw";
    [SerializeField] private string _idleTrigger = "Idle";
    [SerializeField] private string _shootTrigger = "Shoot";
    [SerializeField] private string _hideTrigger = "Hide";
    [SerializeField] private string _reloadTrigger = "Reload";

    [Header("Effects (placeholders - all optional)")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _fireSound;
    [Tooltip("Random pitch +/- applied to each shot. 0 = identical every time.")]
    [SerializeField, Range(0f, 0.2f)] private float _firePitchVariation = 0.04f;
    [SerializeField] private AudioClip _dryFireSound;
    [SerializeField] private AudioClip _reloadSound;
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private Light _muzzleLight;
    [SerializeField] private float _muzzleLightDuration = 0.05f;

    private GunState _state;
    private float _timer;
    private float _timeSinceShot;
    private float _muzzleLightTimer;

    private Vector3 _handsRestLocalPos;
    private Coroutine _blowbackRoutine;

    private int _drawHash, _idleHash, _shootHash, _hideHash, _reloadHash;

    public GunState State => _state;
    public int CurrentAmmo { get; private set; }
    public int ReserveAmmo { get; private set; }
    public bool IsReloading => _state == GunState.Reloading;

    /// <summary>You can't aim while reloading. Everything else is fine (sprint conflicts are handled by the player).</summary>
    public bool CanAim => _state != GunState.Reloading;

    /// <summary>(currentAmmo, reserveAmmo) - hook your HUD here.</summary>
    public event Action<int, int> OnAmmoChanged;

    private void Awake()
    {
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_player == null) _player = GetComponentInParent<PlayerFPSController>();

        _drawHash = Animator.StringToHash(_drawTrigger);
        _idleHash = Animator.StringToHash(_idleTrigger);
        _shootHash = Animator.StringToHash(_shootTrigger);
        _hideHash = Animator.StringToHash(_hideTrigger);
        _reloadHash = string.IsNullOrEmpty(_reloadTrigger) ? 0 : Animator.StringToHash(_reloadTrigger);

        CurrentAmmo = _magazineSize;
        ReserveAmmo = _startingReserve;

        if (_handsContainer != null) _handsRestLocalPos = _handsContainer.localPosition;
        if (_muzzleLight != null) _muzzleLight.enabled = false;
    }

    private void OnEnable()
    {
        _inputReader.OnFireWeaponPerformed += HandleFire;
        _inputReader.OnReloadStarted += HandleReload;
    }

    private void OnDisable()
    {
        _inputReader.OnFireWeaponPerformed -= HandleFire;
        _inputReader.OnReloadStarted -= HandleReload;

        _blowbackRoutine = null;
        if (_handsContainer != null) _handsContainer.localPosition = _handsRestLocalPos;
    }

    private void Start()
    {
        // Game starts with the gun drawn
        StartDraw();
        NotifyAmmo();
    }

    private void Update()
    {
        UpdateMuzzleLight();

        _timer -= Time.deltaTime;
        bool sprinting = _player != null && _player.IsSprinting;

        switch (_state)
        {
            case GunState.Drawing:
                if (sprinting) StartHide();
                else if (_timer <= 0f) ReturnToIdle();
                break;

            case GunState.Idle:
                if (sprinting) StartHide();
                break;

            case GunState.Shooting:
                _timeSinceShot += Time.deltaTime;
                if (sprinting) StartHide();
                else if (_timer <= 0f) ReturnToIdle();
                break;

            case GunState.Reloading:
                if (sprinting)
                {
                    // Running interrupts the reload: ammo is NOT refilled, weapon is hidden
                    StartHide();
                }
                else if (_timer <= 0f)
                {
                    FinishReload();
                    ReturnToIdle();
                }
                break;

            case GunState.Hiding:
                if (_timer <= 0f)
                {
                    if (sprinting) _state = GunState.Hidden;
                    else StartDraw(); // player stopped running during the hide animation
                }
                break;

            case GunState.Hidden:
                if (!sprinting) StartDraw();
                break;
        }
    }

    // ------------------------------------------------------------------
    // Input
    // ------------------------------------------------------------------
    private void HandleFire()
    {
        // Allowed when idle, or during a shot's recovery window (after the mandatory recovery)
        bool canFire = _state == GunState.Idle ||
                       (_state == GunState.Shooting && _timeSinceShot >= _mandatoryRecovery);
        if (!canFire) return;

        if (CurrentAmmo <= 0)
        {
            // Empty: dry fire click, then reload
            if (_state == GunState.Idle)
            {
                PlaySound(_dryFireSound);
                TryStartReload();
            }
            return;
        }

        Fire();
    }

    private void HandleReload()
    {
        if (_state != GunState.Idle) return;
        TryStartReload();
    }

    // ------------------------------------------------------------------
    // Actions
    // ------------------------------------------------------------------
    private void Fire()
    {
        CurrentAmmo--;
        _state = GunState.Shooting;
        _timeSinceShot = 0f;
        _timer = _mandatoryRecovery + _recoveryWindow;

        // Retrigger: restarts the Shoot animation even if it is already playing
        // (needs "Can Transition To Self" enabled on the Any State -> Shoot transition)
        SetTrigger(_shootHash);

        PlayFireEffects();
        PlayBlowback();
        FireBullet();
        NotifyAmmo();
    }

    private void FireBullet()
    {
        Transform origin = _fireOrigin != null ? _fireOrigin : transform;
        if (Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, _range, _hitMask,
                QueryTriggerInteraction.Ignore))
        {
            // TODO: apply damage / spawn impact decal here, e.g.
            // hit.collider.GetComponentInParent<IDamageable>()?.TakeDamage(damage);
            Debug.DrawLine(origin.position, hit.point, Color.red, 0.5f);
        }
    }

    private bool TryStartReload()
    {
        bool hasReserve = _infiniteReserve || ReserveAmmo > 0;
        if (CurrentAmmo >= _magazineSize || !hasReserve) return false;

        _state = GunState.Reloading;
        _timer = _reloadDuration;
        if (_reloadHash != 0) SetTrigger(_reloadHash);
        PlaySound(_reloadSound);
        return true;
    }

    private void FinishReload()
    {
        int needed = _magazineSize - CurrentAmmo;
        int taken = _infiniteReserve ? needed : Mathf.Min(needed, ReserveAmmo);

        CurrentAmmo += taken;
        if (!_infiniteReserve) ReserveAmmo -= taken;
        NotifyAmmo();
    }

    private void StartDraw()
    {
        _state = GunState.Drawing;
        _timer = _drawDuration;
        SetTrigger(_drawHash);
    }

    private void StartHide()
    {
        // Cut the reload sound if we are interrupting a reload
        if (_state == GunState.Reloading && _audioSource != null) _audioSource.Stop();

        _state = GunState.Hiding;
        _timer = _hideDuration;
        SetTrigger(_hideHash);
    }

    private void ReturnToIdle()
    {
        _state = GunState.Idle;
        SetTrigger(_idleHash);

        // Magazine empty (after a shot, or after being interrupted while empty): reload automatically
        if (CurrentAmmo <= 0) TryStartReload();
    }

    // ------------------------------------------------------------------
    // Blowback
    // ------------------------------------------------------------------
    private void PlayBlowback()
    {
        if (_handsContainer == null) return;

        // Restart from wherever the hands currently are, so rapid shots don't snap
        if (_blowbackRoutine != null) StopCoroutine(_blowbackRoutine);
        _blowbackRoutine = StartCoroutine(BlowbackRoutine());
    }

    private IEnumerator BlowbackRoutine()
    {
        Vector3 startPos = _handsContainer.localPosition;
        Vector3 peakPos = _handsRestLocalPos + _blowbackOffset; // absolute target: never accumulates

        // Kick back (fast, ease-out)
        float t = 0f;
        float kick = Mathf.Max(0.001f, _blowbackKickTime);
        while (t < 1f)
        {
            t += Time.deltaTime / kick;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 2f);
            _handsContainer.localPosition = Vector3.LerpUnclamped(startPos, peakPos, eased);
            yield return null;
        }

        // Return to rest (slower, smooth)
        t = 0f;
        float back = Mathf.Max(0.001f, _blowbackReturnTime);
        while (t < 1f)
        {
            t += Time.deltaTime / back;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            _handsContainer.localPosition = Vector3.LerpUnclamped(peakPos, _handsRestLocalPos, eased);
            yield return null;
        }

        _handsContainer.localPosition = _handsRestLocalPos;
        _blowbackRoutine = null;
    }

    // ------------------------------------------------------------------
    // Effects (placeholders)
    // ------------------------------------------------------------------
    private void PlayFireEffects()
    {
        // TODO: replace with your final sound / VFX

        // No Stop(): PlayOneShot layers, so the previous shot's tail keeps playing under the new one
        if (_audioSource != null && _fireSound != null)
        {
            _audioSource.pitch = UnityEngine.Random.Range(1f - _firePitchVariation, 1f + _firePitchVariation);
            _audioSource.PlayOneShot(_fireSound);
        }

        if (_muzzleFlash != null)
        {
            _muzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _muzzleFlash.Play();
        }

        if (_muzzleLight != null)
        {
            _muzzleLight.enabled = true;
            _muzzleLightTimer = _muzzleLightDuration;
        }
    }

    private void UpdateMuzzleLight()
    {
        if (_muzzleLight == null || !_muzzleLight.enabled) return;

        _muzzleLightTimer -= Time.deltaTime;
        if (_muzzleLightTimer <= 0f) _muzzleLight.enabled = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (_audioSource != null && clip != null) _audioSource.PlayOneShot(clip);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------
    private void SetTrigger(int hash)
    {
        if (_animator == null) return;

        // Clear stale triggers so animations never queue up behind each other
        _animator.ResetTrigger(_drawHash);
        _animator.ResetTrigger(_idleHash);
        _animator.ResetTrigger(_shootHash);
        _animator.ResetTrigger(_hideHash);
        if (_reloadHash != 0) _animator.ResetTrigger(_reloadHash);

        _animator.SetTrigger(hash);
    }

    private void NotifyAmmo() => OnAmmoChanged?.Invoke(CurrentAmmo, ReserveAmmo);
}