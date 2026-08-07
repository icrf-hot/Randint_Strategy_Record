using UnityEngine;

public class SpriteToggleGroup : MonoBehaviour
{
    private SpriteToggleButton currentButton;

    public void Select(SpriteToggleButton button)
    {
        if (currentButton == button)
            return;

        if (currentButton != null)
            currentButton.Deselect();

        currentButton = button;
        currentButton.Select();
    }

    public void ResetSelection()
    {
        if (currentButton != null)
            currentButton.Deselect();

        currentButton = null;
    }
}