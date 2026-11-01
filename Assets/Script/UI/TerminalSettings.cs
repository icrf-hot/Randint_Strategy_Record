using UnityEngine;
using UnityEngine.InputSystem;

public sealed class TerminalSettings : MonoBehaviour
{
    private GameObject window;
    private GameObject combatWindow;
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
        var panel = TerminalUI.Panel("SettingsPanel", backdrop.transform, Vector2.zero, new Vector2(560, 400));
        TerminalUI.Label("Heading", panel.transform, "환경 설정", new Vector2(0, 155), new Vector2(500, 40), 26);
        AddOptions(panel.transform, 90);
        var close = TerminalUI.Button("Close", panel.transform, "닫기 / ESC", new Vector2(0, -150), new Vector2(220, 40)); close.onClick.AddListener(Toggle);
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
    private void AddOptions(Transform parent, float y)
    {
        var monitor = TerminalUI.Button("MonitorEffects", parent, "", new Vector2(0, y), new Vector2(310, 44));
        var motion = TerminalUI.Button("MotionEffects", parent, "", new Vector2(0, y - 64), new Vector2(310, 44));
        var combat = TerminalUI.Button("CombatOptions", parent, "전투 연출 설정", new Vector2(0, y - 128), new Vector2(310, 44));
        combat.onClick.AddListener(OpenCombatOptions);
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
        if (combatWindow != null) { Destroy(combatWindow); return; }
        if (lobby != null) { if (lobby.IsModalOpen) lobby.CloseWindows(); else lobby.OpenSettings(); }
        else if (map != null) { if (map.IsSettingsOpen) map.CloseSettings(); else map.OpenSettings(); }
        else Toggle();
    }
    public void OpenCombatOptions()
    {
        if (combatWindow != null) return;
        var root = TerminalUI.Canvas("CombatOptionsWindow", 210); combatWindow = root.gameObject;
        var backdrop = TerminalUI.Panel("Backdrop", root.transform, Vector2.zero, Vector2.zero, UITheme.Tone.Base);
        TerminalUI.Stretch(backdrop.rectTransform); backdrop.raycastTarget = true;
        var panel = TerminalUI.Panel("Dialog", root.transform, Vector2.zero, new Vector2(610, 470));
        TerminalUI.Label("Heading", panel.transform, "전투 연출", new Vector2(0, 190), new Vector2(540, 40), 26);
        var shake = TerminalUI.Label("ShakeLabel", panel.transform, "", new Vector2(0, 130), new Vector2(540, 35), 20);
        var sliderRoot = TerminalUI.Panel("ShakeStrength", panel.transform, new Vector2(0, 90), new Vector2(440, 12), UITheme.Tone.Hover);
        sliderRoot.raycastTarget = true;
        var handle = TerminalUI.Panel("Handle", sliderRoot.transform, Vector2.zero, new Vector2(18, 28), UITheme.Tone.Accent);
        var slider = sliderRoot.gameObject.AddComponent<UnityEngine.UI.Slider>(); slider.minValue = 0; slider.maxValue = 1;
        slider.handleRect = handle.rectTransform; slider.targetGraphic = handle; slider.value = TerminalPreferences.ShakeStrength;
        slider.onValueChanged.AddListener(value => { TerminalPreferences.ShakeStrength = value; shake.text = $"화면 흔들림 강도 {value:P0}"; });
        shake.text = $"화면 흔들림 강도 {TerminalPreferences.ShakeStrength:P0}";
        var stop = TerminalUI.Button("HitStop", panel.transform, "", new Vector2(0, 25), new Vector2(400, 42));
        var flash = TerminalUI.Button("ReduceFlash", panel.transform, "", new Vector2(0, -32), new Vector2(400, 42));
        var speed = TerminalUI.Button("Speed", panel.transform, "", new Vector2(0, -89), new Vector2(400, 42));
        System.Action refresh = () => {
            stop.GetComponentInChildren<TMPro.TMP_Text>().text = "히트스톱 " + (TerminalPreferences.HitStop ? "ON" : "OFF");
            flash.GetComponentInChildren<TMPro.TMP_Text>().text = "섬광 효과 줄이기 " + (TerminalPreferences.ReduceFlash ? "ON" : "OFF");
            speed.GetComponentInChildren<TMPro.TMP_Text>().text = $"전투 속도 {TerminalPreferences.CombatSpeed}x";
        };
        stop.onClick.AddListener(() => { TerminalPreferences.HitStop = !TerminalPreferences.HitStop; refresh(); });
        flash.onClick.AddListener(() => { TerminalPreferences.ReduceFlash = !TerminalPreferences.ReduceFlash; refresh(); });
        speed.onClick.AddListener(() => { TerminalPreferences.CombatSpeed = TerminalPreferences.CombatSpeed % 3 + 1; refresh(); }); refresh();
        var close = TerminalUI.Button("Close", panel.transform, "닫기 / ESC", new Vector2(0, -180), new Vector2(220, 40));
        close.onClick.AddListener(() => Destroy(combatWindow));
    }
    private void Toggle()
    {
        if (window == null || TransitionManager.IsBusy) return;
        bool open = !window.activeSelf;
        if (open) { previousScale = Time.timeScale; Time.timeScale = 0; }
        else Time.timeScale = previousScale;
        window.SetActive(open);
    }
    private void OnDisable() { if (combatWindow != null) Destroy(combatWindow); if (window != null && window.activeSelf) Time.timeScale = previousScale; }
}
