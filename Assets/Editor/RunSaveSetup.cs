using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RunSaveSetup
{
    public static void Run()
    {
        const string path = "Assets/Resources/RunSaveBindings.asset";
        var bindings = AssetDatabase.LoadAssetAtPath<RunSaveBindings>(path);
        if (bindings == null)
        {
            bindings = ScriptableObject.CreateInstance<RunSaveBindings>();
            AssetDatabase.CreateAsset(bindings, path);
        }
        var map = EditorSceneManager.OpenScene("Assets/Scenes/Map_1F.unity");
        int[] nodeIds = UnityEngine.Object.FindObjectsByType<MapNode>(FindObjectsSortMode.None).Select(node => node.NodeID).OrderBy(id => id).ToArray();
        MapSaveControls controls = UnityEngine.Object.FindFirstObjectByType<MapSaveControls>();
        if (controls == null) controls = new GameObject("RunSaveControls").AddComponent<MapSaveControls>();
        Set(controls, "font", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TMP/Cafe24PROSlimMax SDF.asset"));
        EditorSceneManager.SaveScene(map);

        EditorSceneManager.OpenScene("Assets/Scenes/Playing_Scene.unity");
        OperatorData[] party = UnityEngine.Object.FindObjectsByType<Operator>(FindObjectsSortMode.None)
            .Select(op => op.Data).OrderBy(op => op.Position).ToArray();
        bindings = AssetDatabase.LoadAssetAtPath<RunSaveBindings>(path);
        var serialized = new SerializedObject(bindings);
        serialized.FindProperty("progression").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RunProgressionSettings>("Assets/Shop/Data/RunProgression.asset");
        serialized.FindProperty("shop").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ShopCatalogData>("Assets/Shop/Data/PrototypeShopCatalog.asset");
        var allOperators = AssetDatabase.FindAssets("t:OperatorData", new[] { "Assets/OperatorData" })
            .Select(guid => AssetDatabase.LoadAssetAtPath<OperatorData>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
        var operators = serialized.FindProperty("operators");
        operators.arraySize = allOperators.Length;
        for (int i = 0; i < allOperators.Length; i++) operators.GetArrayElementAtIndex(i).objectReferenceValue = allOperators[i];
        var defaults = serialized.FindProperty("defaultPartyIds");
        defaults.arraySize = party.Length;
        for (int i = 0; i < party.Length; i++) defaults.GetArrayElementAtIndex(i).stringValue = party[i].DataId;
        var nodes = serialized.FindProperty("mapNodeIds");
        nodes.arraySize = nodeIds.Length;
        for (int i = 0; i < nodeIds.Length; i++) nodes.GetArrayElementAtIndex(i).intValue = nodeIds[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(bindings);
        AssetDatabase.SaveAssets();

        var lobby = EditorSceneManager.OpenScene("Assets/Scenes/Lobby.unity");
        var controller = UnityEngine.Object.FindFirstObjectByType<LobbySceneController>();
        var lobbyData = new SerializedObject(controller);
        GameObject window = (GameObject)lobbyData.FindProperty("continueWindow").objectReferenceValue;
        Button close = (Button)lobbyData.FindProperty("continueCloseButton").objectReferenceValue;
        var slot = window.GetComponent<SaveSlotWindow>();
        if (slot == null) slot = window.AddComponent<SaveSlotWindow>();
        Button resume = AddButton(close, "ResumeSavedRun", -200);
        Button delete = AddButton(close, "DeleteSavedRun", 0);
        Vector2 closePosition = ((RectTransform)close.transform).anchoredPosition;
        ((RectTransform)close.transform).anchoredPosition = new Vector2(200, closePosition.y);
        TMP_Text message = window.transform.Find("Dialog/Message").GetComponent<TMP_Text>();
        message.enableAutoSizing = true;
        message.fontSizeMin = 14;
        message.fontSizeMax = 23;
        Set(slot, "sceneLoader", lobbyData.FindProperty("sceneLoader").objectReferenceValue);
        Set(slot, "summaryText", message);
        Set(slot, "resumeButton", resume);
        Set(slot, "deleteButton", delete);
        EditorSceneManager.SaveScene(lobby);
        AssetDatabase.SaveAssets();
        Debug.Log("[Run Save Setup] Registry, Map save controls and Lobby slot actions connected.");
    }

    private static Button AddButton(Button template, string name, float x)
    {
        Transform existing = template.transform.parent.Find(name);
        Button button = existing != null ? existing.GetComponent<Button>() : UnityEngine.Object.Instantiate(template, template.transform.parent);
        button.name = name;
        RectTransform rect = (RectTransform)button.transform;
        rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
        return button;
    }

    private static void Set(UnityEngine.Object owner, string name, UnityEngine.Object value)
    {
        var data = new SerializedObject(owner);
        data.FindProperty(name).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
