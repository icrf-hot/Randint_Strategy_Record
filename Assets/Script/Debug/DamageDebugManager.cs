using UnityEngine;

public class DamageDebugManager : MonoBehaviour
{
    public static DamageDebugManager Instance { get; private set; }

    [SerializeField]
    private int damageAmount = 20;

    private void Awake()
    {
        Instance = this;
    }

    public void DamageOperator(Operator target)
    {
        if (target == null)
            return;

        target.DebugDamage(damageAmount);
    }
}