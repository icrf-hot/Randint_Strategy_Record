using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class FrontEndMergeVerification
{
    private const string Key = "RSR.FrontEndMerge.";
    static FrontEndMergeVerification()
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
        SessionState.SetFloat(Key + "Wait", -1);
        SessionState.SetString(Key + "Error", "");
        SessionState.SetString(Key + "Deadline", DateTime.UtcNow.AddSeconds(100).ToString("O"));
        EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
        EditorApplication.EnterPlaymode();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Seed()
    {
        if (!SessionState.GetBool(Key + "Running", false)) return;
        MapRunState.StartNewRun(AssetDatabase.LoadAssetAtPath<RunProgressionSettings>("Assets/Shop/Data/RunProgression.asset"));
        MapRunState.TryPurchaseCache(AssetDatabase.LoadAssetAtPath<ShopCatalogData>("Assets/Shop/Data/PrototypeShopCatalog.asset"), "cache_hp");
        MapRunState.Progression.TryGrantVictory(out _, out _);
        MapRunState.SaveCurrentNode(7, "Previous run");
        MapRunState.BeginMapEntry();
        Time.timeScale = 0.2f;
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key + "Running", false)) return;
        try
        {
            Require(DateTime.UtcNow < DateTime.Parse(SessionState.GetString(Key + "Deadline", "")).ToUniversalTime(), "Front end timed out.");
            if (!EditorApplication.isPlaying) return;
            Require(SessionState.GetString(Key + "Error", "") == "", SessionState.GetString(Key + "Error", ""));
            int stage = SessionState.GetInt(Key + "Stage", 0);
            string scene = SceneManager.GetActiveScene().name;
            if (stage == 0 && scene == "Title")
            {
                TitleSceneController title = UnityEngine.Object.FindFirstObjectByType<TitleSceneController>();
                if (title == null) return;
                float wait = SessionState.GetFloat(Key + "Wait", -1);
                if (wait < 0) { SessionState.SetFloat(Key + "Wait", Time.realtimeSinceStartup + 0.4f); return; }
                if (Time.realtimeSinceStartup < wait) return;
                Require(MapRunState.Caches.HasPurchased("cache_hp"), "Title reset previous run before New Game.");
                Click(title, "enterButton");
                Click(title, "enterButton");
                SessionState.SetInt(Key + "Stage", 1);
            }
            else if (stage == 1 && scene == "Lobby")
            {
                LobbySceneController lobby = UnityEngine.Object.FindFirstObjectByType<LobbySceneController>();
                if (lobby == null) return;
                Require(MapRunState.CurrentNodeID == 7 && MapRunState.Caches.HasPurchased("cache_hp"), "Lobby changed prior run.");
                lobby.OpenSettings();
                lobby.StartNewGame();
                Require(lobby.IsModalOpen && MapRunState.Caches.HasPurchased("cache_hp"), "Modal allowed New Game.");
                lobby.CloseWindows();
                lobby.OpenContinue();
                Require(lobby.IsModalOpen, "Continue placeholder did not open.");
                lobby.CloseWindows();
                Click(lobby, "newGameButton");
                SessionState.SetInt(Key + "Stage", 2);
            }
            else if (stage == 2 && scene == "Map_1F")
            {
                var map = UnityEngine.Object.FindFirstObjectByType<MapPlayerPositionFeedback>();
                if (map == null || map.CurrentNode == null) return;
                Require(map.CurrentNode.NodeID == 1 && MapRunState.Packets.Balance == 30 &&
                    MapRunState.Progression.Layer == 1 && MapRunState.Progression.Experience == 0 &&
                    MapRunState.Caches.Owned.Count == 0 && !MapRunState.IsEnteringMapFromBattle && Time.timeScale == 1,
                    "New Game did not reset complete run state.");
                Finish(true, "Title stays visible -> Lobby/modals -> explicit New Game -> Map reset passed.");
            }
        }
        catch (Exception e) { Finish(false, e.ToString()); }
    }

    private static void Click(object owner, string name) => ((Button)owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner)).onClick.Invoke();
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    private static void Finish(bool ok, string message)
    {
        SessionState.SetBool(Key + "Running", false);
        if (ok) Debug.Log("[Front End Merge Verification] " + message);
        else Debug.LogError("[Front End Merge Verification] " + message);
        EditorApplication.Exit(ok ? 0 : 1);
    }
}
