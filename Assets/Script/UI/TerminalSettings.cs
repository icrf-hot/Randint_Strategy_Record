using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TerminalSettings : MonoBehaviour
{
    private GameObject window;
    private float previousScale;
    private LobbySceneController lobby;
    private MapSaveControls map;
    private void Start()
    {
        lobby = FindFirstObjectByType<LobbySceneController>(); map = FindFirstObjectByType<MapSaveControls>();
        if (lobby != null) {
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) {
                Transform settings = canvas.transform.Find("SettingsWindow/Dialog");
                if (settings == null) continue;
                var message = settings.Find("Message"); if (message != null) message.gameObject.SetActive(false);
                AddOptions(settings, 45); return;
            }
        }
        if (map != null) {
            // MapSaveControls initializes in Start; attachment is deferred one frame.
            StartCoroutine(AttachMap()); return;
        }
        var root = TerminalUI.Canvas("TerminalSettings", 100);
        var gear = TerminalUI.Button("Settings", root.transform, "", new Vector2(32, -32), new Vector2(40, 36));
        var rect = (RectTransform)gear.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Shop") {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one; rect.anchoredPosition = new Vector2(-24, -24);
        }
        TerminalGraphic.Create("Gear", gear.transform, TerminalGraphic.Shape.Gear, Vector2.zero, new Vector2(28, 28));
        gear.onClick.AddListener(Toggle);
        var backdrop = TerminalUI.Panel("SettingsBackdrop", root.transform, Vector2.zero, Vector2.zero, UITheme.Tone.Base);
        TerminalUI.Stretch(backdrop.rectTransform); backdrop.raycastTarget = true;
        backdrop.color = UITheme.Alpha(UITheme.BgBase, .95f); window = backdrop.gameObject;
        var panel = TerminalUI.Panel("SettingsPanel", backdrop.transform, Vector2.zero, new Vector2(560, 310));
        TerminalUI.Label("Heading", panel.transform, "환경 설정", new Vector2(0, 115), new Vector2(500, 40), 26);
        AddOptions(panel.transform, 40);
        var close = TerminalUI.Button("Close", panel.transform, "닫기 / ESC", new Vector2(0, -112), new Vector2(220, 40)); close.onClick.AddListener(Toggle);
        window.SetActive(false);
    }
    private System.Collections.IEnumerator AttachMap()
    {
        yield return null;
        if (map != null && map.SettingsBackdrop != null) {
            var group = TerminalUI.Rect("MonitorOptions", map.SettingsBackdrop.transform, new Vector2(370, 0), new Vector2(330, 220));
            AddOptions(group, 45);
        }
    }
    private static void AddOptions(Transform parent, float y)
    {
        var monitor = TerminalUI.Button("MonitorEffects", parent, "", new Vector2(0, y), new Vector2(310, 44));
        var motion = TerminalUI.Button("MotionEffects", parent, "", new Vector2(0, y - 64), new Vector2(310, 44));
        System.Action refresh = () => {
            monitor.GetComponentInChildren<TMPro.TMP_Text>().text = "모니터 효과  " + (TerminalPreferences.Monitor ? "ON" : "OFF");
            motion.GetComponentInChildren<TMPro.TMP_Text>().text = "동작·깜빡임  " + (TerminalPreferences.Motion ? "ON" : "OFF");
        };
        monitor.onClick.AddListener(() => { TerminalPreferences.Monitor = !TerminalPreferences.Monitor; refresh(); });
        motion.onClick.AddListener(() => { TerminalPreferences.Motion = !TerminalPreferences.Motion; refresh(); }); refresh();
    }
    private void Update()
    {
        if (TransitionManager.IsBusy || Keyboard.current?.escapeKey.wasPressedThisFrame != true) return;
        if (lobby != null) { if (lobby.IsModalOpen) lobby.CloseWindows(); else lobby.OpenSettings(); }
        else if (map != null) { if (map.IsSettingsOpen) map.CloseSettings(); else map.OpenSettings(); }
        else Toggle();
    }
    private void Toggle()
    {
        if (window == null || TransitionManager.IsBusy) return;
        bool open = !window.activeSelf;
        if (open) { previousScale = Time.timeScale; Time.timeScale = 0; }
        else Time.timeScale = previousScale;
        window.SetActive(open);
    }
    private void OnDisable() { if (window != null && window.activeSelf) Time.timeScale = previousScale; }
}
