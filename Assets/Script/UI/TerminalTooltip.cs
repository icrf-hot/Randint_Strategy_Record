using UnityEngine;
using UnityEngine.EventSystems;

public sealed class TerminalTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public string message;
    private GameObject tooltip;
    private void Show()
    {
        var button = GetComponent<UnityEngine.UI.Button>();
        if (button == null || button.IsInteractable() || tooltip != null) return;
        var panel = TerminalUI.Panel("Tooltip", transform, new Vector2(0, 52), new Vector2(300, 34));
        TerminalUI.Label("Message", panel.transform, message, Vector2.zero, new Vector2(290, 32), 16, UITheme.Tone.Warn);
        tooltip = panel.gameObject;
    }
    public void OnPointerEnter(PointerEventData e) => Show();
    public void OnPointerExit(PointerEventData e) => Hide();
    public void OnSelect(BaseEventData e) => Show();
    public void OnDeselect(BaseEventData e) => Hide();
    private void Hide() { if (tooltip != null) Destroy(tooltip); tooltip = null; }
    private void OnDisable() => Hide();
}
