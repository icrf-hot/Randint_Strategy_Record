using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ShopSceneSetup
{
    public static void Run()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ShopSceneRouter.ShopPath) != null)
            throw new InvalidOperationException("Shop Scene already exists; do not overwrite it.");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ShopSceneUI ui = new GameObject("ShopController").AddComponent<ShopSceneUI>();
        SerializedObject serialized = new SerializedObject(ui);
        serialized.FindProperty("catalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<ShopCatalogData>("Assets/Shop/Data/PrototypeShopCatalog.asset");
        serialized.FindProperty("progressionSettings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RunProgressionSettings>("Assets/Shop/Data/RunProgression.asset");
        serialized.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TMP/Cafe24PROSlimMax SDF.asset");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        if (!EditorSceneManager.SaveScene(scene, ShopSceneRouter.ShopPath))
            throw new InvalidOperationException("Shop Scene save failed.");
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ShopSceneRouter.ShopPath, true) }).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("[Shop Scene Setup] Shop scene and Build Settings registered.");
    }
}
