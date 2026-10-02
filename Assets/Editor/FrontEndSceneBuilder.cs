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
using UnityEngine.UI;

/// <summary>이미지 없는 임시 UI를 편집 가능한 Scene으로 저장합니다. 런타임에는 생성하지 않습니다.</summary>
public static class FrontEndSceneBuilder
{
    private const string TitlePath = "Assets/Scenes/Title.unity";
    private const string LobbyPath = "Assets/Scenes/Lobby.unity";
    private static readonly Color Blue = new Color32(22, 72, 177, 255);
    private static readonly Color Paper = new Color32(236, 233, 216, 255);
    private static readonly Color Ink = new Color32(29, 36, 52, 255);
    private static readonly List<LocalizedSceneTexts.Binding> Bindings = new();
    private static TMP_FontAsset font;

    [MenuItem("Tools/Randint/Front End/Rebuild Title and Lobby Scenes")]
    public static void RebuildWithConfirmation()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        // 재생성은 수동 UI 조정을 덮어쓰므로 명시적으로 확인합니다.
        if (!EditorUtility.DisplayDialog("Title / Lobby", "Title・Lobby 씬을 재생성하면 해당 씬의 수동 UI 수정이 덮어써집니다. 계속하시겠습니까?", "재생성", "취소")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Generate();
    }

    public static void GenerateBatch()
    {
        try
        {
            // 배치 에디터의 초기 Untitled Scene은 Additive 생성이 허용되지 않습니다.
            EditorSceneManager.OpenScene("Assets/Scenes/Map_1F.unity", OpenSceneMode.Single);
            Generate();
            GameDataValidationMenu.ValidateAll();
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    private static void Generate()
    {
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TMP/Cafe24PROSlimMax SDF.asset");
        if (font == null) throw new InvalidOperationException("메뉴용 한국어 TMP Font Asset이 없습니다.");
        Scene previous = SceneManager.GetActiveScene();
        BuildScene(TitlePath, true);
        BuildScene(LobbyPath, false);
        if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        // 최초 실행은 Title입니다. 기존 Map/Battle 및 추가된 다른 씬의 순서/활성 상태는 보존합니다.
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene(TitlePath, true), new EditorBuildSettingsScene(LobbyPath, true)
        }.Concat(EditorBuildSettings.scenes.Where(s => s.path != TitlePath && s.path != LobbyPath)).ToArray();
        AssetDatabase.Refresh();
        Debug.Log("[FrontEnd] Title/Lobby 씬 생성 및 Build Settings 등록 완료");
    }

    private static void BuildScene(string path, bool isTitle)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            Bindings.Clear();
            GameObject camera = new("Main Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor = Blue;
            GameObject events = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            GameObject canvasObject = new("MenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasObject.transform;
            GameObject controller = new(isTitle ? "TitleController" : "LobbyController");
            MenuSceneLoader loader = controller.AddComponent<MenuSceneLoader>();
            if (isTitle) BuildTitle(root, controller, loader);
            else BuildLobby(root, controller, loader);
            GameObject transition = Panel("TransitionCanvas", root, Vector2.zero, Vector2.zero, Color.black, true);
            CanvasGroup group = transition.AddComponent<CanvasGroup>();
            group.alpha = 0;
            group.blocksRaycasts = false;
            TMP_Text loading = Text("LoadingText", transition.transform, "menu.loading", Vector2.zero, new Vector2(950, 100), 30, Color.white);
            SetReferences(loader, ("transitionCanvas", group), ("transitionText", loading));
            transition.SetActive(false);
            LocalizedSceneTexts localizer = controller.AddComponent<LocalizedSceneTexts>();
            SerializedObject serialized = new(localizer);
            SerializedProperty bindings = serialized.FindProperty("bindings");
            bindings.arraySize = Bindings.Count;
            for (int i = 0; i < Bindings.Count; i++)
            {
                bindings.GetArrayElementAtIndex(i).FindPropertyRelative("target").objectReferenceValue = Bindings[i].target;
                bindings.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue = Bindings[i].key;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("Scene 저장 실패: " + path);
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
    }

    private static void BuildTitle(Transform root, GameObject owner, MenuSceneLoader loader)
    {
        Panel("Background", root, Vector2.zero, Vector2.zero, new Color32(10, 19, 33, 255), true);
        Panel("AccentLine", root, new Vector2(0, 212), new Vector2(920, 3), new Color32(72, 204, 207, 255));
        Text("AccessTag", root, "menu.title.tag", new Vector2(0, 250), new Vector2(1050, 45), 20, new Color32(72, 204, 207, 255));
        Text("Title", root, "menu.title.name", new Vector2(0, 130), new Vector2(1120, 95), 60, Color.white);
        Text("Subtitle", root, "menu.title.subtitle", new Vector2(0, 55), new Vector2(950, 55), 32, Color.white);
        Text("Context", root, "menu.title.context", new Vector2(0, -70), new Vector2(1020, 120), 24, new Color32(184, 199, 216, 255));
        Button enter = Button("EnterLobbyButton", root, "menu.title.enter", new Vector2(0, -225), new Vector2(350, 65));
        TitleSceneController controller = owner.AddComponent<TitleSceneController>();
        SetReferences(controller, ("sceneLoader", loader), ("enterButton", enter));
    }

    private static void BuildLobby(Transform root, GameObject owner, MenuSceneLoader loader)
    {
        Panel("Desktop", root, Vector2.zero, Vector2.zero, new Color32(57, 116, 181, 255), true);
        GameObject taskbar = Panel("Taskbar", root, new Vector2(0, -336), new Vector2(1280, 48), Blue);
        Text("TaskbarLabel", taskbar.transform, "menu.lobby.taskbar", new Vector2(-430, 0), new Vector2(350, 40), 20, Color.white);
        Text("Status", taskbar.transform, "menu.lobby.status", new Vector2(440, 0), new Vector2(320, 40), 18, Color.white);
        GameObject window = Window("MainWindow", root, "menu.lobby.window", new Vector2(860, 480));
        CanvasGroup mainGroup = window.AddComponent<CanvasGroup>();
        Text("Path", window.transform, "menu.lobby.path", new Vector2(0, 163), new Vector2(780, 40), 22, Ink);
        Text("Hint", window.transform, "menu.lobby.hint", new Vector2(0, -193), new Vector2(780, 40), 19, Ink);
        Button settings = Tile(window.transform, "SettingsButton", "menu.settings", -255);
        Button resume = Tile(window.transform, "ContinueButton", "menu.continue", 0);
        Button newGame = Tile(window.transform, "NewGameButton", "menu.new_game", 255);
        GameObject settingsModal = Modal(root, "SettingsWindow", "menu.settings.label", "menu.settings.pending", out Button settingsClose);
        GameObject continueModal = Modal(root, "ContinueWindow", "menu.continue.label", "menu.continue.empty", out Button continueClose);
        LobbySceneController controller = owner.AddComponent<LobbySceneController>();
        SetReferences(controller, ("sceneLoader", loader), ("mainWindow", mainGroup),
            ("settingsWindow", settingsModal), ("continueWindow", continueModal),
            ("settingsButton", settings), ("continueButton", resume), ("newGameButton", newGame),
            ("settingsCloseButton", settingsClose), ("continueCloseButton", continueClose));
    }

    private static Button Tile(Transform parent, string name, string prefix, float x)
    {
        Button button = Button(name, parent, prefix + ".label", new Vector2(x, -6), new Vector2(225, 255));
        button.GetComponentInChildren<TMP_Text>().rectTransform.anchoredPosition = new Vector2(0, -37);
        Text("Symbol", button.transform, prefix + ".symbol", new Vector2(0, 55), new Vector2(205, 75), 36, Blue);
        Text("Description", button.transform, prefix + ".description", new Vector2(0, -91), new Vector2(205, 40), 18, Ink);
        return button;
    }

    private static GameObject Modal(Transform root, string name, string titleKey, string bodyKey, out Button close)
    {
        GameObject backdrop = Panel(name, root, Vector2.zero, Vector2.zero, new Color(0, 0, 0, 0.35f), true);
        GameObject window = Window("Dialog", backdrop.transform, titleKey, new Vector2(650, 330));
        Text("Message", window.transform, bodyKey, new Vector2(0, 0), new Vector2(590, 190), 23, Ink);
        close = Button("CloseButton", window.transform, "menu.window.close", new Vector2(0, -116), new Vector2(145, 44));
        backdrop.SetActive(false);
        return backdrop;
    }

    private static GameObject Window(string name, Transform parent, string titleKey, Vector2 size)
    {
        GameObject frame = Panel(name, parent, new Vector2(0, 20), size, Paper);
        Outline outline = frame.AddComponent<Outline>();
        outline.effectColor = Blue;
        outline.effectDistance = new Vector2(3, -3);
        GameObject bar = Panel("TitleBar", frame.transform, new Vector2(0, size.y / 2 - 23), new Vector2(size.x - 6, 42), Blue);
        TMP_Text title = Text("WindowTitle", bar.transform, titleKey, Vector2.zero, new Vector2(size.x - 34, 40), 24, Color.white);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        return frame;
    }

    private static Button Button(string name, Transform parent, string key, Vector2 position, Vector2 size)
    {
        GameObject go = Panel(name, parent, position, size, new Color32(246, 244, 235, 255));
        Outline bevel = go.AddComponent<Outline>();
        bevel.effectColor = new Color32(130, 132, 126, 255);
        bevel.effectDistance = new Vector2(1, -2);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color32(206, 226, 255, 255);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color32(154, 188, 231, 255);
        button.colors = colors;
        Text("Label", go.transform, key, Vector2.zero, new Vector2(size.x - 14, 50), 25, Ink);
        return button;
    }

    private static GameObject Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool stretch = false)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        if (stretch)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }
        else
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
        // Sprite 없는 Image는 단색 사각형입니다. 아이콘/배경 이미지 자산은 사용하지 않습니다.
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static TMP_Text Text(string name, Transform parent, string key, Vector2 position, Vector2 size, float fontSize, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.font = font; text.fontSize = fontSize; text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        // Sceneにはキーを保存し、表示値はPlay開始時にJSONから設定します。
        text.text = string.Empty;
        Bindings.Add(new LocalizedSceneTexts.Binding { target = text, key = key });
        return text;
    }

    private static void SetReferences(UnityEngine.Object target, params (string field, UnityEngine.Object value)[] values)
    {
        SerializedObject serialized = new(target);
        foreach (var entry in values) serialized.FindProperty(entry.field).objectReferenceValue = entry.value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
