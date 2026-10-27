using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// One place for every bullet impact effect.
///
/// 1) Surfaces:  PlayImpact(tag, position, normal) looks the tag up in the Surfaces list
///               and plays that surface's sfx / vfx / bullet hole. Unknown tags use "Default Impact".
///               Adding "Wood" later = add one list element in the inspector, no code.
///
/// 2) Helpers:   SpawnEffect / SpawnDecal / PlaySfx are public so the MonsterController
///               (blood, flesh hits...) reuses the same pools instead of making its own.
///
/// Prefab rules: particle prefabs need "Play On Awake" ON and Looping OFF
///               (pooled objects are restarted by disabling/enabling them).
/// </summary>
public class BulletImpactManager : MonoBehaviour
{
    public static BulletImpactManager Instance { get; private set; }

    [System.Serializable]
    public class SurfaceImpact
    {
        [Tooltip("Must match the Unity Tag on the surface, e.g. Metal")]
        public string surfaceTag = "Metal";
        public AudioClip[] sfx;
        [Range(0f, 1f)] public float volume = 1f;
        public GameObject vfxPrefab;
        public float vfxLifetime = 2f;
        [Tooltip("Optional bullet hole (e.g. a URP Decal Projector prefab).")]
        public GameObject bulletHolePrefab;
    }

    [Header("Surfaces")]
    [SerializeField] private SurfaceImpact defaultImpact = new SurfaceImpact { surfaceTag = "Default" };
    [SerializeField] private List<SurfaceImpact> surfaces = new List<SurfaceImpact>();

    [Header("Audio")]
    [Tooltip("How many impact sounds can play at once.")]
    [SerializeField] private int audioVoices = 16;
    [SerializeField] private float pitchVariation = 0.05f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 40f;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("Limits")]
    [Tooltip("Oldest decals are recycled once this many exist.")]
    [SerializeField] private int maxDecals = 150;
    [Tooltip("Pushes effects slightly off the surface to avoid z-fighting.")]
    [SerializeField] private float surfaceOffset = 0.01f;

    private struct ActiveEffect
    {
        public GameObject Instance;
        public GameObject Prefab;
        public float ReleaseTime;
    }

    private readonly Dictionary<string, SurfaceImpact> _lookup = new Dictionary<string, SurfaceImpact>();
    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();
    private readonly List<ActiveEffect> _activeEffects = new List<ActiveEffect>();
    private readonly Queue<ActiveEffect> _activeDecals = new Queue<ActiveEffect>();

    private AudioSource[] _voices;
    private int _nextVoice;

    // ------------------------------------------------------------------
    // Setup
    // ------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach (SurfaceImpact surface in surfaces)
        {
            if (surface != null && !string.IsNullOrEmpty(surface.surfaceTag))
                _lookup[surface.surfaceTag] = surface;
        }

        BuildAudioVoices();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildAudioVoices()
    {
        int count = Mathf.Max(1, audioVoices);
        _voices = new AudioSource[count];

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("SfxVoice_" + i);
            go.transform.SetParent(transform, false);

            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f; // 3D
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.outputAudioMixerGroup = sfxGroup;
            _voices[i] = source;
        }
    }

    // ------------------------------------------------------------------
    // Surfaces (called by the gun for anything that is NOT an IBulletTarget)
    // ------------------------------------------------------------------
    public void PlayImpact(string surfaceTag, Vector3 position, Vector3 normal)
    {
        if (!_lookup.TryGetValue(surfaceTag, out SurfaceImpact impact))
            impact = defaultImpact;

        if (impact == null) return;

        PlaySfx(impact.sfx, position, impact.volume);

        Vector3 spawnPosition = position + normal * surfaceOffset;

        if (impact.vfxPrefab != null)
        {
            // Particles emit along +Z, so +Z = surface normal sprays away from the wall.
            SpawnEffect(impact.vfxPrefab, spawnPosition, Quaternion.LookRotation(normal),
                null, impact.vfxLifetime);
        }

        if (impact.bulletHolePrefab != null)
            SpawnDecal(impact.bulletHolePrefab, spawnPosition, DecalRotation(normal));
    }

    // ------------------------------------------------------------------
    // Public helpers (also used by MonsterController)
    // ------------------------------------------------------------------

    /// <summary>Decals project along +Z, so look INTO the surface, with a random roll.</summary>
    public static Quaternion DecalRotation(Vector3 normal)
    {
        return Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
    }

    /// <summary>Plays a random clip from the array at a world position.</summary>
    public void PlaySfx(AudioClip[] clips, Vector3 position, float volume = 1f)
    {
        if (clips == null || clips.Length == 0) return;
        PlaySfx(clips[Random.Range(0, clips.Length)], position, volume);
    }

    public void PlaySfx(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null || _voices == null) return;

        AudioSource source = GetVoice();
        source.transform.position = position;
        source.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
        source.volume = volume;
        source.clip = clip;
        source.Play();
    }

    /// <summary>Short-lived effect (particles). Optionally parented, e.g. to a bone.</summary>
    public GameObject SpawnEffect(GameObject prefab, Vector3 position, Quaternion rotation,
        Transform parent = null, float lifetime = 2f)
    {
        if (prefab == null) return null;

        GameObject instance = GetFromPool(prefab, position, rotation, parent);

        _activeEffects.Add(new ActiveEffect
        {
            Instance = instance,
            Prefab = prefab,
            ReleaseTime = Time.time + lifetime
        });

        return instance;
    }

    /// <summary>Long-lived decal. When maxDecals is reached the oldest one is recycled.</summary>
    public GameObject SpawnDecal(GameObject prefab, Vector3 position, Quaternion rotation,
        Transform parent = null)
    {
        if (prefab == null) return null;

        while (_activeDecals.Count >= Mathf.Max(1, maxDecals))
        {
            ActiveEffect oldest = _activeDecals.Dequeue();
            Release(oldest.Instance, oldest.Prefab);
        }

        GameObject instance = GetFromPool(prefab, position, rotation, parent);
        _activeDecals.Enqueue(new ActiveEffect { Instance = instance, Prefab = prefab });
        return instance;
    }

    // ------------------------------------------------------------------
    // Internals
    // ------------------------------------------------------------------
    private void Update()
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveEffect effect = _activeEffects[i];

            // Instance == null means it was destroyed together with its parent (e.g. a dead monster).
            if (effect.Instance != null && Time.time < effect.ReleaseTime) continue;

            Release(effect.Instance, effect.Prefab);
            _activeEffects.RemoveAt(i);
        }
    }

    private AudioSource GetVoice()
    {
        // Prefer a free voice...
        for (int i = 0; i < _voices.Length; i++)
        {
            int index = (_nextVoice + i) % _voices.Length;
            if (!_voices[index].isPlaying)
            {
                _nextVoice = (index + 1) % _voices.Length;
                return _voices[index];
            }
        }

        // ...otherwise steal the oldest one.
        AudioSource stolen = _voices[_nextVoice];
        _nextVoice = (_nextVoice + 1) % _voices.Length;
        return stolen;
    }

    private GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        Queue<GameObject> pool;
        if (!_pools.TryGetValue(prefab, out pool))
        {
            pool = new Queue<GameObject>();
            _pools[prefab] = pool;
        }

        GameObject instance = null;
        while (pool.Count > 0 && instance == null)
            instance = pool.Dequeue(); // skips instances that were destroyed with a parent

        if (instance == null)
            instance = Instantiate(prefab);

        Transform t = instance.transform;
        t.SetParent(parent, false);
        t.SetPositionAndRotation(position, rotation);

        // Off/on restarts "Play On Awake" particle systems.
        instance.SetActive(false);
        instance.SetActive(true);
        return instance;
    }

    private void Release(GameObject instance, GameObject prefab)
    {
        if (instance == null) return;

        instance.SetActive(false);
        instance.transform.SetParent(transform, false); // detach from bones before pooling
        _pools[prefab].Enqueue(instance);
    }
}