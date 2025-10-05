using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public SpiderController player;
    public LevelManagerUI UIManager;
    public EnemySpawnerManager spawnManager;

    #region Pools

    [Space(10f)]
    [Header("Pools")]
    [SerializeField] private int projectilePoolSize = 10;
    private ObjectPool<EnemyProjectile>[] projectilePools;
    [SerializeField] private EnemyProjectile[] projectilePrefabs;

    [SerializeField] private int enemyPoolSize = 10;
    private ObjectPool<EnemyBase>[] enemyPools;
    [SerializeField] private EnemyBase[] enemyPrefabs;

    [SerializeField] private int floatingTextPoolSize = 10;
    private ObjectPool<FloatingText> floatingTextPool;
    [SerializeField] private FloatingText floatingTextPrefab;

    [SerializeField] private int deathParticlePoolSize = 10;
    private ObjectPool<DestroyAfterParticles> deathParticlePool;
    [SerializeField] private DestroyAfterParticles deathParticlePrefab;

    [SerializeField] private int bloodPoolSize = 10;
    private ObjectPool<Blood> bloodPool;
    [SerializeField] private Blood bloodPrefab;

    [SerializeField] private int playerProjectilePoolSize = 10;
    private ObjectPool<PlayerProjectile> playerProjectilePool;
    [SerializeField] private PlayerProjectile playerProjectilePrefab;
    #endregion

    public int blood = 0;
    public float timer = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    private void Start()
    {
        UIManager.UpdateBloodText();

        CreatePools();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        UIManager.UpdateTimerText((int)timer);
    }

    #region Pools
    private void CreatePools()
    {
        projectilePools = new ObjectPool<EnemyProjectile>[projectilePrefabs.Length];
        for (int i = 0; i < projectilePrefabs.Length; i++)
        {
            projectilePools[i] = new ObjectPool<EnemyProjectile>(projectilePrefabs[i]);
            projectilePools[i].Populate(projectilePoolSize);
        }

        enemyPools = new ObjectPool<EnemyBase>[enemyPrefabs.Length];
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            enemyPools[i] = new ObjectPool<EnemyBase>(enemyPrefabs[i]);
            enemyPools[i].Populate(enemyPoolSize);
        }

        floatingTextPool = new ObjectPool<FloatingText>(floatingTextPrefab);
        floatingTextPool.Populate(floatingTextPoolSize);

        deathParticlePool = new ObjectPool<DestroyAfterParticles>(deathParticlePrefab);
        deathParticlePool.Populate(deathParticlePoolSize);

        bloodPool = new ObjectPool<Blood>(bloodPrefab);
        bloodPool.Populate(bloodPoolSize);

        playerProjectilePool = new ObjectPool<PlayerProjectile>(playerProjectilePrefab);
        playerProjectilePool.Populate(playerProjectilePoolSize);
    }

    /// <summary>0 = Mine, 1 = Simple, 2 = Follow</summary>
    public EnemyProjectile GetProjectile(int index)
    {
        return projectilePools[index].GetPooledObject();
    }

    /// <summary>0 = Slime, 1 = CubeSlime, 2 = Rat, 3 = Fly, 4 = Bat, 5 = MediumSlime, 6 = Goblin. 7 = Skeleton, 8 = FlyingSkull, 9 = Ghost, 10 = SlimeKing</summary>
    public EnemyBase GetEnemy(int index)
    {
        return enemyPools[index].GetPooledObject();
    }
    public FloatingText GetFloatingText()
    {
        return floatingTextPool.GetPooledObject();
    }
    public DestroyAfterParticles GetDeathParticle()
    {
        return deathParticlePool.GetPooledObject();
    }
    public Blood GetString()
    {
        return bloodPool.GetPooledObject();
    }
    public PlayerProjectile GetPlayerProjectile()
    {
        return playerProjectilePool.GetPooledObject();
    }
    public EnemyBase[] GetEnemyPrefabs()
    {
        return enemyPrefabs;
    }
    #endregion

}