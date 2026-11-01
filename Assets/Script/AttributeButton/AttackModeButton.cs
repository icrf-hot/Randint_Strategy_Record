using UnityEngine;
using UnityEngine.EventSystems;

public enum AttackModeButtonType
{
    PHYS,
    ARTS
}

public class AttackModeButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private AttackModeButtonType buttonType;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (BattleExecuteManager.IsExecuting) return;
        if (TransitionManager.IsBusy) return;
        if (TransitionManager.IsBusy) return;
        if (TransitionManager.IsBusy) return;
        if (MiddleCardSelectionManager.Instance == null)
            return;

        if (buttonType == AttackModeButtonType.PHYS)
            MiddleCardSelectionManager.Instance.SetPhysicalMode();
        else if (buttonType == AttackModeButtonType.ARTS)
            MiddleCardSelectionManager.Instance.SetArtsMode();
    }
}
