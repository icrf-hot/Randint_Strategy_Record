using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

/// <summary>Repairs the existing card/stat panel in place, preserving all selection references.</summary>
public static class BattleSelectionPresentationInstaller
{
    [MenuItem("Tools/Randint/Repair Battle Selection Presentation")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty)
            throw new System.InvalidOperationException("Stop Play Mode and save scene changes first.");
        if (scene.path != "Assets/Scenes/Playing_Scene.unity") scene = EditorSceneManager.OpenScene("Assets/Scenes/Playing_Scene.unity");
        Apply(scene); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
    }
    public static void Apply(Scene scene)
    {
        if (scene.name != "Playing_Scene") return;
        Transform root = null;
        foreach (var go in scene.GetRootGameObjects()) if (go.name == "Canvas") root = go.transform.Find("Base1");
        if (root == null) throw new System.InvalidOperationException("Battle selection panel Canvas/Base1 is missing.");
        Style(root, UITheme.Tone.Hover, 2);
        foreach (string sectionName in new[] { "Front_Panel_unit", "Fight_Panel_unit", "Medic_Panel_unit" }) {
            var section = root.Find(sectionName); if (section == null) continue;
            foreach (var sprite in section.GetComponentsInChildren<SpriteRenderer>(true)) {
                bool art = sprite.sprite != null && !AssetDatabase.GetAssetPath(sprite.sprite).Contains("DefaultAssets/Textures");
                if (art) { sprite.sortingOrder = 5; continue; }
                bool row = sprite.transform.parent == section && (sprite.name == "Base1" || sprite.name == "Base2" || sprite.name == "Square");
                bool header = sprite.transform.parent == section && sprite.name.StartsWith("Button");
                Style(sprite.transform, row ? UITheme.Tone.Panel : header ? UITheme.Tone.Hover : UITheme.Tone.Base, row || header ? 3 : 4);
            }
            foreach (var text in section.GetComponentsInChildren<TMP_Text>(true)) {
                var parentSprite = text.transform.parent.GetComponent<SpriteRenderer>(); if (parentSprite == null) continue;
                var rect = text.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                // Sprite panels are stretched independently on X/Y. Cancel that inherited stretch
                // so glyphs retain their original proportions while the available text box stays wide.
                float axisX = text.transform.parent.TransformVector(Vector3.right).magnitude;
                float axisY = text.transform.parent.TransformVector(Vector3.up).magnitude;
                Vector2 spriteSize = parentSprite.sprite != null ? (Vector2)parentSprite.sprite.bounds.size : Vector2.one;
                Vector2 worldSize = new Vector2(spriteSize.x * axisX, spriteSize.y * axisY);
                float fontWorldScale = Mathf.Max(.0001f, Mathf.Min(worldSize.x, worldSize.y) / 100);
                rect.sizeDelta = new Vector2(worldSize.x * .88f, worldSize.y * .84f) / fontWorldScale;
                rect.localScale = new Vector3(fontWorldScale / Mathf.Max(.0001f, axisX), fontWorldScale / Mathf.Max(.0001f, axisY), 1);
                rect.localPosition = new Vector3(0, 0, -.01f);
                text.alignment = TextAlignmentOptions.Center;
                text.margin = Vector4.zero; text.textWrappingMode = TextWrappingModes.NoWrap;
                text.enableAutoSizing = true; text.fontSizeMin = 8;
                text.fontSizeMax = text.text == "+" ? 64 : 48;
                text.fontSize = text.fontSizeMax;
                text.enabled = true; // Empty slots remain blank through their existing selection manager.
                var binding = text.GetComponent<UIThemeBinding>() ?? text.gameObject.AddComponent<UIThemeBinding>();
                binding.tone = UITheme.Tone.Main;
                binding.monospace = parentSprite.name.Contains("Num") || text.text == "+";
                binding.Apply();
            }
        }
        var canvas = root.GetComponentInParent<Canvas>();
        Transform position = canvas.transform.Find("Position");
        if (position == null) throw new System.InvalidOperationException("Battle position selector is missing.");
        Material panelMaterial = GetPanelMaterial();
        // 2D Light의 대상 레이어와 무관하게 UI 팔레트를 유지합니다.
        foreach (Transform panel in new[] { root, position })
        {
            foreach (var sprite in panel.GetComponentsInChildren<SpriteRenderer>(true)) sprite.sharedMaterial = panelMaterial;
            var sorting = panel.GetComponent<SortingGroup>();
            if (sorting == null) sorting = panel.gameObject.AddComponent<SortingGroup>();
            sorting.sortingOrder = UITheme.BattlePanelSortBase;
        }
        var presentation = canvas.GetComponent<BattleSelectionPresentation>();
        if (presentation == null) presentation = canvas.gameObject.AddComponent<BattleSelectionPresentation>();
        var data = new SerializedObject(presentation);
        data.FindProperty("textCanvas").objectReferenceValue = canvas;
        var panels = data.FindProperty("panels"); panels.arraySize = 2;
        panels.GetArrayElementAtIndex(0).objectReferenceValue = root;
        panels.GetArrayElementAtIndex(1).objectReferenceValue = position;
        var surfaces = data.FindProperty("cardBlockingSurfaces"); surfaces.arraySize = 2;
        surfaces.GetArrayElementAtIndex(0).objectReferenceValue = root.GetComponent<SpriteRenderer>();
        surfaces.GetArrayElementAtIndex(1).objectReferenceValue = position.Find("Square").GetComponent<SpriteRenderer>();
        data.ApplyModifiedPropertiesWithoutUndo();
        presentation.ApplyOrdering();
        foreach (var toggle in root.GetComponentsInChildren<SpriteToggleButton>(true)) toggle.Deselect();
    }
    private static Material GetPanelMaterial()
    {
        const string path = "Assets/UI/Terminal/BattleSelectionUnlit.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) throw new System.InvalidOperationException("Battle selection UI unlit shader is missing.");
        material = new Material(shader) { name = "BattleSelectionUnlit" };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
    private static void Style(Transform target, UITheme.Tone tone, int order)
    {
        if (!target.TryGetComponent<SpriteRenderer>(out var sprite)) return;
        var binding = target.GetComponent<TerminalSpriteBinding>() ?? target.gameObject.AddComponent<TerminalSpriteBinding>();
        binding.tone = tone; binding.opacity = 1; binding.Apply(); sprite.sortingOrder = order;
    }
}
