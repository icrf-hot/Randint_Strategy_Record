using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class CampaignVerification
{
    public static string Run()
    {
        if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
        var bindings=RunSaveBindings.Instance;
        var report=new List<string>();
        foreach(var floor in CampaignDefinition.Active.floors) {
            int count=floor.size*floor.size*floor.size;
            var reached=new HashSet<int>{0};var queue=new Queue<int>();queue.Enqueue(0);int diagonal=0;
            while(queue.Count>0) {
                int index=queue.Dequeue();
                foreach(int next in CampaignDefinition.Connections(floor,index)) {
                    Require(next>=0 && next<count && next>index,"Invalid edge");
                    int n=floor.size;int sum(int i)=>i%n+i/n%n+i/(n*n);
                    Require(sum(next)>sum(index),"Backward edge");
                    if(sum(next)-sum(index)==2)diagonal++;
                    if(reached.Add(next))queue.Enqueue(next);
                }
            }
            Require(reached.Count==count && diagonal>0,"Disconnected or flat map");
            var types=Enumerable.Range(0,count).Select(i=>CampaignDefinition.TypeAt(floor,i)).ToArray();
            Require(types.Contains(MapNodeType.Battle) && types.Contains(MapNodeType.Event),"Missing battle/event variety");
            Require(types.Count(t=>t==MapNodeType.Shop)==floor.shopIndices.Length && floor.shopIndices.Length<=2,"Shop density");
            Require(types.Last()==(floor.floor==2?MapNodeType.Defender:floor.floor==3?MapNodeType.FirstBoss:floor.floor==5?MapNodeType.Boss:MapNodeType.FloorExit),"Endpoint");
            foreach(int i in Enumerable.Range(0,count))Require(bindings.HasNode(CampaignDefinition.NodeId(floor.floor,i)),"Save binding lacks node");
            report.Add($"Floor {floor.floor}: {count} reachable nodes, {diagonal} diagonal links, {floor.shopIndices.Length} shops.");
        }
        Require(!CampaignDefinition.HasNode(6001) && !CampaignDefinition.HasNode(7001),"Hidden floors unexpectedly implemented");
        var original=MapRunState.Progression.IsInitialized && MapRunState.HasCurrentNode && !MapRunState.IsEnded?MapRunState.Capture(bindings):null;
        string folder=Path.Combine(Path.GetTempPath(),"RSR_Campaign_"+Guid.NewGuid().ToString("N"));
        var repo=new RunSaveRepository(folder,bindings);
        try {
            MapRunState.StartNewRun(bindings.Progression);MapRunState.SaveCurrentNode(1,"start");
            foreach(int version in new[]{1,2}) {
                var legacy=MapRunState.Capture(bindings);legacy.schemaVersion=version;legacy.mapFloor=0;
                if(version==1)legacy.integrity=legacy.consistency=-1;
                Require(PreparedRunRestore.TryPrepare(legacy,bindings,out var restore,out _),"Legacy migration failed");
                restore.Apply();Require(MapRunState.MapFloor==1 && legacy.mapFloor==0,"Legacy migration mutated source");
            }
            MapRunState.SaveCurrentNode(8,"shop");MapRunState.CompleteNode(8);
            for(int floor=2;floor<=5;floor++) {
                Require(MapRunState.TryAdvanceMapFloor() && MapRunState.MapFloor==floor,"Floor advance");
                var f=CampaignDefinition.Active.Floor(floor);
                if(floor==2)Require(MapRunState.Caches.Grant(bindings.Cache("defender_relic")),"Relic grant");
                Require(!MapRunState.Caches.HasPurchased("defender_relic"),"Free relic recorded as purchase");
                var save=MapRunState.Capture(bindings);
                Require(repo.TryWrite(save,out _),"Floor save write");
                Require(repo.TryRead(out var loaded,out _),"Floor save read");
                Require(PreparedRunRestore.TryPrepare(loaded,bindings,out var restore,out _),"Floor save prepare");
                restore.Apply();Require(MapRunState.MapFloor==floor && MapRunState.Caches.Owned.Count==1,"Floor/relic restoration");
                var invalid=JsonUtility.FromJson<RunSaveSnapshot>(JsonUtility.ToJson(save));invalid.mapFloor=floor==5?6:floor+1;
                Require(!PreparedRunRestore.TryPrepare(invalid,bindings,out _,out _),"Floor/node mismatch accepted");
                Require(!MapRunState.TryAdvanceMapFloor(),"Advanced without terminal completion");
                MapRunState.SaveCurrentNode(CampaignDefinition.NodeId(floor,f.size*f.size*f.size-1),"terminal");MapRunState.CompleteNode(MapRunState.CurrentNodeID);
            }
            Require(!MapRunState.TryAdvanceMapFloor(),"Floor 6 is reachable");
            report.Add("v1/v2 migration, v3 floors 2-5 round-trip, one free relic, mismatched floor rejection and no floor 6: PASS.");
        } finally {
            repo.TryDelete(out _);if(Directory.Exists(folder))Directory.Delete(folder,false);
            MapRunState.Reset();if(original!=null && PreparedRunRestore.TryPrepare(original,bindings,out var previous,out _))previous.Apply();
        }
        Debug.Log("[Campaign verification] "+string.Join("\n",report));return string.Join("\n",report);
    }
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
