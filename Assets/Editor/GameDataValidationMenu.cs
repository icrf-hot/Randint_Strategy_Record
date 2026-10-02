using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Randint.Data;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GameDataValidationMenu
{
    [MenuItem("Tools/Randint/Game Data/Validate JSON and References")]
    public static void ValidateAll()
    {
        GameDataCatalog catalog = GameDataCatalog.Load(path =>
        {
            string file = "Assets/Resources/" + path + ".json";
            return File.Exists(file) ? File.ReadAllText(file) : null;
        });
        var errors = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/OperatorData", "Assets/SkillData" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            CheckFields(asset, path, catalog, errors);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CheckHierarchy(AssetDatabase.LoadAssetAtPath<GameObject>(path), path, catalog, errors);
        }
        // Preview Scene은 사용자가 열어 둔 Scene과 저장 상태를 변경하지 않습니다.
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                var nodeIds = new HashSet<int>();
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    CheckHierarchy(root, path, catalog, errors);
                    foreach (MapNode node in root.GetComponentsInChildren<MapNode>(true))
                        if (!nodeIds.Add(node.NodeID)) errors.Add($"{path}: 중복 MapNode ID {node.NodeID}");
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        if (errors.Count > 0) throw new GameDataValidationException(errors);
        Debug.Log("[GameData] JSON 및 Scene/Prefab/ScriptableObject ID·텍스트 키·스킬·TMP 참조 검증 통과");
    }

    private static void CheckHierarchy(GameObject root, string path, GameDataCatalog catalog, List<string> errors)
    {
        foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (component == null) { errors.Add($"{path}: Missing Script"); continue; }
            CheckFields(component, path + "/" + component.name, catalog, errors);
            if (component is Operator op && op.Data == null)
                errors.Add($"{path}/{component.name}: OperatorData Asset 참조 누락");
            if (component is BattleResultPresenter)
            {
                var result = new SerializedObject(component);
                foreach (string field in new[] { "resultCanvasGroup", "resultTitleText", "mapNameText", "characterDialogueText", "resultMessageText" })
                    if (result.FindProperty(field).objectReferenceValue == null)
                        errors.Add($"{path}/{component.name}.{field}: 결과 UI 참조 누락");
            }
            if (component is Card)
            {
                var card = new SerializedObject(component);
                foreach (string field in new[] { "numberText1", "numberText2" })
                    if (card.FindProperty(field).objectReferenceValue == null)
                        errors.Add($"{path}/{component.name}.{field}: 카드 TMP 참조 누락");
            }
            if (component is MapBattleSceneLoader)
            {
                string scenePath = new SerializedObject(component).FindProperty("battleScenePath").stringValue;
                if (!Array.Exists(EditorBuildSettings.scenes, scene => scene.enabled && scene.path == scenePath))
                    errors.Add($"{path}: Battle Scene 경로가 활성 Build Settings에 없습니다: '{scenePath}'");
            }
        }
    }

    private static void CheckFields(object owner, string path, GameDataCatalog catalog, List<string> errors)
    {
        if (owner == null) return;
        foreach (FieldInfo field in owner.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            object value = field.GetValue(owner);
            if (field.GetCustomAttribute<GameTextKeyAttribute>() != null && !catalog.ContainsText(value as string))
                errors.Add($"{path}.{field.Name}: 누락 text 참조 '{value}'");
            GameDefinitionIdAttribute id = field.GetCustomAttribute<GameDefinitionIdAttribute>();
            if (id != null && !catalog.ContainsDefinition(id.Kind, value as string))
                errors.Add($"{path}.{field.Name}: 누락 {id.Kind} 참조 '{value}'");
            if (value is LocalizedSceneTexts.Binding[] bindings)
            {
                for (int i = 0; i < bindings.Length; i++)
                {
                    if (bindings[i] == null || bindings[i].target == null) errors.Add($"{path}.bindings[{i}]: TMP 참조 누락");
                    CheckFields(bindings[i], path + $".bindings[{i}]", catalog, errors);
                }
            }
            else if (value is BattleResultTextSet textSet) CheckFields(textSet, path + "." + field.Name, catalog, errors);
        }
    }

    // CI/배치 실행에서도 검증 실패를 종료 코드로 전달합니다.
    public static void ValidateBatch()
    {
        try { ValidateAll(); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}

public sealed class GameDataBuildValidator : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        try { GameDataValidationMenu.ValidateAll(); }
        catch (Exception e) { throw new BuildFailedException(e.Message); }
    }
}
