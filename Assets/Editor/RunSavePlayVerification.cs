using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RunSavePlayVerification
{
    private const string Key = "RSR.SavePlay.";
    private static IDisposable isolation;
    private static RunSaveRepository repository;
    static RunSavePlayVerification()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += (message, trace, type) =>
        {
            if (SessionState.GetBool(Key + "Running", false) && EditorApplication.isPlaying &&
                (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                SessionState.SetString(Key + "Error", message);
        };
    }

    public static void Run()
    {
        SessionState.SetBool(Key + "Running", true);
        SessionState.SetInt(Key + "Stage", 0);
        SessionState.SetString(Key + "Error", "");
        SessionState.SetString(Key + "Directory", Path.Combine(Path.GetTempPath(), "RSR_SavePlay_" + Guid.NewGuid().ToString("N")));
        SessionState.SetString(Key + "Deadline", DateTime.UtcNow.AddSeconds(160).ToString("O"));
        EditorSceneManager.OpenScene("Assets/Scenes/Map_1F.unity");
        EditorApplication.EnterPlaymode();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (!SessionState.GetBool(Key + "Running", false)) return;
        MapRunState.Reset();
        Time.timeScale = 1;
        repository = new RunSaveRepository(SessionState.GetString(Key + "Directory", ""), RunSaveBindings.Instance);
        isolation = RunSaveService.UseRepositoryForVerification(repository);
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key + "Running", false)) return;
        try
        {
            Require(DateTime.UtcNow < DateTime.Parse(SessionState.GetString(Key + "Deadline", "")).ToUniversalTime(), "Save UI scenario timed out.");
            if (!EditorApplication.isPlaying) return;
            Require(SessionState.GetString(Key + "Error", "") == "", SessionState.GetString(Key + "Error", ""));
            int stage = SessionState.GetInt(Key + "Stage", 0);
            string scene = SceneManager.GetActiveScene().name;
            if (stage == 0 && scene == "Map_1F")
            {
                var controls = UnityEngine.Object.FindFirstObjectByType<MapSaveControls>();
                var feedback = UnityEngine.Object.FindFirstObjectByType<MapPlayerPositionFeedback>();
                if (controls == null || controls.SaveButton == null || feedback == null || feedback.CurrentNode == null || !RunSaveService.CanSave) return;
                var nodes = UnityEngine.Object.FindObjectsByType<MapNode>(FindObjectsSortMode.None);
                feedback.MoveToNode(nodes.Single(node => node.NodeID == 2));
                feedback.MoveToNode(nodes.Single(node => node.NodeID == 5));
                var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalogData>("Assets/Shop/Data/PrototypeShopCatalog.asset");
                Require(MapRunState.TryPurchaseCache(catalog, "cache_hp") == CachePurchaseResult.Success, "Fixture Cache purchase failed.");
                MapRunState.TryGrantVictory(out _, out _);
                MapRunState.RecordBattleResult(true);
                controls.SaveButton.onClick.Invoke();
                Require(repository.TryRead(out RunSaveSnapshot data, out _) && data.packets == 26 && data.currentNodeId == 5,
                    "Map Save button did not persist snapshot.");
                SessionState.SetString(Key + "RunId", data.runId);
                controls.LobbyButton.onClick.Invoke();
                SessionState.SetInt(Key + "Stage", 1);
            }
            else if ((stage == 1 || stage == 5) && scene == "Lobby")
            {
                var lobby = UnityEngine.Object.FindFirstObjectByType<LobbySceneController>();
                if (lobby == null) return;
                Click(lobby, "continueButton");
                var window = ((GameObject)Field(lobby, "continueWindow")).GetComponent<SaveSlotWindow>();
                Require(window.ResumeButton.interactable, "Valid saved slot cannot continue.");
                if (stage == 1)
                {
                    MapRunState.Reset(); // Simulates loss of memory across application sessions.
                    Time.timeScale = 0;
                    window.ResumeButton.onClick.Invoke();
                    SessionState.SetInt(Key + "Stage", 2);
                }
                else
                {
                    window.DeleteButton.onClick.Invoke();
                    Require(window.IsConfirmingDelete && repository.HasFiles, "Delete skipped confirmation.");
                    lobby.CloseWindows();
                    Click(lobby, "continueButton");
                    Require(!window.IsConfirmingDelete && repository.HasFiles, "Closing confirmation deleted save.");
                    window.DeleteButton.onClick.Invoke();
                    window.DeleteButton.onClick.Invoke();
                    Require(!repository.HasFiles && !window.ResumeButton.interactable && !window.DeleteButton.interactable,
                        "Confirmed deletion left saved files or enabled actions.");
                    Require(MapRunState.Caches.Owned.Count == 1 && MapRunState.Packets.Balance == 26,
                        "Save deletion erased live run state.");
                    Finish(true, "Map Save -> Lobby Continue after memory reset -> battle save blocked -> Map -> confirmed Lobby Delete passed.");
                }
            }
            else if ((stage == 2 || stage == 4) && scene == "Map_1F")
            {
                var controls = UnityEngine.Object.FindFirstObjectByType<MapSaveControls>();
                var feedback = UnityEngine.Object.FindFirstObjectByType<MapPlayerPositionFeedback>();
                if (controls == null || controls.SaveButton == null || feedback == null || feedback.CurrentNode == null || !RunSaveService.CanSave) return;
                Require(MapRunState.RunId == SessionState.GetString(Key + "RunId", "") && feedback.CurrentNode.NodeID == 5 &&
                    MapRunState.Packets.Balance == 26 && MapRunState.Progression.Experience == 12 &&
                    MapRunState.Caches.HasPurchased("cache_hp") && MapRunState.PartyIds.Count == 3 &&
                    MapRunState.Capture(RunSaveBindings.Instance).completedNodeIds.Contains(5), "Continue lost cycle state.");
                if (stage == 2)
                {
                    SessionState.SetString(Key + "File", File.ReadAllText(repository.FilePath));
                    UnityEngine.Object.FindFirstObjectByType<MapBattleSceneLoader>().LoadBattle(feedback.CurrentNode);
                    Require(!RunSaveService.Save(out RunSaveError blocked) && blocked == RunSaveError.NotAllowed,
                        "Save allowed during battle transition.");
                    SessionState.SetInt(Key + "Stage", 3);
                }
                else
                {
                    controls.LobbyButton.onClick.Invoke();
                    SessionState.SetInt(Key + "Stage", 5);
                }
            }
            else if (stage == 3 && scene == "Playing_Scene")
            {
                var fade = UnityEngine.Object.FindFirstObjectByType<BattleSceneFadeIn>();
                if (fade == null || !fade.IsFadeComplete) return;
                Require(!RunSaveService.Save(out RunSaveError blocked) && blocked == RunSaveError.NotAllowed &&
                    File.ReadAllText(repository.FilePath) == SessionState.GetString(Key + "File", ""), "Battle save changed slot.");
                var operators = UnityEngine.Object.FindObjectsByType<Operator>(FindObjectsSortMode.None);
                Require(operators.Length == 3 && operators.All(op => op.IsCombatActive && op.CurrentHP == op.BaseMaxHP + 100 &&
                    MapRunState.PartyIds.Contains(op.Data.DataId)), "Restored roster/Cache did not apply to battle.");
                operators[0].DebugDamage(int.MaxValue);
                Require(operators[0].IsRetreated, "Retreat fixture failed.");
                UnityEngine.Object.FindFirstObjectByType<BattleToMapSceneLoader>().LoadMap();
                SessionState.SetInt(Key + "Stage", 4);
            }
        }
        catch (Exception error) { Finish(false, error.ToString()); }
    }

    private static object Field(object owner, string name) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    private static void Click(object owner, string name) => ((Button)Field(owner, name)).onClick.Invoke();
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Finish(bool passed, string message)
    {
        SessionState.SetBool(Key + "Running", false);
        repository?.TryDelete(out _);
        string directory = SessionState.GetString(Key + "Directory", "");
        if (Directory.Exists(directory)) Directory.Delete(directory, false);
        isolation?.Dispose();
        if (passed) Debug.Log("[Run Save Play Verification] " + message);
        else Debug.LogError("[Run Save Play Verification] " + message);
        EditorApplication.Exit(passed ? 0 : 1);
    }
}
