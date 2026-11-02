using System;
using System.Collections.Generic;
using System.Linq;
using Randint.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Creates missing floor scenes once. Existing authored floors are never regenerated on load.</summary>
public static class CampaignSceneAuthoring
{
    [MenuItem("Tools/Randint/Campaign/Create Missing Floor Scenes")]
    public static void CreateMissingFloors()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene edits first.");
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try {
            var first=EditorSceneManager.OpenScene(CampaignScenes.PathFor(1));
            EnsureController(1);
            foreach(var node in Nodes(first).Where(n=>n.NodeType==MapNodeType.Event))ConfigureEntry(node);
            EditorSceneManager.SaveScene(first);
            foreach(var entry in CampaignScenes.Floors.Where(e=>e.floor>1)) {
                if(AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.scenePath)!=null)continue;
                if(CampaignDefinition.Active.Floor(entry.floor)==null)throw new InvalidOperationException("Define floor gameplay before authoring its scene.");
                if(!AssetDatabase.CopyAsset(CampaignScenes.PathFor(1),entry.scenePath))throw new InvalidOperationException("Scene copy failed: "+entry.scenePath);
                var scene=EditorSceneManager.OpenScene(entry.scenePath);BuildNodes(scene,entry.floor);EnsureController(entry.floor);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            var build=EditorBuildSettings.scenes.ToList();
            foreach(var entry in CampaignScenes.Floors) {
                int index=build.FindIndex(s=>s.path==entry.scenePath);
                if(index<0)build.Add(new EditorBuildSettingsScene(entry.scenePath,true));
                else if(!build[index].enabled)build[index]=new EditorBuildSettingsScene(entry.scenePath,true);
            }
            EditorBuildSettings.scenes=build.ToArray();
        } finally {EditorSceneManager.RestoreSceneManagerSetup(setup);}
        Debug.Log("[Campaign scenes] Independent floor scenes authored; existing floor scenes preserved.");
    }
    private static MapNode[] Nodes(Scene scene)=>scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapNode>(true)).ToArray();
    private static void EnsureController(int floor)
    {
        var flow=UnityEngine.Object.FindFirstObjectByType<CampaignMapFlow>() ?? new GameObject("CampaignMapFlow").AddComponent<CampaignMapFlow>();
        var data=new SerializedObject(flow);data.FindProperty("floorNumber").intValue=floor;data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void ConfigureEntry(MapNode node)
    {
        var choice=node.Choices[0];choice.Configure("encounter.enter");
        var data=new SerializedObject(node);var options=data.FindProperty("choices");options.arraySize=1;
        options.GetArrayElementAtIndex(0).objectReferenceValue=choice;
        // Floor 1 keeps its original localized labels and chess-piece visual classification.
        data.FindProperty("authored").boolValue=false;data.FindProperty("runtimeName").stringValue="";data.FindProperty("runtimeDescription").stringValue="";
        data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(node);
    }
    private static void BuildNodes(Scene scene,int floor)
    {
        var definition=CampaignDefinition.Active.Floor(floor);var originals=Nodes(scene);
        if(originals.Length!=8)throw new InvalidOperationException("Expected the original eight-node template.");
        var parent=originals[0].transform.parent;Vector3 center=Vector3.zero;
        foreach(var node in originals){center+=node.transform.localPosition;node.gameObject.SetActive(false);}center/=originals.Length;
        var nodes=new List<MapNode>();int n=definition.size;
        for(int i=0;i<n*n*n;i++) {
            var type=CampaignDefinition.TypeAt(definition,i);
            var template=originals.FirstOrDefault(o=>o.NodeType==type) ?? originals.First(o=>o.NodeType==(type==MapNodeType.Start?MapNodeType.Start:type==MapNodeType.Event?MapNodeType.Event:MapNodeType.Battle));
            var go=UnityEngine.Object.Instantiate(template.gameObject,parent);go.name=$"Floor{floor}_Node{i+1}_{type}";
            go.transform.localPosition=center+new Vector3(i%n-(n-1)*.5f,i/n%n-(n-1)*.5f,i/(n*n)-(n-1)*.5f)*definition.spacing;
            go.transform.localScale=template.transform.localScale*.65f;
            var node=go.GetComponent<MapNode>();var choice=go.GetComponents<MapChoice>().First();choice.Configure(type==MapNodeType.Event?"encounter.enter":"campaign.enter");
            string kind=L(TypeKey(type));
            node.Configure(CampaignDefinition.NodeId(floor,i),i%n+i/n%n+i/(n*n),type,L("node",floor,i+1,kind),L("description",kind),new[]{choice});nodes.Add(node);
        }
        for(int i=0;i<nodes.Count;i++){nodes[i].Connect(CampaignDefinition.Connections(definition,i).Select(j=>nodes[j]).ToArray());nodes[i].gameObject.SetActive(true);}
        foreach(var feedback in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapPlayerPositionFeedback>(true))) {
            var data=new SerializedObject(feedback);data.FindProperty("startNode").objectReferenceValue=nodes[0];data.ApplyModifiedPropertiesWithoutUndo();
        }
        // The source map's old paths reference its eight templates. Replace them with the authored graph.
        foreach(var path in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MapPath>(true)).ToArray())UnityEngine.Object.DestroyImmediate(path.gameObject);
        foreach(var node in originals)UnityEngine.Object.DestroyImmediate(node.gameObject);
        new GameObject("CampaignConnections").AddComponent<CampaignMapConnections>().Configure(nodes.ToArray());
    }
    private static string TypeKey(MapNodeType type)=>type==MapNodeType.FirstBoss?"midboss":type==MapNodeType.Boss?"finalboss":type==MapNodeType.Defender?"defender":type==MapNodeType.FloorExit?"exit":type==MapNodeType.Shop?"shop":type==MapNodeType.Event?"event":type==MapNodeType.Start?"start":"battle";
    private static string L(string key,params object[] args)=>string.Format(GameData.Text("campaign."+key),args);
}
