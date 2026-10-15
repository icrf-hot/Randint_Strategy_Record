using Randint.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MapSaveControls : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    private GameObject canvasObject;
    private TMP_Text statusText;
    private Button saveButton;
    private Button lobbyButton;
    private Button settingsButton;
    private GameObject settingsOverlay;
    private bool isLeaving;
    private GameObject picker;
    private SaveSlotBrowser browser;
    private TMP_Text pickerSummary;
    private Button confirmButton;
    private bool returnAfterSave;
    private bool confirmingOverwrite;
    public Button SaveButton => saveButton;
    public Button LobbyButton => lobbyButton;
    public Button SettingsButton => settingsButton;
    public bool IsSettingsOpen => settingsOverlay != null && settingsOverlay.activeSelf;
    public Image SettingsBackdrop => settingsOverlay.GetComponent<Image>();
    public SaveSlotBrowser Browser => browser;
    public Button ConfirmButton => confirmButton;

    private void Start()
    {
        canvasObject = new GameObject("MapSaveCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        settingsButton = Button("Settings", "menu.settings.label", new Vector2(24, -24), new Vector2(104, 44));
        RectTransform settingsRect = (RectTransform)settingsButton.transform;
        settingsRect.anchorMin = settingsRect.anchorMax = settingsRect.pivot = new Vector2(0, 1);
        settingsButton.onClick.AddListener(OpenSettings);
        settingsOverlay = new GameObject("MapSettingsOverlay", typeof(RectTransform), typeof(Image));
        settingsOverlay.transform.SetParent(canvasObject.transform, false);
        RectTransform overlayRect = (RectTransform)settingsOverlay.transform;
        overlayRect.anchorMin = Vector2.zero; overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;
        settingsOverlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        saveButton = Button("Save", "save.save", new Vector2(0, 40), new Vector2(240, 48), settingsOverlay.transform);
        lobbyButton = Button("SaveAndLobby", "save.save_exit", new Vector2(0, -24), new Vector2(240, 48), settingsOverlay.transform);
        Button closeSettings = Button("CloseSettings", "menu.window.close", new Vector2(0, -100), new Vector2(160, 44), settingsOverlay.transform);
        closeSettings.onClick.AddListener(CloseSettings);
        saveButton.onClick.AddListener(() => OpenSlotPicker(false));
        lobbyButton.onClick.AddListener(() => OpenSlotPicker(true));
        statusText = Label("Status", settingsOverlay.transform);
        RectTransform rect = statusText.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, 116);
        rect.sizeDelta = new Vector2(520, 42);
        statusText.alignment = TextAlignmentOptions.Center;
        settingsOverlay.SetActive(false);
    }

    private void Update()
    {
        if (saveButton == null) return;
        bool allowed = !isLeaving && RunSaveService.CanSave;
        settingsButton.interactable = allowed && !IsSettingsOpen && (picker == null || !picker.activeSelf);
        saveButton.interactable = allowed && IsSettingsOpen && (picker == null || !picker.activeSelf);
        lobbyButton.interactable = saveButton.interactable;
        if (picker != null && picker.activeSelf)
        {
            browser.SetInteractable(allowed);
            confirmButton.interactable = allowed && browser.Error == RunSaveError.None;
        }
        MapSceneFadeIn fade = FindFirstObjectByType<MapSceneFadeIn>();
        canvasObject.SetActive(fade == null || fade.IsFadeComplete);
    }

    public void OpenSettings()
    {
        if (isLeaving || !RunSaveService.CanSave || (picker != null && picker.activeSelf)) return;
        settingsOverlay.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsOverlay.SetActive(false);
    }

    public void CancelSlotPicker()
    {
        confirmingOverwrite = false;
        if (picker != null) picker.SetActive(false);
        if (!isLeaving) settingsOverlay.SetActive(true);
    }

    public void OpenSlotPicker(bool returnToLobby)
    {
        if (isLeaving || !IsSettingsOpen || !RunSaveService.CanSave) return;
        EnsurePicker();
        returnAfterSave = returnToLobby;
        confirmingOverwrite = false;
        settingsOverlay.SetActive(false);
        picker.SetActive(true);
        browser.SetInteractable(true);
        browser.Open(RunSaveService.SelectedSlot);
    }

    private void EnsurePicker()
    {
        if (picker != null) return;
        picker = new GameObject("SaveSlotPicker", typeof(RectTransform), typeof(Image));
        picker.transform.SetParent(canvasObject.transform, false);
        var rect = (RectTransform)picker.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        picker.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        var panel = new GameObject("Dialog", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(picker.transform, false);
        SlotUI.Place((RectTransform)panel.transform, Vector2.zero, new Vector2(960, 640));
        panel.GetComponent<Image>().color = new Color(0.98f, 0.98f, 0.99f);
        var title = SlotUI.Label(panel.transform, "Title", font, new Vector2(0, 290), new Vector2(900, 40), 24);
        title.text = GameData.Text("save.choose_slot");
        pickerSummary = SlotUI.Label(panel.transform, "Summary", font, new Vector2(0, -211), new Vector2(900, 72));
        confirmButton = SlotUI.Button(panel.transform, "Confirm", font, GameData.Text("save.save"),
            new Vector2(-200, -280), new Vector2(180, 44));
        var close = SlotUI.Button(panel.transform, "Cancel", font, GameData.Text("menu.window.close"),
            new Vector2(200, -280), new Vector2(180, 44));
        close.onClick.AddListener(CancelSlotPicker);
        confirmButton.onClick.AddListener(ConfirmSave);
        browser = SaveSlotBrowser.Create(panel.transform, font, _ =>
        {
            confirmingOverwrite = false;
            confirmButton.GetComponentInChildren<TMP_Text>().text = GameData.Text("save.save");
            pickerSummary.text = browser.Error == RunSaveError.None ? SlotUI.Summary(browser.SelectedSlot) : RunSaveMessages.Error(browser.Error);
        });
    }

    public void ConfirmSave()
    {
        if (isLeaving || picker == null || !picker.activeSelf || !RunSaveService.CanSave) return;
        if (browser.Error != RunSaveError.None) return;
        if (!confirmingOverwrite && RunSaveService.Slots.At(browser.SelectedSlot).HasFiles)
        {
            confirmingOverwrite = true;
            pickerSummary.text = string.Format(GameData.Text("save.confirm_overwrite"), browser.SelectedSlot);
            confirmButton.GetComponentInChildren<TMP_Text>().text = GameData.Text("save.overwrite");
            return;
        }
        if (returnAfterSave && !Application.CanStreamedLevelBeLoaded("Assets/Scenes/Lobby.unity"))
        { pickerSummary.text = RunSaveMessages.Error(RunSaveError.NotAllowed); return; }
        int previous = RunSaveService.SelectedSlot;
        if (!RunSaveService.SelectSlot(browser.SelectedSlot)) return;
        if (!RunSaveService.Save(out RunSaveError error))
        {
            RunSaveService.SelectSlot(previous);
            confirmingOverwrite = false;
            confirmButton.GetComponentInChildren<TMP_Text>().text = GameData.Text("save.save");
            pickerSummary.text = RunSaveMessages.Error(error);
            return;
        }
        picker.SetActive(false);
        settingsOverlay.SetActive(!returnAfterSave);
        statusText.text = GameData.Text("save.saved");
        if (returnAfterSave)
        {
            isLeaving = true;
            Time.timeScale = 1;
            SceneManager.LoadSceneAsync("Assets/Scenes/Lobby.unity");
        }
    }

    private Button Button(string name, string key, Vector2 position, Vector2 size, Transform parent = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent != null ? parent : canvasObject.transform, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.12f, 0.35f, 0.29f);
        TMP_Text label = Label("Label", go.transform);
        label.text = GameData.Text(key);
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(6, 4);
        label.rectTransform.offsetMax = new Vector2(-6, -4);
        return go.GetComponent<Button>();
    }

    private TMP_Text Label(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TMP_Text text = go.GetComponent<TMP_Text>();
        text.font = font;
        text.fontSize = 20;
        text.enableAutoSizing = true;
        text.fontSizeMin = 14;
        text.fontSizeMax = 20;
        text.raycastTarget = false;
        text.color = Color.white;
        return text;
    }
}
