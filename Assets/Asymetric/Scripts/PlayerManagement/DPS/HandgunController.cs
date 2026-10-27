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
/// Holding the fire button auto-fires, one shot every time the mandatory recovery ends.
///
/// Hands container: blowback + walk sway are both applied to _handsContainer in LateUpdate.
/// Use an object the Animator does NOT animate.
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
    [Tooltip("Hold the fire button to keep shooting.")]
    [SerializeField] private bool _allowAutoFire = true;
    [Tooltip("Right after a shot, fire input is ignored for this long. " +
             "This is also the delay between shots while auto-firing.")]
    [SerializeField] private float _mandatoryRecovery = 0.10f;
    [Tooltip("After the mandatory recovery, firing again retriggers the shot immediately. " +
             "When this ends the gun returns to Idle.")]
    [SerializeField] private float _recoveryWindow = 0.15f;

    [Header("Hands Container")]
    [Tooltip("Container holding the hands + gun. Use an object the Animator does NOT animate " +
             "(e.g. a parent 'HandsRecoil'), otherwise the Animator will overwrite the movement.")]
    [SerializeField] private Transform _handsContainer;

    [Header("Blowback")]
    [Tooltip("Local-space offset at the peak of the recoil. -Z pushes the gun back toward the player.")]
    [SerializeField] private Vector3 _blowbackOffset = new Vector3(0f, 0.01f, -0.07f);
    [SerializeField] private float _blowbackKickTime = 0.04f;
    [SerializeField] private float _blowbackReturnTime = 0.14f;

    [Header("Walk Sway")]
    [SerializeField] private bool _enableWalkSway = true;
    [Tooltip("X = side to side, Y = up/down (bobs twice per side-to-side cycle).")]
    [SerializeField] private Vector2 _swayAmplitude = new Vector2(0.012f, 0.008f);
    [Tooltip("Rocking (roll) in degrees. 0 = no rotation.")]
    [SerializeField] private float _swayRollDegrees = 1.5f;
    [Tooltip("Sway speed (radians/sec) when moving at the reference speed.")]
    [SerializeField] private float _swayFrequency = 7f;
    [Tooltip("Player speed at which the sway is at full strength (≈ your walk speed).")]
    [SerializeField] private float _swayReferenceSpeed = 4f;
    [Tooltip("How fast the sway fades in/out when you start/stop moving.")]
    [SerializeField] private float _swayBlendSpeed = 6f;
    [Tooltip("Sway strength while aiming (0 = none, 1 = same as walking).")]
    [SerializeField, Range(0f, 1f)] private float _aimSwayMultiplier = 0.2f;

    [Header("Shooting")]
    [SerializeField] private float _range = 100f;
    [SerializeField] private float _damage = 25f;
    [Tooltip("Include the enemy HITBOX layer, exclude the enemy root/NavMesh layer.")]
    [SerializeField] private LayerMask _hitMask = ~0;

    [Header("Spread (degrees, cone half-angle)")]
    [Tooltip("Aiming: spread when settled.")]
    [SerializeField] private float _aimRestSpread = 0f;
    [Tooltip("Aiming: spread right after a shot (or after starting to aim).")]
    [SerializeField] private float _aimMaxSpread = 12f;
    [Tooltip("Not aiming: spread when settled.")]
    [SerializeField] private float _hipRestSpread = 8f;
    [Tooltip("Not aiming: spread right after a shot (or after stopping aiming).")]
    [SerializeField] private float _hipMaxSpread = 22f;

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
    [Tooltip("Plays on every shot.")]
    [SerializeField] private ParticleSystem _bulletTrail;
    [Tooltip("Plays a while after a shot, and is stopped/cancelled if another shot is fired. " +
             "Set Looping OFF on this particle system.")]
    [SerializeField] private ParticleSystem _postFireSmoke;
    [SerializeField] private float _smokeDelay = 1f;
    [SerializeField] private Light _muzzleLight;
    [SerializeField] private float _muzzleLightDuration = 0.05f;

    private GunState _state;
    private float _timer;
    private float _timeSinceShot;
    private int _lastFireFrame = -1;

    // Spread: after a "kick" (shot or aim toggle) it holds at max for _mandatoryRecovery,
    // then eases down to rest over _recoveryWindow.
    private float _timeSinceSpreadKick = 999f; // start settled
    private float _lastBroadcastSpread = -1f;
    private bool _spreadDirty = true;
    private float _muzzleLightTimer;

    // Hands container offsets (combined in LateUpdate)
    private Vector3 _handsRestLocalPos;
    private Quaternion _handsRestLocalRot;
    private Vector3 _recoilOffset;
    private Vector3 _swayOffset;
    private float _swayRoll;
    private float _swayIntensity;
    private float _swayPhase;
    private Coroutine _blowbackRoutine;
    private Coroutine _smokeRoutine;

    private int _drawHash, _idleHash, _shootHash, _hideHash, _reloadHash;

    public GunState State => _state;
    public int CurrentAmmo { get; private set; }
    public int ReserveAmmo { get; private set; }
    public bool IsReloading => _state == GunState.Reloading;

    /// <summary>You can't aim while reloading. Everything else is fine (sprint conflicts are handled by the player).</summary>
    public bool CanAim => _state != GunState.Reloading;

    /// <summary>(currentAmmo, reserveAmmo) - hook your HUD here.</summary>
    public event Action<int, int> OnAmmoChanged;

    /// <summary>(currentSpread, restSpread, maxSpread) in degrees - raised whenever the spread changes.</summary>
    public event Action<float, float, float> OnSpreadChanged;

    /// <summary>Reload started; the argument is its duration in seconds.</summary>
    public event Action<float> OnReloadBegan;

    /// <summary>Reload ended; true = completed, false = interrupted.</summary>
    public event Action<bool> OnReloadEnded;

    public bool IsAiming => _player != null && _player.IsAiming;
    public float RestSpread => IsAiming ? _aimRestSpread : _hipRestSpread;
    public float MaxSpread => IsAiming ? _aimMaxSpread : _hipMaxSpread;

    /// <summary>Spread (degrees) a shot fired right now would have.</summary>
    public float CurrentSpread
    {
        get
        {
            float rest = RestSpread;
            float max = MaxSpread;
            float t = _timeSinceSpreadKick;

            if (t <= _mandatoryRecovery) return max;
            if (_recoveryWindow <= 0f) return rest;

            float k = Mathf.Clamp01((t - _mandatoryRecovery) / _recoveryWindow);
            return Mathf.Lerp(max, rest, k);
        }
    }

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

        if (_handsContainer != null)
        {
            _handsRestLocalPos = _handsContainer.localPosition;
            _handsRestLocalRot = _handsContainer.localRotation;
        }

        if (_muzzleLight != null) _muzzleLight.enabled = false;
    }

    private void OnEnable()
    {
        _inputReader.OnFireWeaponPerformed += HandleFire;
        _inputReader.OnReloadStarted += HandleReload;
        if (_player != null) _player.OnAimStateChanged += HandleAimStateChanged;
    }

    private void OnDisable()
    {
        _inputReader.OnFireWeaponPerformed -= HandleFire;
        _inputReader.OnReloadStarted -= HandleReload;
        if (_player != null) _player.OnAimStateChanged -= HandleAimStateChanged;

        _blowbackRoutine = null;
        _smokeRoutine = null;
        _recoilOffset = Vector3.zero;
        _swayOffset = Vector3.zero;
        _swayRoll = 0f;
        if (_handsContainer != null)
        {
            _handsContainer.localPosition = _handsRestLocalPos;
            _handsContainer.localRotation = _handsRestLocalRot;
        }
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

        _timeSinceSpreadKick = Mathf.Min(_timeSinceSpreadKick + Time.deltaTime, 1000f);
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
                    OnReloadEnded?.Invoke(true);
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

        TryAutoFire();
        BroadcastSpread();
    }

    private void LateUpdate()
    {
        if (_handsContainer == null) return;

        UpdateWalkSway();

        _handsContainer.localPosition = _handsRestLocalPos + _recoilOffset + _swayOffset;
        _handsContainer.localRotation = _handsRestLocalRot * Quaternion.Euler(0f, 0f, _swayRoll);
    }

    // ------------------------------------------------------------------
    // Input
    // ------------------------------------------------------------------
    /// <summary>Fire button pressed (single shot, dry-fire click, retrigger in the recovery window).</summary>
    private void HandleFire()
    {
        if (!CanFireNow()) return;

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

    /// <summary>Fire button held: keep shooting every time the mandatory recovery ends.</summary>
    private void TryAutoFire()
    {
        if (!_allowAutoFire || !_inputReader.IsFiring) return;
        if (CurrentAmmo <= 0) return; // reload / dry-fire is handled on the press only
        if (!CanFireNow()) return;

        Fire();
    }

    private void HandleReload()
    {
        if (_state != GunState.Idle) return;
        TryStartReload();
    }

    /// <summary>Idle, or in a shot's recovery window (after the mandatory recovery is over).</summary>
    private bool CanFireNow()
    {
        if (Time.frameCount == _lastFireFrame) return false; // never two shots in one frame

        return _state == GunState.Idle ||
               (_state == GunState.Shooting && _timeSinceShot >= _mandatoryRecovery);
    }

    // ------------------------------------------------------------------
    // Actions
    // ------------------------------------------------------------------
    private void Fire()
    {
        float spread = CurrentSpread; // the shot uses the spread as it is right now

        CurrentAmmo--;
        _state = GunState.Shooting;
        _lastFireFrame = Time.frameCount;
        _timeSinceShot = 0f;
        _timer = _mandatoryRecovery + _recoveryWindow;

        // Retrigger: restarts the Shoot animation even if it is already playing
        // (needs "Can Transition To Self" enabled on the Any State -> Shoot transition)
        SetTrigger(_shootHash);

        PlayFireEffects();
        PlayBlowback();
        FireBullet(spread);
        KickSpread(); // next shot starts from max again
        NotifyAmmo();
    }

    private void FireBullet(float spreadDegrees)
    {
        Transform origin = _fireOrigin != null ? _fireOrigin : transform;
        Vector3 direction = GetSpreadDirection(origin, spreadDegrees);

        if (Physics.Raycast(origin.position, direction, out RaycastHit hit, _range, _hitMask,
                QueryTriggerInteraction.Ignore))
        {
            Debug.DrawLine(origin.position, hit.point, Color.red, 0.5f);
            ResolveHit(hit, direction);
        }
    }

    /// <summary>Enemies / props react by themselves; everything else is a plain surface.</summary>
    private void ResolveHit(RaycastHit hit, Vector3 direction)
    {
        IBulletTarget target = hit.collider.GetComponentInParent<IBulletTarget>();
        if (target != null)
        {
            target.OnBulletHit(new BulletHitInfo
            {
                Collider = hit.collider,
                Point = hit.point,
                Normal = hit.normal,
                Direction = direction,
                Damage = _damage,
                Shooter = transform
            });
            return;
        }

        if (BulletImpactManager.Instance != null)
            BulletImpactManager.Instance.PlayImpact(hit.collider.tag, hit.point, hit.normal);
    }

    // ------------------------------------------------------------------
    // Spread
    // ------------------------------------------------------------------
    /// <summary>Random direction inside a cone of the given half-angle around the origin's forward.</summary>
    private static Vector3 GetSpreadDirection(Transform origin, float spreadDegrees)
    {
        if (spreadDegrees <= 0.001f) return origin.forward;

        float angle = spreadDegrees * Mathf.Sqrt(UnityEngine.Random.value); // uniform over the cone's disc
        float roll = UnityEngine.Random.value * 360f;

        Quaternion local = Quaternion.AngleAxis(roll, Vector3.forward) *
                           Quaternion.AngleAxis(angle, Vector3.right);
        return origin.rotation * (local * Vector3.forward);
    }

    /// <summary>Jump to max spread; it then holds for the mandatory recovery and eases down over the window.</summary>
    private void KickSpread()
    {
        _timeSinceSpreadKick = 0f;
        _spreadDirty = true;
    }

    /// <summary>Starting or stopping aiming jumps the spread to the new mode's max.</summary>
    private void HandleAimStateChanged(bool aiming) => KickSpread();

    private void BroadcastSpread()
    {
        float current = CurrentSpread;
        if (!_spreadDirty && Mathf.Abs(current - _lastBroadcastSpread) < 0.001f) return;

        _spreadDirty = false;
        _lastBroadcastSpread = current;
        OnSpreadChanged?.Invoke(current, RestSpread, MaxSpread);
    }

    private bool TryStartReload()
    {
        bool hasReserve = _infiniteReserve || ReserveAmmo > 0;
        if (CurrentAmmo >= _magazineSize || !hasReserve) return false;

        _state = GunState.Reloading;
        _timer = _reloadDuration;
        if (_reloadHash != 0) SetTrigger(_reloadHash);
        PlaySound(_reloadSound);
        OnReloadBegan?.Invoke(_reloadDuration);
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
        if (_state == GunState.Reloading)
        {
            if (_audioSource != null) _audioSource.Stop();
            OnReloadEnded?.Invoke(false);
        }

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
    // Blowback (writes _recoilOffset, applied in LateUpdate)
    // ------------------------------------------------------------------
    private void PlayBlowback()
    {
        if (_handsContainer == null) return;

        // Restart from the current offset, so rapid shots don't snap
        if (_blowbackRoutine != null) StopCoroutine(_blowbackRoutine);
        _blowbackRoutine = StartCoroutine(BlowbackRoutine());
    }

    private IEnumerator BlowbackRoutine()
    {
        Vector3 start = _recoilOffset;
        Vector3 peak = _blowbackOffset; // absolute target: never accumulates

        // Kick back (fast, ease-out)
        float t = 0f;
        float kick = Mathf.Max(0.001f, _blowbackKickTime);
        while (t < 1f)
        {
            t += Time.deltaTime / kick;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 2f);
            _recoilOffset = Vector3.LerpUnclamped(start, peak, eased);
            yield return null;
        }

        // Return to rest (slower, smooth)
        t = 0f;
        float back = Mathf.Max(0.001f, _blowbackReturnTime);
        while (t < 1f)
        {
            t += Time.deltaTime / back;
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            _recoilOffset = Vector3.LerpUnclamped(peak, Vector3.zero, eased);
            yield return null;
        }

        _recoilOffset = Vector3.zero;
        _blowbackRoutine = null;
    }

    // ------------------------------------------------------------------
    // Walk sway (writes _swayOffset / _swayRoll, applied in LateUpdate)
    // ------------------------------------------------------------------
    private void UpdateWalkSway()
    {
        float speed = _player != null ? _player.CurrentSpeed : 0f;
        float speedFactor = Mathf.Clamp(speed / Mathf.Max(0.01f, _swayReferenceSpeed), 0f, 1.3f);

        // No sway while the weapon is being put away / is hidden
        bool swayAllowed = _enableWalkSway && _state != GunState.Hiding && _state != GunState.Hidden;

        float target = swayAllowed ? speedFactor : 0f;
        if (_player != null && _player.IsAiming) target *= _aimSwayMultiplier;

        _swayIntensity = Mathf.MoveTowards(_swayIntensity, target, _swayBlendSpeed * Time.deltaTime);
        _swayPhase = Mathf.Repeat(_swayPhase + Time.deltaTime * _swayFrequency * speedFactor, Mathf.PI * 2f);

        float x = Mathf.Sin(_swayPhase) * _swayAmplitude.x;
        float y = Mathf.Sin(_swayPhase * 2f) * _swayAmplitude.y; // vertical bob = twice per cycle

        _swayOffset = new Vector3(x, y, 0f) * _swayIntensity;
        _swayRoll = Mathf.Sin(_swayPhase) * _swayRollDegrees * _swayIntensity;
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

        PlayBulletTrail();
        ScheduleFireSmoke();
    }

    private void PlayBulletTrail()
    {
        if (_bulletTrail == null) return;

        // Stop emitting (without clearing) then Play: restarts the emission on every shot,
        // while trails from previous shots finish on their own.
        _bulletTrail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        _bulletTrail.Play();
    }

    /// <summary>Smoke starts _smokeDelay seconds after the shot; any new shot cancels it and starts the wait over.</summary>
    private void ScheduleFireSmoke()
    {
        if (_postFireSmoke == null) return;

        if (_smokeRoutine != null) StopCoroutine(_smokeRoutine);
        _postFireSmoke.Stop(true, ParticleSystemStopBehavior.StopEmitting); // existing smoke fades out
        _smokeRoutine = StartCoroutine(FireSmokeRoutine());
    }

    private IEnumerator FireSmokeRoutine()
    {
        yield return new WaitForSeconds(_smokeDelay);

        _postFireSmoke.Play();
        _smokeRoutine = null;
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