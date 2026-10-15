using System.Collections.Generic;
using UnityEngine;
using Randint.Data;
using System.Linq;

[CreateAssetMenu(fileName = "NewShopCatalog", menuName = "Game/Shop/Catalog")]
public class ShopCatalogData : ScriptableObject
{
    [SerializeField, GameDefinitionId("shop")] private string definitionId;
    [SerializeField] private PacketCurrencyData currency;
    [SerializeField] private CacheData[] items = new CacheData[0];

    public PacketCurrencyData Currency => currency;
    public IReadOnlyList<CacheData> Items => string.IsNullOrEmpty(definitionId) ? items :
        GameData.Catalog.Shop(definitionId).cacheIds.Select(id => items.FirstOrDefault(item => item != null && item.CacheId == id)).ToArray();
}
