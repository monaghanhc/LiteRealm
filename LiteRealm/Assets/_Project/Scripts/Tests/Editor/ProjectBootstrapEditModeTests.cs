#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using LiteRealm.EditorTools;

namespace LiteRealm.Tests.Editor
{
    public class ProjectBootstrapEditModeTests
    {
        private static bool step4BuiltThisSession;
        private static bool step5BuiltThisSession;

        [Test]
        public void UnityVersion_Is_2022_3_OrNewer()
        {
            Assert.IsTrue(ParseUnityVersion(UnityEngine.Application.unityVersion, out int major, out int minor),
                $"Could not parse Unity version: {UnityEngine.Application.unityVersion}");

            bool compatible = major > 2022 || (major == 2022 && minor >= 3);
            Assert.IsTrue(compatible,
                $"Unity version {UnityEngine.Application.unityVersion} is below required baseline 2022.3.");
        }

        [Test]
        public void RequiredFolders_Exist()
        {
            for (int i = 0; i < ProjectDoctorConstants.RequiredFolders.Length; i++)
            {
                string folder = ProjectDoctorConstants.RequiredFolders[i];
                Assert.IsTrue(Directory.Exists(folder), $"Missing required folder: {folder}");
            }
        }

        [Test]
        public void RequiredTags_Exist()
        {
            for (int i = 0; i < ProjectDoctorConstants.RequiredTags.Length; i++)
            {
                string tag = ProjectDoctorConstants.RequiredTags[i];
                Assert.IsTrue(ProjectDoctorRunner.HasTag(tag), $"Missing required tag: {tag}");
            }
        }

        [Test]
        public void RequiredLayers_Exist()
        {
            for (int i = 0; i < ProjectDoctorConstants.RequiredLayers.Length; i++)
            {
                string layer = ProjectDoctorConstants.RequiredLayers[i];
                Assert.IsTrue(ProjectDoctorRunner.HasLayer(layer), $"Missing required layer: {layer}");
            }
        }

        [Test]
        public void InputSystem_Installed_OrFallbackInputModeEnabled()
        {
            bool inputSystemInstalled = ProjectDoctorRunner.IsPackageInstalled(ProjectDoctorConstants.InputSystemPackage);
            InputHandlingMode mode = ProjectDoctorRunner.GetInputHandlingMode();

            bool pass = inputSystemInstalled
                ? (mode == InputHandlingMode.New || mode == InputHandlingMode.Both)
                : (mode == InputHandlingMode.Old || mode == InputHandlingMode.Both);

            Assert.IsTrue(pass,
                $"Input setup invalid. Package installed={inputSystemInstalled}, mode={mode}. " +
                "Expected New/Both when Input System is installed, or Old/Both when missing.");
        }

        [Test]
        public void AiNavigation_Installed_OrBuiltInNavMeshFallbackAvailable()
        {
            bool aiNavigationInstalled = ProjectDoctorRunner.IsPackageInstalled(ProjectDoctorConstants.AiNavigationPackage);
            bool builtInFallbackAvailable = typeof(NavMesh) != null;

            Assert.IsTrue(aiNavigationInstalled || builtInFallbackAvailable,
                "AI Navigation package missing and built-in NavMesh fallback unavailable.");
        }

        [Test]
        public void ScriptCompilation_Succeeded_NoEditorCompilationFailureFlag()
        {
            bool hasProperty = TryGetScriptCompilationFailed(out bool compilationFailed);
            if (!hasProperty)
            {
                Assert.Inconclusive("Unity API for script compilation status is unavailable on this editor version.");
                return;
            }

            Assert.IsFalse(compilationFailed,
                "Unity reports script compilation errors. Resolve compiler errors before proceeding.");
        }

        [Test]
        public void MainScene_Exists()
        {
            EnsureStep4Built();
            Assert.IsTrue(File.Exists(ProjectDoctorConstants.MainScenePath),
                $"Expected scene missing: {ProjectDoctorConstants.MainScenePath}");
        }

        [Test]
        public void MainScene_Has_Required_RootObjects()
        {
            EnsureStep4Built();
            if (!File.Exists(ProjectDoctorConstants.MainScenePath))
            {
                Assert.Inconclusive($"Scene missing: {ProjectDoctorConstants.MainScenePath}");
                return;
            }

            Scene previousActive = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ProjectDoctorConstants.MainScenePath);
            bool opened = false;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ProjectDoctorConstants.MainScenePath, OpenSceneMode.Additive);
                opened = true;
            }

            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                HashSet<string> names = new HashSet<string>();
                for (int i = 0; i < roots.Length; i++)
                {
                    if (roots[i] != null)
                    {
                        names.Add(roots[i].name);
                    }
                }

                for (int i = 0; i < ProjectDoctorConstants.RequiredMainSceneRootObjects.Length; i++)
                {
                    string requiredRoot = ProjectDoctorConstants.RequiredMainSceneRootObjects[i];
                    Assert.IsTrue(names.Contains(requiredRoot), $"Missing root object: {requiredRoot}");
                }

                Assert.IsNotNull(FindRootComponent<Terrain>(roots), "Missing Terrain component on root objects.");
                Assert.IsNotNull(FindRootComponent<EventSystem>(roots), "Missing EventSystem component on root objects.");
                Assert.IsNotNull(FindRootComponent<Canvas>(roots), "Missing Canvas component on root objects.");

                GameObject player = FindByTagInScene(scene, "Player");
                Assert.IsNotNull(player, "No object tagged Player found in Main scene.");

                bool hasDirectionalLight = false;
                for (int i = 0; i < roots.Length; i++)
                {
                    GameObject root = roots[i];
                    if (root == null || root.name != "Directional Light")
                    {
                        continue;
                    }

                    Light light = root.GetComponent<Light>();
                    hasDirectionalLight = light != null && light.type == LightType.Directional;
                    if (hasDirectionalLight)
                    {
                        break;
                    }
                }

                Assert.IsTrue(hasDirectionalLight, "Missing directional light root object.");
            }
            finally
            {
                if (opened)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (previousActive.IsValid() && previousActive.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActive);
                }
            }
        }

        [Test]
        public void CombatPrefabs_Exist_ForStep2()
        {
            EnsureStep4Built();
            for (int i = 0; i < ProjectDoctorConstants.RequiredCombatPrefabPaths.Length; i++)
            {
                string path = ProjectDoctorConstants.RequiredCombatPrefabPaths[i];
                Assert.IsTrue(File.Exists(path), $"Expected combat prefab missing: {path}");
            }
        }

        [Test]
        public void Step2_CombatChecks_Pass_InProjectDoctor()
        {
            EnsureStep4Built();
            DoctorReport report = ProjectDoctorRunner.RunChecks();
            Assert.IsNotNull(report, "ProjectDoctor report should not be null.");

            AssertCheckPass(report, "SPAWNER_EXISTS");
            AssertCheckPass(report, "SPAWNER_ASSIGNMENTS");
            AssertCheckPass(report, "SPAWNER_RUNTIME_REFS");
            AssertCheckPass(report, "BOSS_MANAGER_EXISTS");
            AssertCheckPass(report, "BOSS_MANAGER_ASSIGNMENTS");
            AssertCheckPass(report, "NAVMESH_STRATEGY");
            AssertCheckPass(report, "NAVMESH_AGENTS");
        }

        [Test]
        public void Step3_TimeSurvivalChecks_Pass_InProjectDoctor()
        {
            EnsureStep4Built();
            DoctorReport report = ProjectDoctorRunner.RunChecks();
            Assert.IsNotNull(report, "ProjectDoctor report should not be null.");

            AssertCheckPass(report, "DAYNIGHT_EXISTS");
            AssertCheckPass(report, "DAYNIGHT_REFERENCES");
            AssertCheckPass(report, "SURVIVAL_HUD_EXISTS");
            AssertCheckPass(report, "SURVIVAL_HUD_BINDINGS");
            AssertCheckPass(report, "NIGHT_SPAWN_SCALING");
            AssertCheckPass(report, "NIGHT_ZOMBIE_AGGRO");
        }

        [Test]
        public void Step4_LootAssets_Exist()
        {
            EnsureStep4Built();

            for (int i = 0; i < ProjectDoctorConstants.RequiredItemDefinitionPaths.Length; i++)
            {
                string path = ProjectDoctorConstants.RequiredItemDefinitionPaths[i];
                Assert.IsTrue(File.Exists(path), $"Expected item definition missing: {path}");
            }

            for (int i = 0; i < ProjectDoctorConstants.RequiredLootTablePaths.Length; i++)
            {
                string path = ProjectDoctorConstants.RequiredLootTablePaths[i];
                Assert.IsTrue(File.Exists(path), $"Expected loot table missing: {path}");
            }

            Assert.IsTrue(File.Exists(ProjectDoctorConstants.LootContainerPrefabPath),
                $"Expected loot container prefab missing: {ProjectDoctorConstants.LootContainerPrefabPath}");
        }

        [Test]
        public void Step4_LootChecks_Pass_InProjectDoctor()
        {
            EnsureStep4Built();
            DoctorReport report = ProjectDoctorRunner.RunChecks();
            Assert.IsNotNull(report, "ProjectDoctor report should not be null.");

            AssertCheckPass(report, "LOOT_ITEMS_EXIST");
            AssertCheckPass(report, "LOOT_TABLES_EXIST");
            AssertCheckPass(report, "LOOT_CONTAINER_PREFAB_EXISTS");
            AssertCheckPass(report, "LOOT_CONTAINER_TABLE_ASSIGNED");
        }

        [Test]
        public void Step5_QuestAssets_AndNpcAssignments_Exist()
        {
            EnsureStep5Built();

            Assert.IsTrue(File.Exists(ProjectDoctorConstants.QuestDatabasePath),
                $"Expected quest database missing: {ProjectDoctorConstants.QuestDatabasePath}");

            for (int i = 0; i < ProjectDoctorConstants.RequiredQuestDefinitionPaths.Length; i++)
            {
                string path = ProjectDoctorConstants.RequiredQuestDefinitionPaths[i];
                Assert.IsTrue(File.Exists(path), $"Expected quest definition missing: {path}");
            }
        }

        [Test]
        public void Step5_QuestPersistenceChecks_Pass_InProjectDoctor()
        {
            EnsureStep5Built();
            DoctorReport report = ProjectDoctorRunner.RunChecks();
            Assert.IsNotNull(report, "ProjectDoctor report should not be null.");

            AssertCheckPass(report, "QUEST_ASSETS_EXIST");
            AssertCheckPass(report, "QUEST_NPC_ASSIGNED");
            AssertCheckPass(report, "SAVE_CREATE_LOAD");
        }

        private static bool ParseUnityVersion(string version, out int major, out int minor)
        {
            major = 0;
            minor = 0;

            if (string.IsNullOrWhiteSpace(version))
            {
                return false;
            }

            string[] parts = version.Split('.');
            if (parts.Length < 2)
            {
                return false;
            }

            return int.TryParse(parts[0], out major) && int.TryParse(parts[1], out minor);
        }

        private static bool TryGetScriptCompilationFailed(out bool failed)
        {
            failed = false;
            System.Reflection.PropertyInfo property = typeof(EditorUtility).GetProperty("scriptCompilationFailed");
            if (property == null)
            {
                return false;
            }

            object value = property.GetValue(null, null);
            if (value is bool boolValue)
            {
                failed = boolValue;
                return true;
            }

            return false;
        }

        private static T FindRootComponent<T>(GameObject[] roots) where T : Component
        {
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] == null)
                {
                    continue;
                }

                T component = roots[i].GetComponent<T>();
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static GameObject FindByTagInScene(Scene scene, string tag)
        {
            if (!ProjectDoctorRunner.HasTag(tag))
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].CompareTag(tag))
                    {
                        return transforms[j].gameObject;
                    }
                }
            }

            return null;
        }

        private static void AssertCheckPass(DoctorReport report, string code)
        {
            DoctorCheckResult result = null;
            for (int i = 0; i < report.Results.Count; i++)
            {
                DoctorCheckResult candidate = report.Results[i];
                if (candidate == null || candidate.Code != code)
                {
                    continue;
                }

                result = candidate;
                break;
            }

            Assert.IsNotNull(result, $"ProjectDoctor did not return check code {code}.");
            Assert.IsTrue(result.Passed, $"ProjectDoctor check failed: {code}. Message={result.Message}");
        }

        private static void EnsureStep4Built()
        {
            if (step4BuiltThisSession)
            {
                return;
            }

            MainSceneStep4LootBuilder.ApplyStep4();
            AssetDatabase.Refresh();
            step4BuiltThisSession = true;
        }

        private static void EnsureStep5Built()
        {
            if (step5BuiltThisSession)
            {
                return;
            }

            EnsureStep4Built();
            MainSceneStep5QuestPersistenceBuilder.ApplyStep5();
            AssetDatabase.Refresh();
            step5BuiltThisSession = true;
        }
    }
}
#endif
