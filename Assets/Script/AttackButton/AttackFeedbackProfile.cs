using UnityEngine;

public enum AttackTravel { Melee, ShortDash, Projectile }

[CreateAssetMenu(menuName = "Randint/Combat/Attack Feedback Profile")]
public sealed class AttackFeedbackProfile : ScriptableObject
{
    public AttackTravel travel;
    public bool animationDrivenImpact;
    [Min(0)] public float anticipation = .15f, dash = .12f, impact = .05f, recovery = .2f;
    [Min(0)] public float retreatPixels = 10, knockbackPixels = 15, shakePixels = 4;
    [Min(0)] public float hitStop = .075f, criticalHitStop = .12f, killHitStop = .15f;
    [Min(0)] public float flashTime = .08f, knockbackTime = .18f, shakeTime = .15f;
    [Range(0, 1)] public float approach = .85f;
    public Vector2 squash = new Vector2(1.1f, .88f), stretch = new Vector2(1.15f, .9f);
    [Range(0, 4)] public int afterimages = 4;
    [Min(.01f)] public float afterimageLife = .2f, projectileArcPixels = 65;
    [Range(1, 2)] public int visualHits = 1;
    [Min(0)] public float visualHitInterval = .08f;
    [Header("Shared feedback")]
    [Min(.01f)] public float popupLife = .6f, cardFlight = .2f, hpDelay = .4f, hpCatchup = .2f;
    [Min(.01f)] public float rewardFlight = .3f;
    [Range(0, 1)] public float mediumDamageRatio = .15f, largeDamageRatio = .35f;
    [Min(0)] public float deathSlowDuration = .25f, victorySlowDuration = .5f;
    [Range(.05f, 1)] public float deathPresentationSpeed = .3f;
    [Header("Optional audio (no synthetic clips substituted)")]
    public AudioClip smallHit, mediumHit, largeHit, critical, kill, heal;
    [Range(0, 1)] public float volume = .7f;
    [Min(0)] public float sameClipCooldown = .05f;
}
