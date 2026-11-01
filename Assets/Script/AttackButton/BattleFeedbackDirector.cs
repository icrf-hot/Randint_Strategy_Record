using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class AttackPresentationRequest
{
    public GameObject attacker, target;
    public OperatorPosition slot;
    public AttackCardSnapshot[] cards = Array.Empty<AttackCardSnapshot>();
    public Func<AttackHitInfo[]> commit;
    public int inputValue;
    public AttackDamageType damageType;
}

/// <summary>Presentation clock, movement module and subscribers to confirmed impacts. Owns no combat rules.</summary>
public sealed class BattleFeedbackDirector : MonoBehaviour
{
    public AttackFeedbackLibrary library;
    public IAttackMovementModule Movement { get; set; } = new SpriteAttackMovement();
    public event Action<AttackSequence, AttackStage> StageEntered, StageExited;
    public event Action<GameObject, GameObject, AttackHitInfo> OnImpact;
    public event Action<AttackHitInfo, int> OnPresentationHit;
    public AttackSequence ActiveSequence { get; private set; }
    public bool Skipping { get; private set; }
    public bool Running { get; private set; }
    private Camera view;
    private Canvas canvas;
    private CombatFeedbackPool pool;
    private TMP_Text comboText;
    private TMP_Text speedText;
    private UnityEngine.UI.Button skipButton;
    private AttackFeedbackProfile profile;
    private int combo;
    private float stopRemaining, slowRemaining, slowSpeed = 1, shakeRemaining, shakeDuration, shakeAmplitude;
    private Vector3 shakeAxis, renderOffset;
    private AudioSource[] voices;
    private int voiceIndex;
    private readonly Dictionary<AudioClip, float> lastSound = new Dictionary<AudioClip, float>();
    private readonly Dictionary<Enemy, HealthBar> bars = new Dictionary<Enemy, HealthBar>();
    private Sprite ring;
    private Texture2D ringTexture;
    private AttackHitInfo lastConfirmedHit;
    private sealed class HealthBar
    {
        public RectTransform root, front, trail; public float delayed = 1, last = 1, delay; public Vector3 position;
    }
    public float Delta => Time.timeScale == 0 || TransitionManager.IsBusy ? 0 : Time.unscaledDeltaTime * TerminalPreferences.CombatSpeed;
    private float PresentationDelta => Delta * (slowRemaining > 0 ? slowSpeed : 1);
    private float ActorDelta => stopRemaining > 0 ? 0 : PresentationDelta;

    private void Awake()
    {
        library = Resources.Load<AttackFeedbackLibrary>("CombatFeedback/Library");
        view = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        canvas = TerminalUI.Canvas("CombatFeedback", 80); canvas.transform.SetParent(transform, false);
        pool = gameObject.AddComponent<CombatFeedbackPool>(); pool.Initialize((RectTransform)canvas.transform, view); pool.Delta = () => PresentationDelta;
        comboText = TerminalUI.Label("Combo", canvas.transform, "", new Vector2(340, 180), new Vector2(170, 60), 40, UITheme.Tone.Accent, true);
        var speed = TerminalUI.Button("CombatSpeed", canvas.transform, "", new Vector2(-150, 25), new Vector2(100, 34));
        AnchorBottomRight((RectTransform)speed.transform);
        speedText = speed.GetComponentInChildren<TMP_Text>();
        Action refresh = () => speedText.text = $"{TerminalPreferences.CombatSpeed}x";
        speed.onClick.AddListener(() => { TerminalPreferences.CombatSpeed = TerminalPreferences.CombatSpeed % 3 + 1; refresh(); }); refresh();
        skipButton = TerminalUI.Button("SkipFeedback", canvas.transform, "연출 스킵", new Vector2(-25, 25), new Vector2(120, 34));
        AnchorBottomRight((RectTransform)skipButton.transform); skipButton.onClick.AddListener(Skip);
        skipButton.interactable = false;
        voices = new AudioSource[8]; for (int i = 0; i < voices.Length; i++) { voices[i] = gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake = false; voices[i].spatialBlend = 0; }
        BuildRing();
        OnImpact += ImpactVisual; OnImpact += ImpactSound;
        RenderPipelineManager.beginCameraRendering += BeginRender; RenderPipelineManager.endCameraRendering += EndRender;
    }
    private static void AnchorBottomRight(RectTransform rect) { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 0); }
    public void BeginTurn()
    {
        Skipping = false; Running = true; combo = 0; comboText.text = ""; stopRemaining = slowRemaining = 0; skipButton.interactable = true;
        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None)) EnsureBar(enemy);
    }
    public void EndTurn() { Running = false; skipButton.interactable = false; }
    public void Skip()
    {
        if (!Running || Time.timeScale == 0 || TransitionManager.IsBusy) return;
        Skipping = true; stopRemaining = slowRemaining = shakeRemaining = 0;
        pool.Clear(); comboText.text = "";
        foreach (var visual in FindObjectsByType<CombatSpriteVisual>(FindObjectsSortMode.None)) visual.ResetVisual();
        foreach (var voice in voices) voice.Stop();
        // Pending actions still run through their ordered commit gates in BattleExecuteManager.
    }
    public IEnumerator Wait(float seconds)
    { float elapsed = 0; while (elapsed < seconds && !Skipping) { elapsed += PresentationDelta; yield return null; } }

    public IEnumerator Play(AttackPresentationRequest request)
    {
        if (library == null) { Debug.LogError("CombatFeedback/Library is missing; applying the original action without feedback.", this); request.commit?.Invoke(); yield break; }
        profile = library.For(request.attacker, request.slot);
        if (profile == null) { request.commit?.Invoke(); yield break; }
        var selectedProfile = profile;
        var visual = CombatSpriteVisual.Get(request.attacker);
        int visualVersion = visual != null ? visual.Acquire() : 0;
        Transform pivot = visual != null && visual.Sprite != null ? visual.Pivot : null;
        Vector3 start = pivot != null ? pivot.localPosition : Vector3.zero;
        Vector3 worldStart = pivot != null ? pivot.position : request.attacker.transform.position;
        Vector3 destination = request.target != null ? request.target.transform.position : worldStart;
        Vector3 direction = (destination - worldStart).normalized;
        if (direction.sqrMagnitude < .001f) direction = Vector3.right;
        var sequence = new AttackSequence(request.commit); ActiveSequence = sequence;
        sequence.StageEntered += (s, stage) => StageEntered?.Invoke(s, stage);
        sequence.StageExited += (s, stage) => StageExited?.Invoke(s, stage);
        sequence.OnImpact += PublishImpact;
        var relay = request.attacker.GetComponent<BattleAnimationImpactRelay>(); if (relay != null) relay.Bind(sequence);
        if (!Skipping) FlyCards(request, selectedProfile);
        try {
            yield return sequence.Run(selectedProfile, () => Skipping, () => PresentationDelta, Animate);
        } finally {
            if (visual != null && visual.Version == visualVersion) visual.ResetVisual();
            if (relay != null) relay.Unbind(sequence);
            if (ActiveSequence == sequence) ActiveSequence = null;
        }

        IEnumerator Animate(AttackStage stage, float duration)
        {
            float elapsed = 0; int ghosts = 0; bool projectile = selectedProfile.travel == AttackTravel.Projectile;
            Vector3 from = pivot != null ? pivot.position : worldStart;
            if (stage == AttackStage.Anticipation && !Skipping) {
                Ring(pool.Project(destination), 30, UITheme.Accent, duration);
                if (projectile) Ring(pool.Project(worldStart), 75, UITheme.Ok, duration + selectedProfile.dash);
            }
            if (stage == AttackStage.Dash && !Skipping) {
                Spark(pool.Project(worldStart), UITheme.TextDim, 3, .15f);
                if (projectile && TerminalPreferences.Motion) Projectile(worldStart, destination, selectedProfile);
            }
            int cosmeticHits = 0;
            while ((elapsed < duration || stage == AttackStage.Impact && stopRemaining > 0) && !Skipping) {
                float dt = ActorDelta; elapsed += dt;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(.001f, duration));
                if (pivot != null && visual.Version == visualVersion && stage != AttackStage.Impact) {
                    var pose = Movement.Evaluate(stage,t,from,worldStart,destination,direction,Pixels(1,worldStart),selectedProfile);
                    pivot.position = TerminalPreferences.Motion ? pose.position : worldStart;
                    pivot.localScale = TerminalPreferences.Motion ? pose.scale : Vector3.one;
                    if (stage == AttackStage.Anticipation && !TerminalPreferences.ReduceFlash && TerminalPreferences.Motion)
                        visual.SetFlash(Mathf.Sin(t * Mathf.PI) * .7f);
                }
                if (stage == AttackStage.Dash && !projectile && TerminalPreferences.Motion && selectedProfile.afterimages > 0 &&
                    t >= (ghosts+1f)/(selectedProfile.afterimages+1) && visual != null && visual.Sprite != null) {
                    Afterimage(visual.Sprite, selectedProfile.afterimageLife); ghosts++;
                }
                if (stage == AttackStage.Impact && selectedProfile.visualHits > 1 && cosmeticHits == 0 && elapsed >= selectedProfile.visualHitInterval) {
                    Spark(pool.Project(destination), UITheme.Accent, 3, .12f); cosmeticHits++;
                    OnPresentationHit?.Invoke(lastConfirmedHit, 1);
                    ImpactSound(request.attacker, request.target, lastConfirmedHit);
                    // Cosmetic echo only: no domain callback and no second damage number.
                }
                yield return null;
            }
            if (stage == AttackStage.Recovery && pivot != null) pivot.localPosition = start;
        }
    }
    private void PublishImpact(GameObject attacker, GameObject target, AttackHitInfo info)
    {
        lastConfirmedHit = info;
        if (OnImpact == null) return;
        foreach (Action<GameObject, GameObject, AttackHitInfo> subscriber in OnImpact.GetInvocationList())
            try { subscriber(attacker, target, info); } catch (Exception e) { Debug.LogException(e); }
    }
    private void FlyCards(AttackPresentationRequest request, AttackFeedbackProfile config)
    {
        if (request.cards == null || request.attacker == null) return;
        Vector2 end = pool.Project(request.attacker.transform.position) + Vector2.up * 85;
        float flightDuration = Mathf.Min(config.cardFlight, config.anticipation + config.dash);
        for (int i = 0; i < request.cards.Length; i++) {
            var card = request.cards[i]; Vector2 from = pool.Project(card.worldOrigin); int index = i;
            pool.Emit(card.label, null, new Vector2(85, 50), UITheme.Accent, flightDuration,
                (r,t) => { r.anchoredPosition = TerminalPreferences.Motion ? Vector2.Lerp(from, end + Vector2.right * (index-(request.cards.Length-1)*.5f)*22, 1-(1-t)*(1-t)) + Vector2.up*Mathf.Sin(t*Mathf.PI)*30 : end; r.localScale = Vector3.one * Mathf.Lerp(1, .65f, t); });
        }
        float duration = Mathf.Max(config.cardFlight, config.anticipation + config.dash);
        string type = request.damageType == AttackDamageType.HEAL ? "HEAL" : request.damageType.ToString();
        var banner = pool.Emit($"{type} {request.inputValue}", null, new Vector2(240, 38), UITheme.DamageTypeColor(request.damageType), duration + .08f,
            (r,t) => { r.anchoredPosition = end + Vector2.up * 35; r.localScale = Vector3.one * (TerminalPreferences.Motion ? 1 + Mathf.Sin(t * Mathf.PI) * .12f : 1); });
    }
    private void ImpactVisual(GameObject attacker, GameObject target, AttackHitInfo hit)
    {
        if (Skipping || profile == null) return;
        bool healing = hit.damageType == AttackDamageType.HEAL;
        Color color = healing ? UITheme.Ok : hit.isCritical ? UITheme.Danger : hit.outcome == AttackOutcome.Weak ? UITheme.Warn : hit.outcome == AttackOutcome.Resist ? UITheme.TextDim : UITheme.TextMain;
        Color effectColor = UITheme.DamageTypeColor(hit.damageType);
        Vector2 point = pool.Project(hit.position);
        int overlap = pool.ActiveCount % 4;
        float size = hit.isCritical ? 1.6f : hit.outcome == AttackOutcome.Weak ? 1.3f : hit.outcome == AttackOutcome.Resist ? .8f : 1;
        string text = hit.outcome == AttackOutcome.Miss ? "MISS" : hit.outcome == AttackOutcome.Null ? "NULL" : healing ? "+" + hit.damage : hit.damage.ToString();
        if (hit.outcome == AttackOutcome.Weak || hit.outcome == AttackOutcome.Resist) text += hit.outcome == AttackOutcome.Weak ? "\nWEAK" : "\nRESIST";
        pool.Emit(text, null, new Vector2(180, 50)*size, color, profile.popupLife, (r,t) => {
            r.anchoredPosition = point + new Vector2((overlap-1.5f)*15, 45+overlap*19+(TerminalPreferences.Motion ? t*75 : 0));
            r.localScale = Vector3.one * (hit.isCritical ? Mathf.LerpUnclamped(0, 1, EaseOutBack(Mathf.Min(1,t*5))) : 1);
            if (hit.isCritical && TerminalPreferences.Motion) r.localRotation = Quaternion.Euler(0,0,Mathf.Sin(t*40)*5*(1-t));
        });
        if (healing) { Ring(point, 65, effectColor, .3f); return; }
        if (!hit.targetIsAlly) {
            combo++; if (combo >= 2) { comboText.text = "x" + combo; StartCoroutine(PunchCombo()); }
        }
        float ratio = hit.maxHP > 0 ? hit.damage/(float)hit.maxHP : 0;
        int tier = ratio >= profile.largeDamageRatio ? 3 : ratio >= profile.mediumDamageRatio ? 2 : 1;
        Spark(point, effectColor, tier*3, .2f);
        if (tier >= 2 || profile.travel == AttackTravel.Melee) Ring(point, 45+tier*20, effectColor, .22f);
        if (combo >= 3) SpeedLines();
        if (TerminalPreferences.HitStop) stopRemaining = Mathf.Max(stopRemaining, hit.isKill ? profile.killHitStop : hit.isCritical ? profile.criticalHitStop : profile.hitStop);
        if (TerminalPreferences.Motion) {
            shakeDuration = profile.shakeTime * (hit.targetIsAlly ? 1.3f : 1); shakeRemaining = shakeDuration;
            shakeAmplitude = profile.shakePixels * Mathf.Lerp(.5f,1.5f,Mathf.Clamp01(ratio)) * TerminalPreferences.ShakeStrength * (hit.targetIsAlly ? 1.5f : 1);
            shakeAxis = hit.direction;
            if (!hit.isKill && target != null) StartCoroutine(React(target, hit.direction, profile));
        }
        if (hit.targetIsAlly) EdgeWarning();
        else if (tier == 3 && !TerminalPreferences.ReduceFlash && TerminalPreferences.Motion) ScreenFlash(effectColor, .05f, .12f);
        if (hit.isKill) {
            Spark(point, effectColor, 16, .45f, true);
            slowRemaining = profile.deathSlowDuration; slowSpeed = profile.deathPresentationSpeed;
        }
    }
    private IEnumerator React(GameObject target, Vector3 direction, AttackFeedbackProfile config)
    {
        var visual = CombatSpriteVisual.Get(target); if (visual == null || visual.Sprite == null) yield break;
        int version = visual.Acquire(); var start = visual.Pivot.localPosition;
        Vector3 offset = visual.Pivot.parent.InverseTransformVector(direction * Pixels(config.knockbackPixels, target.transform.position));
        float elapsed = 0;
        try {
            while (elapsed < config.knockbackTime && !Skipping && target != null && visual != null && visual.Sprite != null && visual.Version == version) {
                elapsed += ActorDelta; float t = Mathf.Clamp01(elapsed / Mathf.Max(.001f,config.knockbackTime));
                visual.Pivot.localPosition = start + offset * (t < .25f ? t*4 : 1-EaseOutBack((t-.25f)/.75f));
                if (!TerminalPreferences.ReduceFlash) visual.SetFlash(elapsed < config.flashTime ? 1 : 0);
                yield return null;
            }
        } finally { if (visual != null && visual.Version == version) visual.ResetVisual(); }
    }
    private void ImpactSound(GameObject attacker, GameObject target, AttackHitInfo hit)
    {
        if (Skipping || profile == null) return;
        float ratio = hit.maxHP > 0 ? hit.damage/(float)hit.maxHP : 0;
        AudioClip clip = hit.damageType == AttackDamageType.HEAL ? profile.heal : hit.isKill && profile.kill != null ? profile.kill :
            hit.isCritical && profile.critical != null ? profile.critical : ratio >= profile.largeDamageRatio ? profile.largeHit :
            ratio >= profile.mediumDamageRatio ? profile.mediumHit : profile.smallHit;
        if (clip == null) return;
        if (lastSound.TryGetValue(clip, out float last) && Time.unscaledTime-last < profile.sameClipCooldown) return;
        lastSound[clip] = Time.unscaledTime;
        var voice = voices[voiceIndex++ % voices.Length]; voice.clip = clip;
        voice.pitch = UnityEngine.Random.Range(.95f,1.05f)*Mathf.Pow(2,Mathf.Min(12,Mathf.Max(0,combo-1))/12f); voice.volume = profile.volume; voice.Play();
    }
    public IEnumerator Victory()
    {
        if (Skipping || profile == null) yield break;
        slowRemaining = profile.victorySlowDuration; slowSpeed = profile.deathPresentationSpeed;
        pool.Emit("VICTORY", null, new Vector2(420, 90), UITheme.Ok, .5f, (r,t) => { r.anchoredPosition = new Vector2(0,90); r.localScale = Vector3.one*(1+Mathf.Sin(t*Mathf.PI)*.08f); });
        yield return Wait(.15f);
    }
    public IEnumerator RewardGranted(int amount)
    {
        if (Skipping) yield break;
        float duration = profile != null ? profile.rewardFlight : .3f;
        var hud = FindFirstObjectByType<RunProgressionHUD>();
        if (hud != null) hud.HoldRewardDisplay(true);
        try {
            pool.Emit($"EXP +{amount}", null, new Vector2(180,45), UITheme.Ok, duration,
                (r,t) => r.anchoredPosition = Vector2.Lerp(new Vector2(0,90), pool.Root.rect.max-new Vector2(85,55),t));
            yield return Wait(duration);
        } finally { if (hud != null) hud.HoldRewardDisplay(false); }
    }
    private IEnumerator PunchCombo()
    { float t=0; while(t<.2f && !Skipping) { t+=PresentationDelta; comboText.transform.localScale=Vector3.one*(1+Mathf.Sin(Mathf.Clamp01(t/.2f)*Mathf.PI)*.25f); yield return null; } comboText.transform.localScale=Vector3.one; }
    private void Projectile(Vector3 from, Vector3 to, AttackFeedbackProfile config)
    {
        Vector2 start=pool.Project(from), end=pool.Project(to); int trail=0;
        pool.Emit(null,ring,new Vector2(18,18),UITheme.Ok,config.dash,(r,t)=> {
            r.anchoredPosition=Vector2.Lerp(start,end,t)+Vector2.up*Mathf.Sin(t*Mathf.PI)*config.projectileArcPixels;
            if (TerminalPreferences.Motion && t*8>trail) { Vector2 p=r.anchoredPosition; trail++; pool.Emit(null,ring,new Vector2(9,9),UITheme.Alpha(UITheme.Ok,.5f),.12f,(s,u)=>s.anchoredPosition=p); }
        });
    }
    private void Afterimage(SpriteRenderer source, float life)
    {
        Vector2 position=pool.Project(source.bounds.center);
        Vector2 size = new Vector2(Vector2.Distance(pool.Project(source.bounds.min),pool.Project(source.bounds.min+Vector3.right*source.bounds.size.x)),
            Vector2.Distance(pool.Project(source.bounds.min),pool.Project(source.bounds.min+Vector3.up*source.bounds.size.y)));
        pool.Emit(null,source.sprite,size,UITheme.Alpha(source.color,.4f),life,(r,t)=>r.anchoredPosition=position);
    }
    private void Ring(Vector2 position,float size,Color color,float life)
    { if (!TerminalPreferences.Motion) return; pool.Emit(null,ring,Vector2.one*size,color,life,(r,t)=>{r.anchoredPosition=position;r.localScale=Vector3.one*Mathf.Lerp(.35f,1.3f,t);}); }
    private void Spark(Vector2 position,Color color,int count,float life,bool fragments=false)
    {
        if (!TerminalPreferences.Motion) return;
        for(int i=0;i<count;i++) {
            Vector2 velocity=UnityEngine.Random.insideUnitCircle.normalized*UnityEngine.Random.Range(30,85); if(fragments) velocity.y=Mathf.Abs(velocity.y)+25;
            pool.Emit(null,null,fragments?new Vector2(5,5):new Vector2(3,12),color,life,(r,t)=>{r.anchoredPosition=position+velocity*t+Vector2.down*30*t*t;r.localRotation=Quaternion.Euler(0,0,t*180);});
        }
    }
    private void SpeedLines()
    {
        if (!TerminalPreferences.Motion) return;
        for(int i=0;i<5;i++) { Vector2 p=new Vector2(UnityEngine.Random.Range(-580,580),UnityEngine.Random.Range(-220,260));
            pool.Emit(null,null,new Vector2(100,1),UITheme.Alpha(UITheme.Accent,.15f),.15f,(r,t)=>r.anchoredPosition=p+Vector2.right*t*80); }
    }
    private void ScreenFlash(Color color,float life,float alpha)
    { pool.Emit(null,null,pool.Root.rect.size,UITheme.Alpha(color,alpha),life,(r,t)=>r.anchoredPosition=Vector2.zero); }
    private void EdgeWarning()
    {
        if (TerminalPreferences.ReduceFlash || !TerminalPreferences.Motion) return;
        for(int i=0;i<4;i++) { int edge=i; var size=i<2?new Vector2(12,pool.Root.rect.height):new Vector2(pool.Root.rect.width,12);
            pool.Emit(null,null,size,UITheme.Alpha(UITheme.Danger,.35f),.2f,(r,t)=>r.anchoredPosition=edge<2?new Vector2((edge==0?-1:1)*pool.Root.rect.width*.5f,0):new Vector2(0,(edge==2?-1:1)*pool.Root.rect.height*.5f)); }
    }
    private void EnsureBar(Enemy enemy)
    {
        if (bars.ContainsKey(enemy)) return;
        var root=TerminalUI.Panel("EnemyHP",canvas.transform,Vector2.zero,new Vector2(80,6),UITheme.Tone.Hover).rectTransform;
        var trail=TerminalUI.Panel("LostHP",root,Vector2.zero,new Vector2(80,6),UITheme.Tone.Warn).rectTransform;
        var front=TerminalUI.Panel("HP",root,Vector2.zero,new Vector2(80,6),UITheme.Tone.Ok).rectTransform;
        foreach(var rect in new[]{trail,front}) {rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,.5f); rect.anchoredPosition=Vector2.zero;}
        bars.Add(enemy,new HealthBar{root=root,front=front,trail=trail});
    }
    private void Update()
    {
        string speedLabel = $"{TerminalPreferences.CombatSpeed}x";
        if (speedText != null && speedText.text != speedLabel) speedText.text = speedLabel;
        stopRemaining=Mathf.Max(0,stopRemaining-PresentationDelta); slowRemaining=Mathf.Max(0,slowRemaining-Delta); shakeRemaining=Mathf.Max(0,shakeRemaining-PresentationDelta);
        foreach(var pair in bars) {
            var enemy=pair.Key; var bar=pair.Value;
            float value=enemy!=null && enemy.MaxHP>0?enemy.CurrentHP/(float)enemy.MaxHP:0;
            if(enemy!=null) bar.position=enemy.transform.position;
            bar.root.anchoredPosition=pool.Project(bar.position)+Vector2.up*65;
            if(value<bar.last) bar.delay=profile!=null?profile.hpDelay:.4f;
            if(Skipping) {bar.delayed=value;bar.delay=0;}
            else if(bar.delay>0) bar.delay-=PresentationDelta;
            else bar.delayed=Mathf.MoveTowards(bar.delayed,value,PresentationDelta/Mathf.Max(.01f,profile!=null?profile.hpCatchup:.2f));
            bar.front.sizeDelta=new Vector2(80*value,6); bar.trail.sizeDelta=new Vector2(80*bar.delayed,6); bar.last=value;
            bar.root.gameObject.SetActive(value>0 || bar.delayed>0);
        }
    }
    private float Pixels(float pixels,Vector3 world)
    { if(view==null)return pixels*.01f; var p=view.WorldToScreenPoint(world); return Vector3.Distance(view.ScreenToWorldPoint(p),view.ScreenToWorldPoint(p+Vector3.right*pixels)); }
    private void BeginRender(ScriptableRenderContext context,Camera camera)
    {
        if(camera!=view || shakeRemaining<=0 || !TerminalPreferences.Motion || Delta==0)return;
        float amount=Pixels(shakeAmplitude,view.transform.position+view.transform.forward*10)*shakeRemaining/Mathf.Max(.001f,shakeDuration);
        Vector3 axis=shakeAxis.sqrMagnitude>.001f?shakeAxis.normalized:view.transform.right;
        renderOffset=(axis*UnityEngine.Random.Range(-1f,1f)+view.transform.up*UnityEngine.Random.Range(-.25f,.25f))*amount;
        camera.transform.position+=renderOffset;
    }
    private void EndRender(ScriptableRenderContext context,Camera camera) {if(camera!=view)return;camera.transform.position-=renderOffset;renderOffset=Vector3.zero;}
    private void BuildRing()
    {
        ringTexture=new Texture2D(64,64,TextureFormat.RGBA32,false); var colors=new Color[4096];
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32));colors[y*64+x]=UITheme.Alpha(UITheme.TextureTint,Mathf.Clamp01(1-Mathf.Abs(d-28)/2));}
        ringTexture.SetPixels(colors);ringTexture.Apply();ring=Sprite.Create(ringTexture,new Rect(0,0,64,64),new Vector2(.5f,.5f));
    }
    private static float EaseOutBack(float t) { const float c=1.70158f;return 1+(c+1)*Mathf.Pow(t-1,3)+c*Mathf.Pow(t-1,2); }
    private void OnDestroy()
    {
        RenderPipelineManager.beginCameraRendering-=BeginRender;RenderPipelineManager.endCameraRendering-=EndRender;
        if(view!=null)view.transform.position-=renderOffset;
        if(ring!=null)Destroy(ring);if(ringTexture!=null)Destroy(ringTexture);
    }
}
