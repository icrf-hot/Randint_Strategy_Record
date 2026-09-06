using UnityEngine;

public class MapChoice : MonoBehaviour
{
    [Header("Choice")]
    [TextArea(2, 5)]
    [SerializeField] private string choiceText;

    public string ChoiceText => choiceText;
}