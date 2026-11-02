using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class EncounterRuntimeVerification
{
    private const string Key="Encounter.Verify.";
    public static string Output=>SessionState.GetString(Key+"Output","");
    static EncounterRuntimeVerification()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key+"Running",false))new GameObject("EncounterVerification").AddComponent<EncounterRuntimeProbe>();
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key+"Restore",false)) {
                SessionState.SetBool(Key+"Restore",false);string scene=SessionState.GetString(Key+"Scene","");
                EditorApplication.delayCall+=()=>{if(!string.IsNullOrEmpty(scene))EditorSceneManager.OpenScene(scene);};
            }
        };
        Application.logMessageReceived+=(message,trace,type)=> {
            if(SessionState.GetBool(Key+"Running",false) && EditorApplication.isPlaying && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert))Finish("FAIL: "+message+"\n"+trace);
        };
        EditorApplication.update+=()=>{if(SessionState.GetBool(Key+"Running",false) && DateTime.UtcNow>DateTime.Parse(SessionState.GetString(Key+"Deadline","")).ToUniversalTime())Finish("FAIL: timeout");};
    }
    public static void Run(string output)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene edits first");
        Directory.CreateDirectory(output);SessionState.SetString(Key+"Output",output);
        SessionState.SetString(Key+"Scene",SceneManager.GetActiveScene().path);SessionState.SetBool(Key+"Restore",true);
        SessionState.SetString(Key+"Deadline",DateTime.UtcNow.AddSeconds(120).ToString("O"));SessionState.SetBool(Key+"Running",true);
        EditorSceneManager.OpenScene("Assets/Scenes/Map_1F.unity");EditorApplication.EnterPlaymode();
    }
    public static void Finish(string result)
    {
        if(!SessionState.GetBool(Key+"Running",false))return;
        SessionState.SetBool(Key+"Running",false);File.WriteAllText(Output+"/runtime-result.txt",result);
        Debug.Log("[Encounter runtime] "+result);EditorApplication.ExitPlaymode();
    }
}

public sealed class EncounterRuntimeProbe : MonoBehaviour
{
    private IDisposable scope;
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static UnityEngine.UI.Button Button(string name)=>UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
        .First(b=>b.name==name && b.GetComponentInParent<EncounterWindow>()!=null);
    private static void Click(UnityEngine.UI.Button button)
    {
        Require(button.interactable,"Disabled "+button.name);Canvas.ForceUpdateCanvases();
        var rect=button.GetComponent<RectTransform>();
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
        var results=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,results);
        Require(results.Count>0 && ExecuteEvents.GetEventHandler<UnityEngine.UI.Button>(results[0].gameObject)==button.gameObject,"Covered "+button.name);
        ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
    }
    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        var repo=new RunSaveRepository(EncounterRuntimeVerification.Output+"/isolated-saves-"+Guid.NewGuid().ToString("N"),RunSaveBindings.Instance);
        scope=RunSaveService.UseRepositoryForVerification(repo);yield return new WaitForSecondsRealtime(2);
        var node=FindObjectsByType<MapNode>(FindObjectsSortMode.None).First(n=>n.NodeID==2);
        Require(node.Choices.Length==1,"Legacy three-option encounter still exposed");
        node.Choices[0].Select(node);yield return null;
        Require(EncounterWindow.IsOpen && CampaignMapFlow.ModalOpen,"Choice did not open modal");
        var r=MapRunState.Encounters.Find(2);Require(r!=null && !Button("Verify").interactable,"Initial verification enabled");
        int mask=EncounterJournal.Solution(r);
        for(int i=0;i<6;i++)if((mask&(1<<i))!=0)Click(Button("Card"+i));
        yield return null;Require(Button("Verify").interactable,"Selected proof did not enable verification");
        CheckText();ScreenCapture.CaptureScreenshot(EncounterRuntimeVerification.Output+"/floor-1-cards.png");yield return new WaitForSecondsRealtime(.15f);
        int before=MapRunState.Packets.Balance;Click(Button("Verify"));yield return null;
        Require(r.stage==EncounterStage.Verified && MapRunState.Packets.Balance==before,"Free tutorial verification failed");
        Click(Button("Close"));yield return null;
        var feedback=FindFirstObjectByType<MapPlayerPositionFeedback>();
        Require(!feedback.CanOpenChoicesFor(node.ConnectedNodes.First()),"Unclaimed paid verification can be abandoned");
        var saveUI=FindFirstObjectByType<MapSaveControls>();saveUI.OpenSettings();saveUI.OpenSlotPicker(false);yield return null;
        Require(saveUI.ConfirmButton.interactable,"Save not available after pausing request");saveUI.ConfirmButton.onClick.Invoke();yield return null;
        saveUI.CloseSettings();
        Require(repo.TryRead(out var saved,out _) && saved.encounters.Single().stage==EncounterStage.Verified,"UI save omitted verification");
        Require(PreparedRunRestore.TryPrepare(saved,RunSaveBindings.Instance,out var prepared,out _),"UI save restore rejected");prepared.Apply();
        Require(TransitionManager.TryLoad(RunSaveBindings.Instance.MapScenePath),"Reload request failed");yield return Ready();
        Require(EncounterWindow.IsOpen,"Saved request did not reopen");
        r=MapRunState.Encounters.Find(2);Require(r.stage==EncounterStage.Verified,"Verified state lost");
        Click(Button("Claim"));yield return null;
        Require(MapRunState.Packets.Balance==before+4 && MapRunState.IsCompleted(2),"Claim after continue incorrect");
        Click(Button("Close"));yield return null;
        EncounterVerification.Fixture(5);
        Require(TransitionManager.TryLoad(RunSaveBindings.Instance.MapScenePath),"Floor 5 fixture transition");yield return Ready();
        r=MapRunState.Encounters.Find(MapRunState.CurrentNodeID);Require(r.floor==5,"Wrong floor fixture");
        mask=EncounterJournal.Solution(r);for(int i=0;i<6;i++)if((mask&(1<<i))!=0)Click(Button("Card"+i));
        yield return null;CheckText();ScreenCapture.CaptureScreenshot(EncounterRuntimeVerification.Output+"/floor-5-cards.png");yield return new WaitForSecondsRealtime(.15f);
        before=MapRunState.Packets.Balance;Click(Button("Verify"));yield return null;
        Require(MapRunState.Packets.Balance==before-8,"Floor 5 verification cost incorrect");
        Click(Button("Claim"));yield return null;Require(MapRunState.Packets.Balance==before+16,"Floor 5 net guaranteed reward incorrect");
        Click(Button("Close"));yield return null;
        for(int seed=0;seed<500;seed++){r=EncounterVerification.Fixture(5,seed.ToString("x32"));if(r.hand.Contains(0))break;}
        Require(r.hand.Contains(0),"No JK fixture");MapRunState.RecordHealth.Restore(50,3);
        Require(TransitionManager.TryLoad(RunSaveBindings.Instance.MapScenePath),"JK fixture transition");yield return Ready();
        Click(Button("Card5"));yield return null;Require(!Button("Verify").interactable && Button("Forge").interactable,"JK control state");
        CheckText();ScreenCapture.CaptureScreenshot(EncounterRuntimeVerification.Output+"/floor-5-untrusted-jk.png");yield return new WaitForSecondsRealtime(.15f);
        Click(Button("Forge"));yield return null;
        Require(MapRunState.RecordHealth.Integrity==45 && MapRunState.RecordHealth.Consistency==0,"JK overflow differs from preview");
        Require(repo.TryWrite(MapRunState.Capture(RunSaveBindings.Instance),out _),"JK result save");
        Click(Button("Close"));yield return null;
        Require(!EncounterWindow.IsOpen && !CampaignMapFlow.ModalOpen,"Modal input block leaked");
        EncounterRuntimeVerification.Finish("PASS: real MapChoice, card pointer raycasts, 1F/5F verification and claim, settings UI save, verified resume, unclaimed movement gate, low-consistency/JK preview and overflow, text bounds, modal cleanup. User save files isolated.");
    }
    private static void CheckText()
    {
        Canvas.ForceUpdateCanvases();var window=UnityEngine.Object.FindFirstObjectByType<EncounterWindow>();
        foreach(var text in window.GetComponentsInChildren<TMPro.TMP_Text>()) {
            text.ForceMeshUpdate();Require(!text.isTextOverflowing,"Text overflow: "+text.name+" / "+text.text);
            Require(Mathf.Approximately(text.transform.lossyScale.x,text.transform.lossyScale.y),"Squashed text: "+text.name);
        }
    }
    private static IEnumerator Ready(){yield return new WaitForSecondsRealtime(.2f);while(TransitionManager.IsBusy)yield return null;yield return new WaitForSecondsRealtime(.3f);}
    private void OnDestroy(){scope?.Dispose();}
}
