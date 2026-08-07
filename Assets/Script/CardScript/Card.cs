using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("TMP")]
    [SerializeField] private TMP_Text numberText1;
    [SerializeField] private TMP_Text numberText2;

    [Header("이동")]
    [SerializeField] private float moveTime = 0.7f;
    [SerializeField] private AnimationCurve moveCurve;

    [Header("Hover")]
    [SerializeField] private float hoverHeight = 0.4f;
    [SerializeField] private float selectedHeight = 0.7f;
    [SerializeField] private float hoverTime = 0.15f;

    private int cardNumber;

    private CardSelectionManagerBase ownerManager;

    private string specialCardText;
    private bool isSpecialCard;
    private Vector3 originalPosition;

    private Coroutine moveCoroutine;
    private Coroutine hoverCoroutine;

    public int CardNumber => cardNumber;
    public bool IsSpecialCard => isSpecialCard;
    public string SpecialCardText => specialCardText;
    public bool IsSelected => ownerManager != null;

    public CardSelectionManagerBase OwnerManager => ownerManager;

    private void Awake()
    {
        originalPosition = transform.position;
    }

    public void SetNumber(int number)
    {
        isSpecialCard = false;
        specialCardText = "";
        cardNumber = number;

        numberText1.text = number.ToString();
        numberText2.text = number.ToString();
    }

    public void SetSpecialCard(string text)
    {
        isSpecialCard = true;
        specialCardText = text;
        cardNumber = 0;
    }

    public void MoveTo(Vector3 target)
    {
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);

        originalPosition = target;
        moveCoroutine = StartCoroutine(MoveRoutine(target));
    }

    public void SetSelected(bool selected, CardSelectionManagerBase manager)
    {
        ownerManager = selected ? manager : null;

        if (selected)
            HoverTo(originalPosition + Vector3.up * selectedHeight);
        else
            HoverTo(originalPosition);
    }

    private IEnumerator MoveRoutine(Vector3 target)
    {
        Vector3 start = transform.position;

        float elapsed = 0;

        while (elapsed < moveTime)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / moveTime;
            float curve = moveCurve.Evaluate(t);

            transform.position = Vector3.Lerp(start, target, curve);

            yield return null;
        }

        transform.position = target;
        originalPosition = target;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsSelected)
            return;

        HoverTo(originalPosition + Vector3.up * hoverHeight);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsSelected)
            return;

        HoverTo(originalPosition);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (CardPositionManager.Instance.CurrentSelectionManager != null)
        {
            CardPositionManager.Instance
                .CurrentSelectionManager
                .SelectCard(this);
        }
    }

    private void HoverTo(Vector3 target)
    {
        if (hoverCoroutine != null)
            StopCoroutine(hoverCoroutine);

        hoverCoroutine = StartCoroutine(HoverRoutine(target));
    }

    private IEnumerator HoverRoutine(Vector3 target)
    {
        Vector3 start = transform.position;

        float elapsed = 0;

        while (elapsed < hoverTime)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / hoverTime;

            transform.position = Vector3.Lerp(start, target, t);

            yield return null;
        }

        transform.position = target;
    }
}
