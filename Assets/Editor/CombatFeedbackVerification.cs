using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit opt-in: uses temporary Play Mode state, never writes a campaign save.</summary>
[InitializeOnLoad]
public static class CombatFeedbackVerification
{
    private const string Key="CombatFeedback.Verification.";
    public static string Output=>SessionState.GetString(Key+"Output","");
    static CombatFeedbackVerification()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key+"Running",false)) {
                var go=new GameObject("CombatFeedbackVerification");go.AddComponent<CombatFeedbackVerificationRunner>();
            }
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key+"Restore",false)) {
                SessionState.SetBool(Key+"Restore",false);
                string scene=SessionState.GetString(Key+"Scene","");
                EditorApplication.delayCall+=()=>{if(!string.IsNullOrEmpty(scene))EditorSceneManager.OpenScene(scene);};
            }
        };
        EditorApplication.update+=()=> {
            if(SessionState.GetBool(Key+"Running",false) && DateTime.UtcNow>DateTime.Parse(SessionState.GetString(Key+"Deadline","")).ToUniversalTime())Finish("FAIL: timed out");
        };
        Application.logMessageReceived+=(message,trace,type)=> {
            if(SessionState.GetBool(Key+"Running",false) && EditorApplication.isPlaying && (type==LogType.Error || type==LogType.Exception || type==LogType.Assert))Finish("FAIL: "+message+"\n"+trace);
        };
    }
    public static void Run(string output)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first.");
        for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new InvalidOperationException("Save scene changes first.");
        TestCommitGate();
        System.IO.Directory.CreateDirectory(output);SessionState.SetString(Key+"Output",output);
        SessionState.SetString(Key+"Scene",SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key+"Running",true);SessionState.SetBool(Key+"Restore",true);
        SessionState.SetString(Key+"Deadline",DateTime.UtcNow.AddSeconds(120).ToString("O"));
        EditorSceneManager.OpenScene("Assets/Scenes/Playing_Scene.unity");EditorApplication.EnterPlaymode();
    }
    public static void TestCommitGate()
    {
        var profile=ScriptableObject.CreateInstance<AttackFeedbackProfile>();
        foreach(bool skip in new[]{false,true})foreach(bool animation in new[]{false,true}) {
            profile.animationDrivenImpact=animation;
            int commits=0,events=0,entered=0,exited=0;
            AttackSequence sequence=null;
            sequence=new AttackSequence(()=>{commits++;if(sequence.RequestImpact())throw new Exception("Reentrant duplicate commit");return new[]{new AttackHitInfo()};});
            sequence.StageEntered+=(s,phase)=>{entered++;if(animation && phase==AttackStage.Impact){s.RequestImpact();s.RequestImpact();}};
            sequence.StageExited+=(s,phase)=>exited++;
            sequence.OnImpact+=(a,t,h)=>events++;
            if(sequence.RequestImpact())throw new Exception("Impact accepted outside hit window");
            Drain(sequence.Run(profile,()=>skip,()=>.1f,null));
            if(commits!=1 || events!=1 || entered!=5 || exited!=5 || !sequence.IsComplete || sequence.RequestImpact())throw new Exception("Commit/phase contract failed");
        }
        UnityEngine.Object.DestroyImmediate(profile);
        Debug.Log("[Combat verification] PASS: coroutine, animation event, skip and reentrant impact commit exactly once.");
        void Drain(IEnumerator routine){int limit=1000;while(routine.MoveNext()){if(--limit==0)throw new Exception("Sequence stalled");if(routine.Current is IEnumerator nested)Drain(nested);}}
    }
    public static void Finish(string result)
    {
        if(!SessionState.GetBool(Key+"Running",false))return;
        SessionState.SetBool(Key+"Running",false);System.IO.File.WriteAllText(Output+"/result.txt",result);
        Debug.Log("[Combat verification] "+result);EditorApplication.ExitPlaymode();
    }
}

public sealed class CombatFeedbackVerificationRunner : MonoBehaviour
{
    private readonly List<string> report=new List<string>();
    private readonly List<string> trace=new List<string>();
    private readonly List<AttackHitInfo> hits=new List<AttackHitInfo>();
    private readonly Dictionary<GameObject,Vector3> roots=new Dictionary<GameObject,Vector3>();
    private bool capturing;private float captureNext;private int frame;
    private string capturePrefix="frame";
    private int originalSpeed;private bool originalFlash,originalStop;private float originalShake;
    private static T Find<T>()where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    private static void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        originalSpeed=TerminalPreferences.CombatSpeed;originalFlash=TerminalPreferences.ReduceFlash;originalStop=TerminalPreferences.HitStop;originalShake=TerminalPreferences.ShakeStrength;
        yield return new WaitForSecondsRealtime(4);
        var manager=Find<BattleExecuteManager>();var feedback=manager.Feedback;
        Require(feedback.library!=null,"Profiles not installed");
        Require(GameObject.Find("RecordHealthHUD") == null, "Record health HUD covers battle selection panel");
        feedback.OnImpact+=(a,t,h)=>hits.Add(h);
        feedback.StageEntered+=(s,stage)=> {
            if(stage==AttackStage.Anticipation)trace.Add("begin");
            if(stage==AttackStage.Impact){Require(!s.HasCommitted,"Committed before Impact");}
        };
        foreach(var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) {Set(enemy,"maxHP",20000);Set(enemy,"currentHP",20000);Set(enemy,"attack",60);roots[enemy.gameObject]=enemy.transform.position;}
        foreach(var op in FindObjectsByType<Operator>(FindObjectsSortMode.None)) roots[op.gameObject]=op.transform.position;
        string baseline=null;
        foreach(int mode in new[]{1,2,3,0,-1}) {
            yield return new WaitForSecondsRealtime(2.4f);
            var access=Find<BattleAccessManager>();var front=access.FrontOperator;var middle=access.MiddleOperator;var back=access.BackOperator;
            foreach(var op in new[]{front,middle,back})Set(op,"currentHP",op.MaxHP/2);
            foreach(var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))Set(enemy,"currentHP",20000);
            var cards=FindObjectsByType<Card>(FindObjectsSortMode.InstanceID).Where(c=>c.isActiveAndEnabled).ToArray();
            Require(cards.Length>=6,"Expected six dealt cards");
            int[] values={2,1,1,3,2,4};for(int i=0;i<6;i++)cards[i].SetNumber(values[i]);
            var f=FrontCardSelectionManager.Instance;var m=MiddleCardSelectionManager.Instance;var b=BackCardSelectionManager.Instance;
            f.SetAttack();f.SelectCard(cards[0]);m.SetPhysicalMode();for(int i=1;i<=3;i++)m.SelectCard(cards[i]);b.SelectCard(cards[4]);b.SelectCard(cards[5]);
            var targeting=EnemyTargetingManager.Instance;
            foreach(var op in new[]{front,middle}) {
                targeting.BeginTargeting(op);
                foreach(var candidate in FindObjectsByType<EnemyTargetable>(FindObjectsSortMode.InstanceID)) {targeting.SelectTarget(candidate);if(targeting.GetSelectedTarget(op)!=null)break;}
                Require(targeting.GetSelectedTarget(op)!=null,"No legal target");
            }
            access.Refresh();Require(access.CanBattle,"Selections did not enable battle");
            TerminalPreferences.CombatSpeed=mode<=0?1:mode;TerminalPreferences.HitStop=true;TerminalPreferences.ReduceFlash=mode==3;
            hits.Clear();trace.Clear();float started=Time.realtimeSinceStartup;capturing=mode==1;frame=0;captureNext=0;
            manager.ExecuteBattle();
            if(mode==0){feedback.Skip();feedback.Skip();}
            bool paused=false;
            while(BattleExecuteManager.IsExecuting) {
                if(mode==-1 && hits.Count>0 && !feedback.Skipping)feedback.Skip();
                if(mode==2 && !paused && feedback.ActiveSequence!=null) {
                    Time.timeScale=0;int before=hits.Count;yield return new WaitForSecondsRealtime(.15f);
                    Require(hits.Count==before,"Combat advanced while settings paused");Time.timeScale=1;paused=true;
                }
                Require(Time.realtimeSinceStartup-started<15,"Turn stalled");yield return null;
            }
            capturing=false;
            Require(Mathf.Approximately(Time.timeScale,1),"Feedback changed global timeScale");
            Require(hits.Count>=7,"Missing heal or attack impact");
            Require(trace.Count==7,"Turn action count/order changed");
            Require(hits.Select(x=>x.actionId).Distinct().Count()==7,"MIDDLE duplicated logical damage");
            foreach(var hit in hits) {
                Require(hit.damage==Mathf.Abs(hit.hpBefore-hit.hpAfter),"Popup does not match actual HP change");
                if(hit.damageType==AttackDamageType.HEAL)Require(hit.targetIsAlly && hit.hpAfter>=hit.hpBefore,"Back damaged a target");
                else {int defense=hit.targetIsAlly?hit.target.GetComponent<Operator>().Defense:hit.target.GetComponent<Enemy>().Defense;
                    Require(hit.damage==Mathf.Min(hit.hpBefore,Mathf.Max(1,hit.attackValue-defense)),"Damage formula changed");}
                if(hit.attacker.GetComponent<Operator>()!=null)Require(hit.cards.Length>0,"Cards were lost during discard");
            }
            yield return new WaitForSecondsRealtime(.4f);
            foreach(var pair in roots)Require(Vector3.Distance(pair.Key.transform.position,pair.Value)<.001f,"Logical root moved");
            foreach(var visual in FindObjectsByType<CombatSpriteVisual>(FindObjectsSortMode.None)) {
                Require(!visual.Busy && visual.Pivot.localPosition.sqrMagnitude<.001f && Vector3.Distance(visual.Pivot.localScale,Vector3.one)<.001f,"Visual not restored");
            }
            string signature=string.Join("|",hits.Select(h=>$"{h.attacker.name}>{h.target.name}:{h.damageType}:{h.damage}:{h.hpAfter}"));
            if(baseline==null)baseline=signature;else Require(signature==baseline,"Speed/skip changed turn outcome or action order\n"+signature+"\n"+baseline);
            report.Add($"PASS mode {mode}: {hits.Count} confirmed results, 7 actions, {Time.realtimeSinceStartup-started-.4f:F2}s; identical HP and order.");
            Debug.Log("[Combat verification] "+report.Last());
        }
        Find<TerminalSettings>().OpenCombatOptions();yield return null;ScreenCapture.CaptureScreenshot(CombatFeedbackVerification.Output+"/settings.png");yield return new WaitForSecondsRealtime(.1f);
        Destroy(GameObject.Find("CombatOptionsWindow"));
        yield return new WaitForSecondsRealtime(2.4f);
        var remaining=FindObjectsByType<Enemy>(FindObjectsSortMode.None).First(e=>e.GetComponent<EnemyTargetable>().Position==EnemyPosition.FrontLeft);
        foreach(var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))if(enemy!=remaining)Destroy(enemy.gameObject);
        Set(remaining,"currentHP",1);yield return null;
        var lastAccess=Find<BattleAccessManager>();
        var lastCards=FindObjectsByType<Card>(FindObjectsSortMode.InstanceID).Where(c=>c.isActiveAndEnabled).ToArray();
        for(int i=0;i<6;i++)lastCards[i].SetNumber(i+1);
        FrontCardSelectionManager.Instance.SetAttack();FrontCardSelectionManager.Instance.SelectCard(lastCards[0]);
        MiddleCardSelectionManager.Instance.SetPhysicalMode();for(int i=1;i<=3;i++)MiddleCardSelectionManager.Instance.SelectCard(lastCards[i]);
        BackCardSelectionManager.Instance.SelectCard(lastCards[4]);BackCardSelectionManager.Instance.SelectCard(lastCards[5]);
        foreach(var op in new[]{lastAccess.FrontOperator,lastAccess.MiddleOperator}) {
            EnemyTargetingManager.Instance.BeginTargeting(op);EnemyTargetingManager.Instance.SelectTarget(remaining.GetComponent<EnemyTargetable>());
        }
        lastAccess.Refresh();Require(lastAccess.CanBattle,"Victory setup rejected");
        int oldPackets=MapRunState.Packets.Balance,oldExperience=MapRunState.Progression.Experience;
        TerminalPreferences.CombatSpeed=1;hits.Clear();frame=0;capturePrefix="victory";captureNext=0;capturing=true;
        manager.ExecuteBattle();float deadline=Time.realtimeSinceStartup+12;
        while(SceneManager.GetActiveScene().name!="Map_1F" || TransitionManager.IsBusy) {Require(Time.realtimeSinceStartup<deadline,"Victory did not return to map");yield return null;}
        capturing=false;
        Require(hits.Count(h=>h.isKill)==1,"Lethal impact duplicated");
        Require(MapRunState.Packets.Balance==oldPackets+RunSaveBindings.Instance.Progression.VictoryPackets,"Victory reward applied incorrectly");
        Require(MapRunState.Progression.Experience==oldExperience+RunSaveBindings.Instance.Progression.VictoryExperience,"Victory EXP duplicated or missing");
        report.Add("PASS: lethal impact, death/victory presentation, one victory reward and return to map.");
        CombatFeedbackVerification.Finish("PASS\nCommit gate: coroutine/Animation Event/reentrancy/skip\n"+string.Join("\n",report));
    }
    private void Update()
    {
        if(capturing && Time.realtimeSinceStartup>=captureNext) {captureNext=Time.realtimeSinceStartup+.0667f;ScreenCapture.CaptureScreenshot(CombatFeedbackVerification.Output+$"/{capturePrefix}-{frame++:D3}.png");}
    }
    private void OnDestroy()
    {
        TerminalPreferences.CombatSpeed=originalSpeed;TerminalPreferences.ReduceFlash=originalFlash;TerminalPreferences.HitStop=originalStop;TerminalPreferences.ShakeStrength=originalShake;
    }
}
