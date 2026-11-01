using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class CardPrefabRendering
{
    public static void Apply()
    {
        const string materialPath = "Assets/UI/Terminal/CardNumbersOverlay.mat";
        var numbers = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (numbers == null) {
            numbers = new Material(UITheme.Mono.material) { name = "CardNumbersOverlay" };
            numbers.shader = Shader.Find("TextMeshPro/Mobile/Distance Field Overlay");
            if (numbers.shader == null) throw new System.InvalidOperationException("TMP overlay shader is missing.");
            numbers.renderQueue = 3000;
            AssetDatabase.CreateAsset(numbers, materialPath);
        }
        foreach (string path in new[] { "Assets/Prefab/Card.prefab", "Assets/Prefab/Card_Joker.prefab" }) {
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try {
                if (!prefab.TryGetComponent<CardPresentation>(out _)) prefab.AddComponent<CardPresentation>();
                if (prefab.TryGetComponent<SortingGroup>(out var oldGroup)) Object.DestroyImmediate(oldGroup);
                foreach (var sprite in prefab.GetComponentsInChildren<SpriteRenderer>(true)) {
                    bool body = sprite.gameObject == prefab;
                    bool logo = AssetDatabase.GetAssetPath(sprite.sprite).EndsWith("/Rodoes_Island.png");
                    bool photo = !body && !logo;
                    if (!sprite.TryGetComponent<TerminalCardSurface>(out var surface)) surface = sprite.gameObject.AddComponent<TerminalCardSurface>();
                    surface.preserveTextureColor = photo;
                    sprite.color = UITheme.TextureTint; sprite.sortingOrder = UITheme.CardSortBase + (body ? 0 : 10);
                    if (!body) { var position = sprite.transform.localPosition; position.z = -.01f; sprite.transform.localPosition = position; }
                    if (path.Contains("Joker") && logo) sprite.gameObject.SetActive(false);
                }
                foreach (var canvas in prefab.GetComponentsInChildren<Canvas>(true)) {
                    canvas.overrideSorting = true; canvas.sortingOrder = UITheme.CardSortBase + 20;
                    var position = canvas.transform.localPosition; position.z = -.02f; canvas.transform.localPosition = position;
                }
                foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true)) {
                    TerminalUI.Bind(text.gameObject, UITheme.Tone.Main, true); text.fontSharedMaterial = numbers;
                }
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            } finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        AssetDatabase.SaveAssets();
    }
}
