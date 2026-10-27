
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class MonsterController : MonoBehaviour
{
    private enum State
    {
        Idle,
        Wandering,
        Investigating,
        Chasing,
        Attacking,
        Dead
    }

    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerHealth playerHealth;
    [Header("Movement")] [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float wanderRadius = 6f;
    [SerializeField] private float wanderInterval = 8f;
    [SerializeField, Range(0f, 1f)]
    private float wanderChance = 0.25f;
    [SerializeField] private float arrivalDistance = 1f;
    [SerializeField] private float investigationTimeout = 8f;
    [SerializeField] private float loseInterestTime = 12f;

    [Header("Animation")]
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private float animationDampTime = 0.1f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 10f;
    [SerializeField] private float staminaDrainPerSecond = 1f;
    [SerializeField] private float staminaRecoveryPerSecond = 1.5f;

    [Header("Attacks")]
    [SerializeField] private float lungeRange = 2f;
    [SerializeField] private float lungeDamage = 30f;
    [SerializeField] private float lungeCooldown = 2f;

    [SerializeField] private float screamRange = 5f;
    [SerializeField] private float screamCooldown = 12f;
    [SerializeField] private float screamDamagePerSecond = 20f;
    [SerializeField] private float screamDuration = 5f;

    [SerializeField] private float attackLockout = 1f;

    [Header("Hearing")]
    [SerializeField] private float gunshotHearingRange = 25f;

    [Header("Bullet Hits")]
    [SerializeField] private MonsterHitReaction hitReaction;

    [Tooltip("A random clip is played at the hit point.")]
    [SerializeField] private AudioClip[] hitSounds;

    [SerializeField, Range(0f, 1f)]
    private float hitSoundVolume = 1f;

    [Tooltip("Blood burst, parented to the bone that was hit.")]
    [SerializeField] private GameObject bloodHitVfxPrefab;

    [SerializeField] private float bloodVfxLifetime = 2f;

    [Tooltip("Decal projected onto the monster's body, parented to the bone.")]
    [SerializeField] private GameObject bloodMeshDecalPrefab;

    [SerializeField] private GameObject bloodGroundDecalPrefab;

    [Tooltip("Environment layers only. Exclude the monster's own layers.")]
    [SerializeField] private LayerMask groundMask = ~0;

    [SerializeField] private float groundCheckDistance = 4f;

    [Tooltip("Ground blood lands up to this far along the bullet direction.")]
    [SerializeField] private float groundSplatterDistance = 0.8f;

    
    
    [Header("Monster Audio")]
    [SerializeField] private AudioSource ambientAudioSource;
    [SerializeField] private AudioSource actionAudioSource;

    [SerializeField] private AudioClip[] wheepings;
    [SerializeField] private AudioClip[] chasings;
    [SerializeField] private AudioClip[] attackings;
    [SerializeField] private AudioClip[] screamings;
    [SerializeField] private AudioClip[] chockings;

    [SerializeField, Range(0f, 1f)]
    private float ambientVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float actionVolume = 1f;

    [SerializeField] private FeetAudio feetAudio;

    private enum AmbientAudio
    {
        None,
        Wheeping,
        Chasing
    }

    private AmbientAudio currentAmbientAudio = AmbientAudio.None;

    
    
    private MonsterWatcher watcher;
    private Transform player;

    private State state = State.Idle;

    private float stamina;
    private float nextWanderTime;
    private float lastKnownTime;
    private float lastSightTime;
    private float nextLungeTime;
    private float nextScreamTime;
    private float attackEndTime;

    private Vector3 lastKnownPosition;
    private Vector3 spawnPosition;

    private bool hasTarget;
    private bool isDead;
    private bool playerVisible;
    private bool initialized;

    private string currentAnimationTrigger;

    private Coroutine screamRoutine;

    public float GunshotHearingRange => gunshotHearingRange;

    public bool CanPerceive =>
        state != State.Dead && isActiveAndEnabled;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (hitReaction == null)
            hitReaction = GetComponent<MonsterHitReaction>();

        stamina = maxStamina;
    }

    public void Initialize(MonsterWatcher owner, Transform target)
    {
        watcher = owner;
        player = target;
        initialized = true;

        if (agent != null && agent.isOnNavMesh)
            spawnPosition = agent.transform.position;
        else
            spawnPosition = transform.position;
    }

    private void Start()
    {
        if (!initialized)
        {
            Debug.LogWarning(
                $"{name}: No MonsterWatcher initialization was received.",
                this
            );
        }

        if (watcher != null)
            watcher.RegisterMonster(this);

        if (player == null && watcher != null)
            player = watcher.Player;

        if (agent == null ||
            !agent.isActiveAndEnabled ||
            !agent.isOnNavMesh)
        {
            Debug.LogError(
                $"{name}: Monster needs an enabled NavMeshAgent " +
                "positioned on a baked NavMesh.",
                this
            );

            enabled = false;
            return;
        }

        agent.speed = walkSpeed;
        agent.isStopped = false;

        PlayAnimation("Movement");

        nextWanderTime =
            Time.time + Random.Range(2f, wanderInterval);
    }

    private void Update()
    {
        if (agent == null ||
            !agent.isActiveAndEnabled ||
            !agent.isOnNavMesh)
        {
            return;
        }

        // Update the Blend Tree from actual movement velocity.
        UpdateAnimationSpeed();
        UpdateAmbientAudio();
        
        feetAudio.PlayMonsterFootsteps(
            agent.isOnNavMesh,
            agent.velocity.magnitude
        );
        
        // A dead monster never resumes AI behavior.
        if (state == State.Dead)
            return;

        // Forget the player after losing interest.
        if (player != null && hasTarget)
        {
            if (Time.time - lastSightTime > loseInterestTime &&
                state != State.Attacking)
            {
                ForgetPlayer();
            }
        }

        switch (state)
        {
            case State.Idle:
                UpdateIdle();
                break;

            case State.Wandering:
                UpdateWandering();
                break;

            case State.Investigating:
                UpdateInvestigating();
                break;

            case State.Chasing:
                UpdateChasing();
                break;

            case State.Attacking:
                UpdateAttacking();
                break;
        }
    }

    private void UpdateAnimationSpeed()
    {
        if (animator == null)
            return;

        float speed = agent.velocity.magnitude;

        animator.SetFloat(
            speedParameter,
            speed,
            animationDampTime,
            Time.deltaTime
        );
    }

    // Called by MonsterWatcher when the player is visible.
    public void SeePlayer(Vector3 position)
    {
        if (!CanPerceive)
            return;

        lastKnownPosition = position;
        lastKnownTime = Time.time;
        lastSightTime = Time.time;

        hasTarget = true;
        playerVisible = true;

        if (state != State.Attacking)
            state = State.Chasing;
    }

    public void HearNoise(Vector3 position)
    {
        if (!CanPerceive || playerVisible)
            return;

        // Don't reset the investigation timer every frame.
        if (state == State.Investigating)
            return;

        lastKnownPosition = position;
        lastKnownTime = Time.time;
        hasTarget = true;

        if (state != State.Attacking)
            state = State.Investigating;
    }

    private void UpdateIdle()
    {
        playerVisible = false;

        agent.isStopped = true;
        agent.speed = walkSpeed;

        RecoverStamina();

        // Movement is also the idle Blend Tree state.
        PlayAnimation("Movement");

        if (Time.time >= nextWanderTime)
        {
            nextWanderTime = Time.time + wanderInterval;

            if (Random.value < wanderChance &&
                TryChooseWanderPosition(out Vector3 destination))
            {
                agent.isStopped = false;
                agent.speed = walkSpeed;
                agent.SetDestination(destination);

                state = State.Wandering;
                PlayAnimation("Movement");
            }
        }
    }

    private void UpdateWandering()
    {
        RecoverStamina();

        agent.speed = walkSpeed;
        PlayAnimation("Movement");

        if (!agent.pathPending &&
            (!agent.hasPath ||
             agent.remainingDistance <= arrivalDistance))
        {
            EnterIdle();
        }
    }

    private void UpdateInvestigating()
    {
        RecoverStamina();

        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.SetDestination(lastKnownPosition);

        PlayAnimation("Movement");

        if (Time.time - lastKnownTime > investigationTimeout)
        {
            ForgetPlayer();
            return;
        }

        if (!agent.pathPending &&
            (!agent.hasPath ||
             agent.remainingDistance <= arrivalDistance))
        {
            EnterIdle();
        }
    }

    private void UpdateChasing()
    {
        if (player == null)
        {
            state = State.Investigating;
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        // Lunge when the player is visible and within range.
        if (playerVisible &&
            distance <= lungeRange &&
            Time.time >= nextLungeTime)
        {
            StartLunge();
            return;
        }

        // Scream when the player is visible but outside lunge range.
        if (playerVisible &&
            distance <= screamRange &&
            distance > lungeRange &&
            Time.time >= nextScreamTime)
        {
            StartScream();
            return;
        }

        // Walk while stamina recovers.
        if (stamina <= 0f)
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.SetDestination(lastKnownPosition);

            RecoverStamina();
            PlayAnimation("Movement");
            return;
        }

        stamina = Mathf.Max(
            0f,
            stamina - staminaDrainPerSecond * Time.deltaTime
        );

        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.SetDestination(lastKnownPosition);

        PlayAnimation("Movement");
    }

    private void UpdateAttacking()
    {
        agent.isStopped = true;
        RecoverStamina();

        // Turn towards the player during an attack.
        if (player != null)
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    360f * Time.deltaTime
                );
            }
        }

        if (Time.time >= attackEndTime)
        {
            if (playerVisible && player != null)
                state = State.Chasing;
            else
                state = State.Investigating;

            // Enter the movement Blend Tree again.
            PlayAnimation("Movement");
        }
    }

    private void StartLunge()
    {
        nextLungeTime = Time.time + lungeCooldown;
        attackEndTime = Time.time + attackLockout;

        state = State.Attacking;

        agent.isStopped = true;
        PlayAnimation("Attack");
        PlayOneShotRandom(attackings);
        
        // Temporary damage timing.
        // Replace with an animation event when ready.
        if (player != null &&
            Vector3.Distance(transform.position, player.position)
                <= lungeRange + 0.5f)
        {
            DealDamage(player, lungeDamage);
        }
    }

    private void StartScream()
    {
        nextScreamTime = Time.time + screamCooldown;
        attackEndTime = Time.time + screamDuration;

        state = State.Attacking;

        agent.isStopped = true;
        PlayAnimation("Scream");
        PlayOneShotRandom(screamings);
        
        if (screamRoutine != null)
            StopCoroutine(screamRoutine);

        screamRoutine = StartCoroutine(ScreamDamageRoutine());
    }

    private IEnumerator ScreamDamageRoutine()
    {
        float elapsed = 0f;

        while (elapsed < screamDuration)
        {
            yield return new WaitForSeconds(1f);
            elapsed += 1f;

            if (state == State.Dead)
                break;

            if (player == null)
                continue;

            if (Vector3.Distance(transform.position, player.position)
                <= screamRange)
            {
                DealDamage(player, screamDamagePerSecond);
            }
        }

        screamRoutine = null;
    }

    private void DealDamage(Transform target, float damage)
    {
        MonoBehaviour[] components =
            target.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component is IDamageable damageable)
            {
                damageable.TakeDamage(damage, transform);
                return;
            }
        }

        // Compatibility with an existing health script.
        target.SendMessage(
            "TakeDamage",
            damage,
            SendMessageOptions.DontRequireReceiver
        );
    }

    private bool TryChooseWanderPosition(out Vector3 destination)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector3 candidate = spawnPosition +
                Random.insideUnitSphere * wanderRadius;

            if (NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    2f,
                    agent.areaMask))
            {
                destination = hit.position;
                return true;
            }
        }

        destination = transform.position;
        return false;
    }

    private void RecoverStamina()
    {
        stamina = Mathf.Min(
            maxStamina,
            stamina + staminaRecoveryPerSecond * Time.deltaTime
        );
    }

    private void EnterIdle()
    {
        state = State.Idle;

        agent.isStopped = true;
        PlayAnimation("Movement");

        nextWanderTime =
            Time.time + Random.Range(2f, wanderInterval);
    }

    private void ForgetPlayer()
    {
        hasTarget = false;
        playerVisible = false;
        state = State.Idle;

        agent.isStopped = true;
        PlayAnimation("Movement");

        nextWanderTime = Time.time + 2f;
    }

    // Permanently kills/incapacitates the monster.
    // Call this when the monster's health reaches zero.
    public void DiePermanently()
    {
        if (state == State.Dead)
            return;

        state = State.Dead;
        playerVisible = false;
        hasTarget = false;

        if (screamRoutine != null)
        {
            StopCoroutine(screamRoutine);
            screamRoutine = null;
        }

        if (agent != null &&
            agent.isActiveAndEnabled &&
            agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        StopMonsterAudio();
        PlayOneShotRandom(chockings);

        if (animator != null)
        {
            animator.SetFloat(speedParameter, 0f);
            PlayAnimation("Die");
        }
    }

    // Called by MonsterHitbox.
    public void ReceiveHit(MonsterHitbox hitbox, BulletHitInfo hit)
    {
        if (hitbox == null)
            return;

        BodyPart part = hitbox.Part;
        float damage = hit.Damage * hitbox.DamageMultiplier;


        BulletImpactManager impacts = BulletImpactManager.Instance;

        if (impacts != null)
        {
            Transform bone = hitbox.transform;

            // 1. Random hit sound.
            impacts.PlaySfx(
                hitSounds,
                hit.Point,
                hitSoundVolume
            );

            // 2. Blood burst following the animated bone.
            impacts.SpawnEffect(
                bloodHitVfxPrefab,
                hit.Point + hit.Normal * 0.01f,
                Quaternion.LookRotation(hit.Normal),
                bone,
                bloodVfxLifetime
            );

            // 3. Blood decal on the monster.
            impacts.SpawnDecal(
                bloodMeshDecalPrefab,
                hit.Point + hit.Normal * 0.1f,
                BulletImpactManager.DecalRotation(hit.Normal),
                bone
            );

            // 4. Blood decal on the ground.
            SpawnGroundBlood(impacts, hit);
        }

        // 5. Body reaction.
        if (hitReaction != null)
            hitReaction.ApplyHit(part, hit.Point, hit.Direction);


        // TODO when the monster gets health:
        if (playerHealth != null) playerHealth.TakeDamage(damage);
        if (playerHealth.Current <= 0f) DiePermanently();
    }

    private void SpawnGroundBlood(
        BulletImpactManager impacts,
        BulletHitInfo hit)
    {
        if (bloodGroundDecalPrefab == null)
            return;

        Vector3 flat = Vector3.ProjectOnPlane(
            hit.Direction,
            Vector3.up
        ).normalized;

        Vector3 origin = hit.Point
            + flat * Random.Range(0.1f, groundSplatterDistance)
            + Vector3.up * 0.1f;

        if (Physics.Raycast(
                origin,
                Vector3.down,
                out RaycastHit ground,
                groundCheckDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            impacts.SpawnDecal(
                bloodGroundDecalPrefab,
                ground.point + ground.normal * 0.1f,
                BulletImpactManager.DecalRotation(ground.normal)
            );
        }
    }

    private void PlayAnimation(string trigger)
    {
        if (animator == null ||
            currentAnimationTrigger == trigger)
        {
            return;
        }

        animator.ResetTrigger("Movement");
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Scream");
        animator.ResetTrigger("Die");

        currentAnimationTrigger = trigger;
        animator.SetTrigger(trigger);
    }

    public void LoseSight()
    {
        playerVisible = false;
    }

    private void OnDestroy()
    {
        if (watcher != null)
            watcher.UnregisterMonster(this);
    }
    
    
    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;

        // Avoid selecting empty slots.
        int validCount = 0;

        foreach (AudioClip clip in clips)
        {
            if (clip != null)
                validCount++;
        }

        if (validCount == 0)
            return null;

        int selected = Random.Range(0, validCount);

        foreach (AudioClip clip in clips)
        {
            if (clip == null)
                continue;

            if (selected == 0)
                return clip;

            selected--;
        }

        return null;
    }

    private void PlayOneShotRandom(AudioClip[] clips)
    {
        if (actionAudioSource == null)
            return;

        AudioClip clip = GetRandomClip(clips);

        if (clip == null)
            return;

        actionAudioSource.PlayOneShot(clip, actionVolume);
    }

    private void UpdateAmbientAudio()
    {
        if (ambientAudioSource == null)
            return;

        AmbientAudio desiredAudio;

        if (state == State.Dead)
        {
            desiredAudio = AmbientAudio.None;
        }
        else if (state == State.Chasing)
        {
            desiredAudio = AmbientAudio.Chasing;
        }
        else
        {
            desiredAudio = AmbientAudio.Wheeping;
        }

        if (desiredAudio == currentAmbientAudio)
            return;

        currentAmbientAudio = desiredAudio;

        ambientAudioSource.Stop();
        ambientAudioSource.clip = null;

        if (desiredAudio == AmbientAudio.None)
            return;

        AudioClip[] clips =
            desiredAudio == AmbientAudio.Chasing
                ? chasings
                : wheepings;

        AudioClip clip = GetRandomClip(clips);

        if (clip == null)
            return;

        ambientAudioSource.clip = clip;
        ambientAudioSource.loop = true;
        ambientAudioSource.volume = ambientVolume;
        ambientAudioSource.Play();
    }

    private void StopMonsterAudio()
    {
        currentAmbientAudio = AmbientAudio.None;

        if (ambientAudioSource != null)
            ambientAudioSource.Stop();

        if (actionAudioSource != null)
            actionAudioSource.Stop();
    }

}
