using UnityEditor;
using UnityEngine;

public static class CampaignInstaller
{
    [MenuItem("Tools/Randint/Install Campaign Data")]
    public static void Apply()
    {
        const string path="Assets/Resources/Campaign/DefenderRelic.asset";
        if(AssetDatabase.LoadAssetAtPath<CacheData>(path)==null) {
            var relic=ScriptableObject.CreateInstance<CacheData>();
            var data=new SerializedObject(relic);data.FindProperty("definitionId").stringValue="defender_relic";data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(relic,path);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Campaign] Defender relic asset ready. Floors use Campaign/floors.json; no hidden floors installed.");
    }
}
