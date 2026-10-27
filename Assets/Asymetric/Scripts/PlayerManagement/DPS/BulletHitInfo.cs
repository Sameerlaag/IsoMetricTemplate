using UnityEngine;

/// <summary>Everything a target needs to know about one bullet hit.</summary>
public struct BulletHitInfo
{
    public Collider Collider;   // the exact collider that was hit (e.g. the head hitbox)
    public Vector3 Point;       // world position of the hit
    public Vector3 Normal;      // surface normal at the hit point
    public Vector3 Direction;   // direction the bullet was travelling (normalized)
    public float Damage;        // base damage of the weapon (before body-part multipliers)
    public Transform Shooter;
}

/// <summary>
/// Implement this on anything that wants to react to bullets by itself
/// (enemy hitboxes, destructible props...). Anything WITHOUT it is treated as
/// a plain surface and goes through BulletImpactManager.
/// </summary>
public interface IBulletTarget
{
    void OnBulletHit(BulletHitInfo hit);
}