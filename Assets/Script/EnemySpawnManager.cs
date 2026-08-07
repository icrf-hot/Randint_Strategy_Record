using UnityEngine;

public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance { get; private set; }

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

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
        if (spawnIndex < 0 || spawnIndex >= spawnPoints.Length)
            return null;

        Enemy enemy = Instantiate(
            enemyPrefab,
            spawnPoints[spawnIndex].position,
            Quaternion.identity);

        return enemy;
    }
}