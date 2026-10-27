using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural hit reaction: after the Animator has posed the rig, this adds a small
/// spring-driven rotation on the head and spine chains, so the body snaps and wobbles
/// away from the bullet (and twists when the hit is off-centre).
///
/// No IK needed: this is an additive rotation applied in LateUpdate.
/// The bones MUST be animated by the Animator every frame (any normal clip does that),
/// otherwise the offset would accumulate instead of being overwritten each frame.
/// </summary>
public class MonsterHitReaction : MonoBehaviour
{
    [System.Serializable]
    private class Channel
    {
        [Tooltip("Bones from the base up, e.g. Neck, Head. The rotation is split evenly between them.")]
        public Transform[] bones;

        [HideInInspector] public Vector3 angle;    // current offset: axis * degrees (world space)
        [HideInInspector] public Vector3 velocity; // degrees per second
    }

    [System.Serializable]
    private struct PartResponse
    {
        public BodyPart part;
        [Tooltip("How strongly the head chain reacts when this part is hit.")]
        [Range(0f, 2f)] public float head;
        [Tooltip("How strongly the spine chain reacts when this part is hit.")]
        [Range(0f, 2f)] public float spine;
    }

    [Header("Bones")]
    [SerializeField] private Channel head = new Channel();
    [SerializeField] private Channel spine = new Channel();

    [Header("Strength (approximate peak rotation, degrees)")]
    [Tooltip("Lean / snap-back along the bullet direction.")]
    [SerializeField] private float tiltDegrees = 18f;
    [Tooltip("Spin around the vertical axis, scaled by how off-centre the hit is.")]
    [SerializeField] private float twistDegrees = 25f;

    [Header("Spring")]
    [Tooltip("Oscillations per second. Higher = snappier.")]
    [SerializeField, Range(1f, 6f)] private float frequency = 4f;
    [Tooltip("Low = wobbly, 1 = no overshoot.")]
    [SerializeField, Range(0.1f, 1f)] private float dampingRatio = 0.35f;
    [SerializeField] private float maxAngle = 45f;

    [Header("Per body part")]
    [SerializeField] private PartResponse[] responses;

    /// <summary>Call when a bullet hits. Direction is the way the bullet was travelling.</summary>
    public void ApplyHit(BodyPart part, Vector3 hitPoint, Vector3 bulletDirection)
    {
        // Work on the horizontal plane: shots from above/below still push sideways.
        Vector3 dir = Vector3.ProjectOnPlane(bulletDirection, Vector3.up);
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        // Tilt: rotating around (up x dir) leans "up" toward the bullet direction.
        Vector3 tiltAxis = Vector3.Cross(Vector3.up, dir);

        // Twist: how far the hit is from the monster's vertical centre line.
        // Dead centre = 0, grazing the left/right side = -1 / +1.
        Vector3 offset = Vector3.ProjectOnPlane(hitPoint - transform.position, Vector3.up);
        float lateral = offset.sqrMagnitude > 0.0001f
            ? Vector3.Dot(Vector3.Cross(offset.normalized, dir), Vector3.up)
            : 0f;

        Vector3 impulse = tiltAxis * tiltDegrees + Vector3.up * (lateral * twistDegrees);

        GetWeights(part, out float headWeight, out float spineWeight);
        AddImpulse(head, impulse * headWeight);
        AddImpulse(spine, impulse * spineWeight);
    }

    private void GetWeights(BodyPart part, out float headWeight, out float spineWeight)
    {
        if (responses != null)
        {
            foreach (PartResponse response in responses)
            {
                if (response.part != part) continue;

                headWeight = response.head;
                spineWeight = response.spine;
                return;
            }
        }

        headWeight = 0.3f;
        spineWeight = 1f;
    }

    private void AddImpulse(Channel channel, Vector3 degrees)
    {
        // For a spring, peak displacement ~= initial velocity / omega.
        float omega = 2f * Mathf.PI * frequency;
        channel.velocity += degrees * omega;
    }

    // The Animator runs before LateUpdate, so we add on top of the animated pose.
    private void LateUpdate()
    {
        float dt = Mathf.Min(Time.deltaTime, 0.05f); // keeps the spring stable on frame spikes
        Step(head, dt);
        Step(spine, dt);
    }

    private void Step(Channel channel, float dt)
    {
        if (channel.bones == null || channel.bones.Length == 0) return;

        // Settled: do nothing until the next hit.
        if (channel.angle.sqrMagnitude < 0.0001f && channel.velocity.sqrMagnitude < 0.0001f)
        {
            channel.angle = Vector3.zero;
            channel.velocity = Vector3.zero;
            return;
        }

        float omega = 2f * Mathf.PI * frequency;
        Vector3 acceleration = -omega * omega * channel.angle
                               - 2f * dampingRatio * omega * channel.velocity;

        channel.velocity += acceleration * dt;
        channel.angle = Vector3.ClampMagnitude(channel.angle + channel.velocity * dt, maxAngle);

        float magnitude = channel.angle.magnitude;
        if (magnitude < 0.001f) return;

        // Every bone gets an equal share; they stack down the chain.
        Quaternion share = Quaternion.AngleAxis(magnitude / channel.bones.Length,
            channel.angle / magnitude);

        foreach (Transform bone in channel.bones)
        {
            if (bone != null)
                bone.rotation = share * bone.rotation;
        }
    }

    // Runs when you add the component: fills in sensible defaults.
    private void Reset()
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            head.bones = Collect(animator, HumanBodyBones.Neck, HumanBodyBones.Head);
            spine.bones = Collect(animator, HumanBodyBones.Spine, HumanBodyBones.Chest,
                HumanBodyBones.UpperChest);
        }

        responses = new[]
        {
            new PartResponse { part = BodyPart.Head, head = 1f, spine = 0.35f },
            new PartResponse { part = BodyPart.Torso, head = 0.3f, spine = 1f },
            new PartResponse { part = BodyPart.LeftArm, head = 0.1f, spine = 0.5f },
            new PartResponse { part = BodyPart.RightArm, head = 0.1f, spine = 0.5f },
            new PartResponse { part = BodyPart.LeftLeg, head = 0.05f, spine = 0.3f },
            new PartResponse { part = BodyPart.RightLeg, head = 0.05f, spine = 0.3f },
        };
    }

    private static Transform[] Collect(Animator animator, params HumanBodyBones[] ids)
    {
        List<Transform> list = new List<Transform>();
        foreach (HumanBodyBones id in ids)
        {
            Transform t = animator.GetBoneTransform(id);
            if (t != null) list.Add(t);
        }

        return list.ToArray();
    }
}