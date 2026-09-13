using UnityEngine;

public class MapChoice : MonoBehaviour
{
    [Header("Choice")]
    [TextArea(2, 5)]
    [SerializeField] private string choiceText;

    public string ChoiceText => choiceText;

    public void Select(MapNode targetNode)
    {
        if (targetNode == null)
            return;

        MapPlayerPositionFeedback feedback =
            FindFirstObjectByType<MapPlayerPositionFeedback>();

        if (feedback == null)
        {
            Debug.LogWarning(
                "MapChoice: MapPlayerPositionFeedback을 찾을 수 없습니다."
            );

            return;
        }

        feedback.MoveToNode(targetNode);
    }
}