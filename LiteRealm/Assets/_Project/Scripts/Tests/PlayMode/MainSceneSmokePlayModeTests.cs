using System.Collections;
using LiteRealm.CameraSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LiteRealm.Tests.PlayMode
{
    public class MainSceneSmokePlayModeTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [UnityTest]
        public IEnumerator MainScene_Loads_PlayerSpawns_CameraToggles_NoMissingScripts()
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

            Assert.IsNotNull(player, "Player object not found after scene load.");
            Assert.IsTrue(IsFiniteVector(player.transform.position), "Player position is invalid.");

            GameObject spawn = GameObject.Find("PlayerSpawn");
            Assert.IsNotNull(spawn, "PlayerSpawn object missing.");
            float distance = Vector3.Distance(player.transform.position, spawn.transform.position);
            Assert.LessOrEqual(distance, 5f, $"Player spawned too far from PlayerSpawn. Distance={distance:F2}");

            PlayerCameraController cameraController = Object.FindObjectOfType<PlayerCameraController>();
            Assert.IsNotNull(cameraController, "Camera controller missing in scene.");

            bool before = cameraController.IsFirstPerson;
            bool after = cameraController.ToggleViewModeForTests();
            Assert.AreNotEqual(before, after, "Camera toggle did not change mode.");

            int missingScripts = CountMissingScriptsInLoadedScenes();
            Assert.AreEqual(0, missingScripts, $"Found {missingScripts} missing script reference(s).");
        }

        private static int CountMissingScriptsInLoadedScenes()
        {
            int count = 0;
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    if (roots[i] == null)
                    {
                        continue;
                    }

                    Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                    for (int j = 0; j < transforms.Length; j++)
                    {
                        Component[] components = transforms[j].GetComponents<Component>();
                        for (int k = 0; k < components.Length; k++)
                        {
                            if (components[k] == null)
                            {
                                count++;
                            }
                        }
                    }
                }
            }

            return count;
        }

        private static bool IsFiniteVector(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }
    }
}
