using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CampaignFloorDefinition
{
    public int floor, size, seed;
    public string terminalType;
    public int[] shopIndices;
    public int eventPercent;
    public float spacing;
    public int enemyCount, enemyHP, enemyAttack, enemyDefense, enemyResistance;
    public int bossHP, bossAttack;
    public float enemySpeed;
}

[Serializable]
public sealed class CampaignDefinition
{
    public int schemaVersion;
    public CampaignFloorDefinition[] floors;
    public int encounterPackets, encounterRepair;
    public float artsAttackScale = .65f;
    private static CampaignDefinition active;
    public static CampaignDefinition Active
    {
        get {
            if (active != null) return active;
            var asset = Resources.Load<TextAsset>("Campaign/floors");
            if (asset == null) throw new InvalidOperationException("Campaign/floors is missing.");
            var data = JsonUtility.FromJson<CampaignDefinition>(asset.text);
            if (data == null || data.schemaVersion != 1 || data.floors == null || data.floors.Length != 4)
                throw new InvalidOperationException("Invalid campaign definition.");
            if (data.encounterPackets < 0 || data.encounterRepair < 0 || data.encounterRepair > 100 ||
                data.artsAttackScale <= 0 || data.artsAttackScale > 1 || float.IsNaN(data.artsAttackScale))
                throw new InvalidOperationException("Invalid encounter rewards or damage scale.");
            for (int i = 0; i < 4; i++) {
                var f = data.floors[i];
                if (f.floor != i+2 || f.size != (f.floor <= 3 ? 3 : 4) || f.eventPercent < 0 || f.eventPercent > 100 ||
                    f.spacing <= 0 || f.shopIndices == null || f.enemyCount < 1 || f.enemyCount > 4 || f.enemyHP <= 0 ||
                    f.enemyAttack <= 0 || f.enemySpeed <= 0 || f.bossHP <= 0 || f.bossAttack <= 0)
                    throw new InvalidOperationException("Invalid floor " + (i+2));
                _ = (MapNodeType)Enum.Parse(typeof(MapNodeType), f.terminalType);
                foreach (int index in f.shopIndices) if (index <= 0 || index >= f.size*f.size*f.size-1) throw new InvalidOperationException("Shop overwrites endpoint.");
            }
            active = data; return data;
        }
    }
    public CampaignFloorDefinition Floor(int floor) => Array.Find(floors, f => f.floor == floor);
    public static int NodeId(int floor, int index) => floor == 1 ? index+1 : floor*1000+index+1;
    public static int FloorOf(int id) => id >= 1 && id <= 8 ? 1 : id/1000;
    public static bool HasNode(int id)
    {
        int floor = FloorOf(id); if (floor == 1) return id >= 1 && id <= 8;
        var definition = Active.Floor(floor);
        return definition != null && id%1000 >= 1 && id%1000 <= definition.size*definition.size*definition.size;
    }
    public static MapNodeType TypeAt(CampaignFloorDefinition f, int index)
    {
        int last = f.size*f.size*f.size-1;
        if (index == 0) return MapNodeType.Start;
        if (index == last) return (MapNodeType)Enum.Parse(typeof(MapNodeType), f.terminalType);
        if (Array.IndexOf(f.shopIndices, index) >= 0) return MapNodeType.Shop;
        uint hash = unchecked((uint)(index*1103515245 + f.seed*12345));
        return hash%100 < f.eventPercent ? MapNodeType.Event : MapNodeType.Battle;
    }
    public static List<int> Connections(CampaignFloorDefinition f, int index)
    {
        int n=f.size, x=index%n, y=index/n%n, z=index/(n*n);
        var result=new List<int>();
        if(x+1<n)result.Add(index+1); if(y+1<n)result.Add(index+n); if(z+1<n)result.Add(index+n*n);
        // Face diagonals supplement the original three forward axes, without backward movement.
        if((x+y+z+f.seed)%2==0) {
            if(x+1<n && y+1<n)result.Add(index+1+n);
            if(y+1<n && z+1<n)result.Add(index+n+n*n);
            if(x+1<n && z+1<n)result.Add(index+1+n*n);
        }
        return result;
    }
}
