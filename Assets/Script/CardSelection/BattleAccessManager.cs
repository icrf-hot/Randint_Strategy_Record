using UnityEngine;

public class BattleAccessManager : MonoBehaviour
{
    public static BattleAccessManager Instance { get; private set; }

    [Header("Selection Managers")]
    [SerializeField] private FrontCardSelectionManager frontManager;
    [SerializeField] private MiddleCardSelectionManager middleManager;
    [SerializeField] private BackCardSelectionManager backManager;

    [Header("Battle Button")]
    [SerializeField] private GameObject battleButton;

    private void Awake()
    {
        Instance = this;

        Refresh();
    }

    public void Refresh()
    {
        bool canBattle =
            frontManager.IsAccess &&
            middleManager.IsAccess &&
            backManager.IsAccess;

        battleButton.SetActive(canBattle);
    }
}