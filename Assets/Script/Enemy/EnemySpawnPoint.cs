using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [SerializeField] private EnemyPosition position;

    public EnemyPosition Position => position;
}