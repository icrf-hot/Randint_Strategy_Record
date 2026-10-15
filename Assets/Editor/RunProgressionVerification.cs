using System;
using UnityEditor;
using UnityEngine;

public static class RunProgressionVerification
{
    public static void Run()
    {
        PacketWalletVerification.Run();
        RunProgressionSettings asset = AssetDatabase.LoadAssetAtPath<RunProgressionSettings>(
            "Assets/Shop/Data/RunProgression.asset");
        Require(asset != null && asset.IsValid, "Progression settings asset is invalid.");
        Require(asset.InitialPackets == 30 && asset.VictoryPackets == 8 &&
                asset.VictoryExperience == 12 && asset.LevelUpPackets == 10, "Configured rewards differ from user decisions.");
        int[] thresholds = { 16, 24, 36, 54, 81, 122, 183, 274 };
        for (int i = 0; i < thresholds.Length; i++)
            Require(asset.RequiredExperience(i + 1) == thresholds[i], "Compound threshold or integer rounding is incorrect.");

        RunProgression run = new RunProgression();
        Require(run.Start(asset) && run.Packets.Balance == 30 && run.Layer == 1 && run.Experience == 0,
            "Initial run state is incorrect.");
        Require(run.TryGrantVictory(out int firstPackets, out int firstLayers) && firstPackets == 8 && firstLayers == 0 &&
                run.Packets.Balance == 38 && run.Layer == 1 && run.Experience == 12, "First victory reward is incorrect.");
        Require(run.TryGrantVictory(out int secondPackets, out int secondLayers) && secondPackets == 18 && secondLayers == 1 &&
                run.Packets.Balance == 56 && run.Layer == 2 && run.Experience == 8, "Level-up Packet reward or experience carry failed.");
        Require(run.Start(asset) && run.Packets.Balance == 30 && run.Layer == 1 && run.Experience == 0, "New-run reset failed.");
        Require(run.Packets.TryAdd(int.MaxValue - run.Packets.Balance), "Overflow fixture failed.");
        Require(!run.TryGrantVictory(out _, out _) && run.Packets.Balance == int.MaxValue && run.Layer == 1 && run.Experience == 0,
            "Overflow partially applied a victory reward.");

        RunProgressionSettings testSettings = UnityEngine.Object.Instantiate(asset);
        try
        {
            SerializedObject serialized = new SerializedObject(testSettings);
            serialized.FindProperty("definitionId").stringValue = "";
            serialized.FindProperty("victoryPackets").intValue = 3;
            serialized.FindProperty("victoryExperience").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(run.Start(testSettings) && run.TryGrantVictory(out int rounded, out _, 50, 50) &&
                rounded == 5 && run.Experience == 2, "Half values must round away from zero.");
            serialized.FindProperty("victoryPackets").intValue = 8;
            serialized.FindProperty("victoryExperience").intValue = 100;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(run.Start(testSettings) && run.TryGrantVictory(out int reward, out int levels) &&
                    levels == 3 && reward == 38 && run.Layer == 4 && run.Experience == 24 && run.Packets.Balance == 68,
                "Multiple Layer gains did not pay each Layer or carry experience.");
            serialized.FindProperty("victoryExperience").intValue = 16;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(run.Start(testSettings) && run.TryGrantVictory(out _, out _) && run.Layer == 2 && run.Experience == 0,
                "Exact threshold did not level up.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(testSettings);
        }

        try
        {
            Require(MapRunState.StartNewRun(asset), "Run context initialization failed.");
            MapRunState.Progression.TryGrantVictory(out _, out _);
            MapRunState.SaveCurrentNode(5, "Fixture");
            Require(MapRunState.EnsureStarted(asset) && MapRunState.Packets.Balance == 38 &&
                    MapRunState.Progression.Experience == 12, "Scene initialization granted initial Packet twice or cleared progression.");
            Require(MapRunState.StartNewRun(asset) && MapRunState.Packets.Balance == 30 &&
                    MapRunState.Progression.Layer == 1 && !MapRunState.HasCurrentNode, "New run did not clear prior state.");
        }
        finally
        {
            MapRunState.Reset();
        }

        Debug.Log("[Run Progression Verification] Initial Packet, victory rewards, thresholds, carry, multiple Layer gains, overflow and reset passed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
