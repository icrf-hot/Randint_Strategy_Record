using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class MapBattleRoundTripVerification
{
    private const string Prefix = "RSR.MapBattleRoundTrip.";

    static MapBattleRoundTripVerification()
    {
        EditorApplication.update += CheckProgress;
        Application.logMessageReceived += RecordRuntimeError;
    }

    public static void Run()
    {
        SessionState.SetBool(Prefix + "Running", true);
        SessionState.SetInt(Prefix + "Stage", 0);
        SessionState.SetBool(Prefix + "SawEntryBlock", false);
        SessionState.SetBool(Prefix + "SawResult", false);
        SessionState.SetString(Prefix + "RuntimeError", "");
        SessionState.SetString(Prefix + "Deadline", DateTime.UtcNow.AddSeconds(120).ToString("O"));
        EditorSceneManager.OpenScene("Assets/Scenes/Map_1F.unity");
        EditorApplication.EnterPlaymode();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ResetRun()
    {
        if (!SessionState.GetBool(Prefix + "Running", false))
            return;

        MapRunState.Reset();
        Time.timeScale = 1f;
    }

    private static void CheckProgress()
    {
        if (!SessionState.GetBool(Prefix + "Running", false))
            return;

        if (DateTime.TryParse(SessionState.GetString(Prefix + "Deadline", ""), out DateTime deadline) &&
            DateTime.UtcNow > deadline.ToUniversalTime())
        {
            Finish(false, "Round trip timed out.");
            return;
        }

        if (!EditorApplication.isPlaying)
            return;

        string runtimeError = SessionState.GetString(Prefix + "RuntimeError", "");
        if (!string.IsNullOrEmpty(runtimeError))
        {
            Finish(false, "Runtime error: " + runtimeError);
            return;
        }

        try
        {
            Advance();
        }
        catch (Exception exception)
        {
            Finish(false, exception.ToString());
        }
    }

    private static void Advance()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        int stage = SessionState.GetInt(Prefix + "Stage", 0);

        if (stage == 0 && sceneName == "Map_1F")
        {
            MapPlayerPositionFeedback feedback = UnityEngine.Object.FindFirstObjectByType<MapPlayerPositionFeedback>();
            if (feedback == null || feedback.CurrentNode == null)
                return;

            MapNode[] nodes = UnityEngine.Object.FindObjectsByType<MapNode>(FindObjectsSortMode.None);
            feedback.MoveToNode(nodes.Single(node => node.NodeID == 2));
            feedback.MoveToNode(nodes.Single(node => node.NodeID == 5));
            if (feedback.CurrentNode.NodeID != 5)
                throw new InvalidOperationException("Could not follow start -> event 2 -> battle 5.");

            SessionState.SetInt(Prefix + "Stage", 1);
            UnityEngine.Object.FindFirstObjectByType<MapBattleSceneLoader>().LoadBattle(feedback.CurrentNode);
        }
        else if (stage == 1 && sceneName == "Playing_Scene")
        {
            BattleSceneFadeIn fade = UnityEngine.Object.FindFirstObjectByType<BattleSceneFadeIn>();
            if (fade == null || !fade.IsFadeComplete)
            {
                CanvasGroup entry = UnityEngine.Object.FindObjectsByType<CanvasGroup>(FindObjectsSortMode.None)
                    .FirstOrDefault(group => group.gameObject.name == "BattleEntryCanvas");
                if (entry != null && entry.blocksRaycasts && Mathf.Approximately(Time.timeScale, 0f))
                    SessionState.SetBool(Prefix + "SawEntryBlock", true);
                return;
            }

            Card[] cards = UnityEngine.Object.FindObjectsByType<Card>(FindObjectsSortMode.InstanceID);
            if (cards.Length != 6 || CardSpawner.Instance.IsDiscarding)
                return;

            BattleAccessManager access = BattleAccessManager.Instance;
            if (access == null || access.MiddleOperator == null)
                return;

            Card attributeCard = cards.FirstOrDefault(card => card.IsSpecialCard) ?? cards[1];
            Card[] numberCards = cards.Where(card => card != attributeCard && !card.IsSpecialCard).ToArray();
            if (numberCards.Length != 5)
                throw new InvalidOperationException("Expected five number cards and one attribute card.");
            foreach (Card card in numberCards)
                card.SetNumber(1);
            if (!attributeCard.IsSpecialCard)
                attributeCard.SetNumber(1);

            FrontCardSelectionManager.Instance.SelectCard(numberCards[0]);
            FrontCardSelectionManager.Instance.SetDefense();
            MiddleCardSelectionManager middle = MiddleCardSelectionManager.Instance;
            middle.SelectCard(attributeCard);
            middle.SelectCard(numberCards[1]);
            middle.SelectCard(numberCards[2]);
            middle.SetPhysicalMode();
            BackCardSelectionManager back = UnityEngine.Object.FindFirstObjectByType<BackCardSelectionManager>();
            back.SelectCard(numberCards[3]);
            back.SelectCard(numberCards[4]);

            Enemy[] enemies = UnityEngine.Object.FindObjectsByType<Enemy>(FindObjectsSortMode.InstanceID);
            if (enemies.Length == 0)
                throw new InvalidOperationException("No enemies spawned.");

            // Change runtime HP only, without editing prefab balance values.
            Enemy lastEnemy = enemies[0];
            for (int i = 1; i < enemies.Length; i++)
                enemies[i].TakePhysicalDamage(int.MaxValue);
            lastEnemy.TakePhysicalDamage(lastEnemy.CurrentHP + lastEnemy.Defense - 1);

            EnemyTargetingManager.Instance.BeginTargeting(access.MiddleOperator);
            EnemyTargetingManager.Instance.SelectTarget(lastEnemy.GetComponent<EnemyTargetable>());
            access.Refresh();
            if (!access.CanBattle)
                throw new InvalidOperationException("Battle approval failed after six cards and target selection.");

            SessionState.SetInt(Prefix + "Stage", 2);
            BattleExecuteManager.Instance.ExecuteBattle();
        }
        else if (stage == 2)
        {
            if (sceneName == "Playing_Scene")
            {
                BattleResultPresenter presenter = UnityEngine.Object.FindFirstObjectByType<BattleResultPresenter>();
                if (presenter != null)
                {
                    CanvasGroup result = (CanvasGroup)typeof(BattleResultPresenter)
                        .GetField("resultCanvasGroup", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(presenter);
                    if (result != null && result.alpha > 0.5f && result.blocksRaycasts)
                        SessionState.SetBool(Prefix + "SawResult", true);
                }
                return;
            }

            if (sceneName != "Map_1F")
                return;

            MapSceneFadeIn fade = UnityEngine.Object.FindFirstObjectByType<MapSceneFadeIn>();
            MapPlayerPositionFeedback feedback = UnityEngine.Object.FindFirstObjectByType<MapPlayerPositionFeedback>();
            if (fade == null || !fade.IsFadeComplete || feedback == null || feedback.CurrentNode == null)
                return;

            bool passed = SessionState.GetBool(Prefix + "SawEntryBlock", false) &&
                          SessionState.GetBool(Prefix + "SawResult", false) &&
                          feedback.CurrentNode.NodeID == 5 && MapRunState.CurrentNodeID == 5 &&
                          !MapRunState.IsEnteringMapFromBattle && Mathf.Approximately(Time.timeScale, 1f);
            Finish(passed, passed
                ? "Map -> battle -> victory -> map passed: entry blocking, six-card approval, result screen, node 5 restore, time scale 1."
                : "Round trip completed but input/result/position/time checks failed.");
        }
    }

    private static void RecordRuntimeError(string message, string stackTrace, LogType type)
    {
        if (EditorApplication.isPlaying && SessionState.GetBool(Prefix + "Running", false) &&
            (type == LogType.Exception || type == LogType.Error || type == LogType.Assert))
        {
            SessionState.SetString(Prefix + "RuntimeError", message);
        }
    }

    private static void Finish(bool passed, string message)
    {
        SessionState.SetBool(Prefix + "Running", false);
        if (passed)
            Debug.Log("[Map Battle Verification] " + message);
        else
            Debug.LogError("[Map Battle Verification] " + message);
        EditorApplication.Exit(passed ? 0 : 1);
    }
}
