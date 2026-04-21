using System.Collections;
using System.Reflection;
using LiteRealm.Core;
using LiteRealm.Gameplay;
using LiteRealm.Inventory;
using LiteRealm.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LiteRealm.Tests.PlayMode
{
    public class PhaseOnePlayModeTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [UnityTest]
        public IEnumerator PhaseOne_BuildingConsumesScrapAndPlacesPiece()
        {
            Assert.IsTrue(System.IO.File.Exists(ScenePath), $"Scene missing: {ScenePath}");
            AsyncOperation load = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;
            yield return null;

            GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            Assert.IsNotNull(player, "Player not found.");

            InventoryComponent inventory = player.GetComponent<InventoryComponent>();
            Assert.IsNotNull(inventory, "InventoryComponent missing on player.");

            ItemDefinition scrap = ScriptableObject.CreateInstance<ItemDefinition>();
            SetPrivateField(scrap, "itemId", "item.material.scrap");
            SetPrivateField(scrap, "displayName", "Scrap Metal");
            SetPrivateField(scrap, "maxStack", 100);

            int initialCount = inventory.GetItemCount(scrap.ItemId);
            inventory.AddItem(scrap, 10);

            BuildSystemController builder = player.GetComponent<BuildSystemController>();
            Assert.IsNotNull(builder, "BuildSystemController missing on player.");

            int beforePlaceObjects = Object.FindObjectsOfType<Transform>(true).Length;
            bool placed = builder.TryPlaceCurrentPieceForTests(player.transform.position + player.transform.forward * 3f);
            yield return null;

            Assert.IsTrue(placed, "Expected building placement to succeed with scrap available.");
            Assert.Less(inventory.GetItemCount(scrap.ItemId), initialCount + 10, "Scrap should be consumed when placing.");
            Assert.Greater(Object.FindObjectsOfType<Transform>(true).Length, beforePlaceObjects, "Expected new build piece in scene.");
        }

        [UnityTest]
        public IEnumerator PhaseOne_BossKillEventShowsVictory()
        {
            Assert.IsTrue(System.IO.File.Exists(ScenePath), $"Scene missing: {ScenePath}");
            AsyncOperation load = SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;
            yield return null;

            GameProgressionController progression = Object.FindObjectOfType<GameProgressionController>();
            Assert.IsNotNull(progression, "GameProgressionController missing.");

            GameEventHub hub = Object.FindObjectOfType<GameEventHub>();
            Assert.IsNotNull(hub, "GameEventHub missing.");

            hub.RaiseBossKilled(new BossKilledEvent
            {
                BossId = "boss.alpha",
                BossObject = null,
                Killer = null
            });

            yield return null;
            Assert.IsTrue(progression.IsWinShownForTests, "Victory flow did not trigger after boss kill event.");
            LogAssert.NoUnexpectedReceived();
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing private field: {fieldName}");
            field.SetValue(target, value);
        }
    }
}
