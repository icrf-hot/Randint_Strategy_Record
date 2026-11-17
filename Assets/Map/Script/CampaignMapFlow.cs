using System;
using System.Collections.Generic;
using System.Linq;
using Randint.Data;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Progression and encounter UI for one independently authored floor scene.</summary>
[DefaultExecutionOrder(-400)]
public sealed class CampaignMapFlow : MonoBehaviour
{
    private static CampaignMapFlow current;
    public static bool ModalOpen => EncounterWindow.IsOpen || RunSummaryWindow.IsOpen || (current != null && current.modal != null);
    private Canvas canvas;
    private GameObject modal;
    private TMP_Text floorLabel;
    private bool endingPresented;
    [SerializeField, Min(1)] private int floorNumber=1;
    public int FloorNumber => floorNumber;

    private void Awake()
    {
        current = this;
        if(!MapRunState.EnsureStarted(RunSaveBindings.Instance.Progression) || !MapRunState.EnterMapScene(floorNumber)) {
            Debug.LogError("[Campaign] Map scene does not match the active run floor.",this);enabled=false;
        }
    }
    private void Start()
    {
        canvas = TerminalUI.Canvas("CampaignMapHUD", 55); canvas.transform.SetParent(transform, false);
        floorLabel = TerminalUI.Label("Floor", canvas.transform, L("floor", MapRunState.MapFloor), new Vector2(0,-35), new Vector2(360,40),22,UITheme.Tone.Accent,true);
        floorLabel.rectTransform.anchorMin = floorLabel.rectTransform.anchorMax = new Vector2(.5f,1);
        StartCoroutine(ResumeEncounter());
    }
    private System.Collections.IEnumerator ResumeEncounter()
    {
        while(TransitionManager.IsBusy)yield return null;
        yield return null;
        var record=MapRunState.Encounters.Find(MapRunState.CurrentNodeID);
        if(record!=null && record.stage!=EncounterStage.Resolved) {
            var node=FindObjectsByType<MapNode>(FindObjectsSortMode.None).FirstOrDefault(n=>n.NodeID==record.nodeId);
            EncounterWindow.Open(node);
        }
    }
    private void Update()
    {
        if (MapRunState.IsEnded || TransitionManager.IsBusy || ModalOpen) return;
        if (!IsTerminal(MapRunState.CurrentNodeID) || !MapRunState.IsCompleted(MapRunState.CurrentNodeID)) return;
        bool terminalDefeat = MapRunState.LastBattleNodeId == MapRunState.CurrentNodeID &&
            MapRunState.LastBattleResult == "Defeat" && !(MapRunState.HiddenEndingRouteUnlocked && MapRunState.MapFloor == 3);
        if (terminalDefeat)
        {
            if (!endingPresented) ShowEnding(true);
            return;
        }
        if (MapRunState.MapFloor < MapRunState.EndingFloor) AdvanceFloor();
        else if (!endingPresented) ShowEnding();
    }
    public static bool IsTerminal(int id) => id == CampaignDefinition.TerminalNodeId(MapRunState.MapFloor);

    public void AdvanceFloor()
    {
        if(TransitionManager.IsBusy || ModalOpen || MapRunState.MapFloor>=MapRunState.EndingFloor || !IsTerminal(MapRunState.CurrentNodeID) || !MapRunState.IsCompleted(MapRunState.CurrentNodeID))return;
        // Update state only after the scene request has been accepted. It is installed before the new map is built.
        if(TransitionManager.TryLoad(CampaignScenes.PathFor(MapRunState.MapFloor+1),()=> {
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
        EncounterWindow.Open(node);
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
    private void ShowEnding(bool failed = false)
    {
        endingPresented=true;
        if(!RunSaveService.MarkRunEnded(failed ? "TerminalDefeat" : "Ending",out var error))Debug.LogWarning("[Campaign] Ending save status: "+error);
        RunSummaryWindow.Show(RunEndSummary.Capture(), () =>
            TransitionManager.TryLoad(RunRecordService.LobbyScenePath, message:L("ending_exit")));
    }

    private static string L(string key,params object[] args)=>string.Format(GameData.Text("campaign."+key),args);
    private void OnDestroy(){if(current==this)current=null;}
}
