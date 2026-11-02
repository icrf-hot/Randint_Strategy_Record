using System;
using System.Collections.Generic;
using System.Linq;
using Randint.Data;
using TMPro;
using UnityEngine;

/// <summary>Encounter-only card controls; never invokes the battle Card/sorting singletons.</summary>
public sealed class EncounterWindow : MonoBehaviour
{
    private static EncounterWindow current;
    public static bool IsOpen => current!=null;
    private EncounterRecord record;
    private Transform panel;
    private TMP_Text live, hint, wallet, message;
    private readonly List<(EncounterAction action,UnityEngine.UI.Button button)> actions=new List<(EncounterAction,UnityEngine.UI.Button)>();
    private readonly List<UnityEngine.UI.Button> cards=new List<UnityEngine.UI.Button>();
    private EncounterStage renderedStage;

    public static void Open(MapNode node)
    {
        if(IsOpen || node==null || MapRunState.IsEnded || MapRunState.CurrentNodeID!=node.NodeID || MapRunState.IsCompleted(node.NodeID))return;
        var r=MapRunState.Encounters.Assign(MapRunState.RunId,node.NodeID,RunSaveBindings.Instance);
        FindFirstObjectByType<MapOrbitCamera>()?.CancelFocus();
        var canvas=TerminalUI.Canvas("EncounterWindow",220);
        current=canvas.gameObject.AddComponent<EncounterWindow>();current.record=r;current.Build();
    }
    private TMP_Text Label(string name,string text,float y,float height=34,int size=20,UITheme.Tone tone=UITheme.Tone.Main,bool mono=false)
    {
        var label=TerminalUI.Label(name,panel,text,new Vector2(0,y),new Vector2(1010,height),size,tone,mono);
        label.textWrappingMode=TextWrappingModes.Normal;label.raycastTarget=false;return label;
    }
    private void Build()
    {
        actions.Clear();cards.Clear();renderedStage=record.stage;
        var backdrop=TerminalUI.Panel("InputBlocker",transform,Vector2.zero,Vector2.zero,UITheme.Tone.Base);
        TerminalUI.Stretch(backdrop.rectTransform);backdrop.raycastTarget=true;
        panel=TerminalUI.Panel("Request",backdrop.transform,Vector2.zero,new Vector2(1100,670)).transform;
        var definition=EncounterCatalog.Active.Find(record.eventId);
        Label("Header",EncounterService.Text("header",record.floor),310,25,16,UITheme.Tone.Dim,true);
        Label("Title",GameData.Text(definition.titleKey),270,45,30,UITheme.Tone.Accent);
        Label("Body",GameData.Text(definition.bodyKey),207,70,21);
        wallet=Label("Wallet","",-309,26,17,UITheme.Tone.Dim,true);
        message=Label("Message","",-275,28,17,UITheme.Tone.Warn);
        if(record.stage==EncounterStage.Resolved) {
            Label("Result",EncounterService.Text("result",EncounterService.Text("outcome."+record.outcome),record.packetDelta,record.integrityDelta,record.consistencyDelta),10,140,24);
            AddClose(EncounterService.Text("close"),-205);return;
        }
        if(record.stage==EncounterStage.Verified) {
            Label("Verified",EncounterService.Text("verified",record.signedChannel==0?"A":"B"),65,110,24,UITheme.Tone.Ok);
            Label("Reward",EncounterService.Text("reward",record.tier.signaturePackets,record.tier.signatureHeal),-35,60);
            AddAction(EncounterAction.Claim,EncounterService.Text("claim",record.signedChannel==0?"A":"B"),-160);
            AddClose(EncounterService.Text("back"),-220);return;
        }
        var back=TerminalUI.Button("PauseRequest",panel,EncounterService.Text("back"),new Vector2(410,310),new Vector2(250,26));
        back.GetComponentInChildren<TMP_Text>().fontSize=15;back.onClick.AddListener(()=>Destroy(gameObject));
        if(record.eventId=="signature_request")BuildSignature();
        else {
            int i=0;foreach(string name in definition.actions) {
                var action=(EncounterAction)Enum.Parse(typeof(EncounterAction),name);
                AddAction(action,ActionLabel(action),70-i++*82);
            }
            Label("CostWarning",EncounterService.Text("overflow",MapRunState.RecordHealth.Consistency,
                Math.Max(0,MapRunState.RecordHealth.Consistency-record.tier.riskCost),MapRunState.RecordHealth.Integrity,
                Math.Max(0,MapRunState.RecordHealth.Integrity-Math.Max(0,record.tier.riskCost-MapRunState.RecordHealth.Consistency))),-190,50,18,UITheme.Tone.Dim);
        }
    }
    private void BuildSignature()
    {
        Label("RawRequest",EncounterService.Text(MapRunState.RecordHealth.Consistency<60?"raw_low":"raw"),155,30,17,UITheme.Tone.Dim);
        string conditions=EncounterService.Text("conditions",record.tier.requiredCards,record.targetSum);
        if(record.tier.parity)conditions+=EncounterService.Text("parity",record.targetEven);
        if(record.tier.modulus>0)conditions+=EncounterService.Text("checksum",record.tier.modulus,record.targetChecksum);
        Label("Conditions",conditions,107,62,20,UITheme.Tone.Accent);
        for(int i=0;i<6;i++) {
            int index=i;
            var button=TerminalUI.Button("Card"+i,panel,"",new Vector2((i-2.5f)*125,10),new Vector2(108,102));
            var label=button.GetComponentInChildren<TMP_Text>();label.fontSize=24;TerminalUI.Bind(label.gameObject,UITheme.Tone.Main,true);
            button.onClick.AddListener(()=>{if(record.stage!=EncounterStage.Assigned)return;record.selectedMask^=1<<index;Refresh();});cards.Add(button);
        }
        live=Label("LiveProof","",-60,30,19,UITheme.Tone.Main,true);
        hint=Label("Hint","",-91,26,17,UITheme.Tone.Dim);
        Label("Reward",EncounterService.Text("reward",record.tier.signaturePackets,record.tier.signatureHeal),-123,30,19);
        bool joker=record.hand.Contains(0);
        if(joker)Label("ForgeNote",EncounterService.Text("forge_note",record.tier.signaturePackets),-151,27,17,UITheme.Tone.Warn);
        AddAction(EncounterAction.Verify,record.tier.verifyCost==0?EncounterService.Text("verify_free"):EncounterService.Text("verify",record.tier.verifyCost),-193,joker?-255:0);
        if(joker)AddAction(EncounterAction.Forge,EncounterService.Text("forge",record.tier.forgeCost),-193,255);
        AddAction(EncounterAction.Leave,EncounterService.Text("leave"),-239);
    }
    private string ActionLabel(EncounterAction action)
    {
        var t=record.tier;
        switch(action) {
            case EncounterAction.Supply:return EncounterService.Text("supply",t.supplyPackets);
            case EncounterAction.Repair:return EncounterService.Text("repair",t.repairCost,t.repairAmount);
            case EncounterAction.RiskyPackets:return EncounterService.Text("risk",t.riskCost,t.riskPackets);
            default:return EncounterService.Text("leave");
        }
    }
    private void AddAction(EncounterAction action,string text,float y,float x=0)
    {
        var button=TerminalUI.Button(action.ToString(),panel,text,new Vector2(x,y),new Vector2(490,40));
        button.GetComponentInChildren<TMP_Text>().fontSize=18;
        button.onClick.AddListener(()=> {
            if(!EncounterService.Execute(record,action,out string reason)){message.text=EncounterService.Text("error",reason);return;}
            Rebuild();
        });actions.Add((action,button));
    }
    private void AddClose(string text,float y)
    {
        var button=TerminalUI.Button("Close",panel,text,new Vector2(0,y),new Vector2(490,40));
        button.onClick.AddListener(()=>Destroy(gameObject));
    }
    private void Rebuild(){foreach(Transform child in transform)Destroy(child.gameObject);Build();Refresh();}
    private void Update(){if(record!=null){if(renderedStage!=record.stage)Rebuild();Refresh();}}
    private void Refresh()
    {
        wallet.text=EncounterService.Text("wallet",MapRunState.Packets.Balance,MapRunState.RecordHealth.Integrity,MapRunState.RecordHealth.Consistency);
        foreach(var entry in actions) {
            bool allowed=EncounterService.CanExecute(record,entry.action,out var reason);entry.button.interactable=allowed;
            // Disabled controls retain an accessible explanation in the adjacent status field.
            if(entry.action==EncounterAction.Verify && EncounterJournal.ValidProof(record,record.selectedMask))message.text=allowed?EncounterService.Text("proof_ok"):EncounterService.Text("error",reason);
        }
        if(cards.Count==0)return;
        int count=0,sum=0,even=0,checksum=0;
        for(int i=0;i<6;i++) {
            bool selected=(record.selectedMask&(1<<i))!=0;int n=record.hand[i];
            cards[i].GetComponentInChildren<TMP_Text>().text=(selected?"[ ":"")+(n==0?"JK":n.ToString("00"))+(selected?" ]":"")+"\n"+(i+1);
            cards[i].GetComponent<TerminalButton>().variant=selected?TerminalButton.Variant.Emphasis:TerminalButton.Variant.Default;
            if(selected){count++;sum+=n;if(n>0 && n%2==0)even++;checksum+=n*(i+1);}
        }
        live.text=EncounterService.Text("live",count,record.tier.requiredCards,sum,record.targetSum);
        if(record.tier.parity)live.text+=EncounterService.Text("live_parity",even,record.targetEven);
        if(record.tier.modulus>0)live.text+=EncounterService.Text("live_checksum",checksum%record.tier.modulus,record.targetChecksum);
        int solution=EncounterJournal.Solution(record);int index=Enumerable.Range(0,6).First(i=>(solution&(1<<i))!=0)+1;
        hint.text=MapRunState.RecordHealth.Consistency>=60?EncounterService.Text("hint",index):EncounterService.Text("hint_low");
        if(!EncounterJournal.ValidProof(record,record.selectedMask))message.text=EncounterService.Text("proof_wait");
        if(record.hand.Contains(0) && (record.selectedMask&32)!=0) {
            int c=MapRunState.RecordHealth.Consistency,i=MapRunState.RecordHealth.Integrity;
            message.text=EncounterService.CanExecute(record,EncounterAction.Forge,out string reason)?
                EncounterService.Text("overflow",c,Math.Max(0,c-record.tier.forgeCost),i,Math.Max(0,i-Math.Max(0,record.tier.forgeCost-c))):EncounterService.Text("error",reason);
        }
    }
    private void OnDestroy(){if(current==this)current=null;}
}
