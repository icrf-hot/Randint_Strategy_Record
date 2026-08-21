using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance { get; private set; }

    [Header("Spawn Points")]
    [SerializeField] private EnemySpawnPoint[] spawnPoints;

    [Header("Enemy Prefab")]
    [SerializeField] private Enemy enemyPrefab;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Instance.SpawnEnemy(0);
        Instance.SpawnEnemy(1);
        Instance.SpawnEnemy(2);
        Instance.SpawnEnemy(3);
    }

    public Enemy SpawnEnemy(int spawnIndex)
    {
        if (spawnIndex < 0 ||
            spawnIndex >= spawnPoints.Length)
        {
            return null;
        }

        EnemySpawnPoint spawnPoint =
            spawnPoints[spawnIndex];

        Enemy enemy = Instantiate(
            enemyPrefab,
            spawnPoint.transform.position,
            Quaternion.identity);

        EnemyTargetable targetable =
            enemy.GetComponent<EnemyTargetable>();

        if (targetable != null)
        {
            targetable.SetPosition(
                spawnPoint.Position);
        }

        return enemy;
    }
}