using UnityEngine;

public enum ItemPickupEffect { Inventory, Heal }
public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }
[CreateAssetMenu(fileName="NewItem",menuName="Hunt the Witch/Items/Item Data")]
public sealed class ItemData : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [TextArea,SerializeField] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private ItemRarity rarity;
    [Header("Pickup effect")]
    [SerializeField] private ItemPickupEffect pickupEffect;
    [SerializeField,Range(0,1)] private float healFraction=.3f;
    [SerializeField] private bool isKey;
    public bool IsKey => isKey;
    public ItemPickupEffect PickupEffect => pickupEffect;
    public float HealFraction => Mathf.Clamp01(healFraction);
    public string ItemId => string.IsNullOrWhiteSpace(itemId)?name:itemId;
    public string DisplayName => string.IsNullOrWhiteSpace(displayName)?name:displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public ItemRarity Rarity => rarity;
    public float BaseDropChance => rarity==ItemRarity.Common?.65f:rarity==ItemRarity.Uncommon?.35f:rarity==ItemRarity.Rare?.15f:rarity==ItemRarity.Epic?.05f:.01f;
    public Color RarityColor => rarity==ItemRarity.Common?Color.white:rarity==ItemRarity.Uncommon?Color.green:rarity==ItemRarity.Rare?Color.cyan:rarity==ItemRarity.Epic?new Color(.75f,.3f,1):new Color(1,.7f,.15f);
}
