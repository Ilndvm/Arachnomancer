using System.Collections;
using UnityEngine;

public class EnemySpawnerManager : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float spawnInterval = 1f;

    [Header("Spawn Area (Manual)")]
    [SerializeField] private Vector2 areaMin = new Vector2(-8f, -8f);
    [SerializeField] private Vector2 areaMax = new Vector2(8f, 8f);

    [Header("Player Avoidance")]
    [SerializeField] private float minDistanceFromPlayer = 4f;

    [Header("Safety")]
    [SerializeField] private int maxAttemptsPerSpawn = 20;

    private WaitForSeconds wait;

    // Plain arrays: one array per prefab holding its instances
    [SerializeField] private GameObject[][] pools;

    [SerializeField] private float[] arrayProbabilities = {12f, 12f, 12f, 12f, 12f, 10f, 10f, 8.5f, 5.5f, 4f, 2f};

    [Header("Wave")]
    [SerializeField] private float countdownTime = 120;
    [SerializeField] private float currentTime;
    [SerializeField] private float currentWave = 1; // optinal for now
    [SerializeField] private int activeAmountOfPrefabs = 2;

    public static EnemySpawnerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        wait = new WaitForSeconds(spawnInterval);
    }

    private void Start()
    {
        StartCoroutine(SpawnLoop());
        currentTime = countdownTime;
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            TryProbabilitySpawn();
        }
        currentTime -= Time.deltaTime;

        if (currentTime < 0)
        {
            currentTime = 0;
            if (activeAmountOfPrefabs < GameManager.Instance.GetEnemyPrefabs().Length)
            {
                activeAmountOfPrefabs++;
            }
            currentWave++;
            currentTime = countdownTime;
        }
    }

    private void OnValidate()
    {
        areaMin = new Vector2(Mathf.Min(areaMin.x, areaMax.x), Mathf.Min(areaMin.y, areaMax.y));
        areaMax = new Vector2(Mathf.Max(areaMin.x, areaMax.x), Mathf.Max(areaMin.y, areaMax.y));
        spawnInterval = Mathf.Max(0.01f, spawnInterval);
        minDistanceFromPlayer = Mathf.Max(0f, minDistanceFromPlayer);
        maxAttemptsPerSpawn = Mathf.Max(1, maxAttemptsPerSpawn);
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            TryProbabilitySpawn();
            yield return wait; // fixed interval
        }
    }

    private void TryProbabilitySpawn()
    {
        if (!FindValidSpawn(out Vector2 pos)) return;

        float sum = 0f, coeff;

        for (int i = 0; i < activeAmountOfPrefabs; i++)
        {
            sum += arrayProbabilities[i];
        }

        coeff = 100 / sum;

        int random = (int)Random.Range(1f, 100f);
        float cumulative = 0;

        for (int j = 0; j < activeAmountOfPrefabs; j++)
        {
            cumulative += arrayProbabilities[j] * coeff;

            if (random <= cumulative)
            {
                Debug.Log($"SUM: {sum}; COEFF: {coeff}; PROBABILITY: {(int)arrayProbabilities[j] * coeff}; RANDOM: {random}; CUMULATIVE: {cumulative}; INDEX: {j + 1};");

                EnemyBase enemy = GameManager.Instance.GetEnemy(j);
                enemy.SetPosition(pos);
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

            if (GameManager.Instance.player)
            {
                float sqrDist = ((Vector2)GameManager.Instance.player.transform.position - candidate).sqrMagnitude;
                if (sqrDist < minDistanceFromPlayer * minDistanceFromPlayer)
                    continue;
            }

            spawnPos = candidate;
            return true;
        }

        return false;
    }
}