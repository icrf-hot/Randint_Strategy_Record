using UnityEngine;

public sealed partial class BattleFeedbackDirector
{
    private ActionFraming currentFraming;
    private sealed class ActionFraming
    {
        public Vector3 position;
        public float size, fieldOfView;
        public AttackPresentationRequest request;
        public AttackFeedbackProfile config;
        public OperatorFocusManager focus;
        public Vector3 actorCenter, targetCenter;
    }

    private ActionFraming BeginActionFraming(AttackPresentationRequest request, AttackFeedbackProfile config)
    {
        if (view == null || !TerminalPreferences.Motion || Skipping || config.focusZoom >= 1) return null;
        var framing = new ActionFraming { position = view.transform.position, size = view.orthographicSize,
            fieldOfView = view.fieldOfView, request = request, config = config, focus = OperatorFocusManager.Instance,
            actorCenter = VisualCenter(request.attacker), targetCenter = VisualCenter(request.target) };
        if (framing.focus != null) framing.focus.PresentationCameraActive = true;
        currentFraming = framing;
        return framing;
    }

    private void UpdateActionFraming(ActionFraming framing, AttackStage stage, float progress)
    {
        if (framing == null || view == null) return;
        bool followShot = framing.config.motion == CombatMotion.Shooting;
        float blend = stage == AttackStage.Anticipation ? Mathf.SmoothStep(0, 1, progress) :
            stage == AttackStage.Recovery ? 1-Mathf.SmoothStep(0, 1, progress) : 1;
        if (followShot) blend = stage == AttackStage.Anticipation ? Mathf.SmoothStep(0, 1, progress) :
            stage == AttackStage.Dash ? 1-Mathf.SmoothStep(0, 1, progress) : 0;
        if (!TerminalPreferences.Motion || Skipping) blend = 0;
        Vector3 actor = VisualCenter(framing.request.attacker);
        Vector3 target = framing.request.target != null ? VisualCenter(framing.request.target) : actor;
        Vector3 center = Vector3.Lerp(actor, target, framing.config.focusTargetBias);
        if (followShot) {
            // First frame the weapon user; only after launch follow the projectile toward its target.
            actor = framing.actorCenter;
            target = framing.targetCenter;
            center = stage == AttackStage.Dash ? Vector3.Lerp(actor, target, progress) : actor;
        }
        Vector3 pan = Vector3.ProjectOnPlane(center-framing.position, view.transform.forward);
        view.transform.position = framing.position + pan * blend;
        view.orthographicSize = framing.size * Mathf.Lerp(1, framing.config.focusZoom, blend);
        view.fieldOfView = framing.fieldOfView * Mathf.Lerp(1, framing.config.focusZoom, blend);
        if (followShot && blend <= 0) return; // Impact and HP animation use the restored battlefield view.
        // Fit both ends even when the scene uses a perspective camera or an offset sprite pivot.
        // Zoom may ease back slightly to keep the shot's cause and target visible together.
        float requiredSize = 0, requiredTangent = 0;
        var framingPoints = followShot ? new[] { center } : new[] { actor, target, framing.request.attacker.transform.position,
            framing.request.target != null ? framing.request.target.transform.position : actor };
        foreach (var point in framingPoints)
        {
            Vector3 relative = point - view.transform.position;
            float extent = Mathf.Max(Mathf.Abs(Vector3.Dot(relative, view.transform.up)),
                Mathf.Abs(Vector3.Dot(relative, view.transform.right)) / Mathf.Max(.01f, view.aspect));
            requiredSize = Mathf.Max(requiredSize, extent * 1.15f);
            float depth = Vector3.Dot(relative, view.transform.forward);
            if (depth > .01f) requiredTangent = Mathf.Max(requiredTangent, extent * 1.15f / depth);
        }
        if (view.orthographic) view.orthographicSize = Mathf.Max(view.orthographicSize, requiredSize);
        else view.fieldOfView = Mathf.Max(view.fieldOfView, Mathf.Atan(requiredTangent)*2*Mathf.Rad2Deg);
    }

    private void EndActionFraming(ActionFraming framing)
    {
        if (framing == null) return;
        if (view != null) {
            view.transform.position = framing.position;
            view.orthographicSize = framing.size;
            view.fieldOfView = framing.fieldOfView;
        }
        if (framing.focus != null) framing.focus.PresentationCameraActive = false;
        if (currentFraming == framing) currentFraming = null;
    }

    private static Vector3 VisualCenter(GameObject actor)
    {
        if (actor == null) return Vector3.zero;
        var visual = actor.GetComponent<CombatSpriteVisual>();
        return visual != null && visual.Sprite != null ? visual.Sprite.bounds.center : actor.transform.position;
    }

    private void RoleAnticipation(AttackPresentationRequest request, AttackFeedbackProfile config, Vector3 start, Vector3 destination, float duration)
    {
        if (request.action == CombatActionKind.Defense) {
            Ring(pool.Project(start), 85, UITheme.Accent, duration + config.dash);
            return;
        }
        bool healing = request.action == CombatActionKind.Heal;
        Color color = UITheme.DamageTypeColor(request.damageType);
        Ring(pool.Project(destination), healing ? 50 : 30, color, duration);
        if (config.motion == CombatMotion.Shooting) {
            // Brackets convey aiming without treating the whole sprite as a melee dash.
            Vector2 aim = pool.Project(VisualCenter(request.target));
            for (int side = -1; side <= 1; side += 2) {
                int sign = side;
                pool.Emit("[", null, new Vector2(30, 40), color, duration,
                    (r,t) => { r.anchoredPosition = aim + Vector2.right * sign * Mathf.Lerp(55, 24, t); r.localScale = new Vector3(-sign, 1, 1); });
            }
        }
        if (healing || config.motion == CombatMotion.Casting)
            Ring(pool.Project(start), 75, color, duration + config.dash);
    }

    private void RoleProjectile(AttackPresentationRequest request, Vector3 from, Vector3 to, AttackFeedbackProfile config)
    {
        bool healing = request.action == CombatActionKind.Heal;
        Color color = UITheme.DamageTypeColor(request.damageType);
        if (healing && request.targets.Length > 0) {
            foreach (var target in request.targets)
                if (target != null) EmitProjectile(VisualCenter(request.attacker), VisualCenter(target), config, color, false);
        }
        else EmitProjectile(VisualCenter(request.attacker), VisualCenter(request.target), config, color,
            config.motion == CombatMotion.Shooting);
        if (!healing) {
            Ring(pool.Project(VisualCenter(request.attacker)), 25, color, .09f);
            if (config.motion == CombatMotion.Shooting) Spark(pool.Project(VisualCenter(request.attacker)), color, 4, .12f);
        }
    }

    private void EmitProjectile(Vector3 from, Vector3 to, AttackFeedbackProfile config, Color color, bool shot)
    {
        int trail = 0;
        Vector2 size = shot ? new Vector2(config.projectileLength, config.projectileWidth) : new Vector2(20, 20);
        pool.Emit(null, shot ? null : ring, size, color, config.dash, (r,t) => {
            Vector2 start = pool.Project(from), end = pool.Project(to);
            r.anchoredPosition = Vector2.Lerp(start, end, t) + Vector2.up * Mathf.Sin(t*Mathf.PI) * config.projectileArcPixels;
            if (shot) r.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(end.y-start.y, end.x-start.x)*Mathf.Rad2Deg);
            if (t*8 > trail) {
                Vector2 position = r.anchoredPosition; trail++;
                pool.Emit(null, shot ? null : ring, size*.45f, UITheme.Alpha(color, .45f), .1f,
                    (ghost,u) => { ghost.anchoredPosition = position; ghost.localRotation = r.localRotation; });
            }
        });
    }

    private void GuardFeedback(AttackPresentationRequest request, AttackFeedbackProfile config)
    {
        if (Skipping) return;
        Vector2 center = pool.Project(VisualCenter(request.attacker));
        float life = Mathf.Max(.35f, config.impact + config.recovery);
        Vector2[] outline = { new Vector2(-34, 28), new Vector2(34, 28), new Vector2(30, -12), new Vector2(0, -40), new Vector2(-30, -12), new Vector2(-34, 28) };
        for (int i = 0; i < outline.Length-1; i++) {
            Vector2 a = outline[i], b = outline[i+1], delta = b-a;
            pool.Emit(null, null, new Vector2(delta.magnitude, 3), UITheme.Accent, life, (r,t) => {
                r.anchoredPosition = center + (a+b)*.5f;
                r.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x)*Mathf.Rad2Deg);
                r.localScale = Vector3.one * (TerminalPreferences.Motion ? 1 + Mathf.Sin(t*Mathf.PI)*.1f : 1);
            });
        }
        pool.Emit($"GUARD\nDEF +{request.defenseBonus} / RES +{request.resistanceBonus}", null,
            new Vector2(290, 36), UITheme.Accent, life, (r,t) => r.anchoredPosition = center + Vector2.up*85);
    }

    private void HealingFeedback(AttackHitInfo hit, AttackFeedbackProfile config)
    {
        Vector2 center = pool.Project(hit.position);
        Ring(center, 70, UITheme.Ok, .35f);
        for (int i = 0; i < 3; i++) {
            int index = i;
            pool.Emit("+", null, new Vector2(28, 35), UITheme.Ok, .4f,
                (r,t) => r.anchoredPosition = center + new Vector2((index-1)*24, 15 + (TerminalPreferences.Motion ? t*65 : 0)));
        }
    }
}
