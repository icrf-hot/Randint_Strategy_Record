using UnityEngine;

public abstract class CardSelectionManagerBase : MonoBehaviour
{
    public bool SelectionEnabled { get; private set; } = true;
    public void SetSelectionEnabled(bool available)
    {
        if (SelectionEnabled == available) return;
        SelectionEnabled = available;
        if (!available) ResetSelection(); // Release owned cards for the remaining active positions.
    }
    public virtual Card[] FeedbackCards => System.Array.Empty<Card>();
    public abstract void SelectCard(Card card);

    public virtual void ResetSelection()
    {

    }
}
