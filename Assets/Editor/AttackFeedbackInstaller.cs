using UnityEditor;
using UnityEngine;

/// <summary>Creates editable defaults once; repeated installation preserves designer overrides.</summary>
public static class AttackFeedbackInstaller
{
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
