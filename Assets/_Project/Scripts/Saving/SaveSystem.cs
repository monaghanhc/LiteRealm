using System;
using System.Collections.Generic;
using System.IO;
using LiteRealm.AI;
using LiteRealm.Inventory;
using LiteRealm.Loot;
using LiteRealm.Player;
using LiteRealm.Quests;
using LiteRealm.World;
using UnityEngine;

namespace LiteRealm.Saving
{
    public class SaveSystem : MonoBehaviour
    {
        private const int CurrentSchemaVersion = 1;
        private const string CurrentBuildVersion = "0.5.0";

        [SerializeField] private string fileName = "savegame.json";
        [SerializeField] private KeyCode quickSaveKey = KeyCode.F5;
        [SerializeField] private KeyCode quickLoadKey = KeyCode.F9;

        [Header("References")]
        [SerializeField] private Transform playerRoot;
        [SerializeField] private CharacterController playerCharacterController;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private QuestManager questManager;
        [SerializeField] private DayNightCycleManager dayNightCycle;
        [SerializeField] private BossSpawnManager bossSpawnManager;
        [SerializeField] private LootContainer[] lootContainers;

        public string SavePath => Path.Combine(Application.persistentDataPath, fileName);

        private void Start()
        {
            if (playerRoot == null && playerStats != null)
            {
                playerRoot = playerStats.transform;
            }

            if (playerCharacterController == null && playerRoot != null)
            {
                playerCharacterController = playerRoot.GetComponent<CharacterController>();
            }

            if (lootContainers == null || lootContainers.Length == 0)
            {
                lootContainers = FindObjectsOfType<LootContainer>(true);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(quickSaveKey))
            {
                SaveGame();
            }

            if (Input.GetKeyDown(quickLoadKey))
            {
                LoadGame();
            }
        }

        public bool SaveGame()
        {
            if (playerRoot == null || playerStats == null || inventory == null)
            {
                Debug.LogWarning("SaveSystem: Missing required player references.");
                return false;
            }

            SaveFileData data = new SaveFileData
            {
                SchemaVersion = CurrentSchemaVersion,
                BuildVersion = CurrentBuildVersion,
                SavedUtc = DateTime.UtcNow.ToString("o")
            };

            Vector3 position = playerRoot.position;
            Vector3 rotation = playerRoot.eulerAngles;

            data.Player.Position = new SerializableVector3(position.x, position.y, position.z);
            data.Player.RotationEuler = new SerializableVector3(rotation.x, rotation.y, rotation.z);
            data.Player.Stats = playerStats.CaptureState();

            data.Inventory = inventory.CaptureState();
            if (questManager != null)
            {
                data.Quests = questManager.CaptureState();
            }

            if (dayNightCycle != null)
            {
                data.World.NormalizedTime = dayNightCycle.NormalizedTime;
                data.World.CurrentDay = dayNightCycle.CurrentDay;
            }

            if (bossSpawnManager != null)
            {
                data.World.BossSpawnerState = bossSpawnManager.CaptureState();
            }

            if (lootContainers != null)
            {
                data.World.ContainerStates = new List<LootContainerState>();
                for (int i = 0; i < lootContainers.Length; i++)
                {
                    LootContainer container = lootContainers[i];
                    if (container == null)
                    {
                        continue;
                    }

                    data.World.ContainerStates.Add(container.CaptureState());
                }
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"Saved game to: {SavePath}");
            return true;
        }

        public bool LoadGame()
        {
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning($"SaveSystem: No save file at {SavePath}");
                return false;
            }

            string json = File.ReadAllText(SavePath);
            SaveFileData data = JsonUtility.FromJson<SaveFileData>(json);
            if (data == null)
            {
                Debug.LogError("SaveSystem: Failed to parse save file.");
                return false;
            }

            if (data.SchemaVersion > CurrentSchemaVersion)
            {
                Debug.LogWarning(
                    $"SaveSystem: Save schema {data.SchemaVersion} is newer than runtime schema {CurrentSchemaVersion}. " +
                    "Attempting best-effort load.");
            }

            if (data.Player == null)
            {
                data.Player = new PlayerSaveData();
            }

            if (data.World == null)
            {
                data.World = new WorldSaveData();
            }

            if (data.Inventory == null)
            {
                data.Inventory = new List<InventorySlotState>();
            }

            if (data.Quests == null)
            {
                data.Quests = new QuestManagerState();
            }

            RestorePlayer(data);

            if (inventory != null)
            {
                inventory.RestoreState(data.Inventory, itemDatabase);
            }

            if (questManager != null)
            {
                questManager.RestoreState(data.Quests);
            }

            if (dayNightCycle != null)
            {
                dayNightCycle.SetDayAndTime(data.World.CurrentDay, data.World.NormalizedTime);
            }

            if (bossSpawnManager != null)
            {
                bossSpawnManager.RestoreState(data.World.BossSpawnerState);
            }

            RestoreContainers(data.World.ContainerStates);
            Debug.Log($"Loaded game from: {SavePath}");
            return true;
        }

        private void RestorePlayer(SaveFileData data)
        {
            if (playerRoot == null || playerStats == null)
            {
                return;
            }

            bool hadController = playerCharacterController != null;
            if (hadController)
            {
                playerCharacterController.enabled = false;
            }

            SerializableVector3 position = data.Player.Position;
            SerializableVector3 rotation = data.Player.RotationEuler;

            playerRoot.position = new Vector3(position.X, position.Y, position.Z);
            playerRoot.rotation = Quaternion.Euler(rotation.X, rotation.Y, rotation.Z);
            playerStats.RestoreState(data.Player.Stats);

            if (hadController)
            {
                playerCharacterController.enabled = true;
            }
        }

        private void RestoreContainers(List<LootContainerState> savedStates)
        {
            if (savedStates == null || itemDatabase == null)
            {
                return;
            }

            if (lootContainers == null || lootContainers.Length == 0)
            {
                lootContainers = FindObjectsOfType<LootContainer>(true);
            }

            Dictionary<string, LootContainerState> lookup = new Dictionary<string, LootContainerState>();
            for (int i = 0; i < savedStates.Count; i++)
            {
                LootContainerState state = savedStates[i];
                if (state == null || string.IsNullOrWhiteSpace(state.ContainerId))
                {
                    continue;
                }

                lookup[state.ContainerId] = state;
            }

            for (int i = 0; i < lootContainers.Length; i++)
            {
                LootContainer container = lootContainers[i];
                if (container == null)
                {
                    continue;
                }

                if (lookup.TryGetValue(container.ContainerId, out LootContainerState state))
                {
                    container.RestoreState(state, itemDatabase);
                }
            }
        }
    }
}
