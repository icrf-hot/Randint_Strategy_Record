using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CampaignRuntimeVerification
{
    private const string Key="Campaign.Verify.";
    public static string Output=>SessionState.GetString(Key+"Output","");
    static CampaignRuntimeVerification()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key+"Running",false))new GameObject("CampaignVerification").AddComponent<CampaignRuntimeProbe>();
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key+"Restore",false)) {
                SessionState.SetBool(Key+"Restore",false);string scene=SessionState.GetString(Key+"Scene","");
                EditorApplication.delayCall+=()=>{if(!string.IsNullOrEmpty(scene))EditorSceneManager.OpenScene(scene);};
            }
        };
        Application.logMessageReceived+=(message,trace,type)=>{
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
        SessionState.SetString(Key+"Deadline",DateTime.UtcNow.AddSeconds(180).ToString("O"));SessionState.SetBool(Key+"Running",true);
        EditorSceneManager.OpenScene("Assets/Scenes/Map_1F.unity");EditorApplication.EnterPlaymode();
    }
    public static void Finish(string result)
    {
        if(!SessionState.GetBool(Key+"Running",false))return;
        SessionState.SetBool(Key+"Running",false);File.WriteAllText(Output+"/result.txt",result);Debug.Log("[Campaign runtime] "+result);EditorApplication.ExitPlaymode();
    }
}

public sealed class CampaignRuntimeProbe : MonoBehaviour
{
    private IDisposable scope;
    private readonly List<string> report=new List<string>();
    private static T Find<T>()where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static UnityEngine.UI.Button Button(string name)=>UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(b=>b.name==name);
    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        var repo=new RunSaveRepository(CampaignRuntimeVerification.Output+"/isolated-saves",RunSaveBindings.Instance);scope=RunSaveService.UseRepositoryForVerification(repo);
        yield return new WaitForSecondsRealtime(2);
        var shop=FindObjectsByType<MapNode>(FindObjectsSortMode.None).First(n=>n.NodeID==8);
        MapRunState.SaveCurrentNode(8,shop.DisplayName);
        Require(ShopSceneRouter.EnterShop(shop),"Shop route rejected");yield return Ready("Shop");
        yield return null;
        var ui=Find<ShopSceneUI>();var buy=ui.GetBuyButton("cache_hp");Require(buy!=null && buy.interactable,"Shop buy stayed disabled after transition");
        Canvas.ForceUpdateCanvases();var data=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(null,buy.GetComponent<RectTransform>().TransformPoint(buy.GetComponent<RectTransform>().rect.center))};
        var raycasts=new List<RaycastResult>();EventSystem.current.RaycastAll(data,raycasts);
        Require(raycasts.Count>0 && ExecuteEvents.GetEventHandler<UnityEngine.UI.Button>(raycasts[0].gameObject)==buy.gameObject,"Buy button is covered");
        int before=MapRunState.Packets.Balance;ExecuteEvents.Execute(buy.gameObject,data,ExecuteEvents.pointerClickHandler);
        Require(MapRunState.Packets.Balance==before-12 && MapRunState.Caches.HasPurchased("cache_hp") && !buy.interactable,"Pointer purchase failed");
        ScreenCapture.CaptureScreenshot(CampaignRuntimeVerification.Output+"/shop-purchased.png");
        yield return new WaitForSecondsRealtime(.15f);
        ui.ReturnButton.onClick.Invoke();yield return Ready("Map_1F");yield return null;
        Require(Button("NextFloor").gameObject.activeSelf,"Floor 1 exit missing");Button("NextFloor").onClick.Invoke();yield return Ready("Map_1F",2);
        for(int floor=2;floor<=5;floor++) {
            var f=CampaignDefinition.Active.Floor(floor);var nodes=FindObjectsByType<MapNode>(FindObjectsSortMode.None);
            Require(nodes.Length==f.size*f.size*f.size,"Wrong node count on floor "+floor);
            Require(Find<MapPlayerPositionFeedback>().CurrentNode.NodeID==CampaignDefinition.NodeId(floor,0),"Current node restoration failed");
            ScreenCapture.CaptureScreenshot(CampaignRuntimeVerification.Output+$"/floor-{floor}.png");
            yield return new WaitForSecondsRealtime(.15f);
            // Check a genuine encounter choice and duplicate prevention on the generated floor.
            var ev=nodes.First(n=>n.NodeType==MapNodeType.Event);
            MapRunState.SaveCurrentNode(ev.NodeID,ev.DisplayName);before=MapRunState.Packets.Balance;
            CampaignMapFlow.ResolveEncounter(ev,0);CampaignMapFlow.ResolveEncounter(ev,0);
            Require(EncounterWindow.IsOpen && MapRunState.Packets.Balance==before,"Opening encounter spent resources");
            Button("Leave").onClick.Invoke();
            Require(MapRunState.IsCompleted(ev.NodeID) && MapRunState.Packets.Balance==before,"Encounter leave did not complete safely");
            Button("Close").onClick.Invoke();yield return null;
            // Traverse graph connections with the existing movement API; mark fixtures complete, without simulating those fights.
            var feedback=Find<MapPlayerPositionFeedback>();var path=Path(nodes.First(n=>n.NodeID==CampaignDefinition.NodeId(floor,0)),nodes.First(n=>CampaignMapFlow.IsTerminal(n.NodeID)));
            for(int i=1;i<path.Count-1;i++){feedback.MoveToNode(path[i]);Require(feedback.CurrentNode==path[i],"Axis/diagonal move rejected");MapRunState.CompleteNode(path[i].NodeID);}
            var terminal=path.Last();int terminalId=terminal.NodeID;
            if(floor==2) {
                terminal.Choices[0].Select(terminal);yield return null;
                Require(MapRunState.Caches.Owned.Count(c=>c.CacheId=="defender_relic")==1,"Relic missing");
                CampaignMapFlow.ResolveNode(terminal);Require(MapRunState.Caches.Owned.Count(c=>c.CacheId=="defender_relic")==1,"Relic duplicated");
                Canvas.ForceUpdateCanvases();yield return new WaitForSecondsRealtime(.2f);
                ScreenCapture.CaptureScreenshot(CampaignRuntimeVerification.Output+"/defender.png");yield return new WaitForSecondsRealtime(.15f);
                Button("Confirm").onClick.Invoke();yield return null;
                var save=MapRunState.Capture(RunSaveBindings.Instance);
                Require(repo.TryWrite(save,out _),"Floor 2 write failed");
                Require(repo.TryRead(out var loaded,out _),"Floor 2 read failed");
                Require(PreparedRunRestore.TryPrepare(loaded,RunSaveBindings.Instance,out var restored,out _),"Floor 2 restore failed");
                restored.Apply();Require(MapRunState.MapFloor==2,"Saved floor lost");
                // Exercise the normal encounter profile and the shared arts-impact path separately from boss fixtures.
                MapRunState.SetBattleNodeType(MapNodeType.Battle);
                Require(TransitionManager.TryLoad(RunSaveBindings.Instance.BattleScenePath),"Normal encounter route failed");
                yield return Ready("Playing_Scene");yield return new WaitForSecondsRealtime(.2f);
                var regular=FindObjectsByType<Enemy>(FindObjectsSortMode.None);
                Require(regular.Length==f.enemyCount && regular.Any(e=>e.UsesArtsDamage) && regular.Any(e=>!e.UsesArtsDamage),"Normal encounter lacks mixed damage types");
                var manager=Find<BattleExecuteManager>();
                typeof(BattleExecuteManager).GetMethod("LoadOperators",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,null);
                var front=BattleAccessManager.Instance.FrontOperator;var arts=regular.First(e=>e.UsesArtsDamage);
                int hp=front.CurrentHP,expected=Mathf.Max(1,arts.Attack-front.ArtsResistance);
                typeof(BattleExecuteManager).GetMethod("ExecuteEnemyAction",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(manager,new object[]{arts});
                var request=(AttackPresentationRequest)typeof(BattleExecuteManager).GetField("pendingPresentation",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(manager);
                manager.Feedback.BeginTurn();yield return manager.Feedback.Play(request);manager.Feedback.EndTurn();
                Require(front.CurrentHP==hp-expected,"Enemy arts damage used the wrong defense pipeline");
                Find<BattleToMapSceneLoader>().LoadMap();yield return Ready("Map_1F",2);
                report.Add("PASS: normal encounter mixes physical/arts; arts uses resistance through confirmed Impact.");
            } else if(floor==3 || floor==5) {
                terminal.Choices[0].Select(terminal);yield return Ready("Playing_Scene");yield return new WaitForSecondsRealtime(3);
                var enemies=FindObjectsByType<Enemy>(FindObjectsSortMode.None);Require(enemies.Length==1 && enemies[0].MaxHP==f.bossHP,"Wrong boss profile");
                ScreenCapture.CaptureScreenshot(CampaignRuntimeVerification.Output+$"/boss-{floor}.png");
                typeof(Enemy).GetField("currentHP",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(enemies[0],1);
                PrepareBattle(enemies[0]);Find<BattleExecuteManager>().ExecuteBattle();
                yield return Ready("Map_1F",floor);yield return new WaitForSecondsRealtime(.2f);
                Require(MapRunState.IsCompleted(terminalId),"Boss victory not recorded");
                if(floor==5) {
                    Require(MapRunState.IsEnded && MapRunState.EndReason=="Ending" && CampaignMapFlow.ModalOpen,"Normal ending missing");
                    Require(!repo.TryRead(out _,out var error) && error==RunSaveError.RunEnded,"Ended save can continue");
                    Canvas.ForceUpdateCanvases();yield return new WaitForSecondsRealtime(.2f);
                    ScreenCapture.CaptureScreenshot(CampaignRuntimeVerification.Output+"/normal-ending.png");yield return new WaitForSecondsRealtime(.15f);
                    Button("Confirm").onClick.Invoke();yield return Ready("Lobby");
                }
            } else {terminal.Choices[0].Select(terminal);yield return null;Button("Confirm").onClick.Invoke();yield return null;}
            report.Add($"PASS floor {floor}: generated geometry, encounter and endpoint.");
            if(floor<5){yield return null;Button("NextFloor").onClick.Invoke();yield return Ready("Map_1F",floor+1);}
        }
        CampaignRuntimeVerification.Finish("PASS\nPointer shop purchase after transition\n"+string.Join("\n",report)+"\nSingle relic, saved floor, mid/final boss victory, normal ending and ended-slot rejection. Test fights used HP=1; full balance not tested.");
    }
    private static void PrepareBattle(Enemy target)
    {
        var cards=UnityEngine.Object.FindObjectsByType<Card>(FindObjectsSortMode.InstanceID).Where(c=>c.isActiveAndEnabled).ToArray();Require(cards.Length==6,"Cards not dealt");
        for(int i=0;i<cards.Length;i++)cards[i].SetNumber(i+1);
        FrontCardSelectionManager.Instance.SetAttack();FrontCardSelectionManager.Instance.SelectCard(cards[0]);
        MiddleCardSelectionManager.Instance.SetPhysicalMode();for(int i=1;i<=3;i++)MiddleCardSelectionManager.Instance.SelectCard(cards[i]);
        BackCardSelectionManager.Instance.SelectCard(cards[4]);BackCardSelectionManager.Instance.SelectCard(cards[5]);
        var access=BattleAccessManager.Instance;
        foreach(var op in new[]{access.FrontOperator,access.MiddleOperator}){EnemyTargetingManager.Instance.BeginTargeting(op);EnemyTargetingManager.Instance.SelectTarget(target.GetComponent<EnemyTargetable>());}
        access.Refresh();Require(access.CanBattle,"Boss battle approval failed");
    }
    private static IEnumerator Ready(string scene,int floor=0)
    {
        float timeout=Time.realtimeSinceStartup+25;
        do {yield return null;Require(Time.realtimeSinceStartup<timeout,"Scene transition stalled: "+scene);}
        while(SceneManager.GetActiveScene().name!=scene || TransitionManager.IsBusy || (floor>0 && MapRunState.MapFloor!=floor));
        yield return null;
    }
    private static List<MapNode> Path(MapNode start,MapNode end)
    {
        var previous=new Dictionary<MapNode,MapNode>{{start,null}};var q=new Queue<MapNode>();q.Enqueue(start);
        while(q.Count>0){var n=q.Dequeue();if(n==end)break;foreach(var next in n.ConnectedNodes)if(!previous.ContainsKey(next)){previous[next]=n;q.Enqueue(next);}}
        var path=new List<MapNode>();for(var n=end;n!=null;n=previous[n])path.Add(n);path.Reverse();return path;
    }
    private void OnDestroy(){scope?.Dispose();}
}
