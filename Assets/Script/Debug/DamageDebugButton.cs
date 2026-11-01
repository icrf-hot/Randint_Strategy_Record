using UnityEngine;
using UnityEngine.EventSystems;

public class DamageDebugButton :
    MonoBehaviour,
    IPointerClickHandler
{
    [SerializeField]
    private Operator targetOperator;

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (TransitionManager.IsBusy) return;
        if (TransitionManager.IsBusy) return;
        if (TransitionManager.IsBusy) return;
        if (DamageDebugManager.Instance == null)
            return;

        DamageDebugManager.Instance
            .DamageOperator(targetOperator);
    }
}