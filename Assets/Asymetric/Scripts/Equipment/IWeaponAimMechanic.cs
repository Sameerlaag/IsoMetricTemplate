using UnityEngine;

public interface IWeaponAimMechanic
{
    /// <summary>
    /// Called when the weapon mechanic is initialized or equipped.
    /// </summary>
    void Initialize(WeaponData data);

    /// <summary>
    /// Updates reticle size and precision spread based on player movement speed, mouse cursor velocity, and player-to-cursor distance.
    /// </summary>
    void UpdateAim(Vector3 aimPoint, float playerSpeed, Vector2 mouseDelta, float cursorDistance, Camera camera);

    /// <summary>
    /// Current normalized reticle scale/size (1.0 = base size, lower = smaller/more precise).
    /// </summary>
    float CurrentReticleScale { get; }

    /// <summary>
    /// Evaluates shot accuracy against enemy hit colliders using reticle overlap percentage.
    /// Returns true if hit succeeded, along with calculated hit probability.
    /// </summary>
    bool EvaluateShot(Vector3 aimPoint, Camera camera, LayerMask enemyLayer, out float hitProbability, out RaycastHit hitInfo);
}
