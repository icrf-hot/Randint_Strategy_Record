using System;
using System.Collections.Generic;
using System.Linq;
using Randint.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One map scene, distinct persisted campaign floors; existing node/camera interaction is reused.</summary>
[DefaultExecutionOrder(-400)]
public sealed class CampaignMapFlow : MonoBehaviour
{
    private static CampaignMapFlow current;
    public static bool ModalOpen => current != null && current.modal != null;
    private Canvas canvas;
    private GameObject modal;
    private UnityEngine.UI.Button advance;
    private TMP_Text floorLabel;
    private TMP_Text advanceText;
    private bool endingPresented;
    private Mesh edgeMesh;
    private Material edgeMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        current = null; SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded;
    }
    private static void Loaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Map_1F" && FindFirstObjectByType<CampaignMapFlow>() == null)
            new GameObject("CampaignMapFlow").AddComponent<CampaignMapFlow>();
    }
    private void Awake()
    {
        current = this;
        if (MapRunState.MapFloor > 1) BuildFloor();
    }
    private void Start()
    {
        canvas = TerminalUI.Canvas("CampaignMapHUD", 55); canvas.transform.SetParent(transform, false);
        floorLabel = TerminalUI.Label("Floor", canvas.transform, L("floor", MapRunState.MapFloor), new Vector2(0,-35), new Vector2(360,40),22,UITheme.Tone.Accent,true);
        floorLabel.rectTransform.anchorMin = floorLabel.rectTransform.anchorMax = new Vector2(.5f,1);
        advance = TerminalUI.Button("NextFloor",canvas.transform,"",new Vector2(0,35),new Vector2(320,44));
        advanceText = advance.GetComponentInChildren<TMP_Text>(); advanceText.text = L("advance",MapRunState.MapFloor+1);
        var rect=(RectTransform)advance.transform; rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);
        advance.onClick.AddListener(AdvanceFloor); advance.gameObject.SetActive(false);
    }
    private void Update()
    {
        if (advance == null) return;
        bool terminal = IsTerminal(MapRunState.CurrentNodeID) && MapRunState.IsCompleted(MapRunState.CurrentNodeID);
        advance.gameObject.SetActive(terminal && MapRunState.MapFloor < 5 && !TransitionManager.IsBusy && !ModalOpen && !MapRunState.IsEnded);
        if (terminal && MapRunState.MapFloor == 5 && !endingPresented && !TransitionManager.IsBusy && !MapRunState.IsEnded) ShowEnding();
    }
    public static bool IsTerminal(int id)
    {
        int floor=MapRunState.MapFloor;
        if(floor==1)return id==8;
        var f=CampaignDefinition.Active.Floor(floor);
        return f!=null && id==CampaignDefinition.NodeId(floor,f.size*f.size*f.size-1);
    }
    public void AdvanceFloor()
    {
        if(TransitionManager.IsBusy || ModalOpen || MapRunState.MapFloor>=5 || !IsTerminal(MapRunState.CurrentNodeID) || !MapRunState.IsCompleted(MapRunState.CurrentNodeID))return;
        // Update state only after the scene request has been accepted. It is installed before the new map is built.
        if(TransitionManager.TryLoad(RunSaveBindings.Instance.MapScenePath,()=> {
            if(!MapRunState.TryAdvanceMapFloor())throw new InvalidOperationException("Campaign advance rejected.");
        },message:L("transition"))) Debug.Log("[Campaign] Advancing from floor "+MapRunState.MapFloor);
    }
    public static void ResolveNode(MapNode node)
    {
        if(current==null || node==null || MapRunState.IsEnded || MapRunState.CurrentNodeID!=node.NodeID || MapRunState.IsCompleted(node.NodeID))return;
        if(node.NodeType==MapNodeType.Defender) {
            var relic=RunSaveBindings.Instance.Cache("defender_relic");
            if(!MapRunState.Caches.Grant(relic)) { Debug.LogError("[Campaign] Defender relic could not be granted.");return; }
            MapRunState.CompleteNode(node.NodeID);
            current.Message(L("defender"),relic.DisplayName+"\n"+relic.Description);
        } else if(node.NodeType==MapNodeType.Event) {
            // Selection index is supplied by MapChoice via ResolveEncounter.
            ResolveEncounter(node,0);
        } else if(node.NodeType==MapNodeType.FloorExit || node.NodeType==MapNodeType.Rest) {
            MapRunState.CompleteNode(node.NodeID);current.Message(node.DisplayName,L("exit_info"));
        }
    }
    public static void ResolveEncounter(MapNode node,int choice)
    {
        if(current==null || node==null || MapRunState.IsEnded || MapRunState.CurrentNodeID!=node.NodeID || MapRunState.IsCompleted(node.NodeID))return;
        var rules=CampaignDefinition.Active;string result;
        if(choice==0) {
            if(!MapRunState.Packets.TryAdd(rules.encounterPackets))return;
            result=L("supply_result",rules.encounterPackets);
        } else if(choice==1) {
            int beforeI=MapRunState.RecordHealth.Integrity,beforeC=MapRunState.RecordHealth.Consistency;
            MapRunState.RecordHealth.Restore(Mathf.Min(100,beforeI+rules.encounterRepair),Mathf.Min(100,beforeC+rules.encounterRepair));
            result=L("repair_result",MapRunState.RecordHealth.Integrity-beforeI,MapRunState.RecordHealth.Consistency-beforeC);
        } else result=L("leave_result");
        MapRunState.CompleteNode(node.NodeID);current.Message(L("event"),result);
    }
    private void Message(string title,string text,Action close=null)
    {
        FindFirstObjectByType<MapOrbitCamera>()?.CancelFocus();
        var backdrop=TerminalUI.Panel("CampaignMessage",canvas.transform,Vector2.zero,Vector2.zero,UITheme.Tone.Base);
        TerminalUI.Stretch(backdrop.rectTransform);backdrop.raycastTarget=true;modal=backdrop.gameObject;
        var panel=TerminalUI.Panel("Dialog",backdrop.transform,Vector2.zero,new Vector2(650,320));
        TerminalUI.Label("Title",panel.transform,title,new Vector2(0,105),new Vector2(590,45),28,UITheme.Tone.Accent);
        var message=TerminalUI.Label("Message",panel.transform,text,new Vector2(0,10),new Vector2(570,120),22);
        message.textWrappingMode=TextWrappingModes.Normal;
        var button=TerminalUI.Button("Confirm",panel.transform,L("confirm"),new Vector2(0,-110),new Vector2(220,42));
        button.onClick.AddListener(()=>{Destroy(modal);modal=null;close?.Invoke();});
    }
    private void ShowEnding()
    {
        // Hidden-route requirements remain undecided; no floor 6/7 is constructed or unlocked here.
        endingPresented=true;
        if(!RunSaveService.MarkRunEnded("Ending",out var error))Debug.LogWarning("[Campaign] Ending save status: "+error);
        Message(L("normal_ending"),L("ending_unnamed"),()=>TransitionManager.TryLoad(RunRecordService.LobbyScenePath,message:L("ending_exit")));
    }
    private void BuildFloor()
    {
        var definition=CampaignDefinition.Active.Floor(MapRunState.MapFloor);
        if(definition==null)throw new InvalidOperationException("Unsupported campaign floor.");
        var originals=FindObjectsByType<MapNode>(FindObjectsSortMode.None);
        if(originals.Length!=8)throw new InvalidOperationException("Expected the original eight-node map template.");
        var parent=originals[0].transform.parent;
        Vector3 center=Vector3.zero;foreach(var n in originals)center+=n.transform.localPosition;center/=originals.Length;
        foreach(var node in originals)node.gameObject.SetActive(false);
        var generated=new List<MapNode>();int nSide=definition.size;
        for(int i=0;i<nSide*nSide*nSide;i++) {
            var type=CampaignDefinition.TypeAt(definition,i);
            var template=originals.FirstOrDefault(o=>o.NodeType==type) ?? originals.First(o=>o.NodeType==(type==MapNodeType.Start?MapNodeType.Start:type==MapNodeType.Event?MapNodeType.Event:MapNodeType.Battle));
            var go=Instantiate(template.gameObject,parent);go.name=$"Floor{definition.floor}_Node{i+1}_{type}";
            go.transform.localPosition=center+new Vector3(i%nSide-(nSide-1)*.5f,i/nSide%nSide-(nSide-1)*.5f,i/(nSide*nSide)-(nSide-1)*.5f)*definition.spacing;
            go.transform.localScale=template.transform.localScale*.65f;
            var node=go.GetComponent<MapNode>();var choices=go.GetComponents<MapChoice>();
            int count=type==MapNodeType.Event?3:1;
            if(choices.Length<count)throw new InvalidOperationException("Map template has too few choices.");
            for(int c=0;c<count;c++)choices[c].Configure(type==MapNodeType.Event?new[]{"campaign.supply","campaign.repair","campaign.leave"}[c]:"campaign.enter");
            string kind=L(TypeKey(type));
            node.Configure(CampaignDefinition.NodeId(definition.floor,i),i%nSide+i/nSide%nSide+i/(nSide*nSide),type,
                L("node",definition.floor,i+1,kind),L("description",kind),choices.Take(count).ToArray());
            generated.Add(node);
        }
        for(int i=0;i<generated.Count;i++) {
            generated[i].Connect(CampaignDefinition.Connections(definition,i).Select(index=>generated[index]).ToArray());
            generated[i].gameObject.SetActive(true);
        }
        BuildEdges(parent,generated);
        Debug.Log($"[Campaign] Floor {definition.floor}: {nSide}x{nSide}x{nSide}, {generated.Count} nodes.");
    }
    private void BuildEdges(Transform parent,List<MapNode> nodes)
    {
        var vertices=new List<Vector3>();foreach(var node in nodes)foreach(var next in node.ConnectedNodes){vertices.Add(node.transform.localPosition);vertices.Add(next.transform.localPosition);}
        edgeMesh=new Mesh{name="CampaignConnections"};edgeMesh.SetVertices(vertices);edgeMesh.SetIndices(Enumerable.Range(0,vertices.Count).ToArray(),MeshTopology.Lines,0);
        var go=new GameObject("CampaignConnections",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.GetComponent<MeshFilter>().sharedMesh=edgeMesh;
        edgeMaterial=new Material(Resources.Load<Shader>("CampaignLines"));edgeMaterial.SetColor("_BaseColor",UITheme.Alpha(UITheme.Line,.55f));
        go.GetComponent<MeshRenderer>().sharedMaterial=edgeMaterial;
    }
    private static string TypeKey(MapNodeType type)=>type==MapNodeType.FirstBoss?"midboss":type==MapNodeType.Boss?"finalboss":type==MapNodeType.Defender?"defender":type==MapNodeType.FloorExit?"exit":type==MapNodeType.Shop?"shop":type==MapNodeType.Event?"event":type==MapNodeType.Start?"start":"battle";
    private static string L(string key,params object[] args)=>string.Format(GameData.Text("campaign."+key),args);
    private void OnDestroy(){if(current==this)current=null;if(edgeMesh!=null)Destroy(edgeMesh);if(edgeMaterial!=null)Destroy(edgeMaterial);}
}
