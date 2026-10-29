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
        Fleeing,
        Attacking,
        Dead
    }

    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerHealth playerHealth;

    [Header("Personality & Roaming")]
    [SerializeField] private MonsterPersonality personality = MonsterPersonality.Aggressive;
    [SerializeField] private float walkSpeed = 1.5f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float idleWaitMin = 3f;
    [SerializeField] private float idleWaitMax = 7f;
    [SerializeField] private float arrivalDistance = 1f;
    [SerializeField] private float fleeDistance = 18f;

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
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField, Range(0f, 1f)] private float hitSoundVolume = 1f;
    [SerializeField] private GameObject bloodHitVfxPrefab;
    [SerializeField] private float bloodVfxLifetime = 2f;
    [SerializeField] private GameObject bloodMeshDecalPrefab;
    [SerializeField] private GameObject bloodGroundDecalPrefab;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckDistance = 4f;
    [SerializeField] private float groundSplatterDistance = 0.8f;

    [Header("Monster Audio")]
    [SerializeField] private AudioSource ambientAudioSource;
    [SerializeField] private AudioSource actionAudioSource;
    [SerializeField] private AudioClip[] wheepings;
    [SerializeField] private AudioClip[] chasings;
    [SerializeField] private AudioClip[] attackings;
    [SerializeField] private AudioClip[] screamings;
    [SerializeField] private AudioClip[] chockings;
    [SerializeField, Range(0f, 1f)] private float ambientVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float actionVolume = 1f;
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
    private float lastKnownTime;
    private float lastSightTime;
    private float nextLungeTime;
    private float nextScreamTime;
    private float attackEndTime;
    private float stateTimer;

    private Vector3 lastKnownPosition;
    
    // Waypoint network fields
    private Waypoint currentWaypoint;
    private Waypoint targetWaypoint;
    private bool isStandingAround;

    private bool hasTarget;
    private bool isDead;
    private bool playerVisible;
    private bool initialized;

    private string currentAnimationTrigger;
    private Coroutine screamRoutine;

    public float GunshotHearingRange => gunshotHearingRange;
    public bool CanPerceive => state != State.Dead && isActiveAndEnabled;

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

    public void Initialize(MonsterWatcher owner, Transform target, Waypoint spawnNode)
    {
        watcher = owner;
        player = target;
        currentWaypoint = spawnNode;
        initialized = true;

        PickNextWaypoint();
    }

    public void ReleaseAssignedWaypoint()
    {
        if (currentWaypoint != null)
        {
            currentWaypoint.isOccupied = false;
            currentWaypoint = null;
        }
    }

    private void PickNextWaypoint()
    {
        if (currentWaypoint != null && currentWaypoint.connectedWaypoints.Count > 0)
        {
            targetWaypoint = currentWaypoint.connectedWaypoints[Random.Range(0, currentWaypoint.connectedWaypoints.Count)];
        }
        else if (watcher != null && watcher.AllWaypoints.Count > 0)
        {
            targetWaypoint = watcher.AllWaypoints[Random.Range(0, watcher.AllWaypoints.Count)];
        }
    }

    private void Start()
    {
        if (!initialized)
        {
            Debug.LogWarning($"{name}: No MonsterWatcher initialization was received.", this);
        }

        if (watcher != null)
            watcher.RegisterMonster(this);

        if (player == null && watcher != null)
            player = watcher.Player;

        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            Debug.LogError($"{name}: Monster needs an enabled NavMeshAgent positioned on a baked NavMesh.", this);
            enabled = false;
            return;
        }

        agent.speed = walkSpeed;
        agent.isStopped = false;

        PlayAnimation("Movement");
        
        // Start standing around briefly before first move
        isStandingAround = true;
        stateTimer = Time.time + Random.Range(idleWaitMin, idleWaitMax);
    }

    private void Update()
    {
        if (agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
            return;

        UpdateAnimationSpeed();
        UpdateAmbientAudio();
        
        if (feetAudio != null)
        {
            feetAudio.PlayMonsterFootsteps(agent.isOnNavMesh, agent.velocity.magnitude);
        }
        
        if (state == State.Dead)
            return;

        if (player != null && hasTarget && state != State.Fleeing)
        {
            if (Time.time - lastSightTime > loseInterestTime && state != State.Attacking)
            {
                ForgetPlayer();
            }
        }

        switch (state)
        {
            case State.Idle:
            case State.Wandering:
                UpdateRoamingNetwork();
                break;

            case State.Investigating:
                UpdateInvestigating();
                break;

            case State.Chasing:
                UpdateChasing();
                break;

            case State.Fleeing:
                UpdateFleeing();
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
        animator.SetFloat(speedParameter, speed, animationDampTime, Time.deltaTime);
    }

    public void SeePlayer(Vector3 position)
    {
        if (!CanPerceive)
            return;

        lastKnownPosition = position;
        lastKnownTime = Time.time;
        lastSightTime = Time.time;

        hasTarget = true;
        playerVisible = true;

        if (personality == MonsterPersonality.PassiveNeutral)
            return; // Ignore player unless provoked/shot

        if (personality == MonsterPersonality.CowardlyFlee)
        {
            StartFleeing(position);
            return;
        }

        if (state != State.Attacking)
            state = State.Chasing;
    }

    public void HearNoise(Vector3 position)
    {
        if (!CanPerceive || playerVisible)
            return;

        if (personality == MonsterPersonality.PassiveNeutral)
            return;

        if (personality == MonsterPersonality.CowardlyFlee)
        {
            StartFleeing(position);
            return;
        }

        if (state == State.Investigating)
            return;

        lastKnownPosition = position;
        lastKnownTime = Time.time;
        hasTarget = true;

        if (state != State.Attacking)
            state = State.Investigating;
    }

    public void OnGunshotHeard(Vector3 shotPosition)
    {
        if (!CanPerceive)
            return;

        if (personality == MonsterPersonality.CowardlyFlee)
        {
            StartFleeing(shotPosition);
        }
        else if (personality == MonsterPersonality.Aggressive)
        {
            HearNoise(shotPosition);
        }
        // PassiveNeutral can choose to flee or ignore, let's make them flee on gunshot too for realism, or ignore. Let's make them check distance or run away:
    }

    private void StartFleeing(Vector3 threatPosition)
    {
        state = State.Fleeing;
        Vector3 dirAway = (transform.position - threatPosition).normalized;
        Vector3 fleeDestination = transform.position + dirAway * fleeDistance;

        if (NavMesh.SamplePosition(fleeDestination, out NavMeshHit hit, 6f, agent.areaMask))
        {
            agent.isStopped = false;
            agent.speed = runSpeed;
            agent.SetDestination(hit.position);
        }
    }

    private void UpdateFleeing()
    {
        agent.speed = runSpeed;
        PlayAnimation("Movement");

        if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= arrivalDistance))
        {
            ForgetPlayer();
        }
    }

    private void UpdateRoamingNetwork()
    {
        playerVisible = false;
        RecoverStamina();

        if (isStandingAround)
        {
            agent.isStopped = true;
            PlayAnimation("Movement");

            if (Time.time >= stateTimer)
            {
                isStandingAround = false;
                agent.isStopped = false;
                agent.speed = walkSpeed;

                if (targetWaypoint != null)
                {
                    if (currentWaypoint != null)
                        currentWaypoint.isOccupied = false;

                    currentWaypoint = targetWaypoint;
                    currentWaypoint.isOccupied = true;

                    agent.SetDestination(currentWaypoint.Position);
                    PickNextWaypoint();
                }

                state = State.Wandering;
                PlayAnimation("Movement");
            }
            return;
        }

        agent.speed = walkSpeed;
        PlayAnimation("Movement");

        if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= arrivalDistance))
        {
            // Arrived at destination waypoint, now stand around
            isStandingAround = true;
            stateTimer = Time.time + Random.Range(idleWaitMin, idleWaitMax);
            state = State.Idle;
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

        if (!agent.pathPending && (!agent.hasPath || agent.remainingDistance <= arrivalDistance))
        {
            ForgetPlayer();
        }
    }

    private void UpdateChasing()
    {
        if (player == null)
        {
            state = State.Investigating;
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (playerVisible && distance <= lungeRange && Time.time >= nextLungeTime)
        {
            StartLunge();
            return;
        }

        if (playerVisible && distance <= screamRange && distance > lungeRange && Time.time >= nextScreamTime)
        {
            StartScream();
            return;
        }

        if (stamina <= 0f)
        {
            agent.isStopped = false;
            agent.speed = walkSpeed;
            agent.SetDestination(lastKnownPosition);

            RecoverStamina();
            PlayAnimation("Movement");
            return;
        }

        stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * Time.deltaTime);

        agent.isStopped = false;
        agent.speed = runSpeed;
        agent.SetDestination(player.position);

        PlayAnimation("Movement");
    }

    private void UpdateAttacking()
    {
        agent.isStopped = true;
        RecoverStamina();

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

        if (player != null && Vector3.Distance(transform.position, player.position) <= lungeRange + 0.5f)
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

            if (Vector3.Distance(transform.position, player.position) <= screamRange)
            {
                DealDamage(player, screamDamagePerSecond);
            }
        }

        screamRoutine = null;
    }

    private void DealDamage(Transform target, float damage)
    {
        MonoBehaviour[] components = target.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component is IDamageable damageable)
            {
                damageable.TakeDamage(damage, transform);
                return;
            }
        }

        target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    private void RecoverStamina()
    {
        stamina = Mathf.Min(maxStamina, stamina + staminaRecoveryPerSecond * Time.deltaTime);
    }

    private void ForgetPlayer()
    {
        hasTarget = false;
        playerVisible = false;
        state = State.Idle;

        agent.isStopped = true;
        PlayAnimation("Movement");

        isStandingAround = true;
        stateTimer = Time.time + Random.Range(idleWaitMin, idleWaitMax);
    }

    public void DiePermanently()
    {
        if (state == State.Dead)
            return;

        state = State.Dead;
        playerVisible = false;
        hasTarget = false;
        ReleaseAssignedWaypoint();

        if (screamRoutine != null)
        {
            StopCoroutine(screamRoutine);
            screamRoutine = null;
        }

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
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

        // If a passive or fleeing monster is shot directly, they switch to aggressive
        if (personality == MonsterPersonality.PassiveNeutral)
        {
            personality = MonsterPersonality.Aggressive; // Provoked!
        }

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

        // 6. Apply damage and check for permanent death
        if (playerHealth != null) 
        {
            playerHealth.TakeDamage(damage);
            if (playerHealth.Current <= 0f) 
            {
                DiePermanently();
            }
        }
    }

    private void SpawnGroundBlood(BulletImpactManager impacts, BulletHitInfo hit)
    {
        if (bloodGroundDecalPrefab == null)
            return;

        Vector3 flat = Vector3.ProjectOnPlane(hit.Direction, Vector3.up).normalized;
        Vector3 origin = hit.Point + flat * Random.Range(0.1f, groundSplatterDistance) + Vector3.up * 0.1f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit ground, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
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
        if (animator == null || currentAnimationTrigger == trigger)
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
        ReleaseAssignedWaypoint();
        if (watcher != null)
            watcher.UnregisterMonster(this);
    }

    private AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;

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
            if (clip == null) continue;
            if (selected == 0) return clip;
            selected--;
        }

        return null;
    }

    private void PlayOneShotRandom(AudioClip[] clips)
    {
        if (actionAudioSource == null)
            return;

        AudioClip clip = GetRandomClip(clips);
        if (clip == null) return;

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

        AudioClip[] clips = desiredAudio == AmbientAudio.Chasing ? chasings : wheepings;
        AudioClip clip = GetRandomClip(clips);

        if (clip == null) return;

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