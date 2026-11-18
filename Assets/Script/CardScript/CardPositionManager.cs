using UnityEngine;

public class CardPositionManager : MonoBehaviour
{
    public static CardPositionManager Instance { get; private set; }

    [Header("Selection Managers")]
    [SerializeField] private FrontCardSelectionManager frontManager;
    [SerializeField] private MiddleCardSelectionManager middleManager;
    [SerializeField] private BackCardSelectionManager backManager;

    private CardSelectionManagerBase currentSelectionManager;
    public CardSelectionManagerBase CurrentSelectionManager
    {
        get => currentSelectionManager != null && currentSelectionManager.SelectionEnabled ? currentSelectionManager : null;
        private set => currentSelectionManager = value;
    }

    private void Awake()
    {
        Instance = this;

        // 처음에는 Middle을 사용
        CurrentSelectionManager = middleManager;
    }

    public void SetFront()
    {
        if (frontManager == null || !frontManager.SelectionEnabled) return;
        CurrentSelectionManager = frontManager;
    }

    public void SetMiddle()
    {
        if (middleManager == null || !middleManager.SelectionEnabled) return;
        CurrentSelectionManager = middleManager;
    }

    public void SetBack()
    {
        if (backManager == null || !backManager.SelectionEnabled) return;
        CurrentSelectionManager = backManager;
    }
}
