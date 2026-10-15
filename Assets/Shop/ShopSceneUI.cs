using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class ShopSceneUI : MonoBehaviour
{
    [SerializeField] private ShopCatalogData catalog;
    [SerializeField] private RunProgressionSettings progressionSettings;
    [SerializeField] private TMP_FontAsset font;
    private readonly Dictionary<string, Button> buttons = new Dictionary<string, Button>();
    private readonly Dictionary<string, TMP_Text> buttonTexts = new Dictionary<string, TMP_Text>();
    private TMP_Text balanceText;
    private TMP_Text messageText;
    private int lastBalance = -1;
    public int ItemCount => buttons.Count;
    public Button ReturnButton { get; private set; }
    public TMP_Text BalanceText => balanceText;
    public Button GetBuyButton(string id) => buttons.TryGetValue(id, out Button button) ? button : null;

    private void Start()
    {
        MapRunState.EnsureStarted(progressionSettings);
        if (EventSystem.current == null)
        {
            GameObject events = new GameObject("ShopEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
        }
        RectTransform root = Rect("ShopCanvas", transform);
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 900);
        scaler.matchWidthOrHeight = 0.5f;
        root.gameObject.AddComponent<GraphicRaycaster>();
        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.075f, 0.08f, 0.09f);
        Text(root, "ShopTitle", "CACHE 상점", new Vector2(0, 1), Vector2.one,
            new Vector2(28, -72), new Vector2(-28, -20), 28);
        balanceText = Text(root, "Balance", "", new Vector2(0, 1), Vector2.one,
            new Vector2(28, -110), new Vector2(-28, -76), 20);
        RectTransform viewport = Rect("Viewport", root);
        Layout(viewport, Vector2.zero, Vector2.one, new Vector2(28, 100), new Vector2(-28, -124));
        viewport.gameObject.AddComponent<Image>().color = new Color(0.11f, 0.12f, 0.13f);
        viewport.gameObject.AddComponent<RectMask2D>();
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35;
        RectTransform content = Rect("Items", viewport);
        Layout(content, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero);
        content.pivot = new Vector2(0.5f, 1);
        VerticalLayoutGroup list = content.gameObject.AddComponent<VerticalLayoutGroup>();
        list.childControlHeight = true;
        list.childForceExpandHeight = false;
        list.childControlWidth = true;
        list.childForceExpandWidth = true;
        list.spacing = 2;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;
        if (catalog != null)
            foreach (CacheData item in catalog.Items) AddItem(content, item);
        messageText = Text(root, "Message", "", Vector2.zero, new Vector2(0.72f, 0),
            new Vector2(28, 24), new Vector2(-12, 80), 18);
        ReturnButton = MakeButton(root, "ReturnToMap", "맵으로", new Vector2(0.75f, 0), new Vector2(1, 0),
            new Vector2(0, 24), new Vector2(-28, 80));
        ReturnButton.onClick.AddListener(() =>
        {
            if (ShopSceneRouter.ReturnToMap()) ReturnButton.interactable = false;
        });
        Refresh();
    }

    private void Update()
    {
        if (balanceText != null && lastBalance != MapRunState.Packets.Balance) Refresh();
    }

    private void AddItem(RectTransform content, CacheData item)
    {
        if (item == null || buttons.ContainsKey(item.CacheId)) return;
        RectTransform row = Rect(item.CacheId, content);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 108;
        Text(row, "Description", item.DisplayName + "\n" + Describe(item), Vector2.zero, new Vector2(0.72f, 1),
            new Vector2(16, 8), new Vector2(-12, -8), 22);
        Button buy = MakeButton(row, "Buy", "", new Vector2(0.74f, 0), Vector2.one,
            new Vector2(0, 16), new Vector2(-16, -16));
        buttons.Add(item.CacheId, buy);
        buttonTexts.Add(item.CacheId, buy.GetComponentInChildren<TMP_Text>());
        string id = item.CacheId;
        buy.onClick.AddListener(() => Buy(id));
    }

    public CachePurchaseResult Buy(string id)
    {
        if (ShopSceneRouter.IsTransitioning) return CachePurchaseResult.RunNotStarted;
        CachePurchaseResult result = MapRunState.TryPurchaseCache(catalog, id);
        messageText.text = result == CachePurchaseResult.Success ? "구매 완료" :
            result == CachePurchaseResult.AlreadyPurchased ? "이번 판에서 이미 구매했습니다" :
            result == CachePurchaseResult.InsufficientPackets ? "Packet이 부족합니다" : "구매할 수 없습니다";
        Refresh();
        return result;
    }

    private void Refresh()
    {
        lastBalance = MapRunState.Packets.Balance;
        balanceText.text = $"Packet {lastBalance}   |   Layer {MapRunState.Progression.Layer}";
        if (catalog == null) return;
        foreach (CacheData item in catalog.Items)
        {
            if (item == null || !buttons.TryGetValue(item.CacheId, out Button button)) continue;
            bool purchased = MapRunState.Caches.HasPurchased(item.CacheId);
            bool affordable = MapRunState.Packets.CanAfford(item.PacketPrice);
            button.interactable = !ShopSceneRouter.IsTransitioning && item.IsConfigured && !purchased && affordable;
            buttonTexts[item.CacheId].text = purchased ? "판매 완료" : !item.IsConfigured ? "준비 중" :
                affordable ? $"구매 · {item.PacketPrice} Packet" : $"{item.PacketPrice} Packet\n잔액 부족";
        }
    }

    private static string Describe(CacheData item)
    {
        if (item.EffectKind == CacheEffectKind.GlobalPacketAndExperience)
            return $"승리 Packet +{item.PacketGainPercent}% · EXP +{item.ExperienceGainPercent}%";
        List<string> values = new List<string>();
        if (item.MaxHPBonus > 0) values.Add($"HP +{item.MaxHPBonus}");
        if (item.AttackBonus > 0) values.Add($"ATK +{item.AttackBonus}");
        if (item.DefenseBonus > 0) values.Add($"DEF +{item.DefenseBonus}");
        if (item.ArtsResistanceBonus > 0) values.Add($"RES +{item.ArtsResistanceBonus}");
        if (item.AttackSpeedBonus > 0) values.Add($"SPD +{item.AttackSpeedBonus:0.##}");
        return string.Join(" · ", values);
    }

    private Button MakeButton(Transform parent, string name, string caption, Vector2 min, Vector2 max, Vector2 start, Vector2 end)
    {
        RectTransform rect = Rect(name, parent);
        Layout(rect, min, max, start, end);
        rect.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.42f, 0.35f);
        Button button = rect.gameObject.AddComponent<Button>();
        TMP_Text label = Text(rect, "Label", caption, Vector2.zero, Vector2.one, new Vector2(8, 4), new Vector2(-8, -4), 20);
        label.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private TMP_Text Text(Transform parent, string name, string value, Vector2 min, Vector2 max, Vector2 start, Vector2 end, float size)
    {
        RectTransform rect = Rect(name, parent);
        Layout(rect, min, max, start, end);
        TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font != null ? font : TMP_Settings.defaultFontAsset;
        label.text = value;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14;
        label.fontSizeMax = size;
        label.color = Color.white;
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        return label;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Layout(RectTransform rect, Vector2 min, Vector2 max, Vector2 start, Vector2 end)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = start;
        rect.offsetMax = end;
    }
}
