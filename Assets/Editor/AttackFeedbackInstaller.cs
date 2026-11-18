using UnityEditor;
using UnityEngine;

/// <summary>Creates editable defaults once; repeated installation preserves designer overrides.</summary>
public static class AttackFeedbackInstaller
{
    [MenuItem("Tools/Randint/Install Role Combat Templates")]
    public static void ApplyRoleTemplates()
    {
        const string folder = "Assets/Resources/CombatFeedback/";
        var library = AssetDatabase.LoadAssetAtPath<AttackFeedbackLibrary>(folder + "Library.asset");
        if (library == null) throw new System.InvalidOperationException("Install the base feedback library first.");
        var guard = Create("GuardStance", library.front, p => {
            p.motion=CombatMotion.Guard;p.travel=AttackTravel.Melee;p.approach=0;p.afterimages=0;
            p.anticipation=.12f;p.dash=.05f;p.impact=.08f;p.recovery=.12f;p.animationTrigger="Guard";
        });
        var shooting = Create("DorothyShot", library.middle, p => {
            p.motion=CombatMotion.Shooting;p.travel=AttackTravel.Projectile;p.approach=0;p.afterimages=0;
            p.anticipation=.18f;p.dash=.24f;p.impact=.08f;p.recovery=.15f;p.visualHits=1;
            p.focusZoom=.78f;p.focusTargetBias=.45f;p.recoilPixels=20;p.leanDegrees=8;
            p.projectileArcPixels=8;p.projectileLength=54;p.projectileWidth=6;p.animationTrigger="Shoot";
        });
        var sniper = Create("SniperShot", shooting, p => {p.focusZoom=.88f;p.recoilPixels=12;});
        var heavy = Create("HeavyShooterShot", shooting, p => {
            p.anticipation=.18f;p.recoilPixels=28;p.projectileWidth=9;p.shakePixels=5;
        });
        var casting = Create("CasterCast", library.back, p => {
            p.motion=CombatMotion.Casting;p.focusZoom=.92f;p.projectileArcPixels=55;p.animationTrigger="Cast";
        });
        var healing = Create("HealerPulse", library.back, p => {
            p.motion=CombatMotion.Healing;p.focusZoom=1;p.projectileArcPixels=45;p.animationTrigger="Heal";
        });
        var support = Create("SupportCast", casting, p => {p.focusZoom=1;p.animationTrigger="SupportAttack";});
        if (library.frontDefense == null) library.frontDefense = guard;
        if (library.supporter == null) library.supporter = support;
        var classes = new System.Collections.Generic.List<AttackFeedbackLibrary.ClassProfile>(library.classes ?? System.Array.Empty<AttackFeedbackLibrary.ClassProfile>());
        AddClass(OperatorClass.Defender, library.front, guard);
        AddClass(OperatorClass.Guard, library.front, guard);
        AddClass(OperatorClass.Vanguard, library.front, guard);
        AddClass(OperatorClass.Sniper, sniper);
        AddClass(OperatorClass.Sniper, heavy, null, "heavy_shooter");
        AddClass(OperatorClass.Caster, casting);
        AddClass(OperatorClass.Specialist, casting);
        AddClass(OperatorClass.Healer, healing);
        AddClass(OperatorClass.Supporter, support);
        AddClass(OperatorClass.AttackHealer, support);
        library.classes = classes.ToArray();
        var characters = new System.Collections.Generic.List<AttackFeedbackLibrary.CharacterProfile>(library.characters ?? System.Array.Empty<AttackFeedbackLibrary.CharacterProfile>());
        var dorothy = RunSaveBindings.Instance.Operator("dorothy");
        if (!characters.Exists(item => item.character == dorothy))
            characters.Add(new AttackFeedbackLibrary.CharacterProfile { character=dorothy, profile=shooting });
        library.characters = characters.ToArray();
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssetIfDirty(library);
        Debug.Log("[Combat presentation] Role templates installed; existing character/class overrides preserved. Support conversion not exercised.");

        void AddClass(OperatorClass type, AttackFeedbackProfile profile, AttackFeedbackProfile defense=null, string branch="")
        {
            if (!classes.Exists(item => item.classId==type && (item.branchId ?? "")==branch))
                classes.Add(new AttackFeedbackLibrary.ClassProfile { classId=type, branchId=branch, profile=profile, defense=defense });
        }
        AttackFeedbackProfile Create(string name, AttackFeedbackProfile source, System.Action<AttackFeedbackProfile> configure)
        {
            string path = folder + name + ".asset";
            var result = AssetDatabase.LoadAssetAtPath<AttackFeedbackProfile>(path);
            if (result != null) return result;
            result = ScriptableObject.CreateInstance<AttackFeedbackProfile>();
            EditorUtility.CopySerialized(source, result);
            result.name = name;
            configure(result);
            AssetDatabase.CreateAsset(result, path);
            return result;
        }
    }
    [MenuItem("Tools/Randint/Install Combat Feedback Profiles")]
    public static void Apply()
    {
        const string folder = "Assets/Resources/CombatFeedback";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources", "CombatFeedback");
        var front = Profile("Front", p => {p.anticipation=.2f;p.dash=.08f;p.impact=.03f;p.recovery=.12f;p.shakePixels=6;p.hitStop=.07f;p.approach=.92f;});
        var middle = Profile("Middle", p => {p.travel=AttackTravel.ShortDash;p.anticipation=.08f;p.dash=.07f;p.impact=.09f;p.recovery=.1f;p.visualHits=2;p.hitStop=.06f;p.shakePixels=2;p.approach=.35f;});
        var back = Profile("Back", p => {p.travel=AttackTravel.Projectile;p.anticipation=.1f;p.dash=.18f;p.impact=.03f;p.recovery=.1f;p.retreatPixels=0;p.approach=0;p.afterimages=0;});
        var enemy = Profile("Enemy", p => {p.anticipation=.045f;p.dash=.055f;p.impact=.02f;p.recovery=.07f;p.hitStop=.045f;p.shakePixels=4;p.approach=.8f;p.afterimages=2;});
        string path=folder+"/Library.asset";
        if (AssetDatabase.LoadAssetAtPath<AttackFeedbackLibrary>(path)==null) {
            var library=ScriptableObject.CreateInstance<AttackFeedbackLibrary>();
            library.front=front;library.middle=middle;library.back=back;library.enemy=enemy;
            AssetDatabase.CreateAsset(library,path);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Combat Feedback] Editable profiles installed; existing overrides preserved.");
        AttackFeedbackProfile Profile(string name,System.Action<AttackFeedbackProfile> configure)
        {
            string file=folder+"/"+name+".asset";
            var result=AssetDatabase.LoadAssetAtPath<AttackFeedbackProfile>(file); if(result!=null)return result;
            result=ScriptableObject.CreateInstance<AttackFeedbackProfile>();configure(result);AssetDatabase.CreateAsset(result,file);return result;
        }
    }
}
