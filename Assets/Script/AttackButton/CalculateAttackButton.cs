using UnityEngine;
using UnityEngine.EventSystems;

public class CalculateAttackButton : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (MiddleCardSelectionManager.Instance == null)
            return;

        MiddleCardSelectionManager.Instance.CalculateAttack();
    }
}