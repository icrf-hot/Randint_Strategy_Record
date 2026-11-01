using UnityEngine;

public class EnemyTargetable : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private bool canBeTargeted = true;

    [SerializeField] private Collider2D targetCollider;

    [Header("Indicators")]
    [SerializeField] private GameObject clickableIndicator;
    [SerializeField] private GameObject selectedIndicator;

    [Header("Sprite")]
    private SpriteRenderer targetRenderer;

    [Header("Target Colors")]
    private readonly Color normalColor = UITheme.TextDim;

    // 선택 가능하지만 아직 선택되지 않음
    private readonly Color targetableColor = UITheme.TextMain;

    // 선택 불가능
    private readonly Color untargetableColor = UITheme.TextDim;

    // 선택된 적
    private readonly Color selectedColor = UITheme.Accent;

    [Header("Position")]
    private EnemyPosition position;

    public EnemyPosition Position =>
        position;

    public bool CanBeTargeted =>
        canBeTargeted;

    public Collider2D TargetCollider =>
        targetCollider;


    private void Awake()
    {
        if (targetCollider == null)
            targetCollider =
                GetComponent<Collider2D>();

        targetRenderer =
            GetComponent<SpriteRenderer>();

        if (targetRenderer == null)
        {
            targetRenderer =
                GetComponentInChildren<SpriteRenderer>(
                    true);
        }

        HideAllIndicators();

        SetNormalColor();
    }


    // =========================================================
    // SpawnPoint 위치
    // =========================================================

    public void SetPosition(
        EnemyPosition newPosition)
    {
        position = newPosition;
    }


    // =========================================================
    // 타겟 가능 여부
    // =========================================================

    public bool CanBeTargetedBy(Operator op)
    {
        if (!canBeTargeted)
            return false;

        if (op == null)
            return false;

        return true;
    }


    // =========================================================
    // 선택 가능한 적
    // =========================================================

    public void ShowClickable()
    {
        if (targetRenderer != null)
            targetRenderer.color = targetableColor;

        if (clickableIndicator != null)
            clickableIndicator.SetActive(true);

        if (selectedIndicator != null)
            selectedIndicator.SetActive(false);
    }


    // =========================================================
    // 선택 불가능한 적
    // =========================================================

    public void ShowUntargetable()
    {
        if (targetRenderer != null)
            targetRenderer.color = untargetableColor;

        if (clickableIndicator != null)
            clickableIndicator.SetActive(false);

        if (selectedIndicator != null)
            selectedIndicator.SetActive(false);
    }


    // =========================================================
    // 선택된 적
    // =========================================================

    public void ShowSelected()
    {
        if (targetRenderer != null)
            targetRenderer.color = selectedColor;

        if (clickableIndicator != null)
            clickableIndicator.SetActive(false);

        if (selectedIndicator != null)
            selectedIndicator.SetActive(true);
    }


    // =========================================================
    // 원래 상태
    // =========================================================

    public void SetNormalColor()
    {
        if (targetRenderer != null)
            targetRenderer.color = normalColor;
    }


    // =========================================================
    // 클릭 가능 Indicator 제거
    // =========================================================

    public void HideClickable()
    {
        if (clickableIndicator != null)
            clickableIndicator.SetActive(false);
    }


    // =========================================================
    // 선택 Indicator 제거
    // =========================================================

    public void HideSelected()
    {
        if (selectedIndicator != null)
            selectedIndicator.SetActive(false);
    }


    // =========================================================
    // 전체 초기화
    // =========================================================

    public void HideAllIndicators()
    {
        if (clickableIndicator != null)
            clickableIndicator.SetActive(false);

        if (selectedIndicator != null)
            selectedIndicator.SetActive(false);

        SetNormalColor();
    }
}
