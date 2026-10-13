using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewShopCatalog", menuName = "Game/Shop/Catalog")]
public class ShopCatalogData : ScriptableObject
{
    [SerializeField] private PacketCurrencyData currency;
    [SerializeField] private CacheData[] items = new CacheData[0];

    public PacketCurrencyData Currency => currency;
    public IReadOnlyList<CacheData> Items => items;
}
