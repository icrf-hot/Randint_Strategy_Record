using System;
using System.Linq;
using Randint.Data;
using UnityEngine;

/// <summary>UI-independent encounter transactions. Selection never spends resources.</summary>
public static class EncounterService
{
    public static bool HasUnclaimedVerification => MapRunState.Encounters.Find(MapRunState.CurrentNodeID)?.stage==EncounterStage.Verified;
    public static string Text(string key, params object[] args) => string.Format(GameData.Text("encounter."+key), args);

    public static bool CanExecute(EncounterRecord r, EncounterAction action, out string reason)
    {
        reason="";
        if(r==null || MapRunState.IsEnded || TransitionManager.IsBusy || MapRunState.CurrentNodeID!=r.nodeId ||
            !ReferenceEquals(MapRunState.Encounters.Find(r.nodeId),r) || r.stage==EncounterStage.Resolved ||
            MapRunState.IsCompleted(r.nodeId) || !EncounterCatalog.Active.Find(r.eventId).actions.Contains(action.ToString()))
            return Fail("not_active",out reason);
        if(r.stage==EncounterStage.Verified ? action!=EncounterAction.Claim : action==EncounterAction.Claim)
            return Fail("not_active",out reason);
        if(action==EncounterAction.Verify && !EncounterJournal.ValidProof(r,r.selectedMask))return Fail("no_proof",out reason);
        if(action==EncounterAction.Forge && (r.hand==null || !Enumerable.Range(0,6).Any(i=>r.hand[i]==0 && (r.selectedMask&(1<<i))!=0)))
            return Fail("no_joker",out reason);
        Quote(r,action,out int cost,out int consistency,out int gain,out _,out _);
        if(MapRunState.Packets.Balance<cost)return Fail("no_packet",out reason);
        if((long)MapRunState.Packets.Balance-cost+gain>int.MaxValue)return Fail("capacity",out reason);
        if(MapRunState.RecordHealth.Integrity-Math.Max(0,consistency-MapRunState.RecordHealth.Consistency)<=0)
            return Fail("fatal",out reason);
        return true;
    }

    public static bool Execute(EncounterRecord r,EncounterAction action,out string reason)
    {
        if(!CanExecute(r,action,out reason))return false;
        Quote(r,action,out int cost,out int consistency,out int gain,out int heal,out bool healConsistency);
        int oldI=MapRunState.RecordHealth.Integrity,oldC=MapRunState.RecordHealth.Consistency;
        // No yields/callbacks between validation and publication. Apply cost before restoration.
        if(!MapRunState.Packets.TrySpend(cost))return Fail("no_packet",out reason);
        MapRunState.Packets.TryAdd(gain);
        int newI=oldI-Math.Max(0,consistency-oldC),newC=Math.Max(0,oldC-consistency);
        MapRunState.RecordHealth.Restore(Math.Min(100,newI+heal),Math.Min(100,newC+(healConsistency?heal:0)));
        r.packetDelta+=gain-cost;r.integrityDelta+=MapRunState.RecordHealth.Integrity-oldI;
        r.consistencyDelta+=MapRunState.RecordHealth.Consistency-oldC;r.action=action;
        if(action==EncounterAction.Verify)r.stage=EncounterStage.Verified;
        else {
            r.stage=EncounterStage.Resolved;
            r.outcome=OutcomeKey(action);
            MapRunState.CompleteNode(r.nodeId);
        }
        Debug.Log($"[Encounter] node={r.nodeId} floor={r.floor} event={r.eventId} action={action} stage={r.stage} Packet={MapRunState.Packets.Balance}");
        return true;
    }

    public static string OutcomeKey(EncounterAction action) => action==EncounterAction.Claim?"normal":action==EncounterAction.Forge?"forged":
        action==EncounterAction.Supply?"supply":action==EncounterAction.Repair?"repair":action==EncounterAction.RiskyPackets?"risk":"leave";

    public static void Quote(EncounterRecord r,EncounterAction action,out int cost,out int consistency,out int gain,out int heal,out bool healConsistency)
    {
        cost=consistency=gain=heal=0;healConsistency=false;var t=r.tier;
        switch(action) {
            case EncounterAction.Verify:cost=t.verifyCost;break;
            case EncounterAction.Claim:gain=t.signaturePackets;heal=t.signatureHeal;break;
            case EncounterAction.Forge:consistency=t.forgeCost;gain=t.signaturePackets;break;
            case EncounterAction.Supply:gain=t.supplyPackets;break;
            case EncounterAction.Repair:cost=t.repairCost;heal=t.repairAmount;healConsistency=true;break;
            case EncounterAction.RiskyPackets:consistency=t.riskCost;gain=t.riskPackets;break;
        }
    }
    private static bool Fail(string key,out string reason){reason=Text(key);return false;}
}
