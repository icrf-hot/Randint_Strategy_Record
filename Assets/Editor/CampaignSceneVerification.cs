using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Edit Mode asset/reference and save-routing checks. Does not simulate gameplay.</summary>
public static class CampaignSceneVerification
{
    public static string Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
        for(int i=0;i<SceneManager.sceneCount;i++)Require(!SceneManager.GetSceneAt(i).isDirty,"Save scene edits first.");
        var setup=EditorSceneManager.GetSceneManagerSetup();var bindings=RunSaveBindings.Instance;
        var original=MapRunState.Progression.IsInitialized && MapRunState.HasCurrentNode && !MapRunState.IsEnded?MapRunState.Capture(bindings):null;
        var report=new List<string>();
        string folder=Path.Combine(Path.GetTempPath(),"RSR_FloorScenes_"+Guid.NewGuid().ToString("N"));
        var repo=new RunSaveRepository(folder,bindings);
        try {
            var builds=EditorBuildSettings.scenes;
            string[] prefix={"Title","Map_1F","Playing_Scene","Shop","Lobby","Preparation"};
            Require(builds.Length>=10 && prefix.Select((s,i)=>Path.GetFileNameWithoutExtension(builds[i].path)==s).All(v=>v),"Original build indices changed.");
            foreach(var entry in CampaignScenes.Floors) {
                Require(builds.Any(b=>b.enabled && b.path==entry.scenePath),"Map not enabled in Build Settings.");
                var scene=EditorSceneManager.OpenScene(entry.scenePath);
                bindings=AssetDatabase.LoadAssetAtPath<RunSaveBindings>("Assets/Resources/RunSaveBindings.asset");
                repo=new RunSaveRepository(folder,bindings);
                var nodes=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapNode>(true)).ToArray();
                int expected=entry.floor==1?8:(int)Math.Pow(CampaignDefinition.Active.Floor(entry.floor).size,3);
                Require(nodes.Length==expected && nodes.All(n=>n.gameObject.activeInHierarchy),"Missing/hidden nodes.");
                Require(nodes.Select(n=>n.NodeID).Distinct().Count()==expected && nodes.All(n=>CampaignDefinition.FloorOf(n.NodeID)==entry.floor),"Invalid node IDs.");
                Require(nodes.All(n=>n.ConnectedNodes!=null && n.ConnectedNodes.All(next=>next!=null && nodes.Contains(next))),"Broken/cross-scene connection.");
                var flows=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CampaignMapFlow>(true)).ToArray();
                Require(flows.Length==1 && flows[0].FloorNumber==entry.floor,"Wrong floor controller.");
                var feedback=UnityEngine.Object.FindFirstObjectByType<MapPlayerPositionFeedback>();
                var start=(MapNode)new SerializedObject(feedback).FindProperty("startNode").objectReferenceValue;
                Require(start!=null && start.NodeID==CampaignDefinition.NodeId(entry.floor,0),"Start node reference not replaced.");
                foreach(var go in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go)==0,"Missing script.");
                if(entry.floor>1) {
                    Require(nodes.All(n=>n.IsGenerated && !string.IsNullOrEmpty(n.DisplayName) && !string.IsNullOrEmpty(n.Description)),"Authored text was not persisted.");
                    var edges=UnityEngine.Object.FindFirstObjectByType<CampaignMapConnections>();
                    Require(edges!=null && edges.GetComponent<MeshFilter>().sharedMesh!=null &&
                        edges.GetComponent<MeshFilter>().sharedMesh.vertexCount==nodes.Sum(n=>n.ConnectedNodes.Length)*2,"Authored connection rendering missing.");
                }
                Require(nodes.Where(n=>n.NodeType==MapNodeType.Event).All(n=>n.Choices.Length==1 && n.Choices[0].ChoiceText==EncounterService.Text("enter")),"Encounter entry not serialized.");
                MapRunState.StartNewRun(bindings.Progression);Require(MapRunState.EnterMapScene(entry.floor),"Direct-scene initialization failed.");
                var save=MapRunState.Capture(bindings);
                Require(save.schemaVersion==5 && save.mapScenePath==entry.scenePath && SceneManager.GetActiveScene().path==bindings.MapScenePath,"Floor scene is not saveable.");
                Require(repo.TryWrite(save,out _) && repo.TryRead(out _,out _),"Floor scene save round trip.");
                foreach(int version in entry.floor==1?new[]{1,2,3,4}:new[]{3,4}) {
                    var legacy=JsonUtility.FromJson<RunSaveSnapshot>(JsonUtility.ToJson(save));legacy.schemaVersion=version;legacy.mapScenePath=CampaignScenes.LegacyMapPath;
                    Require(PreparedRunRestore.TryPrepare(legacy,bindings,out var prepared,out _) && prepared.MapScenePath==entry.scenePath,"Legacy destination migration.");
                    prepared.Apply();Require(MapRunState.MapFloor==entry.floor && MapRunState.CurrentNodeID==start.NodeID,"Migration changed floor/node.");
                }
                var wrong=JsonUtility.FromJson<RunSaveSnapshot>(JsonUtility.ToJson(save));wrong.mapScenePath=CampaignScenes.PathFor(entry.floor==1?2:1);
                Require(!PreparedRunRestore.TryPrepare(wrong,bindings,out _,out _),"v5 floor/scene mismatch accepted.");
                Require(ShopSceneRouter.MapPath==entry.scenePath && bindings.MapScenePath==entry.scenePath,"Return route mismatch.");
                Require(!MapRunState.EnterMapScene(entry.floor==1?2:1),"An existing run silently changed floor.");
                report.Add($"Floor {entry.floor}: {nodes.Length} persisted nodes, internal links/start/controller/text/build registration, v5 save and legacy migration PASS.");
            }
        } finally {
            repo.TryDelete(out _);if(Directory.Exists(folder))Directory.Delete(folder,false);
            MapRunState.Reset();if(original!=null && PreparedRunRestore.TryPrepare(original,bindings,out var previous,out _))previous.Apply();
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
        string result=string.Join("\n",report);Debug.Log("[Campaign scene verification] "+result);return result;
    }
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
