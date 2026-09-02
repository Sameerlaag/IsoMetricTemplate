using UnityEngine;

public class RevolverAimMechanic : MonoBehaviour, IWeaponAimMechanic
{
    [Header("Revolver Precision Parameters")]
    [SerializeField] private float _minReticleScale = 0.4f;  // Scale when fully focused
    [SerializeField] private float _maxReticleScale = 1.8f;  // Scale when moving fast
    [SerializeField] private float _focusSpeed = 3f;         // How fast reticle shrinks when still
    [SerializeField] private float _expandSpeed = 6f;        // How fast reticle expands on movement
    [SerializeField] private float _mouseVelocitySensitivity = 0.05f;

    [Header("Reticle Overlap Settings")]
    [SerializeField] private float _baseReticleWorldRadius = 0.5f;

    private WeaponData _data;
    private float _currentScale = 1f;

    public float CurrentReticleScale => _currentScale;

    public void Initialize(WeaponData data)
    {
        _data = data;
        _currentScale = 1f;
    }

    [SerializeField] private float _distanceScaleFactor = 0.002f; // Scale increase per screen pixel distance

    public void UpdateAim(Vector3 aimPoint, float playerSpeed, Vector2 mouseDelta, float cursorDistance, Camera camera)
    {
        float mouseSpeed = mouseDelta.magnitude * _mouseVelocitySensitivity;
        float distanceExpansion = cursorDistance * _distanceScaleFactor;
        float totalMovementFactor = playerSpeed + mouseSpeed;

        float baseTargetScale = _minReticleScale + distanceExpansion;

        if (totalMovementFactor < 0.1f)
        {
            // Player is still and mouse is steady -> shrink reticle towards base distance scale
            _currentScale = Mathf.MoveTowards(_currentScale, Mathf.Clamp(baseTargetScale, _minReticleScale, _maxReticleScale), _focusSpeed * Time.deltaTime);
        }
        else
        {
            // Moving or moving mouse rapidly -> expand reticle further
            float targetScale = Mathf.Clamp(baseTargetScale + (totalMovementFactor * 0.2f), _minReticleScale, _maxReticleScale);
            _currentScale = Mathf.MoveTowards(_currentScale, targetScale, _expandSpeed * Time.deltaTime);
        }
    }

    public bool EvaluateShot(Vector3 aimPoint, Camera camera, LayerMask enemyLayer, out float hitProbability, out RaycastHit hitInfo)
    {
        hitProbability = 0f;
        float reticleRadius = _baseReticleWorldRadius * _currentScale;

        // Check enemies within reticle radius around aimPoint
        Collider[] hitColliders = Physics.OverlapSphere(aimPoint, reticleRadius, enemyLayer);

        if (hitColliders.Length > 0)
        {
            // Find closest enemy collider
            Collider targetCollider = hitColliders[0];
            Vector3 targetCenter = targetCollider.bounds.center;

            // Calculate distance ratio from aim point to target center
            float distanceToCenter = Vector3.Distance(aimPoint, targetCenter);
            float maxDistance = reticleRadius + targetCollider.bounds.extents.magnitude;

            // Overlap percentage: 1.0 at center, decreasing to 0 at edge
            float overlapRatio = Mathf.Clamp01(1f - (distanceToCenter / Mathf.Max(maxDistance, 0.01f)));

            // Precision modifier: smaller reticle gives bonus precision
            float precisionMultiplier = Mathf.Lerp(1.2f, 0.6f, (_currentScale - _minReticleScale) / (_maxReticleScale - _minReticleScale));
            hitProbability = Mathf.Clamp01(overlapRatio * precisionMultiplier);

            // Roll hit chance
            bool isHit = Random.value <= hitProbability;

            Physics.Raycast(camera.transform.position, (aimPoint - camera.transform.position).normalized, out hitInfo, 100f, enemyLayer);
            return isHit;
        }

        // Raycast fallback if direct raycast hits
        Ray ray = camera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        if (Physics.Raycast(aimPoint + Vector3.up * 10f, Vector3.down, out hitInfo, 20f, enemyLayer))
        {
            hitProbability = 0.5f;
            return Random.value <= hitProbability;
        }

        hitInfo = default;
        return false;
    }
}
