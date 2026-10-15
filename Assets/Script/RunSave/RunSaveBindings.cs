using System;
using System.Linq;
using UnityEngine;

public sealed class RunSaveBindings : ScriptableObject
{
    [SerializeField] private RunProgressionSettings progression;
    [SerializeField] private ShopCatalogData shop;
    [SerializeField] private OperatorData[] operators;
    [SerializeField] private string[] defaultPartyIds;
    [SerializeField] private int[] mapNodeIds;
    [SerializeField] private string mapScenePath = "Assets/Scenes/Map_1F.unity";
    [SerializeField] private string battleScenePath = "Assets/Scenes/Playing_Scene.unity";

    public static RunSaveBindings Instance => Resources.Load<RunSaveBindings>("RunSaveBindings");
    public RunProgressionSettings Progression => progression;
    public string MapScenePath => mapScenePath;
    public string BattleScenePath => battleScenePath;
    public string[] DefaultPartyIds => (string[])defaultPartyIds.Clone();
    public bool HasNode(int id) => mapNodeIds != null && Array.IndexOf(mapNodeIds, id) >= 0;
    public OperatorData Operator(string id) => operators?.FirstOrDefault(op => op != null && op.DataId == id);
    public CacheData Cache(string id) => shop?.Items?.FirstOrDefault(cache => cache != null && cache.CacheId == id);
}
