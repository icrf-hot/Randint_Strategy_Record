using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MapNodeTypeVerification
{
    public static void Run()
    {
        string[] names = { "Start", "Battle", "EliteBattle", "Event", "Shop", "Rest",
                           "FirstBoss", "SecondBoss", "ThirdBoss", "Boss" };
        for (int i = 0; i < names.Length; i++)
        {
            if (Enum.GetName(typeof(MapNodeType), i) != names[i])
                throw new InvalidOperationException("Map node enum numeric value changed.");
        }

        EditorSceneManager.OpenScene("Assets/Scenes/Map_1F.unity");
        MapNode[] nodes = UnityEngine.Object.FindObjectsByType<MapNode>(FindObjectsSortMode.None)
            .OrderBy(node => node.NodeID).ToArray();
        int[] types = { 0, 3, 3, 3, 1, 1, 1, 4 };
        if (nodes.Length != types.Length)
            throw new InvalidOperationException("Expected eight map nodes.");
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].NodeID != i + 1 || (int)nodes[i].NodeType != types[i])
                throw new InvalidOperationException("Saved map node type no longer matches its ID.");
        }

        GameObject fixture = new GameObject("MapNodeTypeFixture");
        try
        {
            MapNode node = fixture.AddComponent<MapNode>();
            SerializedObject serialized = new SerializedObject(node);
            serialized.FindProperty("nodeType").intValue = 8;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (node.NodeType != MapNodeType.ThirdBoss)
                throw new InvalidOperationException("Serialized value 8 did not resolve to ThirdBoss.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(fixture);
        }

        Debug.Log("[Map Node Type Verification] Enum values 0-9, eight saved map nodes, and ThirdBoss value 8 passed.");
    }
}
