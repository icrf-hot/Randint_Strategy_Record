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
        if (DamageDebugManager.Instance == null)
            return;

        DamageDebugManager.Instance
            .DamageOperator(targetOperator);
    }
}