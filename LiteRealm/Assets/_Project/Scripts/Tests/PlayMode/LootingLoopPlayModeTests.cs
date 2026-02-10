using System.Collections;
using LiteRealm.AI;
using LiteRealm.Core;
using LiteRealm.Inventory;
using LiteRealm.Loot;
using LiteRealm.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LiteRealm.Tests.PlayMode
{
    public class LootingLoopPlayModeTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [UnityTest]
        public IEnumerator LootingLoop_GiveItem_KillZombie_SpawnPickup_OpenContainer()
        {
            Assert.IsTrue(System.IO.File.Exists(ScenePath), $"Scene missing: {ScenePath}");

            AsyncOperation load = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            while (!load.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }

            Assert.IsNotNull(player, "Player missing in Main scene.");

            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            Assert.IsNotNull(interactor, "PlayerInteractor missing on player.");

            InventoryComponent inventory = player.GetComponent<InventoryComponent>();
            Assert.IsNotNull(inventory, "InventoryComponent missing on player.");

            LootContainer container = Object.FindObjectOfType<LootContainer>(true);
            Assert.IsNotNull(container, "No LootContainer found in Main scene.");

            container.Interact(interactor);
            yield return null;

            Assert.Greater(container.CurrentLoot.Count, 0, "Container generated no loot.");
            ItemDefinition giveItem = container.CurrentLoot[0]?.Item;
            Assert.IsNotNull(giveItem, "Container generated loot with null item.");

            int beforeGive = inventory.GetItemCount(giveItem.ItemId);
            int accepted = inventory.AddItemAndReturnAccepted(giveItem, 1);
            Assert.Greater(accepted, 0, "Inventory did not accept test item.");
            int afterGive = inventory.GetItemCount(giveItem.ItemId);
            Assert.Greater(afterGive, beforeGive, "Item was not added to inventory.");

            SpawnerZone spawner = Object.FindObjectOfType<SpawnerZone>();
            Assert.IsNotNull(spawner, "SpawnerZone missing in Main scene.");

            int pickupBeforeKill = Object.FindObjectsOfType<WorldItemPickup>(true).Length;
            spawner.SpawnZombie();
            yield return null;
            yield return new WaitForSeconds(0.6f);

            ZombieAI[] zombies = Object.FindObjectsOfType<ZombieAI>(true);
            ZombieAI zombie = FindClosestZombie(player.transform.position, zombies);
            Assert.IsNotNull(zombie, "No spawned zombie found.");

            HealthComponent zombieHealth = zombie.GetComponent<HealthComponent>();
            Assert.IsNotNull(zombieHealth, "Spawned zombie missing HealthComponent.");

            zombieHealth.ApplyDamage(new DamageInfo(
                9999f,
                zombie.transform.position,
                Vector3.up,
                Vector3.forward,
                player,
                "tests.kill"));

            yield return null;
            yield return new WaitForSeconds(0.25f);

            WorldItemPickup[] pickupsAfterKill = Object.FindObjectsOfType<WorldItemPickup>(true);
            Assert.Greater(pickupsAfterKill.Length, pickupBeforeKill, "Zombie death did not spawn loot pickup.");

            WorldItemPickup pickup = FindClosestPickup(player.transform.position, pickupsAfterKill);
            Assert.IsNotNull(pickup, "Could not resolve spawned pickup.");
            Assert.IsNotNull(pickup.Item, "Spawned pickup has no item definition.");

            string pickupItemId = pickup.Item.ItemId;
            int beforePickup = inventory.GetItemCount(pickupItemId);
            pickup.Interact(interactor);
            yield return null;
            int afterPickup = inventory.GetItemCount(pickupItemId);
            Assert.Greater(afterPickup, beforePickup, "Pickup interaction did not add item to inventory.");

            int inventoryBeforeLootAll = GetInventoryTotal(inventory);
            int moved = container.TransferAllLoot(inventory, interactor.EventHub);
            yield return null;
            int inventoryAfterLootAll = GetInventoryTotal(inventory);

            Assert.Greater(moved, 0, "TransferAllLoot moved zero items from container.");
            Assert.Greater(inventoryAfterLootAll, inventoryBeforeLootAll, "Container loot transfer did not change inventory.");

            LogAssert.NoUnexpectedReceived();
        }

        private static ZombieAI FindClosestZombie(Vector3 position, ZombieAI[] zombies)
        {
            ZombieAI best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < zombies.Length; i++)
            {
                ZombieAI candidate = zombies[i];
                if (candidate == null || candidate.IsDead || !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float distance = Vector3.Distance(position, candidate.transform.position);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                best = candidate;
            }

            return best;
        }

        private static WorldItemPickup FindClosestPickup(Vector3 position, WorldItemPickup[] pickups)
        {
            WorldItemPickup best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < pickups.Length; i++)
            {
                WorldItemPickup candidate = pickups[i];
                if (candidate == null || !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float distance = Vector3.Distance(position, candidate.transform.position);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                best = candidate;
            }

            return best;
        }

        private static int GetInventoryTotal(InventoryComponent inventory)
        {
            if (inventory == null || inventory.Slots == null)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                InventorySlot slot = inventory.Slots[i];
                if (slot == null || slot.IsEmpty)
                {
                    continue;
                }

                total += slot.Quantity;
            }

            return total;
        }
    }
}
