using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Idempotent in-place migration; preserves serialized gameplay and localization references.</summary>
public static class TerminalThemeInstaller
{
    private static T Ensure<T>(GameObject go) where T : Component => go.TryGetComponent<T>(out var component) ? component : go.AddComponent<T>();
    private static Transform Find(Scene scene, string name) => scene.GetRootGameObjects()
        .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == name);
    [MenuItem("Tools/Randint/UI/Apply Terminal Design System")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        foreach (var scene in Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt))
            if (scene.isDirty) throw new InvalidOperationException("Unsaved scene changes: " + scene.path + ". Save them before migration.");
        CreateTheme(); CreateComponents(); StyleCardPrefabs();
        Scene previous = SceneManager.GetActiveScene();
        foreach (string name in new[] { "Title", "Lobby", "Preparation", "Playing_Scene", "Map_1F", "Shop" }) {
            string path = "Assets/Scenes/" + name + ".unity";
            Scene scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try {
                SceneManager.SetActiveScene(scene);
                StyleExisting(scene);
                var systems = Find(scene, "TerminalSystems");
                if (systems == null) systems = new GameObject("TerminalSystems").transform;
                Ensure<TerminalSettings>(systems.gameObject);
                if (name == "Title") Title(scene);
                if (name == "Lobby") Lobby(scene);
                if (name == "Preparation") Preparation(scene);
                if (name == "Playing_Scene" || name == "Map_1F") {
                    Ensure<TerminalWorld>(systems.gameObject).perspectiveFloor = false;
                    Ensure<TerminalRecordHUD>(systems.gameObject);
                    RemoveGalaxy(scene);
                    if (name == "Map_1F") Holograms(scene);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Failed to save " + path);
            } finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        AssetDatabase.SaveAssets();
        Debug.Log("[TerminalTheme] Six scenes migrated in place; theme and reusable component prefabs installed. No Play Mode was entered.");
    }
    private static void CreateTheme()
    {
        var theme = AssetDatabase.LoadAssetAtPath<UITheme>("Assets/Resources/UITheme.asset");
        if (theme == null) { theme = ScriptableObject.CreateInstance<UITheme>(); AssetDatabase.CreateAsset(theme, "Assets/Resources/UITheme.asset"); }
        theme.titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TMP/Cafe24PROSlimMax SDF.asset");
        var mono = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/UI/Fonts/JetBrainsMono SDF.asset");
        if (mono == null) {
            Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/JetBrainsMono-Regular.ttf");
            if (font == null) throw new InvalidOperationException("JetBrains Mono source missing.");
            mono = TMP_FontAsset.CreateFontAsset(font, 48, 6, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            mono.name = "JetBrainsMono SDF";
            mono.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            AssetDatabase.CreateAsset(mono, "Assets/UI/Fonts/JetBrainsMono SDF.asset");
            foreach (var atlas in mono.atlasTextures) { atlas.name = "JetBrainsMono Atlas"; AssetDatabase.AddObjectToAsset(atlas, mono); }
            mono.material.name = "JetBrainsMono Material"; AssetDatabase.AddObjectToAsset(mono.material, mono);
            mono.TryAddCharacters("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz /:>%._-+[]()", out _);
        }
        if (mono.fallbackFontAssetTable == null) mono.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
        if (theme.titleFont != null && !mono.fallbackFontAssetTable.Contains(theme.titleFont)) mono.fallbackFontAssetTable.Add(theme.titleFont);
        theme.monoFont = mono;
        EditorUtility.SetDirty(mono); EditorUtility.SetDirty(theme); AssetDatabase.SaveAssets();
    }
    private static void CreateComponents()
    {
        System.IO.Directory.CreateDirectory("Assets/UI/Terminal"); AssetDatabase.Refresh();
        var root = new GameObject("ComponentAuthoring", typeof(RectTransform));
        try {
            Save(TerminalUI.Panel("Panel", root.transform, Vector2.zero, new Vector2(360, 220)).gameObject, "Panel");
            foreach (var style in Enum.GetValues(typeof(TerminalButton.Variant)).Cast<TerminalButton.Variant>())
                Save(TerminalUI.Button("Button" + style, root.transform, "접속", Vector2.zero, new Vector2(220, 44), style).gameObject, "Button" + style);
            Save(TerminalUI.Label("Label", root.transform, "기록 열람 단말기", Vector2.zero, new Vector2(320, 36)).gameObject, "Label");
            Save(TerminalUI.Progress("ProgressBar", root.transform, Vector2.zero, new Vector2(320, 8)).gameObject, "ProgressBar");
            var tag = TerminalUI.Tag("Tag", root.transform, "FRONT", Vector2.zero, new Vector2(140, 28)); Save(tag.transform.parent.gameObject, "Tag");
        } finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    private static void Save(GameObject go, string name) { PrefabUtility.SaveAsPrefabAsset(go, "Assets/UI/Terminal/" + name + ".prefab"); UnityEngine.Object.DestroyImmediate(go); }
    private static void StyleCardPrefabs()
    {
        foreach (string path in new[] { "Assets/Prefab/Card.prefab", "Assets/Prefab/Card_Joker.prefab" }) {
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try {
                foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true)) { TerminalUI.Bind(text.gameObject, UITheme.Tone.Main, true); Ensure<TerminalPulse>(text.gameObject); }
                foreach (var sprite in prefab.GetComponentsInChildren<SpriteRenderer>(true)) Ensure<TerminalCardSurface>(sprite.gameObject);
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            } finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
    }
    private static void StyleExisting(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects()) {
            foreach (var graphic in root.GetComponentsInChildren<TerminalGraphic>(true)) { Ensure<CanvasRenderer>(graphic.gameObject); graphic.SetAllDirty(); }
            foreach (var camera in root.GetComponentsInChildren<Camera>(true)) { camera.backgroundColor = UITheme.BgBase; camera.clearFlags = CameraClearFlags.SolidColor; }
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) {
                bool mono = text.name.Contains("Num") || text.name.Contains("Stat") || text.name.Contains("Info-") || text.name == "AccessTag";
                TerminalUI.Bind(text.gameObject, UITheme.Tone.Main, mono);
                if (mono || scene.name == "Playing_Scene" || scene.name == "Map_1F") Ensure<TerminalPulse>(text.gameObject);
            }
            foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.Image>(true)) {
                if (image.TryGetComponent<UIThemeBinding>(out var existingTheme)) { existingTheme.Apply(); continue; }
                bool artwork = image.sprite != null && image.sprite.texture != null && image.sprite.texture.width > 64 && image.sprite.texture.height > 64;
                var tone = artwork ? UITheme.Tone.Texture : image.GetComponent<UnityEngine.UI.Button>() != null ? UITheme.Tone.Clear :
                    image.name.Contains("Background") || image.name == "Desktop" ? UITheme.Tone.Base : UITheme.Tone.Panel;
                TerminalUI.Bind(image.gameObject, tone);
            }
            foreach (var outline in root.GetComponentsInChildren<UnityEngine.UI.Outline>(true)) {
                outline.effectColor = UITheme.Line; outline.effectDistance = new Vector2(UITheme.Border, -UITheme.Border);
            }
            foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>(true)) {
                string path = AssetDatabase.GetAssetPath(sprite.sprite);
                if (path.Contains("DefaultAssets/Textures") || path.EndsWith("/111.png")) {
                    var binding = Ensure<TerminalSpriteBinding>(sprite.gameObject);
                    binding.tone = sprite.name == "Ground" ? UITheme.Tone.BattleFloor : sprite.name == "UnderGround" ? UITheme.Tone.BattleUnderfloor :
                        sprite.name.Contains("Button") ? UITheme.Tone.Hover : UITheme.Tone.Panel;
                    if (sprite.name == "OperUI_info") binding.opacity = UITheme.InfoPanelOpacity;
                    binding.Apply();
                }
            }
            foreach (var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true)) {
                var binding = button.GetComponent<UIThemeBinding>(); if (binding != null) UnityEngine.Object.DestroyImmediate(binding);
                Ensure<TerminalButton>(button.gameObject);
            }
            foreach (var localizer in root.GetComponentsInChildren<LocalizedSceneTexts>(true)) {
                var serialized = new SerializedObject(localizer); var bindings = serialized.FindProperty("bindings");
                for (int i = 0; i < bindings.arraySize; i++) {
                    var entry = bindings.GetArrayElementAtIndex(i); string key = entry.FindPropertyRelative("key").stringValue;
                    var text = entry.FindPropertyRelative("target").objectReferenceValue as TMP_Text;
                    if (text == null) continue;
                    text.text = Randint.Data.GameData.Text(key);
                    if (key.StartsWith("ui.map.integrity") || key.StartsWith("ui.map.consistency")) text.enabled = false;
                    if ((scene.name == "Playing_Scene" || scene.name == "Map_1F") && key == "ui.common.unavailable") text.enabled = false;
                }
            }
        }
    }
    private static void Title(Scene scene)
    {
        var root = Find(scene, "Title")?.parent; if (root == null) return;
        if (root.Find("TerminalGrid") == null) {
            var grid = TerminalGraphic.Create("TerminalGrid", root, TerminalGraphic.Shape.Grid, Vector2.zero, Vector2.zero);
            TerminalUI.Stretch(grid.rectTransform); grid.transform.SetSiblingIndex(1);
            var scan = TerminalGraphic.Create("TerminalScan", root, TerminalGraphic.Shape.Scanline, Vector2.zero, Vector2.zero);
            TerminalUI.Stretch(scan.rectTransform); scan.transform.SetSiblingIndex(2);
        }
        var access = Find(scene, "AccessTag").GetComponent<TMP_Text>(); TerminalUI.Bind(access.gameObject, UITheme.Tone.Accent, true); access.characterSpacing = 4;
        Ensure<TerminalTitleEffect>(Find(scene, "Title").gameObject);
        var context = Find(scene, "Context").GetComponent<TMP_Text>();
        TerminalUI.Bind(context.gameObject, UITheme.Tone.Dim, true);
        SlotUI.Place(context.rectTransform, new Vector2(0, -47), new Vector2(700, 32)); context.fontSize = 18;
        if (root.Find("BootProgress") == null) TerminalUI.Progress("BootProgress", root, new Vector2(0, -77), new Vector2(700, 7));
        if (root.Find("BootLog") == null) {
            var log = TerminalUI.Label("BootLog", root, "", new Vector2(0, -136), new Vector2(700, 90), 17, UITheme.Tone.Dim, true);
            log.alignment = TextAlignmentOptions.TopLeft;
        }
        Find(scene, "EnterLobbyButton").GetComponent<UnityEngine.UI.Button>().interactable = false;
    }
    private static void Lobby(Scene scene)
    {
        var window = Find(scene, "MainWindow"); if (window == null) return;
        foreach (var titlebar in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Where(t => t.name == "TitleBar")) {
            var rect = (RectTransform)titlebar; var frame = (RectTransform)titlebar.parent;
            rect.sizeDelta = new Vector2(frame.sizeDelta.x - 2, UITheme.TitleBarHeight);
            rect.anchoredPosition = new Vector2(0, frame.sizeDelta.y / 2 - UITheme.TitleBarHeight / 2 - 1);
            TerminalUI.Bind(titlebar.gameObject, UITheme.Tone.Hover);
            var title = titlebar.Find("WindowTitle")?.GetComponent<TMP_Text>();
            if (title != null) { title.fontSize = 17; title.rectTransform.sizeDelta = new Vector2(frame.sizeDelta.x - 100, 26); title.rectTransform.anchoredPosition = new Vector2(-30, 0); TerminalUI.Bind(title.gameObject, UITheme.Tone.Accent); }
            if (titlebar.Find("WindowControls") == null) {
                var controls = TerminalUI.Label("WindowControls", titlebar, "[_] [×]", new Vector2(frame.sizeDelta.x / 2 - 58, 0), new Vector2(100, 26), 17, UITheme.Tone.Dim, true);
                controls.raycastTarget = false;
                var line = TerminalUI.Panel("AccentEdge", titlebar, new Vector2(0, -13), new Vector2(frame.sizeDelta.x - 2, 1), UITheme.Tone.Accent);
            }
        }
        Transform row = window.Find("DesktopCards");
        if (row == null) row = TerminalUI.Rect("DesktopCards", window, new Vector2(0, -6), new Vector2(780, 255));
        var layout = Ensure<UnityEngine.UI.HorizontalLayoutGroup>(row.gameObject);
        layout.spacing = UITheme.Gap; layout.childControlWidth = true; layout.childControlHeight = true;
        layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
        string[] cards = { "SettingsButton", "ContinueButton", "NewGameButton" };
        TerminalGraphic.Shape[] icons = { TerminalGraphic.Shape.Gear, TerminalGraphic.Shape.Folder, TerminalGraphic.Shape.Launch };
        for (int i = 0; i < cards.Length; i++) {
            var tile = Find(scene, cards[i]); if (tile == null) continue;
            tile.SetParent(row, false);
            var element = Ensure<UnityEngine.UI.LayoutElement>(tile.gameObject); element.minWidth = 0; element.preferredWidth = 240; element.flexibleWidth = 1;
            Transform content = tile.Find("Content");
            if (content == null) {
                content = TerminalUI.Rect("Content", tile, Vector2.zero, new Vector2(240, 255));
                var old = tile.Cast<Transform>().Where(t => t != content).ToArray(); foreach (var child in old) child.SetParent(content, false);
            }
            var symbol = content.Find("Symbol"); if (symbol != null) symbol.gameObject.SetActive(false);
            var cardFace = Ensure<UnityEngine.UI.Image>(content.gameObject); cardFace.raycastTarget = false; cardFace.color = UITheme.Clear;
            var rootOutline = tile.GetComponent<UnityEngine.UI.Outline>(); if (rootOutline != null) rootOutline.enabled = false;
            Ensure<UnityEngine.UI.Outline>(content.gameObject).effectDistance = new Vector2(UITheme.Border, -UITheme.Border);
            tile.GetComponent<UnityEngine.UI.Button>().targetGraphic = cardFace;
            if (i == 0 && content.Find("Description") != null) content.Find("Description").gameObject.SetActive(false);
            if (content.Find("LineIcon") == null) TerminalGraphic.Create("LineIcon", content, icons[i], new Vector2(0, 55), new Vector2(70, 70));
            var label = content.Find("Label")?.GetComponent<TMP_Text>(); if (label != null) label.fontSize = 24;
            Ensure<TerminalButton>(tile.gameObject).liftOnHover = true;
        }
        var taskbar = Find(scene, "Taskbar");
        if (taskbar != null) {
            var rect = (RectTransform)taskbar; rect.anchorMin = Vector2.zero; rect.anchorMax = new Vector2(1, 0); rect.pivot = new Vector2(.5f, 0);
            rect.sizeDelta = new Vector2(0, 48); rect.anchoredPosition = Vector2.zero;
            if (taskbar.Find("ConnectionDot") == null) TerminalGraphic.Create("ConnectionDot", taskbar, TerminalGraphic.Shape.Dot, new Vector2(260, 0), new Vector2(7, 7));
        }
    }
    private static void Preparation(Scene scene)
    {
        var controller = Find(scene, "PreparationController"); if (controller != null) Ensure<TerminalPreparation>(controller.gameObject);
        string[] slots = { "FrontOperator", "MiddleOperator", "BackOperator" };
        foreach (string name in slots) {
            var slot = Find(scene, name); if (slot == null) continue;
            TerminalUI.Bind(slot.gameObject, UITheme.Tone.Panel);
            var position = slot.Find("Position");
            if (position != null) { TerminalUI.Bind(position.gameObject, UITheme.Tone.Accent, true); position.GetComponent<TMP_Text>().fontSize = 18; }
            if (slot.Find("PositionTagBackdrop") == null) {
                var backdrop = TerminalUI.Panel("PositionTagBackdrop", slot, new Vector2(0, 148), new Vector2(140, 28), UITheme.Tone.Hover);
                backdrop.transform.SetSiblingIndex(0);
            }
            var operatorName = slot.Find("OperatorName")?.GetComponent<TMP_Text>();
            if (operatorName != null) { operatorName.rectTransform.anchoredPosition = new Vector2(0, -61); operatorName.rectTransform.sizeDelta = new Vector2(312, 30); operatorName.fontSize = 22; }
            var portrait = slot.Find("Portrait"); if (portrait != null) { ((RectTransform)portrait).anchoredPosition = new Vector2(0, 54); ((RectTransform)portrait).sizeDelta = new Vector2(265, 180); }
            var change = slot.Find("ChangeOperator"); if (change != null) { ((RectTransform)change).anchoredPosition = new Vector2(0, -154); ((RectTransform)change).sizeDelta = new Vector2(130, 34); change.GetComponentInChildren<TMP_Text>().fontSize = 20; }
        }
        var start = Find(scene, "StartRun"); if (start != null) Ensure<TerminalButton>(start.gameObject).variant = TerminalButton.Variant.Start;
        var lossTitle = Find(scene, "DataLossHeading"); if (lossTitle != null) ((RectTransform)lossTitle).anchoredPosition = new Vector2(-370, -226);
        var stage = Find(scene, "DataLossLevel");
        if (stage != null) { SlotUI.Place((RectTransform)stage, new Vector2(250, -236), new Vector2(490, 42)); TerminalUI.Bind(stage.gameObject, UITheme.Tone.Dim, true); stage.GetComponent<TMP_Text>().fontSize = 20; }
    }
    private static void RemoveGalaxy(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects()) {
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true)) {
                string name = (renderer.name + " " + renderer.sprite?.name + " " + AssetDatabase.GetAssetPath(renderer.sprite)).ToLowerInvariant();
                if (name.Contains("milky") || name.Contains("galaxy") || name.Contains("backdrop")) renderer.enabled = false;
            }
            foreach (var image in root.GetComponentsInChildren<UnityEngine.UI.RawImage>(true)) {
                string path = AssetDatabase.GetAssetPath(image.texture).ToLowerInvariant();
                if (image.name == "MapBackground" || path.Contains("milky") || path.Contains("galaxy")) image.enabled = false;
                else image.color = UITheme.TextureTint;
            }
        }
    }
    private static void Holograms(Scene scene)
    {
        var systems = Find(scene, "TerminalSystems"); if (systems != null) Ensure<TerminalMapCursor>(systems.gameObject);
        foreach (var root in scene.GetRootGameObjects()) {
            foreach (var mesh in root.GetComponentsInChildren<MeshRenderer>(true)) {
                string name = mesh.name.ToLowerInvariant();
                var holo = Ensure<TerminalHologram>(mesh.gameObject);
                holo.tone = name.Contains("pawn") ? UITheme.Tone.Dim : name.Contains("queen") ? UITheme.Tone.Danger : name.Contains("king") ? UITheme.Tone.Warn : UITheme.Tone.Accent;
                if (name.Contains("cube") || name.Contains("player")) { holo.tone = UITheme.Tone.Ok; holo.trail = true; }
            }
        }
    }
}
