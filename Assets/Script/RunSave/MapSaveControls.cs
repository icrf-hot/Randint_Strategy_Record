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
    private bool isLeaving;
    public Button SaveButton => saveButton;
    public Button LobbyButton => lobbyButton;

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
        saveButton = Button("Save", "save.save", new Vector2(24, 24), new Vector2(130, 46));
        lobbyButton = Button("SaveAndLobby", "save.save_lobby", new Vector2(166, 24), new Vector2(210, 46));
        saveButton.onClick.AddListener(() => Save(false));
        lobbyButton.onClick.AddListener(() => Save(true));
        statusText = Label("Status", canvasObject.transform);
        RectTransform rect = statusText.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(24, 80);
        rect.sizeDelta = new Vector2(520, 42);
    }

    private void Update()
    {
        if (saveButton == null) return;
        bool allowed = !isLeaving && RunSaveService.CanSave;
        saveButton.interactable = allowed;
        lobbyButton.interactable = allowed;
        MapSceneFadeIn fade = FindFirstObjectByType<MapSceneFadeIn>();
        canvasObject.SetActive(fade == null || fade.IsFadeComplete);
    }

    public bool Save(bool returnToLobby)
    {
        if (isLeaving) return false;
        if (returnToLobby && !Application.CanStreamedLevelBeLoaded("Assets/Scenes/Lobby.unity"))
        { statusText.text = RunSaveMessages.Error(RunSaveError.NotAllowed); return false; }
        if (!RunSaveService.Save(out RunSaveError error))
        { statusText.text = RunSaveMessages.Error(error); return false; }
        statusText.text = GameData.Text("save.saved");
        if (returnToLobby)
        {
            isLeaving = true;
            Time.timeScale = 1;
            SceneManager.LoadSceneAsync("Assets/Scenes/Lobby.unity");
        }
        return true;
    }

    private Button Button(string name, string key, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
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
