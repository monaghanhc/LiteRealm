using System;
using System.Collections.Generic;
using LiteRealm.AI;
using LiteRealm.Inventory;
using LiteRealm.Loot;
using LiteRealm.Player;
using LiteRealm.Quests;

namespace LiteRealm.Saving
{
    [Serializable]
    public class SaveFileData
    {
        public int SchemaVersion = 1;
        public string BuildVersion = "0.5.0";
        public string SavedUtc = "";

        public PlayerSaveData Player = new PlayerSaveData();
        public List<InventorySlotState> Inventory = new List<InventorySlotState>();
        public QuestManagerState Quests = new QuestManagerState();
        public WorldSaveData World = new WorldSaveData();
    }

    [Serializable]
    public class PlayerSaveData
    {
        public SerializableVector3 Position;
        public SerializableVector3 RotationEuler;
        public PlayerStatsState Stats;
    }

    [Serializable]
    public class WorldSaveData
    {
        public float NormalizedTime;
        public int CurrentDay;
        public List<LootContainerState> ContainerStates = new List<LootContainerState>();
        public BossSpawnerState BossSpawnerState;
    }

    [Serializable]
    public struct SerializableVector3
    {
        public float X;
        public float Y;
        public float Z;

        public SerializableVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
