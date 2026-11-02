using System;
using System.Linq;
using UnityEngine;

/// <summary>One routing table for new runs, campaign travel, return paths and saved runs.</summary>
public static class CampaignScenes
{
    public const string LegacyMapPath = "Assets/Scenes/Map_1F.unity";
    [Serializable] public sealed class Entry { public int floor; public string scenePath; }
    [Serializable] private sealed class Table { public int version; public Entry[] floors; }
    private static Table table;
    public static Entry[] Floors => (Entry[])Data.floors.Clone();
    public static string CurrentPath => PathFor(MapRunState.MapFloor);
    public static string PathFor(int floor) => Data.floors.FirstOrDefault(e=>e.floor==floor)?.scenePath ??
        throw new ArgumentOutOfRangeException(nameof(floor),"No map scene registered for floor.");
    public static bool IsMapScene(string path) => Data.floors.Any(e=>e.scenePath==path);
    private static Table Data {
        get {
            if(table!=null)return table;
            var asset=Resources.Load<TextAsset>("Campaign/scenes");
            var data=asset!=null?JsonUtility.FromJson<Table>(asset.text):null;
            if(data==null || data.version!=1 || data.floors==null || data.floors.Length==0 ||
                data.floors.Any(e=>e==null || e.floor<1 || string.IsNullOrEmpty(e.scenePath) || !e.scenePath.StartsWith("Assets/") || !e.scenePath.EndsWith(".unity")) ||
                data.floors.Select(e=>e.floor).Distinct().Count()!=data.floors.Length || data.floors.Select(e=>e.scenePath).Distinct().Count()!=data.floors.Length)
                throw new InvalidOperationException("Invalid campaign scene registry.");
            table=data;return data;
        }
    }
}
