using UnityEngine;

public abstract class CardSelectionManagerBase : MonoBehaviour
{
    public abstract void SelectCard(Card card);

    public virtual void ResetSelection()
    {

    }
}