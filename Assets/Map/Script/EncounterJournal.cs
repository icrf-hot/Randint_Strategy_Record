using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable] public sealed class EncounterRecord
{
    public int nodeId, floor, catalogVersion, order;
    public string eventId, outcome;
    public EncounterStage stage;
    public EncounterAction action;
    public EncounterTier tier;
    public int[] hand;
    public int targetSum, targetEven, targetChecksum, selectedMask, signedChannel;
    public int packetDelta, integrityDelta, consistencyDelta;
    public EncounterRecord Copy()=>JsonUtility.FromJson<EncounterRecord>(JsonUtility.ToJson(this));
}

public sealed class EncounterJournal
{
    private readonly Dictionary<int,EncounterRecord> records=new Dictionary<int,EncounterRecord>();
    public IEnumerable<EncounterRecord> Records=>records.Values;
    public EncounterRecord Find(int id)=>records.TryGetValue(id,out var record)?record:null;
    public EncounterRecord[] Export()=>records.Values.OrderBy(r=>r.order).Select(r=>r.Copy()).ToArray();
    public void Restore(EncounterRecord[] source){records.Clear();foreach(var r in source ?? Array.Empty<EncounterRecord>())records.Add(r.nodeId,r.Copy());}
    public void Clear()=>records.Clear();
    public EncounterRecord Assign(string runId,int nodeId,RunSaveBindings bindings)
    {
        if(records.TryGetValue(nodeId,out var existing))return existing;
        if(!EncounterCatalog.IsEventNode(nodeId))throw new InvalidOperationException("Not an encounter node.");
        var catalog=EncounterCatalog.Active;int floor=CampaignDefinition.FloorOf(nodeId);
        uint random=Seed(runId,nodeId,"assignment");
        var definition=catalog.Find("signature_request");
        if(records.Values.Any(r=>r.floor==floor && r.eventId==definition.id)) {
            var candidates=catalog.events.Where(e=>e.id!="signature_request" && floor>=e.minFloor && floor<=e.maxFloor &&
                (e.repeatable || !records.Values.Any(r=>r.eventId==e.id))).ToArray();
            int total=candidates.Sum(e=>e.weight),pick=(int)(Next(ref random)%(uint)total);
            definition=candidates[0];foreach(var e in candidates){if(pick<e.weight){definition=e;break;}pick-=e.weight;}
        }
        var record=new EncounterRecord {nodeId=nodeId,floor=floor,catalogVersion=catalog.version,order=records.Count+1,
            eventId=definition.id,tier=catalog.tiers[floor-1].Copy(),outcome="",signedChannel=(int)(Next(ref random)%2)};
        if(record.eventId=="signature_request")BuildHand(runId,record);
        records.Add(nodeId,record);return record;
    }
    public static void BuildHand(string runId,EncounterRecord record)
    {
        uint random=Seed(runId,record.nodeId,"cards");record.hand=new int[6];
        for(int i=0;i<6;i++)record.hand[i]=(int)(Next(ref random)%10)+1;
        if(Next(ref random)%10000<record.tier.jokerChance*10000)record.hand[5]=0;
        int[] indices={0,1,2,3,4};
        for(int i=4;i>0;i--){int j=(int)(Next(ref random)%(uint)(i+1));int old=indices[i];indices[i]=indices[j];indices[j]=old;}
        record.targetSum=record.targetEven=record.targetChecksum=0;
        for(int i=0;i<record.tier.requiredCards;i++) {
            int index=indices[i],value=record.hand[index];record.targetSum+=value;
            if(value%2==0)record.targetEven++;
            record.targetChecksum+=value*(index+1);
        }
        if(record.tier.modulus>0)record.targetChecksum%=record.tier.modulus;else record.targetChecksum=0;
    }
    public static bool ValidProof(EncounterRecord record,int mask)
    {
        if(record.hand==null || mask<0 || mask>63)return false;
        int count=0,sum=0,even=0,checksum=0;
        for(int i=0;i<6;i++)if((mask&(1<<i))!=0){int v=record.hand[i];if(v==0)return false;count++;sum+=v;if(v%2==0)even++;checksum+=v*(i+1);}
        return count==record.tier.requiredCards && sum==record.targetSum && (!record.tier.parity || even==record.targetEven) &&
            (record.tier.modulus==0 || checksum%record.tier.modulus==record.targetChecksum);
    }
    public static int Solution(EncounterRecord record){for(int mask=0;mask<64;mask++)if(ValidProof(record,mask))return mask;return -1;}
    public static bool Validate(EncounterRecord[] source,RunSaveSnapshot save,RunSaveBindings bindings)
    {
        if(source==null || source.Length>190)return false;
        var ids=new HashSet<int>();var orders=new HashSet<int>();var unique=new HashSet<string>();var signatureFloors=new HashSet<int>();
        foreach(var r in source) {
            if(r==null || !ids.Add(r.nodeId) || !orders.Add(r.order) || r.order<1 || r.order>source.Length || !EncounterCatalog.IsEventNode(r.nodeId) ||
                !save.visitedNodeIds.Contains(r.nodeId) || r.floor!=CampaignDefinition.FloorOf(r.nodeId) || r.catalogVersion!=1 || r.tier==null || !r.tier.IsValid || r.tier.floor!=r.floor ||
                !Enum.IsDefined(typeof(EncounterStage),r.stage) || !Enum.IsDefined(typeof(EncounterAction),r.action) || r.selectedMask<0 || r.selectedMask>63 ||
                r.signedChannel<0 || r.signedChannel>1)return false;
            var e=EncounterCatalog.Active.Find(r.eventId);
            if(e==null || r.floor<e.minFloor || r.floor>e.maxFloor || r.outcome==null)return false;
            if(r.eventId=="signature_request") {
                if(!signatureFloors.Add(r.floor) || r.hand==null || r.hand.Length!=6)return false;
                var expected=r.Copy();BuildHand(save.runId,expected);
                if(!expected.hand.SequenceEqual(r.hand) || expected.targetSum!=r.targetSum || expected.targetEven!=r.targetEven || expected.targetChecksum!=r.targetChecksum || Solution(r)<0)return false;
            } else if((!e.repeatable && !unique.Add(e.id)) || r.stage==EncounterStage.Verified)return false;
            bool completed=save.completedNodeIds.Contains(r.nodeId);
            if((r.stage==EncounterStage.Resolved)!=completed)return false;
            if(r.stage==EncounterStage.Assigned && (r.action!=EncounterAction.Leave || r.outcome!="" || r.packetDelta!=0 || r.integrityDelta!=0 || r.consistencyDelta!=0))return false;
            if(r.stage==EncounterStage.Verified && (r.action!=EncounterAction.Verify || !ValidProof(r,r.selectedMask)))return false;
            if(r.stage==EncounterStage.Verified && (r.outcome!="" || r.packetDelta!=-r.tier.verifyCost || r.integrityDelta!=0 || r.consistencyDelta!=0))return false;
            if(r.stage==EncounterStage.Resolved) {
                if(!e.actions.Contains(r.action.ToString()) || r.action==EncounterAction.Verify || r.outcome!=EncounterService.OutcomeKey(r.action))return false;
                if(r.action==EncounterAction.Claim && !ValidProof(r,r.selectedMask))return false;
                if(r.action==EncounterAction.Forge && !Enumerable.Range(0,6).Any(i=>r.hand[i]==0 && (r.selectedMask&(1<<i))!=0))return false;
            }
        }
        return true;
    }
    // Stable across processes/platforms; never use string.GetHashCode for persisted events.
    public static uint Seed(string runId,int nodeId,string domain)
    { uint hash=2166136261;foreach(char c in runId+":"+nodeId+":"+domain){hash^=c;hash=unchecked(hash*16777619);}return hash==0?1:hash; }
    private static uint Next(ref uint state){state^=state<<13;state^=state>>17;state^=state<<5;return state;}
}
