using UnityEngine;

[CreateAssetMenu(fileName = "Packet", menuName = "Game/Shop/Packet Currency")]
public class PacketCurrencyData : ScriptableObject
{
    [SerializeField] private string currencyId = "packet";
    [SerializeField] private string displayName = "Packet";
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    public string CurrencyId => currencyId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
}
