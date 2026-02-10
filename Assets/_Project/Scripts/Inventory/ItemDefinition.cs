using UnityEngine;

namespace LiteRealm.Inventory
{
    [CreateAssetMenu(fileName = "ItemDefinition", menuName = "LiteRealm/Inventory/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId = "item.id";
        [SerializeField] private string displayName = "New Item";
        [SerializeField] [TextArea] private string description = "";
        [SerializeField] private ItemRarity rarity = ItemRarity.Common;

        [Header("Stacking")]
        [SerializeField] [Min(1)] private int maxStack = 1;

        [Header("Presentation")]
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject worldPickupPrefab;

        [Header("Consumable")]
        [SerializeField] private bool consumable;
        [SerializeField] private float healthRestore;
        [SerializeField] private float staminaRestore;
        [SerializeField] private float hungerRestore;
        [SerializeField] private float thirstRestore;
        [SerializeField] private AudioClip useSfx;

        [Header("Quest")]
        [SerializeField] private bool questItem;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public string Description => description;
        public ItemRarity Rarity => rarity;
        public int MaxStack => Mathf.Max(1, maxStack);
        public Sprite Icon => icon;
        public GameObject WorldPickupPrefab => worldPickupPrefab;
        public bool Consumable => consumable;
        public float HealthRestore => healthRestore;
        public float StaminaRestore => staminaRestore;
        public float HungerRestore => hungerRestore;
        public float ThirstRestore => thirstRestore;
        public AudioClip UseSfx => useSfx;
        public bool QuestItem => questItem;
    }
}
