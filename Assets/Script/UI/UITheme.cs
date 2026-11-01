using TMPro;
using UnityEngine;

/// <summary>Single source of terminal palette, typography, spacing and effect timing.</summary>
[CreateAssetMenu(menuName = "Randint/UI Theme")]
public sealed class UITheme : ScriptableObject
{
    public TMP_FontAsset titleFont;
    public TMP_FontAsset monoFont;
    private static UITheme active;
    public static UITheme Active => active != null ? active : active = Resources.Load<UITheme>("UITheme");
    public static TMP_FontAsset Font => Active != null && Active.titleFont != null ? Active.titleFont : TMP_Settings.defaultFontAsset;
    public static TMP_FontAsset Mono => Active != null && Active.monoFont != null ? Active.monoFont : Font;
    public static readonly Color BgBase = new Color32(0x0B, 0x12, 0x20, 255);
    public static readonly Color BgPanel = new Color32(0x11, 0x1A, 0x2B, 255);
    public static readonly Color BgPanelHi = new Color32(0x1A, 0x27, 0x40, 255);
    public static readonly Color Line = new Color32(0x24, 0x34, 0x4F, 255);
    public static readonly Color Accent = new Color32(0x2E, 0xC4, 0xD6, 255);
    public static readonly Color Warn = new Color32(0xFF, 0xB0, 0x00, 255);
    public static readonly Color Danger = new Color32(0xFF, 0x4D, 0x5E, 255);
    public static readonly Color Ok = new Color32(0x4A, 0xDE, 0x9A, 255);
    public static readonly Color TextMain = new Color32(0xD6, 0xE2, 0xF0, 255);
    public static readonly Color TextDim = new Color32(0x7F, 0x93, 0xAD, 255);
    // Neutral texture multiplier, used only to preserve existing illustration colours.
    public static readonly Color TextureTint = new Color32(255, 255, 255, 255);
    // Restored battlefield surfaces; kept here so screen code still has no literal colours.
    public static readonly Color BattleFloor = new Color(.4339623f, .4339623f, .4339623f, 1);
    public static readonly Color BattleUnderfloor = new Color(.2924528f, .2924528f, .2924528f, 1);
    public const float InfoPanelOpacity = .45f;
    public static Color Clear => Alpha(BgBase, 0);
    public static Color DamageTypeColor(AttackDamageType type) => type == AttackDamageType.HEAL ? Ok : type == AttackDamageType.ARTS ? Accent : TextMain;
    public const float Border = 1, Radius = 0, ButtonTime = .08f, PulseTime = .2f;
    public const float GlitchTime = .6f, TransitionTime = .25f, AudioTime = .3f, ShakeTime = .15f;
    public const float GridAlpha = .04f, ScanAlpha = .03f, FloorAlpha = .15f, GlowAlpha = .25f;
    public const float StatusPeriod = 2f, Gap = 20f, TitleBarHeight = 28f;
    public const int ParticleLimit = 200;
    public const int CardSortBase = 5000, CardSortStride = 32, CardSortRaise = 10000;
    public enum Tone { Base, Panel, Hover, Line, Accent, Warn, Danger, Ok, Main, Dim, Clear, Texture, BattleFloor, BattleUnderfloor }
    public static Color Resolve(Tone tone) => tone switch {
        Tone.BattleFloor => BattleFloor, Tone.BattleUnderfloor => BattleUnderfloor, Tone.Base => BgBase, Tone.Panel => BgPanel, Tone.Hover => BgPanelHi,
        Tone.Line => Line, Tone.Accent => Accent, Tone.Warn => Warn, Tone.Danger => Danger,
        Tone.Ok => Ok, Tone.Dim => TextDim, Tone.Clear => Clear, Tone.Texture => TextureTint, _ => TextMain
    };
    public static Color Alpha(Color color, float alpha) { color.a = alpha; return color; }
}
