using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class PreparationSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/Preparation.unity";
    private static readonly List<LocalizedSceneTexts.Binding> bindings = new();
    private static TMP_FontAsset font;

    [MenuItem("Tools/Randint/Front End/Create Preparation Scene")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Preparation scene creation requires Edit Mode.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new InvalidOperationException("Preparation scene already exists; refusing to overwrite it.");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TMP/Cafe24PROSlimMax SDF.asset");
        if (font == null) throw new InvalidOperationException("Menu font is missing.");
        if (!PreparationSceneController.TryGetRoster(out OperatorData[] roster))
            throw new InvalidOperationException("Default FRONT/MIDDLE/BACK roster is invalid.");
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            bindings.Clear();
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = UITheme.BgBase;
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var canvas = new GameObject("PreparationCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvas.transform;
            Text(root, "Heading", "preparation.title", new Vector2(0, 306), new Vector2(1120, 48), 30);
            Text(root, "FormationHeading", "preparation.formation", new Vector2(-390, 244), new Vector2(340, 40), 22);
            var changes = new UnityEngine.UI.Button[3];
            var names = new TMP_Text[3];
            string[] positions = { "ui.position.front", "ui.position.middle", "ui.position.back" };
            string[] portraits = {
                "Assets/Image/Character/Saria/Saria.png",
                "Assets/Image/Character/Dorothy/Dorothy.png",
                "Assets/Image/Character/Ptilopsis/ptilposis.png"
            };
            for (int i = 0; i < 3; i++)
            {
                float x = -370 + i * 370;
                var panel = new GameObject(roster[i].Position + "Operator", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                panel.transform.SetParent(root, false);
                SlotUI.Place((RectTransform)panel.transform, new Vector2(x, 38), new Vector2(344, 356));
                panel.GetComponent<UnityEngine.UI.Image>().color = UITheme.TextMain;
                panel.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
                Text(panel.transform, "Position", positions[i], new Vector2(0, 148), new Vector2(312, 36), 20);
                var portrait = new GameObject("Portrait", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                portrait.transform.SetParent(panel.transform, false);
                SlotUI.Place((RectTransform)portrait.transform, new Vector2(0, 25), new Vector2(280, 200));
                var image = portrait.GetComponent<UnityEngine.UI.Image>();
                image.sprite = AssetDatabase.LoadAllAssetsAtPath(portraits[i]).OfType<Sprite>().FirstOrDefault();
                if (image.sprite == null) throw new InvalidOperationException("Missing character sprite: " + portraits[i]);
                image.preserveAspect = true;
                image.raycastTarget = false;
                names[i] = SlotUI.Label(panel.transform, "OperatorName", font, new Vector2(0, -96), new Vector2(312, 38), 24);
                names[i].text = roster[i].OperatorName;
                changes[i] = Button(panel.transform, "ChangeOperator", "preparation.change", new Vector2(0, -145), new Vector2(130, 40));
            }
            TMP_Text warning = SlotUI.Label(root, "Warning", font, new Vector2(0, -176), new Vector2(1120, 52), 18);
            warning.color = UITheme.Danger;
            warning.text = "";
            Text(root, "DataLossHeading", "preparation.loss_title", new Vector2(-370, -240), new Vector2(344, 42), 22);
            TMP_Text level = Text(root, "DataLossLevel", "preparation.loss_zero", new Vector2(0, -240), new Vector2(240, 42), 24);
            var back = Button(root, "ReturnToLobby", "preparation.back", new Vector2(-410, -310), new Vector2(220, 48));
            var start = Button(root, "StartRun", "preparation.start", new Vector2(410, -310), new Vector2(220, 48));
            start.GetComponent<UnityEngine.UI.Image>().color = UITheme.Ok;
            var owner = new GameObject("PreparationController");
            var loader = owner.AddComponent<MenuSceneLoader>();
            var controller = owner.AddComponent<PreparationSceneController>();
            Set(loader, "progressionSettings", AssetDatabase.LoadAssetAtPath<RunProgressionSettings>("Assets/Shop/Data/RunProgression.asset"));
            var transition = new GameObject("TransitionCanvas", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
            transition.transform.SetParent(root, false);
            var rect = (RectTransform)transition.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            transition.GetComponent<UnityEngine.UI.Image>().color = UITheme.BgBase;
            var group = transition.GetComponent<CanvasGroup>();
            group.alpha = 0; group.blocksRaycasts = false;
            TMP_Text loading = Text(transition.transform, "Loading", "menu.loading", Vector2.zero, new Vector2(1120, 70), 24);
            loading.color = UITheme.TextMain;
            Set(loader, "transitionCanvas", group); Set(loader, "transitionText", loading);
            Set(controller, "sceneLoader", loader); Set(controller, "startButton", start); Set(controller, "backButton", back);
            Set(controller, "warningText", warning); Set(controller, "dataLossText", level);
            var data = new SerializedObject(controller);
            SetArray(data.FindProperty("changeButtons"), changes);
            SetArray(data.FindProperty("operatorNames"), names);
            data.ApplyModifiedPropertiesWithoutUndo();
            var localizer = owner.AddComponent<LocalizedSceneTexts>();
            var localized = new SerializedObject(localizer);
            var entries = localized.FindProperty("bindings");
            entries.arraySize = bindings.Count;
            for (int i = 0; i < bindings.Count; i++)
            {
                entries.GetArrayElementAtIndex(i).FindPropertyRelative("target").objectReferenceValue = bindings[i].target;
                entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue = bindings[i].key;
            }
            localized.ApplyModifiedPropertiesWithoutUndo();
            transition.SetActive(false);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Preparation save failed.");
            RegisterBuildScene();
            Debug.Log("[Preparation] Scene created and appended to Build Settings. Play Mode was not entered.");
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    public static void RegisterBuildScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            throw new InvalidOperationException("Create Preparation before registering it.");
        // Unity 6 can keep the active profile's scene list separate from the project list.
        EditorBuildSettings.globalScenes = AppendScene(EditorBuildSettings.globalScenes);
        EditorBuildSettings.scenes = AppendScene(EditorBuildSettings.scenes);
    }

    private static EditorBuildSettingsScene[] AppendScene(EditorBuildSettingsScene[] original)
    {
        var scenes = original.ToList();
        var existing = scenes.FirstOrDefault(item => item.path == ScenePath);
        if (existing == null) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        else existing.enabled = true;
        return scenes.ToArray();
    }

    private static TMP_Text Text(Transform parent, string name, string key, Vector2 position, Vector2 size, int fontSize)
    {
        TMP_Text text = SlotUI.Label(parent, name, font, position, size, fontSize);
        text.text = Randint.Data.GameData.Text(key);
        bindings.Add(new LocalizedSceneTexts.Binding { target = text, key = key });
        return text;
    }

    private static UnityEngine.UI.Button Button(Transform parent, string name, string key, Vector2 position, Vector2 size)
    {
        var button = SlotUI.Button(parent, name, font, "", position, size);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        text.text = Randint.Data.GameData.Text(key);
        bindings.Add(new LocalizedSceneTexts.Binding { target = text, key = key });
        return button;
    }

    private static void Set(UnityEngine.Object owner, string field, UnityEngine.Object value)
    {
        var data = new SerializedObject(owner);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(SerializedProperty property, UnityEngine.Object[] values)
    {
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
