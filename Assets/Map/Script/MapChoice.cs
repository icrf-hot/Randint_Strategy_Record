using UnityEngine;

using Randint.Data;

public class MapChoice : MonoBehaviour
{
    [Header("Choice")]
    [SerializeField, GameTextKey] private string choiceTextKey;

    public string ChoiceText => GameData.Text(choiceTextKey);

    public void Select(MapNode targetNode)
    {
        if (targetNode == null)
            return;

        MapPlayerPositionFeedback feedback =
            FindFirstObjectByType<MapPlayerPositionFeedback>();

        if (feedback == null)
        {
            Debug.LogWarning(
                "[Map Choice] MapPlayerPositionFeedback를 찾을 수 없습니다.",
                this
            );
            return;
        }

        feedback.MoveToNode(targetNode);

        if (feedback.CurrentNode != targetNode)
        {
            Debug.LogWarning(
                $"[Map Choice] Node {targetNode.NodeID}로 이동할 수 없습니다.",
                targetNode
            );
            return;
        }

        Debug.Log(
            $"[Map Choice] Node {targetNode.NodeID} 이동 완료. " +
            $"Type: {targetNode.NodeType}",
            targetNode
        );

        if (targetNode.NodeType == MapNodeType.Battle)
        {
            EnterBattle(targetNode);
        }
    }

    private void EnterBattle(MapNode battleNode)
    {
        MapBattleSceneLoader sceneLoader =
            FindFirstObjectByType<MapBattleSceneLoader>();

        if (sceneLoader == null)
        {
            Debug.LogError(
                "[Map Battle] MapBattleSceneLoader를 찾을 수 없습니다.",
                this
            );
            return;
        }

        sceneLoader.LoadBattle(battleNode);
    }
}
