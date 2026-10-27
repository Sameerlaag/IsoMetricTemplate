
using System.Collections.Generic;
using UnityEngine;

public enum PlayerMovementMode
{
    Walking,
    Running,
    Sneaking
}

public class MonsterWatcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private MonsterController monsterPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private FlashlightController playerFlashlight;

    [Header("Flashlight Detection")]
    [SerializeField, Min(1f)]
    private float flashlightSightMultiplier = 1.75f;
    
    
    [Header("Spawning")]
    [SerializeField, Min(0)] private int initialMonsterCount = 4;
    [SerializeField, Min(0f)] private float spawnInterval = 0f;
    [SerializeField, Min(0)] private int maxMonsters = 12;

    [Header("Player Movement")]
    [SerializeField] private PlayerMovementMode movementMode =
        PlayerMovementMode.Walking;

    [Header("Detection Ranges")]
    [SerializeField] private float walkingNoiseRange = 4f;
    [SerializeField] private float runningNoiseRange = 12f;
    [SerializeField] private float sneakingNoiseRange = 1f;
    [SerializeField] private float normalSightRange = 16f;
    [SerializeField] private float sneakingSightRange = 5f;

    [Header("Line Of Sight")]
    [SerializeField] private LayerMask obstructionMask = ~0;
    [SerializeField] private float eyeHeight = 1.5f;

    private readonly List<MonsterController> monsters = new();
    private float nextSpawnTime;
    private int spawnedCount;
    
    [Header("Detection Debug")]
    [SerializeField] private bool debugDetection = true;

    private readonly Dictionary<MonsterController, bool> previousSight = new();

    public Transform Player => player;
    public PlayerMovementMode MovementMode => movementMode;

    private void Start()
    {
        for (int i = 0; i < initialMonsterCount; i++)
        {
            if (!SpawnMonster())
                break;
        }

        nextSpawnTime = Time.time + spawnInterval;
    }

    private void Update()
    {
        RemoveMissingMonsters();
        DetectPlayer();

        if (spawnInterval > 0f &&
            Time.time >= nextSpawnTime &&
            monsters.Count < maxMonsters)
        {
            if (SpawnMonster())
                nextSpawnTime = Time.time + spawnInterval;
            else
                nextSpawnTime = Time.time + 1f;
        }
    }

    // Call this from your player movement script.
    public void SetMovementMode(PlayerMovementMode mode)
    {
        movementMode = mode;
    }

    // Call this whenever the player fires a gun.
    public void NotifyGunshot(Vector3 shotPosition)
    {
        foreach (MonsterController monster in monsters)
        {
            if (monster == null)
                continue;

            float distance = Vector3.Distance(
                monster.transform.position,
                shotPosition
            );

            if (distance <= monster.GunshotHearingRange)
            {
                monster.HearNoise(shotPosition);
            }
        }
    }

    public void RegisterMonster(MonsterController monster)
    {
        if (monster != null && !monsters.Contains(monster))
            monsters.Add(monster);
    }

    public void UnregisterMonster(MonsterController monster)
    {
        monsters.Remove(monster);
    }

    private bool SpawnMonster()
    {
        if (monsterPrefab == null ||
            spawnPoints == null ||
            spawnPoints.Length == 0 ||
            monsters.Count >= maxMonsters)
        {
            return false;
        }

        Transform point = spawnPoints[spawnedCount % spawnPoints.Length];
        spawnedCount++;

        MonsterController monster = Instantiate(
            monsterPrefab,
            point.position,
            point.rotation
        );

        monster.Initialize(this, player);
        RegisterMonster(monster);

        return true;
    }

    private void DetectPlayer()
    {
        if (player == null)
            return;

        foreach (MonsterController monster in monsters)
        {
            if (monster == null || !monster.CanPerceive)
                continue;

            Vector3 origin = monster.transform.position +
                             Vector3.up * eyeHeight;

            Vector3 target = player.position +
                             Vector3.up * eyeHeight;

            float distance = Vector3.Distance(origin, target);

            float sightRange = movementMode ==
                               PlayerMovementMode.Sneaking
                ? sneakingSightRange
                : normalSightRange;

            if (playerFlashlight != null && playerFlashlight.IsOn)
            {
                sightRange *= flashlightSightMultiplier;
            }

            
            bool canSee = distance <= sightRange &&
                          HasLineOfSight(origin, target);

            var previously = !previousSight.TryGetValue(monster, out bool previouslySawPlayer);
            if (canSee)
            {
                if (previously
                    || !previouslySawPlayer)
                {
                    if (debugDetection)
                        Debug.Log($"[Watcher] {monster.name}: SAW PLAYER", monster);
                }

                previousSight[monster] = true;
                monster.SeePlayer(player.position);
                continue;
            }

            if (previously
                || previouslySawPlayer)
            {
                if (debugDetection)
                    Debug.Log($"[Watcher] {monster.name}: LOST SIGHT", monster);

                monster.LoseSight();
            }

            previousSight[monster] = false;
            
            // Hearing/movement detection can work through walls.
            float noiseRange = movementMode switch
            {
                PlayerMovementMode.Running => runningNoiseRange,
                PlayerMovementMode.Sneaking => sneakingNoiseRange,
                _ => walkingNoiseRange
            };

            if (distance <= noiseRange)
            {
                monster.HearNoise(player.position);
            }
        }
    }

    private bool HasLineOfSight(Vector3 origin, Vector3 target)
    {
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
            return true;

        if (!Physics.Raycast(
                origin,
                direction.normalized,
                out RaycastHit hit,
                distance,
                obstructionMask,
                QueryTriggerInteraction.Ignore))
        {
            return true;
        }

        return hit.transform == player ||
               hit.transform.IsChildOf(player);
    }

    private void RemoveMissingMonsters()
    {
        monsters.RemoveAll(monster => monster == null);
    }
}
