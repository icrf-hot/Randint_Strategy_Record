using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class RunRecordPlayVerification
{
    private const string Key = "RSR.RecordPlay.";
    private static RunSaveRepository repository;
    private static IDisposable isolation;

    static RunRecordPlayVerification()
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
        SessionState.SetString(Key + "Directory", Path.Combine(Path.GetTempPath(), "RSR_RecordPlay_" + Guid.NewGuid().ToString("N")));
        SessionState.SetString(Key + "Deadline", DateTime.UtcNow.AddSeconds(150).ToString("O"));
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
            Require(DateTime.UtcNow < DateTime.Parse(SessionState.GetString(Key + "Deadline", "")).ToUniversalTime(), "Record scenario timed out.");
            if (!EditorApplication.isPlaying) return;
            Require(SessionState.GetString(Key + "Error", "") == "", SessionState.GetString(Key + "Error", ""));
            int stage = SessionState.GetInt(Key + "Stage", 0);
            string scene = SceneManager.GetActiveScene().name;
            if ((stage == 0 || stage == 2 || stage == 5) && scene == "Map_1F")
            {
                if (!RunSaveService.CanSave) return;
                var feedback = UnityEngine.Object.FindFirstObjectByType<MapPlayerPositionFeedback>();
                if (feedback == null || feedback.CurrentNode == null) return;
                if (stage == 5)
                {
                    Require(MapRunState.RecordHealth.Integrity == 100 && MapRunState.RecordHealth.Consistency == 100 &&
                        !MapRunState.IsEnded, "New game retained ejected health.");
                    CheckLabels(100, 100);
                    Finish(true, "Map status -> three-survivor defeat (70/80) -> next battle fresh allies -> zero integrity -> Lobby/continue blocked -> new game reset passed.");
                    return;
                }
                if (stage == 0)
                {
                    CheckLabels(100, 100);
                    Require(RunRecordService.ReduceConsistency(20), "Consistency API failed.");
                    feedback.MoveToNode(UnityEngine.Object.FindObjectsByType<MapNode>(FindObjectsSortMode.None).Single(node => node.NodeID == 2));
                    feedback.MoveToNode(UnityEngine.Object.FindObjectsByType<MapNode>(FindObjectsSortMode.None).Single(node => node.NodeID == 5));
                }
                else
                {
                    Require(MapRunState.RecordHealth.Integrity == 70 && MapRunState.RecordHealth.Consistency == 80 &&
                        !MapRunState.IsEnded && MapRunState.LastBattleResult == "Defeat", "Nonterminal defeat was incorrect.");
                    CheckLabels(70, 80);
                    Require(RunRecordService.ReduceIntegrity(50), "Second battle health fixture failed.");
                }
                Require(RunSaveService.Save(out _), "Map save failed.");
                Require(feedback.CurrentNode.NodeID == 5, "Fixture did not reach the battle node.");
                Require(repository.TryRead(out var saved, out _) && saved.consistency == 80 &&
                    saved.integrity == (stage == 0 ? 100 : 20), "Saved health is incorrect.");
                UnityEngine.Object.FindFirstObjectByType<MapBattleSceneLoader>().LoadBattle(feedback.CurrentNode);
                SessionState.SetInt(Key + "Stage", stage + 1);
            }
            else if ((stage == 1 || stage == 3) && scene == "Playing_Scene")
            {
                var fade = UnityEngine.Object.FindFirstObjectByType<BattleSceneFadeIn>();
                Enemy[] enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
                if (fade == null || !fade.IsFadeComplete || enemies.Length != 4) return;
                Operator[] allies = UnityEngine.Object.FindObjectsByType<Operator>(FindObjectsSortMode.None);
                Require(allies.Length == 3 && allies.All(op => op.IsCombatActive && op.CurrentHP == op.MaxHP),
                    "Retreat/HP incorrectly carried to next battle.");
                if (stage == 1) enemies[0].TakePhysicalDamage(enemies[0].CurrentHP + enemies[0].Defense);
                foreach (Operator op in allies) op.DebugDamage(int.MaxValue);
                BattleExecuteManager manager = BattleExecuteManager.Instance;
                Require((bool)Invoke(manager, "LoadOperators"), "Operator lookup failed.");
                manager.StartCoroutine((IEnumerator)Invoke(manager, "ExecuteBattleRoutine"));
                SessionState.SetInt(Key + "Stage", stage + 1);
            }
            else if (stage == 4 && scene == "Lobby")
            {
                Require(MapRunState.IsEnded && MapRunState.EndReason == "Ejected" &&
                    MapRunState.RecordHealth.Integrity == 0 && MapRunState.RecordHealth.Consistency == 80,
                    "Terminal battle did not eject with integrity zero.");
                Require(!repository.TryRead(out _, out RunSaveError error) && error == RunSaveError.RunEnded,
                    "Old saved run can continue after ejection.");
                var lobby = UnityEngine.Object.FindFirstObjectByType<LobbySceneController>();
                if (lobby == null) return;
                ((Button)Field(lobby, "continueButton")).onClick.Invoke();
                var slot = ((GameObject)Field(lobby, "continueWindow")).GetComponent<SaveSlotWindow>();
                Require(!slot.ResumeButton.interactable && slot.DeleteButton.interactable, "Ended slot actions are incorrect.");
                lobby.CloseWindows();
                ((Button)Field(lobby, "newGameButton")).onClick.Invoke();
                SessionState.SetInt(Key + "Stage", 5);
            }
        }
        catch (Exception error) { Finish(false, error.ToString()); }
    }

    private static void CheckLabels(int integrity, int consistency)
    {
        var texts = UnityEngine.Object.FindObjectsByType<RunRecordStatusText>(FindObjectsSortMode.None);
        Require(texts.Length == 2 &&
            texts.Any(text => text.GetComponent<TMP_Text>().text == "Integrity " + integrity + "%") &&
            texts.Any(text => text.GetComponent<TMP_Text>().text == "Consistency " + consistency + "%"),
            "Map TMP values are missing or incorrect.");
    }

    private static object Field(object owner, string name) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    private static object Invoke(object owner, string name) => owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, null);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Finish(bool passed, string message)
    {
        SessionState.SetBool(Key + "Running", false);
        repository?.TryDelete(out _);
        string directory = SessionState.GetString(Key + "Directory", "");
        if (Directory.Exists(directory)) Directory.Delete(directory, false);
        isolation?.Dispose();
        if (passed) Debug.Log("[Run Record Play Verification] " + message);
        else Debug.LogError("[Run Record Play Verification] " + message);
        EditorApplication.Exit(passed ? 0 : 1);
    }
}
