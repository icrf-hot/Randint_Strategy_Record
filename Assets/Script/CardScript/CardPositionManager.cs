using UnityEngine;

public class CardPositionManager : MonoBehaviour
{
    public static CardPositionManager Instance { get; private set; }

    [Header("Selection Managers")]
    [SerializeField] private FrontCardSelectionManager frontManager;
    [SerializeField] private MiddleCardSelectionManager middleManager;
    [SerializeField] private BackCardSelectionManager backManager;

    public CardSelectionManagerBase CurrentSelectionManager { get; private set; }

    private void Awake()
    {
        Instance = this;

        // 처음에는 Middle을 사용
        CurrentSelectionManager = middleManager;
    }

    public void SetFront()
    {
        CurrentSelectionManager = frontManager;
    }

    public void SetMiddle()
    {
        CurrentSelectionManager = middleManager;
    }

    public void SetBack()
    {
        CurrentSelectionManager = backManager;
    }
}