#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using LiteRealm.AI;
using LiteRealm.CameraSystem;
using LiteRealm.Loot;
using LiteRealm.Quests;
using LiteRealm.Saving;
using LiteRealm.UI;
using LiteRealm.World;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace LiteRealm.EditorTools
{
    public static class ProjectDoctorRunner
    {
        private static AddRequest activeAddRequest;
        private static readonly Queue<string> pendingPackageInstalls = new Queue<string>();
        private static Action<string> installLog;
        private static Action installFinished;

        public static bool IsInstallInProgress => activeAddRequest != null || pendingPackageInstalls.Count > 0;

        public static DoctorReport RunChecks()
        {
            DoctorReport report = new DoctorReport();

            CheckUnityVersion(report);
            CheckRequiredFolders(report);
            CheckInputHandling(report);
            CheckPackages(report);
            CheckTags(report);
            CheckLayers(report);
            CheckContentPresence(report);
            CheckMainSceneStructure(report);
            CheckCombatAndEnemySetup(report);
            CheckTimeAndSurvivalSetup(report);
            CheckLootLoopSetup(report);
            CheckQuestAndPersistenceSetup(report);

            return report;
        }

        public static void EnsureRequiredFoldersExist()
        {
            for (int i = 0; i < ProjectDoctorConstants.RequiredFolders.Length; i++)
            {
                string folder = ProjectDoctorConstants.RequiredFolders[i];
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }

            AssetDatabase.Refresh();
        }

        public static bool EnsureRequiredTagsAndLayers(out string summary)
        {
            bool changed = false;
            List<string> lines = new List<string>();

            for (int i = 0; i < ProjectDoctorConstants.RequiredTags.Length; i++)
            {
                string tag = ProjectDoctorConstants.RequiredTags[i];
                if (HasTag(tag))
                {
                    continue;
                }

                bool added = AddTag(tag);
                changed |= added;
                lines.Add(added ? $"Added tag: {tag}" : $"Failed to add tag: {tag}");
            }

            for (int i = 0; i < ProjectDoctorConstants.RequiredLayers.Length; i++)
            {
                string layer = ProjectDoctorConstants.RequiredLayers[i];
                if (HasLayer(layer))
                {
                    continue;
                }

                bool added = AddLayer(layer);
                changed |= added;
                lines.Add(added ? $"Added layer: {layer}" : $"Failed to add layer: {layer} (no free slot)");
            }

            summary = lines.Count == 0 ? "Tags/layers already satisfied." : string.Join("\n", lines);
            return changed;
        }

        public static InputHandlingMode GetInputHandlingMode()
        {
            UnityEngine.Object[] settingsObjects = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settingsObjects == null || settingsObjects.Length == 0 || settingsObjects[0] == null)
            {
                return InputHandlingMode.Unknown;
            }

            SerializedObject serialized = new SerializedObject(settingsObjects[0]);
            SerializedProperty property = serialized.FindProperty("activeInputHandler");
            if (property == null)
            {
                return InputHandlingMode.Unknown;
            }

            int value = property.intValue;
            if (value == 0)
            {
                return InputHandlingMode.Old;
            }

            if (value == 1)
            {
                return InputHandlingMode.New;
            }

            if (value == 2)
            {
                return InputHandlingMode.Both;
            }

            return InputHandlingMode.Unknown;
        }

        public static void SetInputHandlingMode(InputHandlingMode mode)
        {
            if (mode == InputHandlingMode.Unknown)
            {
                return;
            }

            UnityEngine.Object[] settingsObjects = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settingsObjects == null || settingsObjects.Length == 0 || settingsObjects[0] == null)
            {
                Debug.LogWarning("ProjectDoctor: Could not locate ProjectSettings.asset to set input handling.");
                return;
            }

            SerializedObject serialized = new SerializedObject(settingsObjects[0]);
            SerializedProperty property = serialized.FindProperty("activeInputHandler");
            if (property == null)
            {
                Debug.LogWarning("ProjectDoctor: activeInputHandler property not found.");
                return;
            }

            property.intValue = (int)mode;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        public static bool IsPackageInstalled(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                return false;
            }

            UnityEditor.PackageManager.PackageInfo[] packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            if (packages == null)
            {
                return false;
            }

            for (int i = 0; i < packages.Length; i++)
            {
                UnityEditor.PackageManager.PackageInfo package = packages[i];
                if (package != null && package.name == packageId)
                {
                    return true;
                }
            }

            return false;
        }

        public static List<string> GetMissingRequiredPackages()
        {
            List<string> missing = new List<string>();
            for (int i = 0; i < ProjectDoctorConstants.RequiredPackageIds.Length; i++)
            {
                string packageId = ProjectDoctorConstants.RequiredPackageIds[i];
                if (!IsPackageInstalled(packageId))
                {
                    missing.Add(packageId);
                }
            }

            return missing;
        }

        public static void InstallMissingRequiredPackages(Action<string> logCallback = null, Action onFinished = null)
        {
            if (activeAddRequest != null)
            {
                logCallback?.Invoke("Package installation already in progress.");
                return;
            }

            pendingPackageInstalls.Clear();
            List<string> missing = GetMissingRequiredPackages();
            for (int i = 0; i < missing.Count; i++)
            {
                pendingPackageInstalls.Enqueue(missing[i]);
            }

            if (pendingPackageInstalls.Count == 0)
            {
                logCallback?.Invoke("No missing required packages.");
                onFinished?.Invoke();
                return;
            }

            installLog = logCallback;
            installFinished = onFinished;
            EditorApplication.update -= UpdateInstallRequest;
            EditorApplication.update += UpdateInstallRequest;
            StartNextInstall();
        }

        public static bool HasTag(string tag)
        {
            string[] tags = InternalEditorUtility.tags;
            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == tag)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasLayer(string layer)
        {
            if (string.IsNullOrWhiteSpace(layer))
            {
                return false;
            }

            return LayerMask.NameToLayer(layer) >= 0;
        }

        public static void LogReport(DoctorReport report)
        {
            if (report == null)
            {
                Debug.LogWarning("ProjectDoctor: No report to log.");
                return;
            }

            Debug.Log($"ProjectDoctor report: {report.ErrorCount} errors, {report.WarningCount} warnings.");
            for (int i = 0; i < report.Results.Count; i++)
            {
                DoctorCheckResult result = report.Results[i];
                if (result == null)
                {
                    continue;
                }

                string prefix = result.Severity switch
                {
                    DoctorSeverity.Error => "ERROR",
                    DoctorSeverity.Warning => "WARN",
                    _ => "INFO"
                };

                string state = result.Passed ? "PASS" : "FAIL";
                string line = $"[ProjectDoctor:{prefix}:{state}] {result.Code} - {result.Message}";
                if (!string.IsNullOrWhiteSpace(result.FixHint))
                {
                    line += $" | Fix: {result.FixHint}";
                }

                if (result.Passed)
                {
                    Debug.Log(line);
                }
                else if (result.Severity == DoctorSeverity.Error)
                {
                    Debug.LogError(line);
                }
                else
                {
                    Debug.LogWarning(line);
                }
            }
        }

        public static bool RunQuickPlayCheck(out List<string> lines)
        {
            lines = new List<string>();
            string scenePath = ProjectDoctorConstants.MainScenePath;

            if (!File.Exists(scenePath))
            {
                lines.Add($"FAIL: Missing scene at {scenePath}");
                return false;
            }

            Scene previouslyActive = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedByDoctor = false;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                openedByDoctor = true;
            }

            bool success = true;
            try
            {
                GameObject player = FindSceneObjectByName(scene, "Player") ?? FindSceneObjectByTag(scene, "Player");
                if (player == null)
                {
                    success = false;
                    lines.Add("FAIL: Player object not found.");
                }
                else
                {
                    bool finitePosition = IsFiniteVector(player.transform.position);
                    if (!finitePosition)
                    {
                        success = false;
                        lines.Add("FAIL: Player position contains NaN/Infinity.");
                    }
                    else
                    {
                        lines.Add($"PASS: Player found at {player.transform.position}.");
                    }
                }

                GameObject spawn = FindSceneObjectByName(scene, "PlayerSpawn");
                if (player != null && spawn != null)
                {
                    float distance = Vector3.Distance(player.transform.position, spawn.transform.position);
                    bool nearSpawn = distance <= 5f;
                    success &= nearSpawn;
                    lines.Add(nearSpawn
                        ? $"PASS: Player is near start point (distance {distance:F2})."
                        : $"FAIL: Player is too far from start point (distance {distance:F2}).");
                }
                else
                {
                    lines.Add("WARN: PlayerSpawn not found, skipped spawn-distance check.");
                }

                PlayerCameraController cameraController = FindComponentInScene<PlayerCameraController>(scene);
                if (cameraController == null)
                {
                    success = false;
                    lines.Add("FAIL: Camera controller not found.");
                }
                else
                {
                    bool before = cameraController.IsFirstPerson;
                    bool after = cameraController.ToggleViewModeForTests();
                    bool toggled = before != after;
                    success &= toggled;
                    lines.Add(toggled
                        ? "PASS: Camera view toggle executed."
                        : "FAIL: Camera view toggle did not change state.");
                    cameraController.ToggleViewModeForTests();
                }

                int missingScripts = CountMissingScripts(scene);
                if (missingScripts == 0)
                {
                    lines.Add("PASS: No missing script references in scene.");
                }
                else
                {
                    success = false;
                    lines.Add($"FAIL: Found {missingScripts} missing script reference(s).");
                }
            }
            finally
            {
                if (openedByDoctor)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (previouslyActive.IsValid() && previouslyActive.isLoaded)
                {
                    SceneManager.SetActiveScene(previouslyActive);
                }
            }

            return success;
        }

        private static void CheckUnityVersion(DoctorReport report)
        {
            bool ok = TryParseUnityVersion(Application.unityVersion, out int major, out int minor) && (major > 2022 || (major == 2022 && minor >= 3));

            report.Results.Add(new DoctorCheckResult
            {
                Code = "UNITY_VERSION",
                Passed = ok,
                Severity = ok ? DoctorSeverity.Info : DoctorSeverity.Warning,
                Message = ok
                    ? $"Unity version {Application.unityVersion} is compatible."
                    : $"Unity version {Application.unityVersion} is below 2022.3 LTS recommended baseline.",
                FixHint = ok ? string.Empty : "Use Unity 2022.3 LTS or newer."
            });
        }

        private static void CheckRequiredFolders(DoctorReport report)
        {
            for (int i = 0; i < ProjectDoctorConstants.RequiredFolders.Length; i++)
            {
                string folder = ProjectDoctorConstants.RequiredFolders[i];
                bool exists = Directory.Exists(folder);
                report.Results.Add(new DoctorCheckResult
                {
                    Code = "FOLDER",
                    Passed = exists,
                    Severity = exists ? DoctorSeverity.Info : DoctorSeverity.Error,
                    Message = exists ? $"Folder exists: {folder}" : $"Missing folder: {folder}",
                    FixHint = exists ? string.Empty : "Use ProjectDoctor > Ensure Folder Structure."
                });
            }
        }

        private static void CheckInputHandling(DoctorReport report)
        {
            InputHandlingMode mode = GetInputHandlingMode();
            bool inputSystemInstalled = IsPackageInstalled(ProjectDoctorConstants.InputSystemPackage);

            bool pass = inputSystemInstalled
                ? (mode == InputHandlingMode.New || mode == InputHandlingMode.Both)
                : (mode == InputHandlingMode.Old || mode == InputHandlingMode.Both);

            string modeText = mode.ToString();
            string message;
            string fix;

            if (pass)
            {
                message = $"Input handling mode is {modeText}.";
                fix = string.Empty;
            }
            else
            {
                message = inputSystemInstalled
                    ? $"Input System is installed but Active Input Handling is {modeText}."
                    : $"Input System is missing and Active Input Handling is {modeText}.";
                fix = "Set Active Input Handling to Both (safe) or New if Input System is installed.";
            }

            report.Results.Add(new DoctorCheckResult
            {
                Code = "INPUT_HANDLING",
                Passed = pass,
                Severity = pass ? DoctorSeverity.Info : DoctorSeverity.Warning,
                Message = message,
                FixHint = fix
            });
        }

        private static void CheckPackages(DoctorReport report)
        {
            for (int i = 0; i < ProjectDoctorConstants.RequiredPackageIds.Length; i++)
            {
                string packageId = ProjectDoctorConstants.RequiredPackageIds[i];
                bool installed = IsPackageInstalled(packageId);
                report.Results.Add(new DoctorCheckResult
                {
                    Code = "PACKAGE_REQUIRED",
                    Passed = installed,
                    Severity = installed ? DoctorSeverity.Info : DoctorSeverity.Warning,
                    Message = installed ? $"Installed required package: {packageId}" : $"Missing required package: {packageId}",
                    FixHint = installed ? string.Empty : "Use Package Manager or ProjectDoctor install action."
                });
            }

            for (int i = 0; i < ProjectDoctorConstants.OptionalPackageIds.Length; i++)
            {
                string packageId = ProjectDoctorConstants.OptionalPackageIds[i];
                bool installed = IsPackageInstalled(packageId);
                report.Results.Add(new DoctorCheckResult
                {
                    Code = "PACKAGE_OPTIONAL",
                    Passed = installed,
                    Severity = installed ? DoctorSeverity.Info : DoctorSeverity.Warning,
                    Message = installed
                        ? $"Optional package installed: {packageId}"
                        : $"Optional package not installed: {packageId} (custom camera fallback is valid)",
                    FixHint = installed ? string.Empty : "Optional only. Install for camera polish if desired."
                });
            }
        }

        private static void CheckTags(DoctorReport report)
        {
            for (int i = 0; i < ProjectDoctorConstants.RequiredTags.Length; i++)
            {
                string tag = ProjectDoctorConstants.RequiredTags[i];
                bool exists = HasTag(tag);
                report.Results.Add(new DoctorCheckResult
                {
                    Code = "TAG",
                    Passed = exists,
                    Severity = exists ? DoctorSeverity.Info : DoctorSeverity.Error,
                    Message = exists ? $"Tag exists: {tag}" : $"Missing tag: {tag}",
                    FixHint = exists ? string.Empty : "Use ProjectDoctor > Ensure Required Tags/Layers."
                });
            }
        }

        private static void CheckLayers(DoctorReport report)
        {
            for (int i = 0; i < ProjectDoctorConstants.RequiredLayers.Length; i++)
            {
                string layer = ProjectDoctorConstants.RequiredLayers[i];
                bool exists = HasLayer(layer);
                report.Results.Add(new DoctorCheckResult
                {
                    Code = "LAYER",
                    Passed = exists,
                    Severity = exists ? DoctorSeverity.Info : DoctorSeverity.Error,
                    Message = exists ? $"Layer exists: {layer}" : $"Missing layer: {layer}",
                    FixHint = exists ? string.Empty : "Use ProjectDoctor > Ensure Required Tags/Layers."
                });
            }
        }

        private static void CheckContentPresence(DoctorReport report)
        {
            string[] scenes = Directory.Exists("Assets/_Project/Scenes")
                ? Directory.GetFiles("Assets/_Project/Scenes", "*.unity", SearchOption.AllDirectories)
                : Array.Empty<string>();

            string[] prefabs = Directory.Exists("Assets/_Project/Prefabs")
                ? Directory.GetFiles("Assets/_Project/Prefabs", "*.prefab", SearchOption.AllDirectories)
                : Array.Empty<string>();

            bool hasScene = scenes.Length > 0;
            bool hasPrefab = prefabs.Length > 0;

            report.Results.Add(new DoctorCheckResult
            {
                Code = "CONTENT_SCENES",
                Passed = hasScene,
                Severity = hasScene ? DoctorSeverity.Info : DoctorSeverity.Warning,
                Message = hasScene ? $"Found {scenes.Length} scene(s) under Assets/_Project/Scenes." : "No scenes found under Assets/_Project/Scenes yet.",
                FixHint = hasScene ? string.Empty : "Expected during bootstrap. Add scenes in the next build step."
            });

            report.Results.Add(new DoctorCheckResult
            {
                Code = "CONTENT_PREFABS",
                Passed = hasPrefab,
                Severity = hasPrefab ? DoctorSeverity.Info : DoctorSeverity.Warning,
                Message = hasPrefab ? $"Found {prefabs.Length} prefab(s) under Assets/_Project/Prefabs." : "No prefabs found under Assets/_Project/Prefabs yet.",
                FixHint = hasPrefab ? string.Empty : "Expected during bootstrap. Add prefabs in the next build step."
            });
        }

        private static void CheckMainSceneStructure(DoctorReport report)
        {
            string scenePath = ProjectDoctorConstants.MainScenePath;
            bool exists = File.Exists(scenePath);

            report.Results.Add(new DoctorCheckResult
            {
                Code = "MAIN_SCENE_EXISTS",
                Passed = exists,
                Severity = exists ? DoctorSeverity.Info : DoctorSeverity.Error,
                Message = exists ? $"Main scene exists: {scenePath}" : $"Main scene missing: {scenePath}",
                FixHint = exists ? string.Empty : "Run Tools/LiteRealm/Scenes/Build Main Scene (Step 1 Exploration)."
            });

            if (!exists)
            {
                return;
            }

            Scene previousActive = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool openedByDoctor = false;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                openedByDoctor = true;
            }

            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                HashSet<string> rootNames = new HashSet<string>();
                for (int i = 0; i < roots.Length; i++)
                {
                    GameObject root = roots[i];
                    if (root != null)
                    {
                        rootNames.Add(root.name);
                    }
                }

                for (int i = 0; i < ProjectDoctorConstants.RequiredMainSceneRootObjects.Length; i++)
                {
                    string requiredRoot = ProjectDoctorConstants.RequiredMainSceneRootObjects[i];
                    bool hasRoot = rootNames.Contains(requiredRoot);

                    report.Results.Add(new DoctorCheckResult
                    {
                        Code = "MAIN_SCENE_ROOT",
                        Passed = hasRoot,
                        Severity = hasRoot ? DoctorSeverity.Info : DoctorSeverity.Error,
                        Message = hasRoot ? $"Root object exists: {requiredRoot}" : $"Missing root object: {requiredRoot}",
                        FixHint = hasRoot ? string.Empty : "Rebuild scene via Main Scene builder."
                    });
                }

                bool hasTerrain = FindRootWithComponent<Terrain>(roots) != null;
                bool hasCanvas = FindRootWithComponent<Canvas>(roots) != null;
                bool hasEventSystem = FindRootWithComponent<EventSystem>(roots) != null;
                bool hasPlayerTagged = FindSceneObjectByTag(scene, "Player") != null;
                bool hasDirectionalLight = HasDirectionalRootLight(roots);

                report.Results.Add(BuildMainSceneComponentCheck("MAIN_SCENE_TERRAIN", hasTerrain, "Terrain component"));
                report.Results.Add(BuildMainSceneComponentCheck("MAIN_SCENE_PLAYER", hasPlayerTagged, "Player (tagged)"));
                report.Results.Add(BuildMainSceneComponentCheck("MAIN_SCENE_DIRECTIONAL_LIGHT", hasDirectionalLight, "Directional Light"));
                report.Results.Add(BuildMainSceneComponentCheck("MAIN_SCENE_EVENTSYSTEM", hasEventSystem, "EventSystem"));
                report.Results.Add(BuildMainSceneComponentCheck("MAIN_SCENE_CANVAS", hasCanvas, "Canvas"));
            }
            finally
            {
                if (openedByDoctor)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (previousActive.IsValid() && previousActive.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActive);
                }
            }
        }

        private static void CheckCombatAndEnemySetup(DoctorReport report)
        {
            for (int i = 0; i < ProjectDoctorConstants.RequiredCombatPrefabPaths.Length; i++)
            {
                string path = ProjectDoctorConstants.RequiredCombatPrefabPaths[i];
                bool exists = File.Exists(path);
                report.Results.Add(new DoctorCheckResult
                {
                    Code = "COMBAT_PREFAB",
                    Passed = exists,
                    Severity = exists ? DoctorSeverity.Info : DoctorSeverity.Error,
                    Message = exists ? $"Combat prefab exists: {path}" : $"Missing combat prefab: {path}",
                    FixHint = exists ? string.Empty : "Run Tools/LiteRealm/Scenes/Apply Step 2 (Combat + Enemies)."
                });
            }

            if (!File.Exists(ProjectDoctorConstants.MainScenePath))
            {
                report.Results.Add(new DoctorCheckResult
                {
                    Code = "COMBAT_SCENE",
                    Passed = false,
                    Severity = DoctorSeverity.Error,
                    Message = "Main scene missing, skipping combat scene checks.",
                    FixHint = "Build Main scene first."
                });
                return;
            }

            Scene previousActive = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ProjectDoctorConstants.MainScenePath);
            bool openedByDoctor = false;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ProjectDoctorConstants.MainScenePath, OpenSceneMode.Additive);
                openedByDoctor = true;
            }

            try
            {
                SpawnerZone[] spawners = FindComponentsInScene<SpawnerZone>(scene);
                bool hasSpawners = spawners.Length > 0;
                report.Results.Add(BuildStep2Check(
                    "SPAWNER_EXISTS",
                    hasSpawners,
                    hasSpawners ? $"Found {spawners.Length} spawner zone(s)." : "No SpawnerZone found in Main scene.",
                    "Add at least one SpawnerZone via Step 2 scene builder."));

                bool allSpawnerAssignmentsValid = hasSpawners;
                for (int i = 0; i < spawners.Length; i++)
                {
                    SpawnerZone spawner = spawners[i];
                    bool valid = spawner != null && spawner.HasValidConfiguration;
                    allSpawnerAssignmentsValid &= valid;
                }

                report.Results.Add(BuildStep2Check(
                    "SPAWNER_ASSIGNMENTS",
                    allSpawnerAssignmentsValid,
                    allSpawnerAssignmentsValid
                        ? "Spawner references are assigned (zombie prefab + spawn points)."
                        : "One or more spawners have missing zombie prefab or spawn points.",
                    "Assign zombie prefab and spawn points on each SpawnerZone."));

                bool spawnersHaveRuntimeRefs = hasSpawners;
                for (int i = 0; i < spawners.Length; i++)
                {
                    SerializedObject so = new SerializedObject(spawners[i]);
                    SerializedProperty target = so.FindProperty("target");
                    SerializedProperty hub = so.FindProperty("eventHub");
                    SerializedProperty dayNight = so.FindProperty("dayNight");
                    bool valid = target != null && target.objectReferenceValue != null
                                 && hub != null && hub.objectReferenceValue != null
                                 && dayNight != null && dayNight.objectReferenceValue != null;
                    spawnersHaveRuntimeRefs &= valid;
                }

                report.Results.Add(BuildStep2Check(
                    "SPAWNER_RUNTIME_REFS",
                    spawnersHaveRuntimeRefs,
                    spawnersHaveRuntimeRefs
                        ? "Spawner runtime references are assigned (target/event hub/day-night)."
                        : "Spawner runtime references are incomplete.",
                    "Assign target, event hub, and day-night manager on each SpawnerZone."));

                BossSpawnManager bossManager = FindComponentInScene<BossSpawnManager>(scene);
                bool hasBossManager = bossManager != null;
                report.Results.Add(BuildStep2Check(
                    "BOSS_MANAGER_EXISTS",
                    hasBossManager,
                    hasBossManager ? "BossSpawnManager found." : "BossSpawnManager missing.",
                    "Create BossSpawnManager via Step 2 scene builder."));

                bool bossManagerConfigured = false;
                if (bossManager != null)
                {
                    SerializedObject so = new SerializedObject(bossManager);
                    SerializedProperty bossPrefab = so.FindProperty("bossPrefab");
                    SerializedProperty spawnPoint = so.FindProperty("spawnPoint");
                    SerializedProperty target = so.FindProperty("target");
                    SerializedProperty hub = so.FindProperty("eventHub");
                    SerializedProperty dayNight = so.FindProperty("dayNight");

                    bossManagerConfigured = bossPrefab != null && bossPrefab.objectReferenceValue != null
                                            && spawnPoint != null && spawnPoint.objectReferenceValue != null
                                            && target != null && target.objectReferenceValue != null
                                            && hub != null && hub.objectReferenceValue != null
                                            && dayNight != null && dayNight.objectReferenceValue != null;
                }

                report.Results.Add(BuildStep2Check(
                    "BOSS_MANAGER_ASSIGNMENTS",
                    bossManagerConfigured,
                    bossManagerConfigured
                        ? "Boss spawn manager references are assigned."
                        : "Boss spawn manager references are incomplete.",
                    "Assign boss prefab/spawn point/target/event hub/day-night."));

                bool hasBakedNavMesh = HasAnyNavMeshData();
                bool hasRuntimeBootstrap = FindComponentInScene<RuntimeNavMeshBootstrap>(scene) != null;
                bool hasNavMeshSurface = HasNavMeshSurfaceComponent(scene);
                bool navMeshStrategyExists = hasBakedNavMesh || hasRuntimeBootstrap || hasNavMeshSurface;

                string navMessage = navMeshStrategyExists
                    ? $"NavMesh strategy present (baked={hasBakedNavMesh}, runtimeBootstrap={hasRuntimeBootstrap}, surface={hasNavMeshSurface})."
                    : "No NavMesh strategy found (no baked data, RuntimeNavMeshBootstrap, or NavMeshSurface).";

                report.Results.Add(BuildStep2Check(
                    "NAVMESH_STRATEGY",
                    navMeshStrategyExists,
                    navMessage,
                    "Bake NavMesh, add RuntimeNavMeshBootstrap, or add NavMeshSurface."));

                NavMeshAgent[] agents = FindComponentsInScene<NavMeshAgent>(scene);
                bool hasAgents = agents.Length > 0;
                report.Results.Add(BuildStep2Check(
                    "NAVMESH_AGENTS",
                    hasAgents,
                    hasAgents ? $"Found {agents.Length} NavMeshAgent component(s)." : "No NavMeshAgent found in scene.",
                    "Ensure zombie/boss actors with NavMeshAgent exist in scene or spawned prefabs."));
            }
            finally
            {
                if (openedByDoctor)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (previousActive.IsValid() && previousActive.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActive);
                }
            }
        }

        private static void CheckTimeAndSurvivalSetup(DoctorReport report)
        {
            if (!File.Exists(ProjectDoctorConstants.MainScenePath))
            {
                report.Results.Add(new DoctorCheckResult
                {
                    Code = "TIME_SURVIVAL_SCENE",
                    Passed = false,
                    Severity = DoctorSeverity.Error,
                    Message = "Main scene missing, skipping time/survival checks.",
                    FixHint = "Build Main scene first."
                });
                return;
            }

            Scene previousActive = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ProjectDoctorConstants.MainScenePath);
            bool openedByDoctor = false;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ProjectDoctorConstants.MainScenePath, OpenSceneMode.Additive);
                openedByDoctor = true;
            }

            try
            {
                DayNightCycleManager dayNight = FindComponentInScene<DayNightCycleManager>(scene);
                bool hasDayNight = dayNight != null;
                report.Results.Add(BuildStep2Check(
                    "DAYNIGHT_EXISTS",
                    hasDayNight,
                    hasDayNight ? "DayNightCycleManager found." : "DayNightCycleManager missing.",
                    "Add DayNightCycleManager under __App/DayNightCycle."));

                bool dayNightRefs = dayNight != null
                                    && dayNight.SunLight != null
                                    && dayNight.EventHub != null
                                    && dayNight.DayLengthMinutes >= 0.1f;
                report.Results.Add(BuildStep2Check(
                    "DAYNIGHT_REFERENCES",
                    dayNightRefs,
                    dayNightRefs
                        ? "DayNightCycleManager references are assigned (sun/event hub/day length)."
                        : "DayNightCycleManager has missing references or invalid day length.",
                    "Assign sun light, event hub, and keep day length above 0.1."));

                SurvivalHUDController hud = FindComponentInScene<SurvivalHUDController>(scene);
                bool hasHud = hud != null;
                report.Results.Add(BuildStep2Check(
                    "SURVIVAL_HUD_EXISTS",
                    hasHud,
                    hasHud ? "SurvivalHUDController found." : "SurvivalHUDController missing.",
                    "Add SurvivalHUDController to UI Canvas."));

                bool hudBound = hud != null && hud.HasCoreHudBindings && hud.HasRuntimeReferences;
                report.Results.Add(BuildStep2Check(
                    "SURVIVAL_HUD_BINDINGS",
                    hudBound,
                    hudBound
                        ? "Survival HUD bindings are complete."
                        : "Survival HUD has missing bars/text or runtime references.",
                    "Assign HP/Stamina/Hunger/Thirst bars, time text, player stats, and day-night refs."));

                SpawnerZone[] spawners = FindComponentsInScene<SpawnerZone>(scene);
                bool spawnerNightScaling = spawners.Length > 0;
                for (int i = 0; i < spawners.Length; i++)
                {
                    SerializedObject so = new SerializedObject(spawners[i]);
                    SerializedProperty dayNightRef = so.FindProperty("dayNight");
                    SerializedProperty nightMultiplier = so.FindProperty("nightSpawnMultiplier");
                    bool valid = dayNightRef != null
                                 && dayNightRef.objectReferenceValue != null
                                 && nightMultiplier != null
                                 && nightMultiplier.floatValue > 1f;
                    spawnerNightScaling &= valid;
                }

                report.Results.Add(BuildStep2Check(
                    "NIGHT_SPAWN_SCALING",
                    spawnerNightScaling,
                    spawnerNightScaling
                        ? "Spawner night scaling is configured (day-night ref + multiplier > 1)."
                        : "Spawner night scaling is missing or invalid.",
                    "Assign day-night references and set night spawn multiplier above 1."));

                ZombieAI zombie = FindComponentInScene<ZombieAI>(scene);
                bool zombieNightAggroConfigured = false;
                if (zombie != null)
                {
                    SerializedObject zombieSo = new SerializedObject(zombie);
                    SerializedProperty sense = zombieSo.FindProperty("nightSenseMultiplier");
                    SerializedProperty move = zombieSo.FindProperty("nightMoveSpeedMultiplier");
                    SerializedProperty damage = zombieSo.FindProperty("nightDamageMultiplier");
                    zombieNightAggroConfigured = sense != null && sense.floatValue > 1f
                                                 && move != null && move.floatValue > 1f
                                                 && damage != null && damage.floatValue > 1f;
                }

                report.Results.Add(BuildStep2Check(
                    "NIGHT_ZOMBIE_AGGRO",
                    zombieNightAggroConfigured,
                    zombieNightAggroConfigured
                        ? "Zombie night aggression multipliers are configured above daytime baseline."
                        : "Zombie night aggression multipliers are missing or not above baseline.",
                    "Set zombie night move/sense/damage multipliers above 1."));
            }
            finally
            {
                if (openedByDoctor)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (previousActive.IsValid() && previousActive.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActive);
                }
            }
        }

        private static void CheckLootLoopSetup(DoctorReport report)
        {
            bool allItemsExist = true;
            for (int i = 0; i < ProjectDoctorConstants.RequiredItemDefinitionPaths.Length; i++)
            {
                string path = ProjectDoctorConstants.RequiredItemDefinitionPaths[i];
                bool exists = File.Exists(path);
                allItemsExist &= exists;
            }

            report.Results.Add(BuildStep2Check(
                "LOOT_ITEMS_EXIST",
                allItemsExist,
                allItemsExist
                    ? "Required ItemDefinition assets exist."
                    : "One or more required ItemDefinition assets are missing.",
                "Run Tools/LiteRealm/Scenes/Apply Step 4 (Looting Loop)."));

            bool allLootTablesExist = true;
            for (int i = 0; i < ProjectDoctorConstants.RequiredLootTablePaths.Length; i++)
            {
                string path = ProjectDoctorConstants.RequiredLootTablePaths[i];
                bool exists = File.Exists(path);
                allLootTablesExist &= exists;
            }

            report.Results.Add(BuildStep2Check(
                "LOOT_TABLES_EXIST",
                allLootTablesExist,
                allLootTablesExist
                    ? "Required LootTable assets exist."
                    : "One or more required LootTable assets are missing.",
                "Run Tools/LiteRealm/Scenes/Apply Step 4 (Looting Loop)."));

            bool containerPrefabExists = File.Exists(ProjectDoctorConstants.LootContainerPrefabPath);
            report.Results.Add(BuildStep2Check(
                "LOOT_CONTAINER_PREFAB_EXISTS",
                containerPrefabExists,
                containerPrefabExists
                    ? $"Loot container prefab exists: {ProjectDoctorConstants.LootContainerPrefabPath}"
                    : $"Loot container prefab missing: {ProjectDoctorConstants.LootContainerPrefabPath}",
                "Run Tools/LiteRealm/Scenes/Apply Step 4 (Looting Loop)."));

            bool containerHasTable = false;
            if (containerPrefabExists)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectDoctorConstants.LootContainerPrefabPath);
                if (prefab != null)
                {
                    LootContainer container = prefab.GetComponent<LootContainer>();
                    containerHasTable = container != null && container.LootTable != null;
                }
            }

            report.Results.Add(BuildStep2Check(
                "LOOT_CONTAINER_TABLE_ASSIGNED",
                containerHasTable,
                containerHasTable
                    ? "Loot container prefab has a LootTable assigned."
                    : "Loot container prefab is missing LootContainer or LootTable reference.",
                "Assign a LootTable on the LootContainer prefab."));
        }

        private static void CheckQuestAndPersistenceSetup(DoctorReport report)
        {
            bool questDatabaseExists = File.Exists(ProjectDoctorConstants.QuestDatabasePath);
            bool allQuestDefinitionsExist = true;
            for (int i = 0; i < ProjectDoctorConstants.RequiredQuestDefinitionPaths.Length; i++)
            {
                string path = ProjectDoctorConstants.RequiredQuestDefinitionPaths[i];
                bool exists = File.Exists(path);
                allQuestDefinitionsExist &= exists;
            }

            bool questAssetsExist = questDatabaseExists && allQuestDefinitionsExist;
            report.Results.Add(BuildStep2Check(
                "QUEST_ASSETS_EXIST",
                questAssetsExist,
                questAssetsExist
                    ? "QuestDefinition assets and QuestDatabase exist."
                    : "Quest assets missing (QuestDatabase and/or QuestDefinition assets).",
                "Run Tools/LiteRealm/Scenes/Apply Step 5 (Quests + Persistence)."));

            if (!File.Exists(ProjectDoctorConstants.MainScenePath))
            {
                report.Results.Add(BuildStep2Check(
                    "QUEST_NPC_ASSIGNED",
                    false,
                    "Main scene missing, cannot validate NPC quest assignment.",
                    "Build Main scene first."));

                report.Results.Add(BuildStep2Check(
                    "SAVE_CREATE_LOAD",
                    false,
                    "Main scene missing, cannot validate save/load roundtrip.",
                    "Build Main scene first."));
                return;
            }

            Scene previousActive = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(ProjectDoctorConstants.MainScenePath);
            bool openedByDoctor = false;

            if (!scene.IsValid() || !scene.isLoaded)
            {
                scene = EditorSceneManager.OpenScene(ProjectDoctorConstants.MainScenePath, OpenSceneMode.Additive);
                openedByDoctor = true;
            }

            try
            {
                NPCQuestGiver[] npcs = FindComponentsInScene<NPCQuestGiver>(scene);
                bool hasQuestAssignedNpc = false;

                for (int i = 0; i < npcs.Length; i++)
                {
                    NPCQuestGiver npc = npcs[i];
                    if (npc == null)
                    {
                        continue;
                    }

                    SerializedObject so = new SerializedObject(npc);
                    SerializedProperty questLine = so.FindProperty("questLine");
                    if (questLine == null || questLine.arraySize <= 0)
                    {
                        continue;
                    }

                    for (int j = 0; j < questLine.arraySize; j++)
                    {
                        SerializedProperty questRef = questLine.GetArrayElementAtIndex(j);
                        if (questRef != null && questRef.objectReferenceValue != null)
                        {
                            hasQuestAssignedNpc = true;
                            break;
                        }
                    }

                    if (hasQuestAssignedNpc)
                    {
                        break;
                    }
                }

                report.Results.Add(BuildStep2Check(
                    "QUEST_NPC_ASSIGNED",
                    hasQuestAssignedNpc,
                    hasQuestAssignedNpc
                        ? "At least one NPC quest giver has quests assigned."
                        : "No NPC quest giver with assigned quests was found.",
                    "Assign quest assets to NPCQuestGiver.questLine in Main scene."));

                SaveSystem saveSystem = FindComponentInScene<SaveSystem>(scene);
                bool saveRoundTripOk = TrySaveRoundTrip(saveSystem, out string saveMessage);
                report.Results.Add(BuildStep2Check(
                    "SAVE_CREATE_LOAD",
                    saveRoundTripOk,
                    saveMessage,
                    "Assign SaveSystem references and verify save/load dependencies."));
            }
            finally
            {
                if (openedByDoctor)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (previousActive.IsValid() && previousActive.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActive);
                }
            }
        }

        private static bool TrySaveRoundTrip(SaveSystem saveSystem, out string message)
        {
            if (saveSystem == null)
            {
                message = "SaveSystem missing in Main scene.";
                return false;
            }

            string savePath = saveSystem.SavePath;
            bool hadExistingSave = File.Exists(savePath);
            string previousContent = hadExistingSave ? File.ReadAllText(savePath) : null;
            bool success = false;
            string restoreError = string.Empty;

            try
            {
                bool saved = saveSystem.SaveGame();
                bool created = File.Exists(savePath);
                bool loaded = saved && created && saveSystem.LoadGame();
                success = saved && created && loaded;
                message = $"SaveGame={saved}, FileExists={created}, LoadGame={loaded}";
            }
            catch (Exception ex)
            {
                message = $"Save/load roundtrip threw exception: {ex.Message}";
            }
            finally
            {
                try
                {
                    if (hadExistingSave)
                    {
                        File.WriteAllText(savePath, previousContent ?? string.Empty);
                    }
                    else if (File.Exists(savePath))
                    {
                        File.Delete(savePath);
                    }
                }
                catch (Exception ex)
                {
                    restoreError = ex.Message;
                }
            }

            if (!string.IsNullOrWhiteSpace(restoreError))
            {
                message += $" | Save restore warning: {restoreError}";
            }

            return success;
        }

        /// <summary>
        /// If the report has failed loot-related checks, runs Step 4 (Looting Loop) to create missing assets.
        /// Returns true if Step 4 was run so the caller can re-run checks.
        /// </summary>
        public static bool TryAutoFixLootSetup(DoctorReport report)
        {
            if (report == null || report.Results == null)
            {
                return false;
            }

            bool needsStep4 = false;
            for (int i = 0; i < report.Results.Count; i++)
            {
                DoctorCheckResult r = report.Results[i];
                if (r == null || r.Passed)
                {
                    continue;
                }
                string code = r.Code ?? string.Empty;
                if (code == "LOOT_TABLES_EXIST" || code == "LOOT_ITEMS_EXIST" || code == "LOOT_CONTAINER_PREFAB_EXISTS")
                {
                    needsStep4 = true;
                    break;
                }
            }

            if (!needsStep4)
            {
                return false;
            }

            try
            {
                MainSceneStep4LootBuilder.ApplyStep4();
                AssetDatabase.Refresh();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ProjectDoctor] Auto-fix (Step 4) failed: {ex.Message}");
                return false;
            }
        }

        private static DoctorCheckResult BuildStep2Check(string code, bool passed, string message, string fixHint)
        {
            return new DoctorCheckResult
            {
                Code = code,
                Passed = passed,
                Severity = passed ? DoctorSeverity.Info : DoctorSeverity.Error,
                Message = message,
                FixHint = passed ? string.Empty : fixHint
            };
        }

        private static DoctorCheckResult BuildMainSceneComponentCheck(string code, bool passed, string componentLabel)
        {
            return new DoctorCheckResult
            {
                Code = code,
                Passed = passed,
                Severity = passed ? DoctorSeverity.Info : DoctorSeverity.Error,
                Message = passed
                    ? $"Main scene contains {componentLabel}."
                    : $"Main scene missing {componentLabel}.",
                FixHint = passed ? string.Empty : "Run Main Scene builder and verify scene objects."
            };
        }

        private static bool HasDirectionalRootLight(GameObject[] roots)
        {
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null || root.name != "Directional Light")
                {
                    continue;
                }

                Light light = root.GetComponent<Light>();
                if (light != null && light.type == LightType.Directional)
                {
                    return true;
                }
            }

            return false;
        }

        private static T FindRootWithComponent<T>(GameObject[] roots)
            where T : Component
        {
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                T component = root.GetComponent<T>();
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static T FindComponentInScene<T>(Scene scene)
            where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                T found = root.GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static T[] FindComponentsInScene<T>(Scene scene)
            where T : Component
        {
            List<T> list = new List<T>();
            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                T[] found = root.GetComponentsInChildren<T>(true);
                if (found == null || found.Length == 0)
                {
                    continue;
                }

                list.AddRange(found);
            }

            return list.ToArray();
        }

        private static bool HasAnyNavMeshData()
        {
            try
            {
                NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
                return triangulation.vertices != null && triangulation.vertices.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private static bool HasNavMeshSurfaceComponent(Scene scene)
        {
            System.Type navMeshSurfaceType = ResolveNavMeshSurfaceType();
            if (navMeshSurfaceType == null)
            {
                return false;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                Component[] found = root.GetComponentsInChildren(navMeshSurfaceType, true);
                if (found != null && found.Length > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static System.Type ResolveNavMeshSurfaceType()
        {
            System.Type type = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (type != null)
            {
                return type;
            }

            type = System.Type.GetType("UnityEngine.AI.NavMeshSurface, Unity.AI.Navigation");
            if (type != null)
            {
                return type;
            }

            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                System.Reflection.Assembly assembly = assemblies[i];
                type = assembly.GetType("Unity.AI.Navigation.NavMeshSurface");
                if (type != null)
                {
                    return type;
                }

                type = assembly.GetType("UnityEngine.AI.NavMeshSurface");
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static GameObject FindSceneObjectByName(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject found = FindByNameRecursive(roots[i].transform, objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject FindByNameRecursive(Transform transform, string objectName)
        {
            if (transform == null)
            {
                return null;
            }

            if (transform.name == objectName)
            {
                return transform.gameObject;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject found = FindByNameRecursive(transform.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject FindSceneObjectByTag(Scene scene, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag) || !HasTag(tag))
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject found = FindByTagRecursive(roots[i].transform, tag);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject FindByTagRecursive(Transform transform, string tag)
        {
            if (transform == null)
            {
                return null;
            }

            if (transform.CompareTag(tag))
            {
                return transform.gameObject;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject found = FindByTagRecursive(transform.GetChild(i), tag);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static int CountMissingScripts(Scene scene)
        {
            int missing = 0;
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
                            missing++;
                        }
                    }
                }
            }

            return missing;
        }

        private static bool IsFiniteVector(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }

        private static bool TryParseUnityVersion(string version, out int major, out int minor)
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

        private static bool AddTag(string tag)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0 || assets[0] == null)
            {
                return false;
            }

            SerializedObject serialized = new SerializedObject(assets[0]);
            SerializedProperty tags = serialized.FindProperty("tags");
            if (tags == null)
            {
                return false;
            }

            for (int i = 0; i < tags.arraySize; i++)
            {
                SerializedProperty item = tags.GetArrayElementAtIndex(i);
                if (item != null && item.stringValue == tag)
                {
                    return true;
                }
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            SerializedProperty newItem = tags.GetArrayElementAtIndex(tags.arraySize - 1);
            if (newItem == null)
            {
                return false;
            }

            newItem.stringValue = tag;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return true;
        }

        private static bool AddLayer(string layer)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0 || assets[0] == null)
            {
                return false;
            }

            SerializedObject serialized = new SerializedObject(assets[0]);
            SerializedProperty layers = serialized.FindProperty("layers");
            if (layers == null)
            {
                return false;
            }

            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty item = layers.GetArrayElementAtIndex(i);
                if (item == null)
                {
                    continue;
                }

                if (item.stringValue == layer)
                {
                    return true;
                }
            }

            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty item = layers.GetArrayElementAtIndex(i);
                if (item == null || !string.IsNullOrEmpty(item.stringValue))
                {
                    continue;
                }

                item.stringValue = layer;
                serialized.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                return true;
            }

            return false;
        }

        private static void StartNextInstall()
        {
            if (pendingPackageInstalls.Count == 0)
            {
                activeAddRequest = null;
                EditorApplication.update -= UpdateInstallRequest;
                installLog?.Invoke("Package installation queue complete.");
                Action finished = installFinished;
                installFinished = null;
                installLog = null;
                finished?.Invoke();
                return;
            }

            string packageId = pendingPackageInstalls.Dequeue();
            installLog?.Invoke($"Installing package: {packageId}");
            activeAddRequest = Client.Add(packageId);
        }

        private static void UpdateInstallRequest()
        {
            if (activeAddRequest == null)
            {
                StartNextInstall();
                return;
            }

            if (!activeAddRequest.IsCompleted)
            {
                return;
            }

            if (activeAddRequest.Status == StatusCode.Success)
            {
                installLog?.Invoke($"Installed: {activeAddRequest.Result?.name}");
            }
            else
            {
                string message = activeAddRequest.Error != null ? activeAddRequest.Error.message : "Unknown error";
                installLog?.Invoke($"Failed to install package: {message}");
            }

            activeAddRequest = null;
            StartNextInstall();
        }
    }
}
#endif
