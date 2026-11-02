using System;
using System.Linq;
using UnityEngine;

public enum EncounterStage { Assigned, Verified, Resolved }
public enum EncounterAction { Leave, Supply, Repair, RiskyPackets, Verify, Claim, Forge }

[Serializable] public sealed class EncounterTier
{
    public int floor, requiredCards, modulus, verifyCost, forgeCost, signaturePackets, signatureHeal;
    public bool parity;
    public float jokerChance;
    public int supplyPackets, repairCost, repairAmount, riskCost, riskPackets;
    public EncounterTier Copy() => JsonUtility.FromJson<EncounterTier>(JsonUtility.ToJson(this));
    public bool IsValid => floor>=1 && floor<=5 && requiredCards>=2 && requiredCards<=4 && modulus>=0 && modulus<=7 &&
        verifyCost>=0 && verifyCost<=100 && forgeCost>=0 && forgeCost<=30 && signaturePackets>=0 && signaturePackets<=100 &&
        signatureHeal>=0 && signatureHeal<=100 && jokerChance>=0 && jokerChance<=1 && supplyPackets>=0 && supplyPackets<=100 &&
        repairCost>=0 && repairCost<=100 && repairAmount>=0 && repairAmount<=100 && riskCost>=0 && riskCost<=30 && riskPackets>=0 && riskPackets<=100;
}
[Serializable] public sealed class EncounterDefinition
{
    public string id, titleKey, bodyKey;
    public int minFloor=1,maxFloor=5,weight=1;
    public bool repeatable;
    public string[] actions;
}
[Serializable] public sealed class EncounterCatalog
{
    public int version;
    public EncounterTier[] tiers;
    public EncounterDefinition[] events;
    private static EncounterCatalog active;
    public static EncounterCatalog Active {
        get {
            if(active!=null)return active;
            var text=Resources.Load<TextAsset>("Campaign/encounters");
            var data=text!=null?JsonUtility.FromJson<EncounterCatalog>(text.text):null;
            if(data==null || data.version!=1 || data.tiers==null || data.tiers.Length!=5 || data.events==null || data.events.Length<2)
                throw new InvalidOperationException("Invalid encounter catalog.");
            for(int i=0;i<5;i++)if(data.tiers[i].floor!=i+1 || !data.tiers[i].IsValid)throw new InvalidOperationException("Invalid encounter tier.");
            if(data.events.Select(e=>e.id).Distinct().Count()!=data.events.Length)throw new InvalidOperationException("Duplicate encounter ID.");
            foreach(var e in data.events) {
                if(string.IsNullOrEmpty(e.id) || e.minFloor<1 || e.maxFloor>5 || e.minFloor>e.maxFloor || e.weight<1 || e.actions==null ||
                    !e.actions.Contains("Leave") || e.actions.Distinct().Count()!=e.actions.Length)throw new InvalidOperationException("Invalid encounter: "+e.id);
                foreach(var action in e.actions)if(!Enum.TryParse<EncounterAction>(action,out var value) || !Enum.IsDefined(typeof(EncounterAction),value))throw new InvalidOperationException("Invalid action.");
                _=Randint.Data.GameData.Text(e.titleKey);_=Randint.Data.GameData.Text(e.bodyKey);
            }
            if(!data.events.Any(e=>e.id=="signature_request") || !data.events.Any(e=>e.id=="supply_cache" && e.repeatable))throw new InvalidOperationException("Encounter fallback missing.");
            active=data;return data;
        }
    }
    public EncounterDefinition Find(string id)=>events.FirstOrDefault(e=>e.id==id);
    public static bool IsEventNode(int id)
    {
        int floor=CampaignDefinition.FloorOf(id);
        if(floor==1)return id>=2 && id<=4;
        var f=CampaignDefinition.Active.Floor(floor);
        return CampaignDefinition.HasNode(id) && CampaignDefinition.TypeAt(f,id%1000-1)==MapNodeType.Event;
    }
}
