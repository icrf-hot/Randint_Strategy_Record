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
        bool boss = MapRunState.BattleNodeType == MapNodeType.FirstBoss || MapRunState.BattleNodeType == MapNodeType.Boss;
        var floor = MapRunState.MapFloor > 1 ? CampaignDefinition.Active.Floor(MapRunState.MapFloor) : null;
        int count = floor == null ? 4 : boss ? 1 : floor.enemyCount;
        for (int i=0;i<count;i++) SpawnEnemy(i);
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
        if (MapRunState.MapFloor > 1) {
            var floor=CampaignDefinition.Active.Floor(MapRunState.MapFloor);
            bool boss=MapRunState.BattleNodeType==MapNodeType.FirstBoss || MapRunState.BattleNodeType==MapNodeType.Boss;
            bool arts=!boss && spawnIndex%3==1;
            int power=boss?floor.bossAttack:arts?Mathf.RoundToInt(floor.enemyAttack*CampaignDefinition.Active.artsAttackScale):floor.enemyAttack;
            enemy.ConfigureEncounter(boss?floor.bossHP:floor.enemyHP,power,floor.enemyDefense,floor.enemyResistance,floor.enemySpeed,arts);
            if(!boss)enemy.name=Randint.Data.GameData.Text(arts?"campaign.enemy_arts":"campaign.enemy_phys")+" "+(spawnIndex+1);
            if(boss) {
                enemy.name=Randint.Data.GameData.Text(MapRunState.BattleNodeType==MapNodeType.Boss?"campaign.finalboss":"campaign.midboss");
                enemy.transform.localScale*=1.35f;
            }
        }

        return enemy;
    }
}
