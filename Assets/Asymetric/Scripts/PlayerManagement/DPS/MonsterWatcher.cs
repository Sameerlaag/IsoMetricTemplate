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
    [SerializeField] private FlashlightController playerFlashlight;

    [Header("Flashlight Detection")] 
    [SerializeField, Min(1f)] private float flashlightSightMultiplier = 1.75f;

    [Header("Spawning & Waypoint Network")] 
    [SerializeField, Min(0)] private int initialMonsterCount = 4;
    [SerializeField, Min(0f)] private float spawnInterval = 5f;
    [SerializeField, Min(0)] private int maxMonsters = 12;
    [SerializeField, Min(0f)] private float initialSpawnDelay = 2f;
    [SerializeField, Min(0f)] private float minimumSpawnDistance = 30f;
    [SerializeField, Min(0f)] private float maximumSpawnDistance = 60f;
    [SerializeField, Min(0f)] private float despawnDistance = 75f;
    [SerializeField, Min(5f)] private float maxWaypointLinkDistance = 25f;

    private readonly List<Waypoint> allWaypoints = new();
    private readonly List<MonsterController> monsters = new();

    private int currentMonsterLimit;
    private float nextSpawnTime;
    private bool initialSpawnComplete;

    [Header("Player Movement")] 
    [SerializeField] private PlayerMovementMode movementMode = PlayerMovementMode.Walking;

    [Header("Detection Ranges")] 
    [SerializeField] private float walkingNoiseRange = 4f;
    [SerializeField] private float runningNoiseRange = 12f;
    [SerializeField] private float sneakingNoiseRange = 1f;
    [SerializeField] private float normalSightRange = 16f;
    [SerializeField] private float sneakingSightRange = 5f;

    [Header("Line Of Sight")] 
    [SerializeField] private LayerMask obstructionMask = ~0;
    [SerializeField] private float eyeHeight = 1.5f;

    [Header("Detection Debug")] 
    [SerializeField] private bool debugDetection = true;

    private readonly Dictionary<MonsterController, bool> previousSight = new();

    public Transform Player => player;
    public PlayerMovementMode MovementMode => movementMode;
    public List<Waypoint> AllWaypoints => allWaypoints;

    private void Start()
    {
        FindAndBuildWaypointNetwork();
        currentMonsterLimit = maxMonsters;
        nextSpawnTime = Time.time + initialSpawnDelay;
    }

    private void Update()
    {
        RemoveMissingMonsters();
        DespawnDistantMonsters();
        DetectPlayer();

        if (!initialSpawnComplete)
        {
            if (Time.time < nextSpawnTime)
                return;

            initialSpawnComplete = true;

            for (int i = 0; i < initialMonsterCount; i++)
            {
                if (!SpawnMonster())
                    break;
            }

            nextSpawnTime = Time.time + spawnInterval;
            return;
        }

        if (spawnInterval > 0f &&
            Time.time >= nextSpawnTime &&
            monsters.Count < currentMonsterLimit)
        {
            if (SpawnMonster())
                nextSpawnTime = Time.time + spawnInterval;
            else
                nextSpawnTime = Time.time + 1f;
        }
    }

    private void FindAndBuildWaypointNetwork()
    {
        allWaypoints.Clear();

        Waypoint[] waypoints = FindObjectsByType<Waypoint>(FindObjectsSortMode.None);

        foreach (Waypoint waypoint in waypoints)
        {
            if (waypoint != null && waypoint.Category == "spawn_point")
            {
                allWaypoints.Add(waypoint);
            }
        }

        // AAA Network Analysis: Link nodes within range that have valid NavMesh paths
        foreach (Waypoint wpA in allWaypoints)
        {
            wpA.connectedWaypoints.Clear();
            foreach (Waypoint wpB in allWaypoints)
            {
                if (wpA == wpB) continue;

                float dist = Vector3.Distance(wpA.Position, wpB.Position);
                if (dist <= maxWaypointLinkDistance)
                {
                    UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
                    if (UnityEngine.AI.NavMesh.CalculatePath(wpA.Position, wpB.Position, UnityEngine.AI.NavMesh.AllAreas, path))
                    {
                        if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
                        {
                            wpA.connectedWaypoints.Add(wpB);
                        }
                    }
                }
            }
        }

        Debug.Log($"[Watcher] Found {allWaypoints.Count} spawn points and built waypoint network graph.");
    }

    public void SetMovementMode(PlayerMovementMode mode)
    {
        movementMode = mode;
    }

    public void NotifyGunshot(Vector3 shotPosition)
    {
        foreach (MonsterController monster in monsters)
        {
            if (monster == null)
                continue;

            float distance = Vector3.Distance(monster.transform.position, shotPosition);

            if (distance <= monster.GunshotHearingRange)
            {
                monster.OnGunshotHeard(shotPosition);
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
        if (monster != null)
        {
            monster.ReleaseAssignedWaypoint();
            monsters.Remove(monster);
        }
    }

    private bool SpawnMonster()
    {
        if (player == null ||
            monsterPrefab == null ||
            monsters.Count >= currentMonsterLimit)
        {
            return false;
        }

        List<Waypoint> eligiblePoints = new();

        float minDistanceSqr = minimumSpawnDistance * minimumSpawnDistance;
        float maxDistanceSqr = maximumSpawnDistance * maximumSpawnDistance;

        foreach (Waypoint point in allWaypoints)
        {
            if (point == null || point.isOccupied)
                continue;

            float distanceSqr = (point.Position - player.position).sqrMagnitude;

            if (distanceSqr < minDistanceSqr || distanceSqr > maxDistanceSqr)
            {
                continue;
            }

            if (IsWaypointVisible(point))
                continue;

            eligiblePoints.Add(point);
        }

        if (eligiblePoints.Count == 0)
            return false;

        Waypoint pointToUse = eligiblePoints[Random.Range(0, eligiblePoints.Count)];
        pointToUse.isOccupied = true; // Enforce exclusivity rule

        MonsterController monster = Instantiate(
            monsterPrefab,
            pointToUse.Position,
            pointToUse.Rotation
        );

        monster.Initialize(this, player, pointToUse);
        RegisterMonster(monster);

        return true;
    }

    private bool IsWaypointVisible(Waypoint waypoint)
    {
        if (player == null || waypoint == null)
            return false;

        Vector3 origin = player.position + Vector3.up * eyeHeight;
        Vector3 target = waypoint.Position + Vector3.up * eyeHeight;
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

        return hit.distance >= distance - 0.5f;
    }

    private void DespawnDistantMonsters()
    {
        if (player == null)
            return;

        float despawnDistanceSqr = despawnDistance * despawnDistance;

        for (int i = monsters.Count - 1; i >= 0; i--)
        {
            MonsterController monster = monsters[i];

            if (monster == null)
                continue;

            float distanceSqr = (monster.transform.position - player.position).sqrMagnitude;

            if (distanceSqr > despawnDistanceSqr)
            {
                previousSight.Remove(monster);
                monster.ReleaseAssignedWaypoint();
                monsters.RemoveAt(i);

                Destroy(monster.gameObject);
            }
        }
    }

    private void DetectPlayer()
    {
        if (player == null)
            return;

        foreach (MonsterController monster in monsters)
        {
            if (monster == null || !monster.CanPerceive)
                continue;

            Vector3 origin = monster.transform.position + Vector3.up * eyeHeight;
            Vector3 target = player.position + Vector3.up * eyeHeight;

            float distance = Vector3.Distance(origin, target);

            float sightRange = movementMode == PlayerMovementMode.Sneaking
                ? sneakingSightRange
                : normalSightRange;

            if (playerFlashlight != null && playerFlashlight.IsOn)
            {
                sightRange *= flashlightSightMultiplier;
            }

            bool canSee = distance <= sightRange && HasLineOfSight(origin, target);

            var previously = !previousSight.TryGetValue(monster, out bool previouslySawPlayer);
            if (canSee)
            {
                if (previously || !previouslySawPlayer)
                {
                    if (debugDetection)
                        Debug.Log($"[Watcher] {monster.name}: SAW PLAYER", monster);
                }

                previousSight[monster] = true;
                monster.SeePlayer(player.position);
                continue;
            }

            if (previously || previouslySawPlayer)
            {
                if (debugDetection)
                    Debug.Log($"[Watcher] {monster.name}: LOST SIGHT", monster);

                monster.LoseSight();
            }

            previousSight[monster] = false;

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

        return hit.transform == player || hit.transform.IsChildOf(player);
    }

    private void RemoveMissingMonsters()
    {
        monsters.RemoveAll(monster => monster == null);
    }
}