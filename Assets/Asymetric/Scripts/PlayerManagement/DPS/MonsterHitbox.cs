using UnityEngine;

public enum BodyPart
{
    Head,
    Torso,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg
}

/// <summary>
/// Put one of these on every bone that has a hit collider (head, spine, upper arms, legs...).
/// The collider must NOT be a trigger, and should sit on the "EnemyHitbox" layer.
/// The gun's raycast finds this component, which tells the MonsterController what was hit.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MonsterHitbox : MonoBehaviour, IBulletTarget
{
    [SerializeField] private BodyPart part = BodyPart.Torso;
    [Tooltip("Head = 2+, limbs = 0.5 for example. Applied to the weapon's damage.")]
    [SerializeField] private float damageMultiplier = 1f;

    private MonsterController owner;

    public BodyPart Part => part;
    public float DamageMultiplier => damageMultiplier;

    private void Awake()
    {
        owner = GetComponentInParent<MonsterController>();

        if (owner == null)
            Debug.LogWarning($"{name}: MonsterHitbox has no MonsterController in its parents.", this);
    }

    public void OnBulletHit(BulletHitInfo hit)
    {
        if (owner != null)
            owner.ReceiveHit(this, hit);
    }
}