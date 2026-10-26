using System;
using UnityEngine;

/// <summary>
/// Handgun state machine + animator driver.
///
/// States:  Drawing -> Idle <-> Shooting
///                      Idle -> Reloading -> Idle
///          (any)  -> Hiding -> Hidden  (when sprinting)   Hidden -> Drawing (when sprint stops)
///
/// Animator: this script fires the triggers "Draw", "Idle", "Shoot", "Hide" (+ optional "Reload").
/// State lengths are driven by the timers below, so no animation events are required.
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
    [SerializeField] private float _fireCooldown = 0.25f;
    [SerializeField] private float _reloadDuration = 1.8f;

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
    [SerializeField] private AudioClip _dryFireSound;
    [SerializeField] private AudioClip _reloadSound;
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private Light _muzzleLight;
    [SerializeField] private float _muzzleLightDuration = 0.05f;

    private GunState _state;
    private float _timer;
    private float _muzzleLightTimer;

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
        if (_state != GunState.Idle) return;

        if (CurrentAmmo <= 0)
        {
            // Empty: dry fire click, then reload
            PlaySound(_dryFireSound);
            if (!TryStartReload()) return;
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
        _timer = _fireCooldown;
        SetTrigger(_shootHash);

        PlayFireEffects();
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
    // Effects (placeholders)
    // ------------------------------------------------------------------
    private void PlayFireEffects()
    {
        // TODO: replace with your final sound / VFX
        PlaySound(_fireSound);

        if (_muzzleFlash != null) _muzzleFlash.Play();

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