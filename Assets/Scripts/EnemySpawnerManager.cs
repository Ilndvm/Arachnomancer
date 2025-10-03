using System.Collections;
using UnityEngine;

public class EnemySpawnerManager : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyArray;

    [Header("Timing")]
    [SerializeField] private float spawnInterval = 1f;

    [Header("Spawn Area (Manual)")]
    [SerializeField] private Vector2 areaMin = new Vector2(-8f, -8f);
    [SerializeField] private Vector2 areaMax = new Vector2(8f, 8f);

    [Header("Player Avoidance")]
    [SerializeField] private Transform playerRef;
    [SerializeField] private float minDistanceFromPlayer = 4f;

    [Header("Safety")]
    [SerializeField] private int maxAttemptsPerSpawn = 20;

    [Header("Pool Settings")]
    [Tooltip("How many instances of each prefab to create at start.")]
    [SerializeField] private int prewarmPerPrefab = 16;

    private WaitForSeconds wait;

    // Plain arrays: one array per prefab holding its instances
    [SerializeField] private GameObject[][] pools;

    public static EnemySpawnerManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        wait = new WaitForSeconds(spawnInterval);

        Prewarm();
    }

    private void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    private void OnValidate()
    {
        areaMin = new Vector2(Mathf.Min(areaMin.x, areaMax.x), Mathf.Min(areaMin.y, areaMax.y));
        areaMax = new Vector2(Mathf.Max(areaMin.x, areaMax.x), Mathf.Max(areaMin.y, areaMax.y));
        spawnInterval = Mathf.Max(0.01f, spawnInterval);
        minDistanceFromPlayer = Mathf.Max(0f, minDistanceFromPlayer);
        maxAttemptsPerSpawn = Mathf.Max(1, maxAttemptsPerSpawn);
        prewarmPerPrefab = Mathf.Max(1, prewarmPerPrefab);
    }

    private void Prewarm()
    {
        if (enemyArray == null || enemyArray.Length == 0) return;

        pools = new GameObject[enemyArray.Length][];

        for (int i = 0; i < enemyArray.Length; i++)
        {
            var prefab = enemyArray[i];
            if (!prefab) continue;

            var arr = new GameObject[prewarmPerPrefab];
            for (int j = 0; j < prewarmPerPrefab; j++)
            {
                var inst = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                inst.SetActive(false);
                arr[j] = inst;
            }
            pools[i] = arr;
        }
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            TrySpawnFromPools();
            yield return wait; // fixed interval
        }
    }

    private void TrySpawnFromPools()
    {
        if (enemyArray == null || enemyArray.Length == 0) return;
        if (!FindValidSpawn(out Vector2 pos)) return;

        // Start from a random prefab index; try each once
        int start = Random.Range(0, enemyArray.Length);
        for (int k = 0; k < enemyArray.Length; k++)
        {
            int i = (start + k) % enemyArray.Length;
            var arr = pools[i];
            if (arr == null) continue;

            // Find first inactive instance
            for (int j = 0; j < arr.Length; j++)
            {
                var go = arr[j];
                if (!go) continue;
                if (!go.activeSelf)
                {
                    // Activate and place
                    go.transform.SetParent(null, true);
                    go.transform.position = pos;
                    go.transform.rotation = Quaternion.identity;
                    go.SetActive(true);
                    return;
                }
            }
        }
    }

    private void TrySpawnFromPools(int index, Vector2 pos)
    {
        if (enemyArray == null || enemyArray.Length == 0) return;

        var arr = pools[index];

        // Find first inactive instance
        for (int j = 0; j < arr.Length; j++)
        {
            var go = arr[j];
            if (!go) continue;
            if (!go.activeSelf)
            {
                // Activate and place
                go.transform.SetParent(null, true);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.identity;
                go.SetActive(true);
                return;
            }
        }
    }

    private bool FindValidSpawn(out Vector2 spawnPos)
    {
        spawnPos = Vector2.zero;

        for (int i = 0; i < maxAttemptsPerSpawn; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(areaMin.x, areaMax.x),
                Random.Range(areaMin.y, areaMax.y)
            );

            if (playerRef)
            {
                float sqrDist = ((Vector2)playerRef.position - candidate).sqrMagnitude;
                if (sqrDist < minDistanceFromPlayer * minDistanceFromPlayer)
                    continue;
            }

            spawnPos = candidate;
            return true;
        }

        return false;
    }

    /// Call this instead of Destroy(gameObject)
    public static void Despawn(GameObject go)
    {
        if (!go) return;
        go.SetActive(false);
    }
}
