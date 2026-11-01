using UnityEngine;

public abstract class CardSelectionManagerBase : MonoBehaviour
{
    public virtual Card[] FeedbackCards => System.Array.Empty<Card>();
    public abstract void SelectCard(Card card);

    public virtual void ResetSelection()
    {

    }
}
