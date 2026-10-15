using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class TitleRouteVerification
{
    private const string RunningKey = "RSR.TitleRouteVerification.Running";
    private const string SeededKey = "RSR.TitleRouteVerification.Seeded";
    private const string DeadlineKey = "RSR.TitleRouteVerification.Deadline";

    static TitleRouteVerification()
    {
        EditorApplication.update += CheckResult;
    }

    // Run in batchmode without -quit; the verification exits Unity itself.
    public static void Run()
    {
        FrontEndMergeVerification.Run();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SeedPreviousRun()
    {
        if (!SessionState.GetBool(RunningKey, false))
            return;

        MapRunState.SaveCurrentNode(7, "Previous run");
        MapRunState.BeginMapEntry();
        Time.timeScale = 0.2f;
        SessionState.SetBool(SeededKey, true);
    }

    private static void CheckResult()
    {
        if (!SessionState.GetBool(RunningKey, false))
            return;

        if (DateTime.TryParse(SessionState.GetString(DeadlineKey, ""), out DateTime deadline) &&
            DateTime.UtcNow > deadline.ToUniversalTime())
        {
            Finish(false, "Title to map route timed out.");
            return;
        }

        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != "Map_1F")
            return;

        MapPlayerPositionFeedback feedback = UnityEngine.Object.FindFirstObjectByType<MapPlayerPositionFeedback>();
        if (feedback == null || feedback.CurrentNode == null)
            return;

        bool passed = SessionState.GetBool(SeededKey, false) &&
                      feedback.CurrentNode.NodeID == 1 &&
                      MapRunState.CurrentNodeID == 1 &&
                      !MapRunState.IsEnteringMapFromBattle &&
                      Mathf.Approximately(Time.timeScale, 1f) && MapRunState.Packets.Balance == 30 &&
                      MapRunState.Progression.Layer == 1 && MapRunState.Progression.Experience == 0;

        Finish(passed, passed
            ? "Title -> Map_1F passed: previous run reset, start node 1, time scale 1."
            : "Map opened but new-run state or time scale was incorrect.");
    }

    private static void Finish(bool passed, string message)
    {
        SessionState.SetBool(RunningKey, false);
        if (passed)
            Debug.Log("[Title Route Verification] " + message);
        else
            Debug.LogError("[Title Route Verification] " + message);

        EditorApplication.Exit(passed ? 0 : 1);
    }
}
