using UnityEngine;
using UnityEngine.Events;

public class FlashlightController : MonoBehaviour
{
    [Header("Lights")] [SerializeField] private Light mainLight;
    [SerializeField] private Light longRangeLight;
    [SerializeField] private Light gunLight;

    [Header("Input")] [SerializeField] private KeyCode toggleKey = KeyCode.F;
    [SerializeField] private bool useKeyboardInput = true;

    [Header("Audio - Assign Later")] [SerializeField]
    private AudioSource audioSource;

    [SerializeField] private AudioClip turnOnClip;
    [SerializeField] private AudioClip turnOffClip;
    [SerializeField] private AudioClip flickerClip;

    [Header("Raycast")] [SerializeField] private Transform rayOrigin;
    [SerializeField] private float rayDistance = 60f;
    [SerializeField] private LayerMask raycastMask = ~0;

    [Header("Long Range Light")] [SerializeField]
    private float closeDistance = 3f;

    [SerializeField] private float farDistance = 30f;
    [SerializeField] private float minLongRangeIntensity = 0f;
    [SerializeField] private float maxLongRangeIntensity = 3f;
    [SerializeField] private float intensitySmoothSpeed = 5f;

    [Header("Random Flicker")] [SerializeField]
    private bool enableFlicker = true;

    [SerializeField] private Vector2 flickerInterval = new Vector2(3f, 10f);
    [SerializeField] private Vector2 flickerDuration = new Vector2(0.05f, 0.25f);
    [SerializeField, Range(0f, 1f)] private float flickerChance = 0.5f;
    [SerializeField, Range(0f, 1f)] private float flickerStrength = 0.8f;

    [Header("Battery")] [SerializeField] private bool useBattery = true;
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float batteryLevel = 100f;
    [SerializeField] private float batteryDrainPerSecond = 1f;
    [SerializeField] private bool rechargeWhenOff = false;
    [SerializeField] private float rechargePerSecond = 5f;

    [Header("Events - Optional UI Hooks")] public UnityEvent onTurnedOn;
    public UnityEvent onTurnedOff;
    public UnityEvent onBatteryEmpty;

    public float BatteryLevel => batteryLevel;

    public float BatteryPercent =>
        maxBattery > 0f ? batteryLevel / maxBattery : 0f;

    public bool IsOn => isOn;

    public bool UsesBattery
    {
        get => useBattery;
        set => useBattery = value;
    }

    private bool isOn;
    private bool isFlickering;
    private float nextFlickerTime;
    private float flickerEndTime;
    private float nextLongRangeIntensity;
    private float mainBaseIntensity;
    private float longBaseIntensity;

    private void Awake()
    {
        if (mainLight != null)
            mainBaseIntensity = mainLight.intensity;

        if (longRangeLight != null)
            longBaseIntensity = longRangeLight.intensity;

        maxBattery = Mathf.Max(0f, maxBattery);
        batteryLevel = Mathf.Clamp(batteryLevel, 0f, maxBattery);

        if (rayOrigin == null && Camera.main != null)
            rayOrigin = Camera.main.transform;

        isOn = true;
        ApplyLightState();
        ScheduleNextFlicker();
    }

    private void Update()
    {
        if (useKeyboardInput && Input.GetKeyDown(toggleKey))
            ToggleFlashlight();

        UpdateBattery();
        UpdateLongRangeLight();
        UpdateFlicker();
    }

    public void ToggleFlashlight()
    {
        if (isOn)
            TurnOff();
        else
            TurnOn();
    }

    public void TurnOn()
    {
        if (isOn)
            return;

        if (useBattery && batteryLevel <= 0f)
            return;

        isOn = true;
        isFlickering = false;

        ScheduleNextFlicker();
        ApplyLightState();

        PlayAudio(turnOnClip);
        onTurnedOn?.Invoke();
    }

    public void TurnOff()
    {
        if (!isOn)
            return;

        isOn = false;
        isFlickering = false;

        ApplyLightState();

        PlayAudio(turnOffClip);
        onTurnedOff?.Invoke();
    }

    public void SetBatteryEnabled(bool enabled)
    {
        useBattery = enabled;

        if (!useBattery && batteryLevel <= 0f && !isOn)
            TurnOn();
    }

    public void SetBatteryLevel(float amount)
    {
        batteryLevel = Mathf.Clamp(amount, 0f, maxBattery);

        if (useBattery && batteryLevel <= 0f && isOn)
            DepleteBattery();
    }

    public void RechargeBattery(float amount)
    {
        batteryLevel = Mathf.Clamp(batteryLevel + amount, 0f, maxBattery);
    }

    private void UpdateBattery()
    {
        if (!useBattery)
            return;

        if (isOn)
        {
            batteryLevel -= batteryDrainPerSecond * Time.deltaTime;
            batteryLevel = Mathf.Max(0f, batteryLevel);

            if (batteryLevel <= 0f)
                DepleteBattery();
        }
        else if (rechargeWhenOff)
        {
            batteryLevel += rechargePerSecond * Time.deltaTime;
            batteryLevel = Mathf.Min(maxBattery, batteryLevel);
        }
    }

    private void DepleteBattery()
    {
        if (!isOn)
            return;

        isOn = false;
        isFlickering = false;

        ApplyLightState();
        onBatteryEmpty?.Invoke();
    }

    private void UpdateLongRangeLight()
    {
        if (longRangeLight == null)
            return;

        float targetIntensity = minLongRangeIntensity;

        if (isOn && rayOrigin != null)
        {
            float distance = rayDistance;

            if (Physics.Raycast(
                    rayOrigin.position,
                    rayOrigin.forward,
                    out RaycastHit hit,
                    rayDistance,
                    raycastMask,
                    QueryTriggerInteraction.Ignore))
            {
                distance = hit.distance;
            }

            float t = Mathf.InverseLerp(
                closeDistance,
                farDistance,
                distance
            );

            targetIntensity = Mathf.Lerp(
                minLongRangeIntensity,
                maxLongRangeIntensity,
                t
            );
        }

        nextLongRangeIntensity = Mathf.Lerp(
            nextLongRangeIntensity,
            targetIntensity,
            Time.deltaTime * intensitySmoothSpeed
        );

        float flickerMultiplier = isFlickering
            ? 1f - flickerStrength
            : 1f;

        longRangeLight.intensity = isOn
            ? nextLongRangeIntensity * flickerMultiplier
            : 0f;
    }

    private void UpdateFlicker()
    {
        if (!isOn || !enableFlicker)
        {
            isFlickering = false;
            return;
        }

        if (isFlickering)
        {
            if (Time.time >= flickerEndTime)
            {
                isFlickering = false;
                ScheduleNextFlicker();
            }

            return;
        }

        if (Time.time >= nextFlickerTime)
        {
            if (Random.value <= flickerChance)
            {
                isFlickering = true;
                flickerEndTime = Time.time +
                                 Random.Range(flickerDuration.x, flickerDuration.y);

                PlayAudio(flickerClip);
            }
            else
            {
                ScheduleNextFlicker();
            }
        }
    }

    private void ScheduleNextFlicker()
    {
        nextFlickerTime = Time.time +
                          Random.Range(flickerInterval.x, flickerInterval.y);
    }

    private void ApplyLightState()
    {
        if (mainLight != null)
        {
            mainLight.enabled = isOn;
            mainLight.intensity = isOn
                ? nextLongRangeIntensity
                : 0f;
        }

        if (longRangeLight != null)
        {
            longRangeLight.enabled = isOn;
            longRangeLight.intensity = isOn
                ? nextLongRangeIntensity
                : 0f;
        }

        if (gunLight != null)
        {
            gunLight.enabled = isOn;
        }
    }

    private void PlayAudio(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    public void Switch()
    {
        if (isOn) TurnOff();
        else TurnOn();
    }
}