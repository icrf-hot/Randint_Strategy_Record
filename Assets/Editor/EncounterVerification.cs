using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class EncounterVerification
{
    public static int EventNode(int floor) => floor==1?2:Enumerable.Range(0,(int)Math.Pow(CampaignDefinition.Active.Floor(floor).size,3))
        .Where(i=>CampaignDefinition.TypeAt(CampaignDefinition.Active.Floor(floor),i)==MapNodeType.Event)
        .Select(i=>CampaignDefinition.NodeId(floor,i)).First();

    public static EncounterRecord Fixture(int floor,string runId=null)
    {
        var bindings=RunSaveBindings.Instance;
        MapRunState.StartNewRun(bindings.Progression);MapRunState.SaveCurrentNode(EventNode(floor),"Encounter fixture");
        var snapshot=MapRunState.Capture(bindings);snapshot.mapFloor=floor;
        if(runId!=null)snapshot.runId=runId;
        Require(PreparedRunRestore.TryPrepare(snapshot,bindings,out var ready,out _),"Fixture restore");ready.Apply();
        return MapRunState.Encounters.Assign(MapRunState.RunId,snapshot.currentNodeId,bindings);
    }
    public static string Run()
    {
        if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
        var bindings=RunSaveBindings.Instance;
        var original=MapRunState.Progression.IsInitialized && MapRunState.HasCurrentNode && !MapRunState.IsEnded?MapRunState.Capture(bindings):null;
        var report=new List<string>();
        string directory=Path.Combine(Path.GetTempPath(),"RSR_Encounter_"+Guid.NewGuid().ToString("N"));
        var repository=new RunSaveRepository(directory,bindings);
        try {
            for(int floor=1;floor<=5;floor++) {
                var tier=EncounterCatalog.Active.tiers[floor-1];
                for(int seed=0;seed<500;seed++) {
                    string id=seed.ToString("x32");
                    var record=new EncounterRecord{nodeId=EventNode(floor),tier=tier.Copy()};
                    EncounterJournal.BuildHand(id,record);int solution=EncounterJournal.Solution(record);
                    Require(solution>=0 && EncounterJournal.ValidProof(record,solution),"Unsolvable hand");
                    var copy=record.Copy();EncounterJournal.BuildHand(id,copy);
                    Require(record.hand.SequenceEqual(copy.hand) && record.targetSum==copy.targetSum,"Unstable hand");
                    if(floor==1)Require(!record.hand.Contains(0),"Tutorial contains JK");
                }
                var r=Fixture(floor);int before=MapRunState.Packets.Balance;
                Require(!EncounterService.Execute(r,EncounterAction.Verify,out _) && before==MapRunState.Packets.Balance,"Invalid proof charged");
                r.selectedMask=EncounterJournal.Solution(r);
                Require(EncounterService.Execute(r,EncounterAction.Verify,out _),"Valid proof rejected");
                Require(MapRunState.Packets.Balance==before-tier.verifyCost && r.stage==EncounterStage.Verified,"Verification cost/state");
                Require(!EncounterService.Execute(r,EncounterAction.Verify,out _) && !EncounterService.Execute(r,EncounterAction.Leave,out _),"Verified request can be charged/abandoned");
                var verified=MapRunState.Capture(bindings);
                Require(repository.TryWrite(verified,out _) && repository.TryRead(out _,out _),"Verified file round trip");
                Require(PreparedRunRestore.TryPrepare(verified,bindings,out var restore,out _),"Verified save rejected");restore.Apply();
                r=MapRunState.Encounters.Find(r.nodeId);
                Require(r.stage==EncounterStage.Verified && EncounterService.Execute(r,EncounterAction.Claim,out _),"Verified resume claim");
                Require(MapRunState.Packets.Balance==before-tier.verifyCost+tier.signaturePackets && MapRunState.IsCompleted(r.nodeId),"Reward amount/completion");
                Require(!EncounterService.Execute(r,EncounterAction.Claim,out _),"Duplicate claim");
                Require(PreparedRunRestore.TryPrepare(MapRunState.Capture(bindings),bindings,out _,out _),"Resolved save rejected");
                foreach(Action<EncounterRecord> corrupt in new Action<EncounterRecord>[] {
                    x=>x.hand[0]=999,x=>x.targetSum++,x=>x.stage=EncounterStage.Resolved,x=>x.selectedMask=0,x=>x.tier.forgeCost=-1,x=>x.order=0,x=>x.packetDelta--
                }) {
                    var invalid=JsonUtility.FromJson<RunSaveSnapshot>(JsonUtility.ToJson(verified));corrupt(invalid.encounters[0]);
                    Require(!PreparedRunRestore.TryPrepare(invalid,bindings,out _,out _),"Corrupt journal accepted");
                }
                report.Add($"Floor {floor}: 500 solvable/stable hands, cost {tier.verifyCost}, guaranteed reward {tier.signaturePackets}; verified/resolved save and duplicate guards PASS.");
            }
            // An encountered node keeps its event, hand and tier even after other nodes are assigned.
            var first=Fixture(1);var journal=MapRunState.Encounters;
            var second=journal.Assign(MapRunState.RunId,3,bindings);var third=journal.Assign(MapRunState.RunId,4,bindings);
            Require(first.eventId=="signature_request" && second.eventId!="signature_request" && third.eventId!="signature_request","Signature repeats on same floor");
            Require(ReferenceEquals(first,journal.Assign(MapRunState.RunId,2,bindings)),"Assignment replaced");
            var assigned=MapRunState.Capture(bindings);
            // Journal entries must only describe visited nodes.
            MapRunState.SaveCurrentNode(3,"visited");MapRunState.SaveCurrentNode(4,"visited");
            Require(PreparedRunRestore.TryPrepare(MapRunState.Capture(bindings),bindings,out _,out _),"Multiple event ledger rejected");
            for(int version=1;version<=3;version++) {
                var legacy=MapRunState.Capture(bindings);legacy.schemaVersion=version;legacy.encounters=null;
                Require(PreparedRunRestore.TryPrepare(legacy,bindings,out var migrated,out _),"Legacy migration");migrated.Apply();
                Require(MapRunState.Encounters.Export().Length==0 && MapRunState.Capture(bindings).schemaVersion==4,"Legacy defaults");
            }
            EncounterRecord joker=null;
            for(int seed=0;seed<500 && joker==null;seed++) {
                var candidate=Fixture(5,seed.ToString("x32"));if(candidate.hand.Contains(0))joker=candidate;
            }
            Require(joker!=null,"No JK fixture");joker.selectedMask=32;
            MapRunState.RecordHealth.Restore(7,0);
            Require(!EncounterService.Execute(joker,EncounterAction.Forge,out _) && MapRunState.RecordHealth.Integrity==7,"Fatal overflow allowed");
            MapRunState.RecordHealth.Restore(50,3);int packet=MapRunState.Packets.Balance;
            Require(EncounterService.Execute(joker,EncounterAction.Forge,out _) && MapRunState.RecordHealth.Integrity==45 &&
                MapRunState.RecordHealth.Consistency==0 && MapRunState.Packets.Balance==packet+24,"JK overflow/reward");
            Require(!EncounterService.Execute(joker,EncounterAction.Forge,out _),"JK paid twice");
            var poor=Fixture(5);poor.selectedMask=EncounterJournal.Solution(poor);MapRunState.Packets.TrySpend(MapRunState.Packets.Balance);
            Require(!EncounterService.Execute(poor,EncounterAction.Verify,out _) && poor.stage==EncounterStage.Assigned,"Insufficient funds spent");
            MapRunState.Packets.TryAdd(int.MaxValue);poor.selectedMask=0;
            Require(EncounterService.Execute(poor,EncounterAction.Leave,out _),"Free exit blocked");
            report.Add("Per-floor signature assignment, v1/v2/v3 migration, JK consistency overflow/fatal guard, insufficient funds and free exit PASS.");
        } finally {
            repository.TryDelete(out _);if(Directory.Exists(directory))Directory.Delete(directory,false);
            MapRunState.Reset();if(original!=null && PreparedRunRestore.TryPrepare(original,bindings,out var previous,out _))previous.Apply();
        }
        string result=string.Join("\n",report);Debug.Log("[Encounter verification] "+result);return result;
    }
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
