using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ShopSceneVerification
{
    private const string Key = "RSR.ShopVerify.";
    static ShopSceneVerification()
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
        SessionState.SetString(Key + "Deadline", DateTime.UtcNow.AddSeconds(100).ToString("O"));
        EditorSceneManager.OpenScene(ShopSceneRouter.MapPath);
        EditorApplication.EnterPlaymode();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Reset()
    {
        if (SessionState.GetBool(Key + "Running", false))
        {
            MapRunState.Reset();
            Time.timeScale = 1;
        }
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key + "Running", false)) return;
        try
        {
            Require(DateTime.UtcNow < DateTime.Parse(SessionState.GetString(Key + "Deadline", "" )).ToUniversalTime(), "Shop test timed out.");
            if (!EditorApplication.isPlaying) return;
            Require(string.IsNullOrEmpty(SessionState.GetString(Key + "Error", "")), SessionState.GetString(Key + "Error", ""));
            int stage = SessionState.GetInt(Key + "Stage", 0);
            string scene = SceneManager.GetActiveScene().name;
            if (stage == 0 && scene == "Map_1F")
            {
                MapPlayerPositionFeedback feedback = UnityEngine.Object.FindFirstObjectByType<MapPlayerPositionFeedback>();
                if (feedback == null || feedback.CurrentNode == null) return;
                MapNode[] nodes = UnityEngine.Object.FindObjectsByType<MapNode>(FindObjectsSortMode.None);
                feedback.MoveToNode(nodes.Single(node => node.NodeID == 2));
                feedback.MoveToNode(nodes.Single(node => node.NodeID == 5));
                new GameObject("ShopChoiceFixture").AddComponent<MapChoice>().Select(nodes.Single(node => node.NodeID == 8));
                Require(MapRunState.CurrentNodeID == 8, "Shop choice did not move to node 8.");
                SessionState.SetInt(Key + "Stage", 1);
            }
            else if ((stage == 1 || stage == 3) && scene == "Shop")
            {
                ShopSceneUI ui = UnityEngine.Object.FindFirstObjectByType<ShopSceneUI>();
                if (ui == null || ui.ItemCount != 7 || ui.ReturnButton == null || ShopSceneRouter.IsTransitioning) return;
                Require(ui.BalanceText.font != null, "Shop TMP font is missing.");
                Canvas.ForceUpdateCanvases();
                Require(ui.GetBuyButton("cache_hp").GetComponent<RectTransform>().rect.width > 0, "Shop controls have no width.");
                if (stage == 1)
                {
                    ui.GetBuyButton("cache_hp").onClick.Invoke();
                    Require(MapRunState.Packets.Balance == 18 && MapRunState.Caches.HasPurchased("cache_hp"), "UI purchase did not charge 12 Packet.");
                    Require(!ui.GetBuyButton("cache_hp").interactable && !ui.GetBuyButton("cache_rewards").interactable,
                        "Sold/insufficient buttons remain enabled.");
                    Require(ui.Buy("cache_hp") == CachePurchaseResult.AlreadyPurchased &&
                        ui.Buy("cache_rewards") == CachePurchaseResult.InsufficientPackets && MapRunState.Packets.Balance == 18,
                        "Rejected UI purchase changed wallet.");
                    ui.ReturnButton.onClick.Invoke();
                    SessionState.SetInt(Key + "Stage", 2);
                }
                else
                {
                    Require(!ui.GetBuyButton("cache_hp").interactable && MapRunState.Caches.Owned.Count == 1 &&
                        MapRunState.Packets.Balance == 18, "Shop revisit lost purchase history or balance.");
                    Finish(true, "Map choice -> Shop UI purchase/rejections -> Map node 8 -> Shop revisit passed.");
                }
            }
            else if (stage == 2 && scene == "Map_1F")
            {
                MapPlayerPositionFeedback feedback = UnityEngine.Object.FindFirstObjectByType<MapPlayerPositionFeedback>();
                if (feedback == null || feedback.CurrentNode == null || ShopSceneRouter.IsTransitioning) return;
                Require(feedback.CurrentNode.NodeID == 8 && MapRunState.Caches.HasPurchased("cache_hp"), "Map return lost node or Cache.");
                Require(ShopSceneRouter.EnterShop(feedback.CurrentNode), "Shop revisit failed.");
                SessionState.SetInt(Key + "Stage", 3);
            }
        }
        catch (Exception error) { Finish(false, error.ToString()); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Finish(bool success, string message)
    {
        SessionState.SetBool(Key + "Running", false);
        if (success) Debug.Log("[Shop Scene Verification] " + message);
        else Debug.LogError("[Shop Scene Verification] " + message);
        EditorApplication.Exit(success ? 0 : 1);
    }
}
