using System;
using UnityEngine;

public static class PacketWalletVerification
{
    public static void Run()
    {
        PacketWallet wallet = new PacketWallet();
        Require(wallet.Balance == 0, "New wallet is not empty.");
        Require(!wallet.TryAdd(-1) && !wallet.TrySpend(-1), "Negative amounts were accepted.");
        Require(!wallet.TrySpend(1) && wallet.Balance == 0, "Insufficient spend changed the balance.");
        Require(wallet.TryAdd(25) && wallet.Balance == 25, "Packet credit failed.");
        Require(wallet.CanAfford(10) && wallet.TrySpend(10) && wallet.Balance == 15, "Purchase-sized spend failed.");
        Require(!wallet.TrySpend(16) && wallet.Balance == 15, "Failed spend was not atomic.");
        Require(wallet.TrySpend(15) && wallet.Balance == 0, "Exact spend failed.");
        Require(wallet.TryAdd(int.MaxValue), "Maximum credit failed.");
        Require(!wallet.TryAdd(1) && wallet.Balance == int.MaxValue, "Overflow was not rejected.");
        Require(!wallet.TryAdd(int.MinValue) && wallet.Balance == int.MaxValue, "Minimum integer was accepted.");
        Require(wallet.TrySpend(int.MaxValue) && wallet.Balance == 0, "Maximum spend failed.");
        Require(wallet.TrySpend(0) && wallet.TryAdd(0) && wallet.Balance == 0, "Zero amount changed balance.");

        try
        {
            MapRunState.Reset();
            Require(MapRunState.Packets.TryAdd(40), "Run wallet credit failed.");
            MapRunState.SaveCurrentNode(5, "Test node");
            MapRunState.BeginMapEntry();
            MapRunState.CompleteMapEntry();
            Require(MapRunState.Packets.Balance == 40, "Map transition state cleared Packet balance.");
            MapRunState.Reset();
            Require(MapRunState.Packets.Balance == 0 && !MapRunState.HasCurrentNode,
                "New-run reset did not clear Packet and node state.");
        }
        finally
        {
            MapRunState.Reset();
        }

        Debug.Log("[Packet Wallet Verification] Credit, spend, invalid amounts, overflow, run retention and reset passed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
