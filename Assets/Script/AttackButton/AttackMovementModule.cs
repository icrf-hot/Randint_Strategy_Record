using UnityEngine;

public struct AttackVisualPose { public Vector3 position, scale; }

/// <summary>Replace with root-motion or stationary animation without changing commit/feedback code.</summary>
public interface IAttackMovementModule
{
    AttackVisualPose Evaluate(AttackStage stage, float t, Vector3 from, Vector3 origin, Vector3 target,
        Vector3 direction, float worldUnitsPerPixel, AttackFeedbackProfile profile);
}

public sealed class SpriteAttackMovement : IAttackMovementModule
{
    public AttackVisualPose Evaluate(AttackStage stage, float t, Vector3 from, Vector3 origin, Vector3 target,
        Vector3 direction, float worldUnitsPerPixel, AttackFeedbackProfile profile)
    {
        Vector3 to = stage == AttackStage.Anticipation ? origin-direction*profile.retreatPixels*worldUnitsPerPixel :
            stage == AttackStage.Dash ? Vector3.Lerp(origin,target,profile.travel==AttackTravel.Projectile?0:profile.approach) : origin;
        float ease = stage == AttackStage.Dash ? t*t*t : stage == AttackStage.Recovery ? 1-(1-t)*(1-t) : t;
        Vector2 scale = stage == AttackStage.Anticipation ? Vector2.Lerp(Vector2.one,profile.squash,t) :
            stage == AttackStage.Dash ? Vector2.Lerp(profile.squash,profile.stretch,t) : Vector2.Lerp(profile.stretch,Vector2.one,t);
        return new AttackVisualPose { position=Vector3.Lerp(from,to,ease), scale=new Vector3(scale.x,scale.y,1) };
    }
}
