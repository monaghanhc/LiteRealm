using LiteRealm.AI;
using LiteRealm.Inventory;
using LiteRealm.Player;
using LiteRealm.World;
using UnityEngine;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    public class DebugPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text feedbackText;
        [SerializeField] private InputField itemIdInput;
        [SerializeField] private InputField itemAmountInput;
        [SerializeField] private InputField timeNormalizedInput;

        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private SpawnerZone[] spawners;
        [SerializeField] private BossSpawnManager bossSpawnManager;
        [SerializeField] private DayNightCycleManager dayNightCycle;

        [Header("Toggle")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F1;

        private bool visible;

        private void Start()
        {
            SetVisible(false);
            if ((spawners == null || spawners.Length == 0))
            {
                spawners = FindObjectsOfType<SpawnerZone>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                SetVisible(!visible);
            }
        }

        public void SpawnZombie()
        {
            if (spawners == null || spawners.Length == 0)
            {
                SetFeedback("No spawner zones assigned.");
                return;
            }

            spawners[0].SpawnZombie();
            SetFeedback("Spawned one zombie.");
        }

        public void SpawnBoss()
        {
            if (bossSpawnManager == null)
            {
                SetFeedback("BossSpawnManager missing.");
                return;
            }

            bossSpawnManager.RequestQuestSpawn();
            bossSpawnManager.SpawnBoss();
            SetFeedback("Spawned boss.");
        }

        public void GiveItemFromInput()
        {
            if (itemDatabase == null || inventory == null)
            {
                SetFeedback("Inventory or ItemDatabase missing.");
                return;
            }

            string itemId = itemIdInput != null ? itemIdInput.text : string.Empty;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                SetFeedback("Enter item id.");
                return;
            }

            int amount = 1;
            if (itemAmountInput != null && !int.TryParse(itemAmountInput.text, out amount))
            {
                amount = 1;
            }

            amount = Mathf.Max(1, amount);
            ItemDefinition item = itemDatabase.GetById(itemId);
            if (item == null)
            {
                SetFeedback($"Unknown item id: {itemId}");
                return;
            }

            int accepted = inventory.AddItemAndReturnAccepted(item, amount);
            SetFeedback($"Given {accepted} x {item.DisplayName}");
        }

        public void SetTimeOfDayFromInput()
        {
            if (dayNightCycle == null)
            {
                SetFeedback("DayNightCycleManager missing.");
                return;
            }

            float normalized = 0.5f;
            if (timeNormalizedInput != null)
            {
                float.TryParse(timeNormalizedInput.text, out normalized);
            }

            dayNightCycle.SetTimeNormalized(Mathf.Repeat(normalized, 1f));
            SetFeedback($"Time set to {dayNightCycle.GetDisplayTime24h()}");
        }

        public void ToggleGodMode()
        {
            if (playerStats == null)
            {
                SetFeedback("PlayerStats missing.");
                return;
            }

            playerStats.ToggleGodMode();
            SetFeedback($"God mode: {(playerStats.GodMode ? "ON" : "OFF")}");
        }

        private void SetVisible(bool isVisible)
        {
            visible = isVisible;
            if (root != null)
            {
                root.SetActive(visible);
            }
        }

        private void SetFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
            }
        }
    }
}
