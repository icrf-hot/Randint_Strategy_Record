using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunProgressionHUD : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    private GameObject canvasObject;
    private TMP_Text statusText;
    private BattleSceneFadeIn battleEntry;
    private MapSceneFadeIn mapEntry;
    private BattleResultPresenter result;
    private MapBattleSceneLoader battleLoader;
    private BattleToMapSceneLoader mapLoader;
    private int lastPackets = -1;
    private int lastLayer = -1;
    private int lastExperience = -1;
    private int lastRequired = -1;

    public TMP_Text StatusText => statusText;

    private void Start()
    {
        battleEntry = FindFirstObjectByType<BattleSceneFadeIn>();
        mapEntry = FindFirstObjectByType<MapSceneFadeIn>();
        result = FindFirstObjectByType<BattleResultPresenter>();
        battleLoader = FindFirstObjectByType<MapBattleSceneLoader>();
        mapLoader = FindFirstObjectByType<BattleToMapSceneLoader>();

        canvasObject = new GameObject("RunStatusCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject textObject = new GameObject("RunStatus", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvasObject.transform, false);
        statusText = textObject.GetComponent<TextMeshProUGUI>();
        statusText.font = font != null ? font : TMP_Settings.defaultFontAsset;
        statusText.raycastTarget = false;
        statusText.alignment = TextAlignmentOptions.TopRight;
        statusText.fontSize = 20f;
        statusText.enableAutoSizing = true;
        statusText.fontSizeMin = 14f;
        statusText.fontSizeMax = 20f;
        statusText.color = Color.white;
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-20f, -20f);
        rect.sizeDelta = new Vector2(280f, 90f);
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (canvasObject == null || statusText == null)
            return;

        bool visible = MapRunState.Progression.IsInitialized &&
                       (battleEntry == null || battleEntry.IsFadeComplete) &&
                       (mapEntry == null || mapEntry.IsFadeComplete) &&
                       (result == null || !result.IsShowing) &&
                       (battleLoader == null || !battleLoader.IsLoading) &&
                       (mapLoader == null || !mapLoader.IsLoading);
        canvasObject.SetActive(visible);
        RunProgression run = MapRunState.Progression;
        if (lastPackets == run.Packets.Balance && lastLayer == run.Layer &&
            lastExperience == run.Experience && lastRequired == run.RequiredExperience)
            return;

        lastPackets = run.Packets.Balance;
        lastLayer = run.Layer;
        lastExperience = run.Experience;
        lastRequired = run.RequiredExperience;
        statusText.text = $"Packet {lastPackets}\nLayer {lastLayer}\nEXP {lastExperience} / {lastRequired}";
    }

    private void OnDisable()
    {
        if (canvasObject != null)
            canvasObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (canvasObject != null)
            Destroy(canvasObject);
    }
}
