using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD controller. It never polls gameplay code: everything arrives through events.
///
///   PlayerHealth.OnHealthChanged   -> health bar + text
///   HandgunController.OnAmmoChanged       -> ammo text
///   HandgunController.OnReloadBegan/Ended -> reload indicator
///   PlayerFPSController.OnAimStateChanged -> STD / ADS cursor switch
///   HandgunController.OnSpreadChanged     -> cursor scale (+25% on a shot, back to normal as the spread recovers)
///
/// Setup: Image components used as fills must have Image Type = Filled.
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private PlayerFPSController _player;
    [SerializeField] private HandgunController _gun;
    [SerializeField] private PlayerHealth _health;

    [Header("Health")]
    [SerializeField] private TMP_Text _healthText;

    [Header("Ammo")]
    [SerializeField] private TMP_Text _ammoText;
    [SerializeField] private Color _ammoNormalColor = Color.white;
    [SerializeField] private Color _ammoLowColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private int _lowAmmoThreshold = 3;

    [Header("Reload")]
    [Tooltip("Shown only while reloading.")]
    [SerializeField] private GameObject _reloadRoot;
    [Tooltip("Image Type = Filled. Goes from 0 to 1 over the reload.")]
    [SerializeField] private Image _reloadFill;

    [Header("Cursors")]
    [Tooltip("STD = cursor when not aiming.")]
    [SerializeField] private RectTransform _stdCursor;
    [Tooltip("ADS = cursor when aiming.")]
    [SerializeField] private RectTransform _adsCursor;
    [Tooltip("Extra scale at max spread (0.25 = +25%).")]
    [SerializeField, Range(0f, 1f)] private float _shotScaleBoost = 0.25f;

    private Vector3 _stdBaseScale = Vector3.one;
    private Vector3 _adsBaseScale = Vector3.one;

    private bool _reloading;
    private float _reloadElapsed;
    private float _reloadDuration = 1f;

    private void Awake()
    {
        if (_stdCursor != null) _stdBaseScale = _stdCursor.localScale;
        if (_adsCursor != null) _adsBaseScale = _adsCursor.localScale;
    }

    private void OnEnable()
    {
        if (_health != null) _health.OnHealthChanged += HandleHealthChanged;

        if (_gun != null)
        {
            _gun.OnAmmoChanged += HandleAmmoChanged;
            _gun.OnSpreadChanged += HandleSpreadChanged;
            _gun.OnReloadBegan += HandleReloadBegan;
            _gun.OnReloadEnded += HandleReloadEnded;
        }

        if (_player != null) _player.OnAimStateChanged += HandleAimStateChanged;
    }

    private void OnDisable()
    {
        if (_health != null) _health.OnHealthChanged -= HandleHealthChanged;

        if (_gun != null)
        {
            _gun.OnAmmoChanged -= HandleAmmoChanged;
            _gun.OnSpreadChanged -= HandleSpreadChanged;
            _gun.OnReloadBegan -= HandleReloadBegan;
            _gun.OnReloadEnded -= HandleReloadEnded;
        }

        if (_player != null) _player.OnAimStateChanged -= HandleAimStateChanged;
    }

    private void Start()
    {
        // Initial sync (events only fire on changes)
        if (_health != null) HandleHealthChanged(_health.Current, _health.Max);
        if (_gun != null) HandleAmmoChanged(_gun.CurrentAmmo, _gun.ReserveAmmo);
        HandleAimStateChanged(_player != null && _player.IsAiming);
        SetReloadVisible(false);
    }

    private void Update()
    {
        // The reload bar animates itself from the duration it was told about
        if (!_reloading) return;

        _reloadElapsed += Time.deltaTime;
        if (_reloadFill != null)
            _reloadFill.fillAmount = Mathf.Clamp01(_reloadElapsed / Mathf.Max(0.01f, _reloadDuration));
    }

    // ------------------------------------------------------------------
    // Health
    // ------------------------------------------------------------------
    private void HandleHealthChanged(float current, float max)
    {
        if (_healthText != null) _healthText.text = Mathf.CeilToInt(current).ToString();
    }

    // ------------------------------------------------------------------
    // Ammo
    // ------------------------------------------------------------------
    private void HandleAmmoChanged(int current, int reserve)
    {
        if (_ammoText == null) return;

        _ammoText.text = $"{current} / {reserve}";
        _ammoText.color = current <= _lowAmmoThreshold ? _ammoLowColor : _ammoNormalColor;
    }

    // ------------------------------------------------------------------
    // Reload
    // ------------------------------------------------------------------
    private void HandleReloadBegan(float duration)
    {
        _reloading = true;
        _reloadElapsed = 0f;
        _reloadDuration = duration;
        if (_reloadFill != null) _reloadFill.fillAmount = 0f;
        SetReloadVisible(true);
    }

    private void HandleReloadEnded(bool completed)
    {
        _reloading = false;
        SetReloadVisible(false);
    }

    private void SetReloadVisible(bool visible)
    {
        if (_reloadRoot != null) _reloadRoot.SetActive(visible);
    }

    // ------------------------------------------------------------------
    // Cursors
    // ------------------------------------------------------------------
    private void HandleAimStateChanged(bool aiming)
    {
        if (_adsCursor != null) _adsCursor.gameObject.SetActive(aiming);
        if (_stdCursor != null) _stdCursor.gameObject.SetActive(!aiming);
    }

    /// <summary>
    /// Cursor size follows the spread: normal size at rest, +25% at max spread.
    /// The gun holds max spread until the end of the mandatory recovery and eases it back
    /// to rest by the end of the full recovery, so the cursor does exactly the same.
    /// </summary>
    private void HandleSpreadChanged(float current, float rest, float max)
    {
        float t = Mathf.InverseLerp(rest, max, current); // 0 = settled, 1 = max
        float scale = 1f + _shotScaleBoost * t;

        if (_stdCursor != null) _stdCursor.localScale = _stdBaseScale * scale;
        if (_adsCursor != null) _adsCursor.localScale = _adsBaseScale * scale;
    }
}