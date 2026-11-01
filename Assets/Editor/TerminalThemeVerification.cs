using System;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in smoke check of real boot/scene routing; never writes a save file.</summary>
[InitializeOnLoad]
public static class TerminalThemeVerification
{
    private static Card renderNormal, renderJoker;
    private static Vector3 orbitPosition;
    private static Quaternion orbitRotation;
    private const string Key = "TerminalTheme.Verification.";
    private static string Output => SessionState.GetString(Key + "Output", System.IO.Path.GetFullPath("Temp/TerminalVerification"));
    static TerminalThemeVerification()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += (message, trace, type) => {
            if (SessionState.GetBool(Key + "Running", false) && EditorApplication.isPlaying &&
                (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) SessionState.SetString(Key + "Error", message + "\n" + trace);
        };
    }
    public static void Run(string outputPath = null)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++) if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene edits before verification.");
        SessionState.SetString(Key + "Output", outputPath ?? System.IO.Path.GetFullPath("Temp/TerminalVerification"));
        System.IO.Directory.CreateDirectory(Output);
        SessionState.SetString(Key + "Scene", SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key + "Background", Application.runInBackground); Application.runInBackground = true;
        SessionState.SetInt(Key + "Stage", 0); SessionState.SetString(Key + "Error", "");
        SessionState.SetFloat(Key + "Wait", 0); SessionState.SetString(Key + "Deadline", DateTime.UtcNow.AddSeconds(120).ToString("O"));
        SessionState.SetBool(Key + "Running", true); SessionState.SetString(Key + "Result", "running");
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity"); EditorApplication.EnterPlaymode();
    }
    private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>();
    private static UnityEngine.UI.Button Button(string name) => UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).FirstOrDefault(x => x.name == name);
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Next(int stage, float delay = .25f) { SessionState.SetInt(Key + "Stage", stage); SessionState.SetFloat(Key + "Wait", Time.realtimeSinceStartup + delay); }
    private static void Capture(string name) { ScreenCapture.CaptureScreenshot(Output + "/" + name + ".png"); Debug.Log("[Terminal verification] " + name); }
    private static Card SpawnRenderCard(string path, float x, float y, int index, bool joker)
    {
        var go = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        var cam = Camera.main;
        const float depth = 8;
        go.transform.position = cam.ViewportToWorldPoint(new Vector3(x, y, depth));
        var body = go.GetComponent<SpriteRenderer>();
        go.transform.localScale *= Vector3.Distance(cam.ViewportToWorldPoint(new Vector3(.5f, .18f, depth)), cam.ViewportToWorldPoint(new Vector3(.5f, 0, depth))) / body.bounds.size.y;
        var card = go.GetComponent<Card>();
        if (joker) card.SetSpecialCard("T"); else card.SetNumber(10);
        go.GetComponent<CardPresentation>().SetHandIndex(index);
        return card;
    }
    private static void Tick()
    {
        if (!SessionState.GetBool(Key + "Running", false)) return;
        try {
            Require(DateTime.UtcNow < DateTime.Parse(SessionState.GetString(Key + "Deadline", "")).ToUniversalTime(), "Timed out.");
            if (!EditorApplication.isPlaying) return;
            Require(SessionState.GetString(Key + "Error", "") == "", SessionState.GetString(Key + "Error", ""));
            if (Time.realtimeSinceStartup < SessionState.GetFloat(Key + "Wait", 0)) return;
            int stage = SessionState.GetInt(Key + "Stage", 0); string scene = SceneManager.GetActiveScene().name;
            if (stage == 0 && scene == "Title") {
                if (Button("EnterLobbyButton") == null || !Button("EnterLobbyButton").interactable) return;
                Require(UITheme.Mono != UITheme.Font, "Monospace font is missing.");
                Require(Find<TerminalProgressBar>() != null, "Boot progress missing.");
                Capture("01-title"); Next(1);
            } else if (stage == 1) {
                Require(!TransitionManager.TryLoad("Assets/Scenes/DoesNotExist.unity") && !TransitionManager.IsBusy, "Invalid scene locked input.");
                Time.timeScale = 0; Find<TitleSceneController>().EnterLobby(); Require(TransitionManager.IsBusy, "Title did not start transition.");
                Require(!TransitionManager.TryLoad("Assets/Scenes/Lobby.unity"), "Duplicate transition was accepted."); Next(2);
            }
            else if (stage == 2 && scene == "Lobby" && !TransitionManager.IsBusy) {
                var cards = new[] { Button("SettingsButton"), Button("ContinueButton"), Button("NewGameButton") };
                Require(cards.All(x => x != null), "Desktop cards missing.");
                float width = ((RectTransform)cards[0].transform).rect.width;
                Require(cards.All(x => Mathf.Abs(((RectTransform)x.transform).rect.width - width) < 1), "Desktop cards have unequal widths.");
                Capture("02-desktop"); Next(3);
            } else if (stage == 3) { Find<LobbySceneController>().StartNewGame(); Next(4); }
            else if (stage == 4 && scene == "Preparation" && !TransitionManager.IsBusy) {
                Require(Button("StartRun").interactable, "Valid roster cannot start.");
                Require(UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Count(x => x.name == "CoreStats") == 3, "Roster stats missing.");
                Capture("03-preparation"); Next(5);
            } else if (stage == 5) { Button("ChangeOperator").onClick.Invoke(); Next(6); }
            else if (stage == 6) {
                Require(GameObject.Find("RosterDrawer") != null, "Roster drawer did not open."); Capture("04-roster"); Next(7);
            } else if (stage == 7) {
                Button("Select").onClick.Invoke(); Find<PreparationSceneController>().StartRun(); Next(8);
            } else if (stage == 8 && scene == "Map_1F" && !TransitionManager.IsBusy && Find<MapSceneFadeIn>().IsFadeComplete) {
                Require(Find<TerminalRecordHUD>() != null && GameObject.Find("TerminalFloorGrid") == null && RenderSettings.skybox != null, "Map presentation missing.");
                Require(Find<ParticleSystem>().main.maxParticles == UITheme.ParticleLimit, "Particle cap mismatch.");
                Require(RenderSettings.skybox.shader.name == "Randint/TerminalStarfield", "Global star background missing.");
                var skyCamera = Find<MapOrbitCamera>().GetComponent<Camera>();
                if (skyCamera.TryGetComponent<Skybox>(out var skyOverride) && skyOverride.enabled)
                    Require(skyOverride.material.shader.name == "Randint/TerminalStarfield", "Legacy camera skybox overrides the starfield.");
                Capture("05-map"); Next(200);
            } else if (stage == 200) {
                var orbit = Find<MapOrbitCamera>(); var cam = orbit.GetComponent<Camera>();
                orbitPosition = cam.transform.position; orbitRotation = cam.transform.rotation; orbit.enabled = false;
                cam.transform.RotateAround(Vector3.zero, Vector3.up, 120); Next(201);
            } else if (stage == 201) {
                Require(GameObject.Find("TerminalFloorGrid") == null, "Horizon returned during orbit."); Capture("05b-map-rotated"); Next(202);
            } else if (stage == 202) {
                var orbit = UnityEngine.Object.FindObjectsByType<MapOrbitCamera>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();
                orbit.transform.SetPositionAndRotation(orbitPosition, orbitRotation); orbit.enabled = true; Next(9);
            } else if (stage == 9) { Require(TransitionManager.TryLoad(RunSaveBindings.Instance.BattleScenePath), "Battle route rejected."); Next(10); }
            else if (stage == 10 && scene == "Playing_Scene" && !TransitionManager.IsBusy && Find<BattleSceneFadeIn>().IsFadeComplete) {
                Require(GameObject.Find("RecordHealthPanel") == null, "Record health HUD covers battle selection panel."); Next(100, 3.2f);
            } else if (stage == 100) {
                foreach (var card in UnityEngine.Object.FindObjectsByType<Card>(FindObjectsSortMode.None)) {
                    var sprite = card.GetComponentInChildren<SpriteRenderer>(); if (sprite == null) continue;
                    Vector3 min = Camera.main.WorldToViewportPoint(sprite.bounds.min), max = Camera.main.WorldToViewportPoint(sprite.bounds.max);
                    Require(min.x >= .29f && min.y >= 0 && max.x <= 1 && max.y <= 1, $"Card bounds: {min} .. {max}; orthographic={Camera.main.orthographic}");
                }
                var surfaces = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                Require(surfaces.First(x => x.name == "Ground").color == UITheme.BattleFloor, "Original battlefield floor not restored.");
                Require(Mathf.Abs(surfaces.First(x => x.name == "OperUI_info").color.a - UITheme.InfoPanelOpacity) < .001f, "Information panel is opaque.");
                Capture("06-battle"); Next(300);
            } else if (stage == 300) {
                renderNormal = SpawnRenderCard("Assets/Prefab/Card.prefab", .49f, .27f, 20, false);
                renderJoker = SpawnRenderCard("Assets/Prefab/Card_Joker.prefab", .55f, .30f, 21, true); Next(301);
            } else if (stage == 301) {
                var ng = renderNormal.GetComponent<CardPresentation>();
                var jg = renderJoker.GetComponent<CardPresentation>();
                Require(ng.RenderOrder >= UITheme.CardSortBase && jg.RenderOrder > ng.RenderOrder, "Cards do not have independent front ordering.");
                var photo = renderJoker.GetComponentsInChildren<TerminalCardSurface>().Single(x => x.preserveTextureColor).GetComponent<SpriteRenderer>();
                Require(photo.sharedMaterial.GetFloat("_PreserveColor") == 1 && photo.color == UITheme.TextureTint, "Joker photo is being recoloured.");
                Require(renderJoker.GetComponentsInChildren<SpriteRenderer>().Length == 2, "Joker still contains overlapping logo artwork.");
                foreach (var card in new[] { renderNormal, renderJoker }) {
                    var canvas = card.GetComponentInChildren<Canvas>();
                    Require((canvas.isRootCanvas || canvas.overrideSorting) && canvas.sortingOrder > card.GetComponent<CardPresentation>().RenderOrder, "Card numbers are behind their face.");
                    Require(card.GetComponentsInChildren<TMP_Text>().All(t => t.fontSharedMaterial.shader.name.Contains("Overlay")), "Number shader still depth-tests against the battlefield.");
                }
                Capture("08-card-overlap"); Next(302);
            } else if (stage == 302) {
                UnityEngine.EventSystems.ExecuteEvents.Execute(renderNormal.gameObject,
                    new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current),
                    UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler); Next(303, .3f);
            } else if (stage == 303) {
                Require(renderNormal.GetComponent<CardPresentation>().RenderOrder > renderJoker.GetComponent<CardPresentation>().RenderOrder, "Hovered normal card did not move to the front.");
                Capture("09-card-hover"); Next(304);
            } else if (stage == 304) {
                UnityEngine.Object.Destroy(renderNormal.gameObject); UnityEngine.Object.Destroy(renderJoker.gameObject); Next(210);
            } else if (stage == 210) {
                var op = UnityEngine.Object.FindObjectsByType<OperatorClickHandler>(FindObjectsSortMode.None).First(x => x.Position == OperatorPosition.Back);
                Find<OperatorFocusManager>().EnterFocus(op); Next(211, 1.5f);
            } else if (stage == 211) {
                Capture("06c-character-information"); Find<OperatorFocusManager>().ExitFocus(); Next(101, .8f);
            } else if (stage == 101) {
                Button("Settings").onClick.Invoke(); Require(Mathf.Approximately(Time.timeScale, 0), "Settings did not pause battle.");
                bool monitor = TerminalPreferences.Monitor;
                Button("MonitorEffects").onClick.Invoke(); Require(TerminalPreferences.Monitor != monitor, "Monitor toggle did not change.");
                Button("MonitorEffects").onClick.Invoke(); Require(TerminalPreferences.Monitor == monitor, "Monitor setting was not restored.");
                Button("Close").onClick.Invoke(); Require(Mathf.Approximately(Time.timeScale, 1), "Settings did not resume battle.");
                Require(RunRecordService.ReduceIntegrity(75), "Integrity loss was not applied.");
                TerminalWorld.Hit(Vector3.zero, true); Next(102);
            } else if (stage == 102) {
                Require(MapRunState.RecordHealth.Integrity == 25, "Critical integrity is wrong.");
                Require(GameObject.Find("RecordHealthPanel") == null, "Record health HUD returned in battle.");
                Require(Find<ParticleSystem>().particleCount <= UITheme.ParticleLimit, "Particle count exceeded cap.");
                Capture("06b-battle-critical"); Next(103);
            } else if (stage == 103) {
                MapRunState.RecordHealth.Restore(100, 100); Next(11);
            } else if (stage == 11) { Find<BattleToMapSceneLoader>().LoadMap(); Next(12); }
            else if (stage == 12 && scene == "Map_1F" && !TransitionManager.IsBusy && Find<MapSceneFadeIn>().IsFadeComplete) {
                Require(!MapRunState.IsEnteringMapFromBattle, "Map entry flag is stuck.");
                Require(TransitionManager.TryLoad(ShopSceneRouter.ShopPath), "Shop route rejected."); Next(13);
            } else if (stage == 13 && scene == "Shop" && !TransitionManager.IsBusy) { Require(Find<ShopSceneUI>() != null, "Shop missing."); Capture("07-shop"); Next(14); }
            else if (stage == 14) { Require(ShopSceneRouter.ReturnToMap(), "Shop return failed."); Next(15); }
            else if (stage == 15 && scene == "Map_1F" && !TransitionManager.IsBusy) { RunRecordService.ReturnToLobby(); Next(16); }
            else if (stage == 16 && scene == "Lobby" && !TransitionManager.IsBusy) {
                Require(Mathf.Approximately(Time.timeScale, 1), "Time scale was not restored."); Finish("PASS: boot, equal desktop cards, formation stats/drawer, map HUD, battle round-trip, shop round-trip, lobby return.");
            }
        } catch (Exception e) { Finish("FAIL: stage " + SessionState.GetInt(Key + "Stage", 0) + " / " + e); }
    }
    private static void Finish(string result)
    {
        SessionState.SetBool(Key + "Running", false); SessionState.SetString(Key + "Result", result);
        System.IO.File.WriteAllText(Output + "/result.txt", result);
        Debug.Log("[Terminal verification] " + result);
        EditorApplication.ExitPlaymode(); EditorApplication.delayCall += Restore;
    }
    private static void Restore()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.delayCall += Restore; return; }
        Application.runInBackground = SessionState.GetBool(Key + "Background", false);
        string scene = SessionState.GetString(Key + "Scene", "");
        if (!string.IsNullOrEmpty(scene)) EditorSceneManager.OpenScene(scene);
    }
}
