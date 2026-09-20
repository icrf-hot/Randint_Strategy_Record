using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSpawner : MonoBehaviour
{
    public static CardSpawner Instance { get; private set; }

    [Header("Card Prefabs")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private GameObject specialCardPrefab;

    [Header("생성 위치")]
    [SerializeField] private Transform spawnPoint;

    [Header("카드 개수")]
    [SerializeField] private int cardCount = 10;

    [Header("카드 간격")]
    [SerializeField] private float spacing = 2f;

    [Header("특별 카드 범위")]
    [SerializeField] private int specialRangeMin = 1;
    [SerializeField] private int specialRangeMax = 5;
    [SerializeField] private string specialCardText = "JK";

    [Header("카드 생성 간격")]
    [SerializeField] private float spawnDelay = 0.2f;

    [Header("전투 시작 카드 회수")]
    [SerializeField] private float gatherDuration = 0.35f;
    [SerializeField] private float dropDuration = 0.4f;
    [SerializeField] private float dropDistance = 3f;

    private readonly List<Card> spawnedCards =
        new List<Card>();

    private Coroutine spawnRoutine;
    private Coroutine discardRoutine;

    public bool IsDiscarding =>
        discardRoutine != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        DealNewCards();
    }

    public void BeginDiscardAnimation()
    {
        if (discardRoutine != null)
            return;

        StopSpawnRoutine();

        discardRoutine =
            StartCoroutine(DiscardAnimationRoutine());
    }

    public IEnumerator WaitForDiscardAnimation()
    {
        while (discardRoutine != null)
        {
            yield return null;
        }
    }

    public void ReplaceCards()
    {
        ClearCurrentCards();
        DealNewCards();
    }

    public void ClearCurrentCards()
    {
        StopSpawnRoutine();
        StopDiscardRoutine();

        foreach (Card card in spawnedCards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }

        spawnedCards.Clear();

        Debug.Log("[카드] 사용한 카드를 제거했습니다.");
    }

    public void DealNewCards()
    {
        if (spawnRoutine != null ||
            discardRoutine != null)
        {
            return;
        }

        if (cardPrefab == null ||
            specialCardPrefab == null ||
            spawnPoint == null)
        {
            Debug.LogError(
                "[카드] CardSpawner의 Inspector 참조가 없습니다.");

            return;
        }

        spawnRoutine =
            StartCoroutine(SpawnCards());
    }

    private IEnumerator DiscardAnimationRoutine()
    {
        if (spawnPoint == null)
        {
            Debug.LogError(
                "[카드 회수] Spawn Point가 없습니다.");

            discardRoutine = null;
            yield break;
        }

        List<Card> cardsToDiscard =
            new List<Card>();

        List<Vector3> startPositions =
            new List<Vector3>();

        foreach (Card card in spawnedCards)
        {
            if (card == null)
                continue;

            card.StopVisualAnimations();

            cardsToDiscard.Add(card);
            startPositions.Add(card.transform.position);
        }

        if (cardsToDiscard.Count == 0)
        {
            discardRoutine = null;
            yield break;
        }

        Debug.Log(
            $"[카드 회수 시작] {cardsToDiscard.Count}장의 카드를 " +
            "Spawn Point로 모읍니다.");

        Vector3 gatherPosition =
            spawnPoint.position;

        float elapsed = 0f;

        while (elapsed < gatherDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                gatherDuration > 0f
                    ? Mathf.Clamp01(elapsed / gatherDuration)
                    : 1f;

            t = SmoothStep(t);

            for (int i = 0; i < cardsToDiscard.Count; i++)
            {
                Card card = cardsToDiscard[i];

                if (card == null)
                    continue;

                card.transform.position =
                    Vector3.Lerp(
                        startPositions[i],
                        gatherPosition,
                        t);
            }

            yield return null;
        }

        foreach (Card card in cardsToDiscard)
        {
            if (card != null)
            {
                card.transform.position =
                    gatherPosition;
            }
        }

        Debug.Log(
            "[카드 회수] 카드가 Spawn Point에 모였습니다.");

        Vector3 dropPosition =
            gatherPosition +
            Vector3.down * dropDistance;

        elapsed = 0f;

        while (elapsed < dropDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t =
                dropDuration > 0f
                    ? Mathf.Clamp01(elapsed / dropDuration)
                    : 1f;

            t = SmoothStep(t);

            foreach (Card card in cardsToDiscard)
            {
                if (card == null)
                    continue;

                card.transform.position =
                    Vector3.Lerp(
                        gatherPosition,
                        dropPosition,
                        t);
            }

            yield return null;
        }

        foreach (Card card in cardsToDiscard)
        {
            if (card == null)
                continue;

            card.transform.position = dropPosition;
            card.gameObject.SetActive(false);
        }

        Debug.Log(
            "[카드 회수 완료] 카드가 아래로 사라졌습니다.");

        discardRoutine = null;
    }

    private IEnumerator SpawnCards()
    {
        Debug.Log(
            $"[카드] 새로운 카드 {cardCount}장 배분을 시작합니다.");

        // 이번 카드 묶음에서 JK가 들어갈 위치를 먼저 하나만 결정한다.
        // -1이면 이번 묶음에는 JK가 없다.
        int specialCardIndex =
            SelectSpecialCardIndex();

        if (specialCardIndex >= 0)
        {
            Debug.Log(
                $"[카드] 이번 배분에는 JK 카드가 1장 포함됩니다. " +
                $"카드 위치: {specialCardIndex}");
        }
        else
        {
            Debug.Log(
                "[카드] 이번 배분에는 JK 카드가 없습니다.");
        }

        for (int i = 0; i < cardCount; i++)
        {
            // 선택된 단 하나의 위치에서만 JK를 생성한다.
            bool isSpecialCard =
                i == specialCardIndex;

            GameObject prefabToSpawn =
                isSpecialCard
                    ? specialCardPrefab
                    : cardPrefab;

            GameObject cardObject =
                Instantiate(
                    prefabToSpawn,
                    spawnPoint.position,
                    Quaternion.identity);

            Card card =
                cardObject.GetComponent<Card>();

            if (card == null)
            {
                Debug.LogError(
                    "[카드] 생성된 프리팹에 Card가 없습니다.");

                Destroy(cardObject);
                continue;
            }

            spawnedCards.Add(card);

            if (isSpecialCard)
            {
                card.SetSpecialCard(
                    specialCardText);
            }
            else
            {
                int cardNumber =
                    Random.Range(1, 11);

                card.SetNumber(cardNumber);
            }

            Vector3 targetPosition =
                spawnPoint.position +
                new Vector3(
                    i * spacing,
                    0f,
                    0f);

            card.MoveTo(targetPosition);

            if (spawnDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    spawnDelay);
            }
            else
            {
                yield return null;
            }
        }

        spawnRoutine = null;

        Debug.Log(
            $"[카드] 새로운 카드 {spawnedCards.Count}장 " +
            "배분을 완료했습니다.");
    }


    private int SelectSpecialCardIndex()
    {
        List<int> candidates =
            new List<int>();

        // 기존과 동일하게 카드 자리마다 JK 확률을 판정한다.
        for (int i = 0; i < cardCount; i++)
        {
            int chanceNumber =
                Random.Range(1, 101);

            bool passedSpecialChance =
                chanceNumber >= specialRangeMin &&
                chanceNumber <= specialRangeMax;

            if (passedSpecialChance)
            {
                candidates.Add(i);
            }
        }

        // 모든 자리가 확률 판정에 실패하면 JK는 0장이다.
        if (candidates.Count == 0)
        {
            return -1;
        }

        // 후보가 여러 개여도 그중 하나만 최종 JK 위치로 선택한다.
        int selectedCandidateIndex =
            Random.Range(
                0,
                candidates.Count);

        return candidates[selectedCandidateIndex];
    }

    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private void StopSpawnRoutine()
    {
        if (spawnRoutine == null)
            return;

        StopCoroutine(spawnRoutine);
        spawnRoutine = null;
    }

    private void StopDiscardRoutine()
    {
        if (discardRoutine == null)
            return;

        StopCoroutine(discardRoutine);
        discardRoutine = null;
    }
}