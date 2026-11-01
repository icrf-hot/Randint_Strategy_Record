using UnityEngine;
using UnityEngine.EventSystems;

public class CalculateAttackButton :
    MonoBehaviour,
    IPointerClickHandler
{
    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (TransitionManager.IsBusy) return;
        if (TransitionManager.IsBusy) return;
        if (TransitionManager.IsBusy) return;
        if (BattleExecuteManager.Instance == null)
            return;

        BattleExecuteManager.Instance.ExecuteBattle();
    }
}