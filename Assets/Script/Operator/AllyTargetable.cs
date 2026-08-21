using UnityEngine;

public class AllyTargetable : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private bool canBeTargeted = true;

    [SerializeField] private Collider2D targetCollider;

    [Header("Indicators")]
    [SerializeField] private GameObject clickableIndicator;
    [SerializeField] private GameObject selectedIndicator;

    private Operator operatorData;

    public Operator Operator =>
        operatorData;

    public bool CanBeTargeted =>
        canBeTargeted;

    public Collider2D TargetCollider =>
        targetCollider;


    private void Awake()
    {
        operatorData =
            GetComponent<Operator>();

        if (targetCollider == null)
            targetCollider =
                GetComponent<Collider2D>();

        HideAllIndicators();
    }


    public void ShowClickable()
    {
        if (clickableIndicator != null)
            clickableIndicator.SetActive(true);

        if (selectedIndicator != null)
            selectedIndicator.SetActive(false);
    }


    public void ShowSelected()
    {
        if (clickableIndicator != null)
            clickableIndicator.SetActive(false);

        if (selectedIndicator != null)
            selectedIndicator.SetActive(true);
    }


    public void HideAllIndicators()
    {
        if (clickableIndicator != null)
            clickableIndicator.SetActive(false);

        if (selectedIndicator != null)
            selectedIndicator.SetActive(false);
    }
}