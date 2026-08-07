using System.Collections;
using UnityEngine;

public class CardSpawner : MonoBehaviour
{
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

    private void Start()
    {
        StartCoroutine(SpawnCards());
    }

    private IEnumerator SpawnCards()
    {
        for (int i = 0; i < cardCount; i++)
        {
            int chanceNumber = Random.Range(1, 101);

            bool isSpecialCard =
                chanceNumber >= specialRangeMin &&
                chanceNumber <= specialRangeMax;

            GameObject prefabToSpawn =
                isSpecialCard ? specialCardPrefab : cardPrefab;

            GameObject obj = Instantiate(
                prefabToSpawn,
                spawnPoint.position,
                Quaternion.identity);

            Card card = obj.GetComponent<Card>();

            if (card != null)
            {
                if (isSpecialCard)
                {
                    card.SetSpecialCard(specialCardText);
                }
                else
                {
                    int cardNumber = Random.Range(1, 11);
                    card.SetNumber(cardNumber);
                }

                Vector3 target =
                    spawnPoint.position +
                    new Vector3(i * spacing, 0, 0);

                card.MoveTo(target);
            }

            yield return new WaitForSeconds(spawnDelay);
        }
    }
}
