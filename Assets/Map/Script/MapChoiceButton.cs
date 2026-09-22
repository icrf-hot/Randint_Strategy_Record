using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MapChoiceButton : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private MapNodeFocusObject focusObject;

    [Header("Choice")]
    [SerializeField] private int choiceIndex;

    public int ChoiceIndex => choiceIndex;

    private void Awake()
    {
        EnsureRaycastGraphic();

        if (focusObject == null)
        {
            focusObject = FindFirstObjectByType<MapNodeFocusObject>();
        }
    }

    private void EnsureRaycastGraphic()
    {
        Image raycastImage = GetComponent<Image>();

        if (raycastImage == null)
        {
            raycastImage = gameObject.AddComponent<Image>();
            raycastImage.color = Color.clear;
        }

        raycastImage.raycastTarget = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (focusObject == null)
            return;

        focusObject.SelectChoice(choiceIndex);
    }
}
