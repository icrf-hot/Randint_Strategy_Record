using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>Isolated result injection checks transitions and summary UI, not combat balance.</summary>
[InitializeOnLoad]
public static class RunResolutionRuntimeVerification
{
    private const string Key = "RunResolution.Verify.";
    public static string Output => SessionState.GetString(Key + "Output", "");

    static RunResolutionRuntimeVerification()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key + "Running", false))
                new GameObject("RunResolutionVerification").AddComponent<RunResolutionRuntimeProbe>();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key + "Restore", false))
            {
                SessionState.SetBool(Key + "Restore", false);
                string scene = SessionState.GetString(Key + "Scene", "");
                EditorApplication.delayCall += () => { if (!string.IsNullOrEmpty(scene)) EditorSceneManager.OpenScene(scene); };
            }
        };
        Application.logMessageReceived += (message, trace, type) =>
        {
            if (SessionState.GetBool(Key + "Running", false) && EditorApplication.isPlaying &&
                (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Finish("FAIL: " + message + "\n" + trace);
        };
        EditorApplication.update += () =>
        {
            if (SessionState.GetBool(Key + "Running", false) && DateTime.UtcNow >
                DateTime.Parse(SessionState.GetString(Key + "Deadline", "")).ToUniversalTime()) Finish("FAIL: timeout");
        };
    }

    public static void Run(string output)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene edits first.");
        Directory.CreateDirectory(output);
        SessionState.SetString(Key + "Output", output);
        SessionState.SetString(Key + "Scene", SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key + "Restore", true);
        SessionState.SetString(Key + "Deadline", DateTime.UtcNow.AddSeconds(120).ToString("O"));
        SessionState.SetBool(Key + "Running", true);
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        EditorApplication.EnterPlaymode();
    }

    public static void Finish(string result)
    {
        if (!SessionState.GetBool(Key + "Running", false)) return;
        SessionState.SetBool(Key + "Running", false);
        File.WriteAllText(Path.Combine(Output, "result.txt"), result);
        Debug.Log("[Run resolution runtime] " + result);
        EditorApplication.ExitPlaymode();
    }
}

public sealed class RunResolutionRuntimeProbe : MonoBehaviour
{
    private IDisposable scope;
    private readonly List<string> report = new List<string>();
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        scope = RunSaveService.UseRepositoryForVerification(new RunSaveRepository(
            Path.Combine(RunResolutionRuntimeVerification.Output, "isolated-saves"), RunSaveBindings.Instance));
        yield return new WaitForSecondsRealtime(1);
        foreach (var scenario in new[] { "normal-clear", "normal-defeat", "hidden-continue", "hidden-lethal", "hidden-clear" })
        {
            bool hidden = scenario.StartsWith("hidden");
            int floor = scenario == "hidden-clear" ? 5 : 3;
            EncounterVerification.Fixture(floor);
            if (hidden) MapRunState.TryUnlockHiddenEndingRoute();
            MapRunState.SaveCurrentNode(CampaignDefinition.TerminalNodeId(floor), "Terminal fixture");
            MapRunState.RecordHealth.Restore(scenario == "hidden-lethal" ? 20 : 100, 100);
            if (scenario.EndsWith("clear"))
            {
                MapRunState.Caches.Grant(RunSaveBindings.Instance.Cache("defender_relic"));
                MapRunState.RecordBattleResult(true);
                Require(TransitionManager.TryLoad(CampaignScenes.PathFor(floor)), "Map request rejected.");
            }
            else
            {
                MapRunState.SetBattleNodeType(MapNodeType.FirstBoss);
                Require(TransitionManager.TryLoad(RunSaveBindings.Instance.BattleScenePath), "Battle request rejected.");
                yield return WaitFor(() => SceneManager.GetActiveScene().name == "Playing_Scene" && !TransitionManager.IsBusy);
                yield return new WaitForSecondsRealtime(1);
                var manager = FindFirstObjectByType<BattleExecuteManager>();
                var routine = (IEnumerator)typeof(BattleExecuteManager).GetMethod("FinishBattle", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(manager, new object[] { BattleResultType.Failure });
                manager.StartCoroutine(routine);
            }
            if (scenario == "hidden-continue")
            {
                yield return WaitFor(() => SceneManager.GetActiveScene().name == "Map_4F" && !TransitionManager.IsBusy);
                Require(MapRunState.RecordHealth.Integrity == 80 && !MapRunState.IsEnded && !RunSummaryWindow.IsOpen,
                    "Hidden boss defeat did not continue to floor 4 with double loss.");
                Require(MapRunState.IsCompleted(CampaignDefinition.TerminalNodeId(3)), "Defeated boss not consumed.");
            }
            else
            {
                yield return WaitFor(() => RunSummaryWindow.IsOpen && !TransitionManager.IsBusy);
                Require(MapRunState.IsEnded && (MapRunState.EndReason == "Ending") == scenario.EndsWith("clear"), "Wrong outcome.");
                if (scenario == "normal-defeat") Require(MapRunState.RecordHealth.Integrity == 90, "Normal loss changed.");
                if (scenario == "hidden-lethal") Require(MapRunState.RecordHealth.Integrity == 0 && MapRunState.EndReason == "Ejected", "Lethal precedence lost.");
                Canvas.ForceUpdateCanvases();
                yield return new WaitForSecondsRealtime(.25f);
                var scroll = FindFirstObjectByType<RunSummaryWindow>().GetComponentInChildren<UnityEngine.UI.ScrollRect>();
                Require(scroll.content.rect.height > scroll.viewport.rect.height, "Report cannot scroll.");
                ScreenCapture.CaptureScreenshot(Path.Combine(RunResolutionRuntimeVerification.Output, scenario + ".png"));
                yield return new WaitForSecondsRealtime(.2f);
                var button = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b => b.name == "ReturnToLobby");
                var rect = button.GetComponent<RectTransform>();
                var data = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(data, hits);
                Require(hits.Count > 0 && ExecuteEvents.GetEventHandler<UnityEngine.UI.Button>(hits[0].gameObject) == button.gameObject, "Summary button covered.");
                ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
                yield return WaitFor(() => SceneManager.GetActiveScene().name == "Lobby" && !TransitionManager.IsBusy);
                Require(!RunSummaryWindow.IsOpen, "Summary survived Lobby return.");
            }
            report.Add("PASS: " + scenario);
            Debug.Log("[Run resolution runtime] " + report.Last());
        }
        RunResolutionRuntimeVerification.Finish(string.Join("\n", report) + "\nResult injection only; combat balance not tested.");
    }

    private static IEnumerator WaitFor(Func<bool> ready)
    {
        float deadline = Time.realtimeSinceStartup + 20;
        while (!ready()) { Require(Time.realtimeSinceStartup < deadline, "Transition/summary timeout."); yield return null; }
        yield return null;
    }

    private void OnDestroy() { scope?.Dispose(); }
}
