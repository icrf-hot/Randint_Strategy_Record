using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Opt-in current-party presentation checks. Never spawns or invokes supporter conversion.</summary>
[InitializeOnLoad]
public static class RoleFeedbackVerification
{
    private const string Key = "RoleFeedback.Verify.";
    public static string Output => SessionState.GetString(Key+"Output", "");
    public static bool RetreatOnly => SessionState.GetBool(Key+"RetreatOnly", false);
    private static RetreatSelectionProbe retreatProbe;
    static RoleFeedbackVerification()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key+"Running", false))
            {
                if (RetreatOnly)
                {
                    var host = UnityEngine.Object.FindFirstObjectByType<BattleExecuteManager>();
                    if (host == null) { Finish("FAIL: battle coroutine host missing"); return; }
                    retreatProbe = new RetreatSelectionProbe();
                    host.StartCoroutine(retreatProbe.Run());
                }
                else new GameObject("RoleFeedbackVerification").AddComponent<RoleFeedbackProbe>();
            }
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key+"Restore", false)) {
                SessionState.SetBool(Key+"Restore", false);
                string scene = SessionState.GetString(Key+"Scene", "");
                EditorApplication.delayCall += () => { if (!string.IsNullOrEmpty(scene)) EditorSceneManager.OpenScene(scene); };
            }
        };
        Application.logMessageReceived += (message, trace, type) => {
            if (EditorApplication.isPlaying && SessionState.GetBool(Key+"Running", false) &&
                (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Finish("FAIL: "+message+"\n"+trace);
        };
        EditorApplication.update += () => {
            if (SessionState.GetBool(Key+"Running", false) && DateTime.UtcNow >
                DateTime.Parse(SessionState.GetString(Key+"Deadline", "")).ToUniversalTime()) Finish("FAIL: timeout");
        };
    }
    public static void Run(string output, bool retreatOnly = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        for (int i=0; i<SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scene edits first.");
        Directory.CreateDirectory(output);
        SessionState.SetBool(Key+"RetreatOnly", retreatOnly);
        SessionState.SetString(Key+"Output", output);
        SessionState.SetString(Key+"Scene", SceneManager.GetActiveScene().path);
        SessionState.SetBool(Key+"Restore", true);
        SessionState.SetBool(Key+"Running", true);
        SessionState.SetString(Key+"Deadline", DateTime.UtcNow.AddSeconds(75).ToString("O"));
        EditorSceneManager.OpenScene("Assets/Scenes/Playing_Scene.unity");
        EditorApplication.EnterPlaymode();
    }
    public static void Finish(string result)
    {
        if (!SessionState.GetBool(Key+"Running", false)) return;
        SessionState.SetBool(Key+"Running", false);
        File.WriteAllText(Path.Combine(Output, "result.txt"), result);
        retreatProbe?.Dispose(); retreatProbe=null;
        Debug.Log("[Role feedback verification] "+result);
        EditorApplication.ExitPlaymode();
    }
}

public sealed class RoleFeedbackProbe : MonoBehaviour
{
    private int originalSpeed;
    private IDisposable scope;
    private Keyboard testKeyboard;
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static object Invoke(object actor, string name) => actor.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(actor, null);
    private static void HP(object actor, int value) => actor.GetType().GetField("currentHP", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(actor, value);

    private IEnumerator Start()
    {
        originalSpeed = TerminalPreferences.CombatSpeed;
        TerminalPreferences.CombatSpeed = 1;
        scope = RunSaveService.UseRepositoryForVerification(new RunSaveRepository(Path.Combine(RoleFeedbackVerification.Output, "isolated-saves"), RunSaveBindings.Instance));
        yield return new WaitForSecondsRealtime(3);
        var manager = FindFirstObjectByType<BattleExecuteManager>();
        Invoke(manager, "LoadOperators");
        var feedback = manager.Feedback;
        var access = BattleAccessManager.Instance;
        var front = access.FrontOperator; var middle = access.MiddleOperator; var back = access.BackOperator;
        var settings = FindFirstObjectByType<TerminalSettings>();
        var focus = OperatorFocusManager.Instance;
        focus.EnterFocus(front.GetComponent<OperatorClickHandler>());
        var keyboard = Keyboard.current;
        if (keyboard == null) keyboard = testKeyboard = InputSystem.AddDevice<Keyboard>();
        yield return Escape();
        Require(!settings.IsSettingsOpen && Time.timeScale==1, "First ESC also opened settings or left selection slow motion.");
        yield return Escape();
        Require(settings.IsSettingsOpen && Time.timeScale==0, "Second ESC did not pause/open settings.");
        yield return Escape();
        Require(!settings.IsSettingsOpen && Time.timeScale==1, "Third ESC did not close/resume settings.");
        IEnumerator Escape()
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null; yield return null;
        }
        var cards = FindObjectsByType<Card>(FindObjectsSortMode.InstanceID).Where(card => card.isActiveAndEnabled).ToArray();
        Require(cards.Length==6, "Six cards not dealt.");
        for (int i=0; i<6; i++) cards[i].SetNumber(i+1);
        FrontCardSelectionManager.Instance.SetAttack(); FrontCardSelectionManager.Instance.SelectCard(cards[0]);
        MiddleCardSelectionManager.Instance.SetPhysicalMode();
        for (int i=1; i<=3; i++) MiddleCardSelectionManager.Instance.SelectCard(cards[i]);
        BackCardSelectionManager.Instance.SelectCard(cards[4]); BackCardSelectionManager.Instance.SelectCard(cards[5]);
        foreach (var actor in new[] { front, middle }) {
            EnemyTargetingManager.Instance.BeginTargeting(actor);
            foreach (var candidate in FindObjectsByType<EnemyTargetable>(FindObjectsSortMode.InstanceID)) {
                EnemyTargetingManager.Instance.SelectTarget(candidate);
                if (EnemyTargetingManager.Instance.GetSelectedTarget(actor)!=null) break;
            }
            Require(EnemyTargetingManager.Instance.GetSelectedTarget(actor)!=null, "No target for "+actor.name);
        }
        OperatorFocusManager.Instance.ExitFocus();
        yield return new WaitForSecondsRealtime(.8f);
        var report = new List<string> { "PASS: keyboard ESC selection cancel -> settings pause -> settings close, one action per press" };
        foreach (string action in new[] { "front-attack", "front-defense", "dorothy-shot", "back-heal", "dorothy-skip" }) {
            foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) {
                enemy.GetType().GetField("maxHP", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, 20000);
                HP(enemy, 20000);
            }
            typeof(BattleExecuteManager).GetField("pendingPresentation", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, null);
            foreach (var actor in new[] { front, middle, back }) HP(actor, actor.MaxHP/2);
            if (action=="front-defense") FrontCardSelectionManager.Instance.SetDefense();
            Invoke(manager, "SnapshotCards");
            string method = action.StartsWith("front") ? "ExecuteFrontAction" : action.StartsWith("dorothy") ? "ExecuteMiddleAction" : "ExecuteBackAction";
            Invoke(manager, method);
            var request = (AttackPresentationRequest)typeof(BattleExecuteManager).GetField("pendingPresentation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manager);
            Require(request!=null, "Action did not enqueue.");
            var profile = feedback.library.For(request.attacker, request.slot, request.action);
            if (action.StartsWith("dorothy")) Require(profile.motion==CombatMotion.Shooting && profile.focusZoom<1, "Dorothy override missing.");
            if (action=="back-heal") Require(profile.motion==CombatMotion.Healing && request.targets.Length>0, "Heal template/targets missing.");
            int impacts=0, guards=0;
            float hitAt=-1;
            Action<GameObject, GameObject, AttackHitInfo> onHit=(a,t,h)=> { impacts++; hitAt=Time.realtimeSinceStartup; };
            Action<AttackPresentationRequest> onGuard=r=>guards++;
            feedback.OnImpact+=onHit; feedback.OnDefense+=onGuard;
            var camera = Camera.main;
            Vector3 position=camera.transform.position, root=request.attacker.transform.position;
            float size=camera.orthographicSize, smallest=size;
            float fov=camera.fieldOfView;
            int targetHP=request.target.GetComponent<Enemy>()!=null ? request.target.GetComponent<Enemy>().CurrentHP : 0;
            feedback.BeginTurn();
            float started=Time.realtimeSinceStartup;
            var routine=feedback.Play(request);
            var running=StartCoroutine(routine);
            bool captured=false;
            yield return null;
            if (action.EndsWith("skip")) feedback.Skip();
            while (feedback.ActiveSequence!=null) {
                smallest=Mathf.Min(smallest,camera.orthographicSize);
                if (action=="dorothy-shot" && feedback.ActiveSequence.Stage==AttackStage.Dash)
                    Require(request.target.GetComponent<Enemy>().CurrentHP==targetHP, "Damage applied before projectile arrival.");
                if (action=="dorothy-shot" && feedback.ActiveSequence.Stage==AttackStage.Impact) {
                    Vector3 targetView=camera.WorldToViewportPoint(request.target.transform.position);
                    Require(targetView.x>0 && targetView.x<1, "Shot target left the frame.");
                    Require(Vector3.Distance(camera.transform.position,position)<.04f && Mathf.Abs(camera.fieldOfView-fov)<.01f &&
                        Mathf.Abs(camera.orthographicSize-size)<.01f, "Shot did not zoom out before impact.");
                }
                if (!captured && feedback.ActiveSequence.Stage==(action=="front-defense" ? AttackStage.Recovery : AttackStage.Dash)) {
                    ScreenCapture.CaptureScreenshot(Path.Combine(RoleFeedbackVerification.Output, action+".png")); captured=true;
                }
                yield return null;
            }
            yield return running;
            feedback.EndTurn();
            if (action=="dorothy-shot") {
                float expected=(profile.anticipation+profile.dash+profile.impact+profile.recovery)/feedback.library.basePlaybackSpeed;
                Require(Time.realtimeSinceStartup-started>=expected*.9f, "Presentation did not use slower baseline.");
                Require(hitAt>0 && Time.realtimeSinceStartup-hitAt>=(profile.hpDelay+profile.hpCatchup)/feedback.library.basePlaybackSpeed*.9f,
                    "Next actor released before the HP damage trail could finish.");
                report.Add("PASS: slow baseline shot elapsed="+(Time.realtimeSinceStartup-started).ToString("0.00")+"s (minimum profile "+expected.ToString("0.00")+"s)");
                report.Add("PASS: zoom before launch, no flight damage, wide impact and HP result hold="+(Time.realtimeSinceStartup-hitAt).ToString("0.00")+"s");
            }
            feedback.OnImpact-=onHit; feedback.OnDefense-=onGuard;
            Require(Vector3.Distance(root, request.attacker.transform.position)<.001f, "Logical actor moved.");
            Require(Vector3.Distance(position,camera.transform.position)<.001f && Mathf.Abs(size-camera.orthographicSize)<.001f, "Camera did not restore.");
            Require(!OperatorFocusManager.Instance.PresentationCameraActive, "Camera lease leaked.");
            if (action=="front-defense") Require(guards==1 && impacts==0, "Guard caused damage/duplicate event.");
            else Require(impacts==(action=="back-heal" ? request.targets.Length : 1), "Impact count changed.");
            if (action=="dorothy-shot") Require(smallest<size*.95f, "Shot never zoomed.");
            report.Add("PASS: "+action+" (impacts="+impacts+", guard="+guards+")");
            yield return new WaitForSecondsRealtime(.7f);
        }
        RoleFeedbackVerification.Finish(string.Join("\n", report)+"\nSupporter/damage-healing conversion deliberately not invoked. Balance/full-turn tests not run.");
    }
    private void OnDestroy() { if(testKeyboard!=null)InputSystem.RemoveDevice(testKeyboard); TerminalPreferences.CombatSpeed=originalSpeed; scope?.Dispose(); }
}
