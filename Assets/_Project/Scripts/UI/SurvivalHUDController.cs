using LiteRealm.Combat;
using LiteRealm.Inventory;
using LiteRealm.Player;
using LiteRealm.World;
using UnityEngine;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    public class SurvivalHUDController : MonoBehaviour
    {
        [Header("Bars")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider staminaBar;
        [SerializeField] private Slider hungerBar;
        [SerializeField] private Slider thirstBar;

        [Header("Text")]
        [SerializeField] private Text quickSlotsText;
        [SerializeField] private Text ammoText;
        [SerializeField] private Text timeText;

        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private WeaponManager weaponManager;
        [SerializeField] private DayNightCycleManager dayNight;

        [Header("Quick Slots")]
        [SerializeField] [Range(1, 8)] private int quickSlotCount = 4;

        public bool HasCoreHudBindings => healthBar != null
                                          && staminaBar != null
                                          && hungerBar != null
                                          && thirstBar != null
                                          && timeText != null;
        public bool HasRuntimeReferences => playerStats != null && dayNight != null;
        public string CurrentTimeLabel => timeText != null ? timeText.text : string.Empty;
        public float HungerBarValue => hungerBar != null ? hungerBar.value : -1f;
        public float ThirstBarValue => thirstBar != null ? thirstBar.value : -1f;

        private void Awake()
        {
            if (playerStats == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerStats = player.GetComponent<PlayerStats>();
                }
            }

            if (inventory == null && playerStats != null)
            {
                inventory = playerStats.GetComponent<InventoryComponent>();
            }

            if (weaponManager == null && playerStats != null)
            {
                weaponManager = playerStats.GetComponent<WeaponManager>();
            }

            if (dayNight == null)
            {
                dayNight = FindObjectOfType<DayNightCycleManager>();
            }

            if (inventory != null)
            {
                quickSlotCount = Mathf.Clamp(inventory.HotbarSlotCount, 1, 8);
            }
        }

        private void OnEnable()
        {
            if (playerStats != null)
            {
                playerStats.StatsChanged += OnStatsChanged;
                OnStatsChanged(playerStats.GetSnapshot());
            }

            if (inventory != null)
            {
                inventory.InventoryChanged += RefreshQuickSlots;
                RefreshQuickSlots();
            }

            if (weaponManager != null)
            {
                weaponManager.AmmoChanged += OnAmmoChanged;
                WeaponBase weapon = weaponManager.ActiveWeapon;
                if (weapon != null)
                {
                    OnAmmoChanged(weapon.CurrentAmmo, weapon.MagazineSize);
                }
            }

            if (dayNight != null)
            {
                dayNight.TimeUpdated += OnTimeUpdated;
                OnTimeUpdated(dayNight.NormalizedTime, dayNight.CurrentDay);
            }
        }

        private void OnDisable()
        {
            if (playerStats != null)
            {
                playerStats.StatsChanged -= OnStatsChanged;
            }

            if (inventory != null)
            {
                inventory.InventoryChanged -= RefreshQuickSlots;
            }

            if (weaponManager != null)
            {
                weaponManager.AmmoChanged -= OnAmmoChanged;
            }

            if (dayNight != null)
            {
                dayNight.TimeUpdated -= OnTimeUpdated;
            }
        }

        private void Update()
        {
            if (inventory == null || playerStats == null)
            {
                return;
            }

            // Consumable quick-use slots are mapped to 5,6,7,8 to avoid weapon swap keys.
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                inventory.TryUseSlot(0, playerStats);
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                inventory.TryUseSlot(1, playerStats);
            }
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                inventory.TryUseSlot(2, playerStats);
            }
            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                inventory.TryUseSlot(3, playerStats);
            }
        }

        private void OnStatsChanged(PlayerStatsSnapshot snapshot)
        {
            SetBar(healthBar, snapshot.Health, snapshot.MaxHealth);
            SetBar(staminaBar, snapshot.Stamina, snapshot.MaxStamina);
            SetBar(hungerBar, snapshot.Hunger, snapshot.MaxHunger);
            SetBar(thirstBar, snapshot.Thirst, snapshot.MaxThirst);
        }

        private void OnAmmoChanged(int current, int max)
        {
            if (ammoText != null)
            {
                ammoText.text = $"Ammo {current}/{max}";
            }
        }

        private void OnTimeUpdated(float _, int day)
        {
            if (timeText != null && dayNight != null)
            {
                string nightText = dayNight.IsNight ? "Night" : "Day";
                timeText.text = $"Day {day}  {dayNight.GetDisplayTime24h()}  {nightText}";
            }
        }

        private void RefreshQuickSlots()
        {
            if (quickSlotsText == null || inventory == null)
            {
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < quickSlotCount; i++)
            {
                if (i >= inventory.SlotCount)
                {
                    break;
                }

                InventorySlot slot = inventory.Slots[i];
                string key = (i + 5).ToString();
                if (slot == null || slot.IsEmpty)
                {
                    builder.AppendLine($"[{key}] Empty");
                    continue;
                }

                builder.AppendLine($"[{key}] {slot.Item.DisplayName} x{slot.Quantity}");
            }

            quickSlotsText.text = builder.ToString();
        }

        private void SetBar(Slider slider, float current, float max)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = Mathf.Max(1f, max);
            slider.value = Mathf.Clamp(current, 0f, slider.maxValue);
        }
    }
}
