#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using LiteRealm.AI;
using LiteRealm.CameraSystem;
using LiteRealm.Combat;
using LiteRealm.Core;
using LiteRealm.Inventory;
using LiteRealm.Loot;
using LiteRealm.Player;
using LiteRealm.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace LiteRealm.EditorTools
{
    public static class MainSceneStep2CombatBuilder
    {
        private const string ZombiePrefabPath = "Assets/_Project/Prefabs/Enemies/Zombie.prefab";
        private const string CityBruteZombiePrefabPath = "Assets/_Project/Prefabs/Enemies/CityBruteZombie.prefab";
        private const string BossPrefabPath = "Assets/_Project/Prefabs/Enemies/Boss.prefab";
        private const string ProjectilePrefabPath = "Assets/_Project/Prefabs/Enemies/BossProjectile.prefab";
        private const string RiflePrefabPath = "Assets/_Project/Prefabs/Weapons/Rifle.prefab";
        private const string BloodImpactPrefabPath = "Assets/_Project/Prefabs/Effects/BloodImpact.prefab";
        private const string BossTokenPath = "Assets/_Project/ScriptableObjects/Items/Item_BossToken.asset";

        [MenuItem("Tools/LiteRealm/Scenes/Apply Step 2 (Combat + Enemies)")]
        public static void ApplyStep2()
        {
            ProjectDoctorRunner.EnsureRequiredFoldersExist();
            ProjectDoctorRunner.EnsureRequiredTagsAndLayers(out _);
            EnsureDirectory("Assets/_Project/Prefabs/Enemies");
            EnsureDirectory("Assets/_Project/Prefabs/Weapons");
            EnsureDirectory("Assets/_Project/Prefabs/Effects");
            EnsureDirectory("Assets/_Project/ScriptableObjects/Items");

            if (!File.Exists(ProjectDoctorConstants.MainScenePath))
            {
                MainSceneStep1Builder.BuildMainScene();
            }

            Scene scene = EditorSceneManager.OpenScene(ProjectDoctorConstants.MainScenePath, OpenSceneMode.Single);
            GameObject app = GetOrCreateRoot(scene, "__App");
            GameObject world = GetOrCreateRoot(scene, "World");

            GameEventHub hub = GetOrAdd<GameEventHub>(GetOrCreateChild(app.transform, "GameEventHub"));
            DayNightCycleManager dayNight = GetOrAdd<DayNightCycleManager>(GetOrCreateChild(app.transform, "DayNightCycle"));
            RuntimeNavMeshBootstrap navBootstrap = GetOrAdd<RuntimeNavMeshBootstrap>(GetOrCreateChild(app.transform, "RuntimeNavMesh"));
            SerializedObject navSo = new SerializedObject(navBootstrap);
            SetBool(navSo, "buildOnStart", true);
            SetBool(navSo, "skipBuildWhenNavMeshAlreadyPresent", true);
            navSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject daySo = new SerializedObject(dayNight);
            SetObject(daySo, "sunLight", FindDirectionalLight(scene));
            SetObject(daySo, "eventHub", hub);
            daySo.ApplyModifiedPropertiesWithoutUndo();
            ApplyExistingWorldVisuals(scene, world.transform);

            ItemDefinition bossToken = GetOrCreateBossToken();
            GameObject projectilePrefab = GetOrCreateProjectilePrefab();
            GameObject zombiePrefab = GetOrCreateZombiePrefab();
            GameObject cityBrutePrefab = GetOrCreateCityBruteZombiePrefab(zombiePrefab);
            GameObject bossPrefab = GetOrCreateBossPrefab(projectilePrefab, bossToken);
            GameObject bloodPrefab = GetOrCreateBloodImpactPrefab();
            GameObject riflePrefab = GetOrCreateRiflePrefab(bloodPrefab);

            Transform player = FindPlayer(scene);
            if (player == null)
            {
                Debug.LogError("Step 2 builder: missing Player in Main scene.");
                return;
            }

            ConfigurePlayer(scene, player, hub, riflePrefab);
            ConfigureSpawner(scene, world.transform, player, hub, dayNight, zombiePrefab);
            ConfigureEnemyPatrols(scene, world.transform, player, hub, dayNight, navBootstrap, zombiePrefab);
            ConfigureCityDangerZone(scene, world.transform, player, hub, dayNight, navBootstrap, cityBrutePrefab, bossPrefab);
            ConfigureBoss(scene, app.transform, world.transform, player, hub, dayNight, bossPrefab);

            GameObject canvasRoot = GetOrCreateRoot(scene, "UI Canvas");
            MainSceneStep3SurvivalBuilder.EnsureCanvasComponents(canvasRoot);
            MainSceneStep3SurvivalBuilder.EnsureReticle(canvasRoot);
            MainSceneStep3SurvivalBuilder.EnsurePauseMenu(canvasRoot);
            MainSceneStep3SurvivalBuilder.EnsureGameOverPanel(canvasRoot, scene);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ProjectDoctorConstants.MainScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Step 2 combat setup applied.");
        }

        [InitializeOnLoadMethod]
        private static void AutoApplyIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                Scene active = SceneManager.GetActiveScene();
                if (active.IsValid() && active.isDirty)
                {
                    return;
                }

                bool hasPrefabs = File.Exists(RiflePrefabPath)
                                  && File.Exists(ZombiePrefabPath)
                                  && File.Exists(CityBruteZombiePrefabPath)
                                  && File.Exists(BossPrefabPath)
                                  && File.Exists(ProjectilePrefabPath);

                if (hasPrefabs && File.Exists(ProjectDoctorConstants.MainScenePath))
                {
                    return;
                }

                ApplyStep2();
            };
        }

        private static GameObject GetOrCreateBloodImpactPrefab()
        {
            return ProceduralArtKit.UpgradeBloodImpactPrefab(BloodImpactPrefabPath);
        }

        private static GameObject GetOrCreateRiflePrefab(GameObject bloodImpactPrefab)
        {
            return ProceduralArtKit.UpgradeRiflePrefab(RiflePrefabPath, bloodImpactPrefab);
        }

        private static GameObject GetOrCreateZombiePrefab()
        {
            return ProceduralArtKit.UpgradeZombiePrefab(ZombiePrefabPath);
        }

        private static GameObject GetOrCreateCityBruteZombiePrefab(GameObject baseZombiePrefab)
        {
            if (baseZombiePrefab == null)
            {
                return null;
            }

            EnsureDirectory(Path.GetDirectoryName(CityBruteZombiePrefabPath));
            bool existed = File.Exists(CityBruteZombiePrefabPath);
            bool loadedPrefabContents = existed;
            GameObject root = existed
                ? PrefabUtility.LoadPrefabContents(CityBruteZombiePrefabPath)
                : Object.Instantiate(baseZombiePrefab);

            if (root == null)
            {
                return null;
            }

            root.name = "CityBruteZombie";
            root.transform.localScale = new Vector3(1.16f, 1.14f, 1.16f);
            SetEnemyLayerAndTag(root);

            HealthComponent health = GetOrAdd<HealthComponent>(root);
            SerializedObject healthSo = new SerializedObject(health);
            SetFloat(healthSo, "maxHealth", 185f);
            SetBool(healthSo, "destroyOnDeath", false);
            SetBool(healthSo, "disableGameObjectOnDeath", false);
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            ZombieAI zombie = GetOrAdd<ZombieAI>(root);
            SerializedObject zombieSo = new SerializedObject(zombie);
            SetString(zombieSo, "enemyId", "zombie.city.brute");
            SetFloat(zombieSo, "baseMoveSpeed", 3.15f);
            SetFloat(zombieSo, "sightRange", 38f);
            SetFloat(zombieSo, "hearingRange", 24f);
            SetFloat(zombieSo, "attackDamage", 18f);
            SetFloat(zombieSo, "attackCooldown", 0.95f);
            SetFloat(zombieSo, "nightMoveSpeedMultiplier", 1.35f);
            SetFloat(zombieSo, "nightSenseMultiplier", 1.5f);
            SetFloat(zombieSo, "nightDamageMultiplier", 1.32f);
            zombieSo.ApplyModifiedPropertiesWithoutUndo();

            NavMeshAgent agent = GetOrAdd<NavMeshAgent>(root);
            agent.speed = 3.15f;
            agent.acceleration = 10f;
            agent.angularSpeed = 240f;
            agent.stoppingDistance = 1.55f;
            agent.radius = 0.48f;

            PrefabUtility.SaveAsPrefabAsset(root, CityBruteZombiePrefabPath);
            if (loadedPrefabContents)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(CityBruteZombiePrefabPath);
        }

        private static GameObject GetOrCreateProjectilePrefab()
        {
            return ProceduralArtKit.UpgradeProjectilePrefab(ProjectilePrefabPath);
        }

        private static GameObject GetOrCreateBossPrefab(GameObject projectilePrefab, ItemDefinition bossToken)
        {
            return ProceduralArtKit.UpgradeBossPrefab(BossPrefabPath, projectilePrefab, bossToken);
        }

        private static void ApplyExistingWorldVisuals(Scene scene, Transform world)
        {
            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain != null && terrain.terrainData != null)
            {
                ProceduralArtKit.ApplyTerrainSurface(terrain.terrainData);
                terrain.drawInstanced = true;
                terrain.treeDistance = 360f;
                terrain.basemapDistance = 1000f;

                GameObject pine = ProceduralArtKit.EnsureTreePrefab(
                    "Assets/_Project/Prefabs/Environment/TreePrototype_A.prefab",
                    ProceduralTreeStyle.Pine);
                GameObject broadleaf = ProceduralArtKit.EnsureTreePrefab(
                    "Assets/_Project/Prefabs/Environment/TreePrototype_B.prefab",
                    ProceduralTreeStyle.Broadleaf);
                terrain.terrainData.treePrototypes = new[]
                {
                    new TreePrototype { prefab = pine, bendFactor = 0.22f },
                    new TreePrototype { prefab = broadleaf, bendFactor = 0.18f }
                };
                EditorUtility.SetDirty(terrain);
                EditorUtility.SetDirty(terrain.terrainData);
            }

            Light sun = FindDirectionalLight(scene);
            if (sun != null)
            {
                ProceduralArtKit.ApplySkyRenderSettings(sun);
                EditorUtility.SetDirty(sun);
            }

            if (world != null)
            {
                ProceduralArtKit.EnsureCloudBank(world);
                Transform water = world.Find("WaterPlane");
                if (water != null)
                {
                    Renderer renderer = water.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.sharedMaterial = ProceduralArtKit.EnsureStandardMaterial(
                            ProceduralArtKit.WaterMaterialPath,
                            new Color(0.17f, 0.42f, 0.62f, 0.62f),
                            0.72f,
                            0f,
                            false,
                            default,
                            true);
                    }
                }
            }

            MainSceneStep1Builder.ApplyExpandedWorldLayout(scene);
        }

        private static ItemDefinition GetOrCreateBossToken()
        {
            ItemDefinition item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(BossTokenPath);
            if (item != null)
            {
                return item;
            }

            item = ScriptableObject.CreateInstance<ItemDefinition>();
            SerializedObject so = new SerializedObject(item);
            SetString(so, "itemId", "item.special.boss_token");
            SetString(so, "displayName", "Mutant Core Token");
            SetString(so, "description", "A volatile token dropped by the Alpha boss.");
            SetInt(so, "rarity", (int)ItemRarity.Epic);
            SetInt(so, "maxStack", 5);
            SetBool(so, "consumable", false);
            SetBool(so, "questItem", true);
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(item, BossTokenPath);
            return item;
        }

        private static void ConfigurePlayer(Scene scene, Transform player, GameEventHub hub, GameObject riflePrefab)
        {
            GetOrAdd<PlayerStats>(player.gameObject);
            GetOrAdd<PlayerDamageAudioController>(player.gameObject);
            ExplorationInput input = GetOrAdd<ExplorationInput>(player.gameObject);
            WeaponManager manager = GetOrAdd<WeaponManager>(player.gameObject);

            Transform rig = player.Find("WeaponRig");
            if (rig == null)
            {
                GameObject rigGo = new GameObject("WeaponRig");
                rigGo.transform.SetParent(player, false);
                rigGo.transform.localPosition = new Vector3(0.35f, 1.25f, 0.45f);
                rig = rigGo.transform;
            }

            WeaponBase weapon = rig.GetComponentInChildren<WeaponBase>(true);
            if (weapon == null && riflePrefab != null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(riflePrefab) as GameObject;
                if (instance != null)
                {
                    instance.transform.SetParent(rig, false);
                    weapon = instance.GetComponent<WeaponBase>();
                }
            }

            Camera cam = FindMainCamera(scene);
            PlayerCameraController cameraController = cam != null ? cam.GetComponent<PlayerCameraController>() : null;

            SerializedObject so = new SerializedObject(manager);
            SerializedProperty weapons = so.FindProperty("weapons");
            if (weapons != null)
            {
                weapons.arraySize = weapon != null ? 1 : 0;
                if (weapon != null)
                {
                    weapons.GetArrayElementAtIndex(0).objectReferenceValue = weapon;
                }
            }

            SetInt(so, "startingWeaponIndex", 0);
            SetObject(so, "aimCamera", cam);
            SetObject(so, "cameraController", cameraController);
            SetObject(so, "eventHub", hub);
            SetObject(so, "explorationInput", input);

            int mask = ~0;
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                mask &= ~(1 << playerLayer);
            }

            SetInt(so, "hitMask", mask);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSpawner(Scene scene, Transform world, Transform player, GameEventHub hub, DayNightCycleManager dayNight, GameObject zombiePrefab)
        {
            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            GameObject spawnerGo = GetOrCreateChild(world, "ZombieSpawner_01");
            SpawnerZone spawner = GetOrAdd<SpawnerZone>(spawnerGo);

            Vector3[] points =
            {
                new Vector3(125f, 0f, 140f),
                new Vector3(200f, 0f, 230f),
                new Vector3(290f, 0f, 265f),
                new Vector3(370f, 0f, 315f)
            };

            List<Transform> spawnPoints = new List<Transform>();
            for (int i = 0; i < points.Length; i++)
            {
                GameObject point = GetOrCreateChild(spawnerGo.transform, $"SpawnPoint_{i + 1:00}");
                point.transform.position = ToSurface(terrain, points[i], 0.2f);
                GetOrAdd<WorldSpawnPoint>(point);
                spawnPoints.Add(point.transform);
            }

            SerializedObject so = new SerializedObject(spawner);
            SetObject(so, "zombiePrefab", zombiePrefab != null ? zombiePrefab.GetComponent<ZombieAI>() : null);
            SerializedProperty pointArray = so.FindProperty("spawnPoints");
            if (pointArray != null)
            {
                pointArray.arraySize = spawnPoints.Count;
                for (int i = 0; i < spawnPoints.Count; i++)
                {
                    pointArray.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
                }
            }

            SetInt(so, "maxAliveDay", 6);
            SetInt(so, "maxAliveNight", 12);
            SetFloat(so, "respawnInterval", 8f);
            SetInt(so, "initialSpawnCount", 1);
            SetObject(so, "target", player);
            SetObject(so, "eventHub", hub);
            SetObject(so, "dayNight", dayNight);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureEnemyPatrols(Scene scene, Transform world, Transform player, GameEventHub hub, DayNightCycleManager dayNight, RuntimeNavMeshBootstrap navBootstrap, GameObject zombiePrefab)
        {
            if (zombiePrefab == null)
            {
                return;
            }

            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            GameObject patrolRoot = GetOrCreateChild(world, "EnemyPatrols");
            patrolRoot.SetActive(true);

            for (int i = patrolRoot.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(patrolRoot.transform.GetChild(i).gameObject);
            }

            Vector3[] positions =
            {
                new Vector3(150f, 0f, 165f),
                new Vector3(232f, 0f, 247f),
                new Vector3(318f, 0f, 290f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject patrol = PrefabUtility.InstantiatePrefab(zombiePrefab, scene) as GameObject;
                if (patrol == null)
                {
                    patrol = Object.Instantiate(zombiePrefab);
                    SceneManager.MoveGameObjectToScene(patrol, scene);
                }

                patrol.name = $"ZombiePatrol_{i + 1:00}";
                patrol.transform.SetParent(patrolRoot.transform, true);
                patrol.transform.position = ToSurface(terrain, positions[i], 0.05f);
                patrol.transform.rotation = Quaternion.Euler(0f, 40f + (i * 115f), 0f);
                patrol.SetActive(true);
                SetEnemyLayerAndTag(patrol);

                ZombieAI zombie = patrol.GetComponent<ZombieAI>();
                if (zombie != null)
                {
                    SerializedObject zombieSo = new SerializedObject(zombie);
                    SetObject(zombieSo, "target", player);
                    SetObject(zombieSo, "eventHub", hub);
                    SetObject(zombieSo, "dayNight", dayNight);
                    zombieSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            patrolRoot.SetActive(false);

            if (navBootstrap != null)
            {
                SerializedObject navSo = new SerializedObject(navBootstrap);
                SerializedProperty activateAfterBuild = navSo.FindProperty("activateAfterBuild");
                if (activateAfterBuild != null)
                {
                    activateAfterBuild.arraySize = 1;
                    activateAfterBuild.GetArrayElementAtIndex(0).objectReferenceValue = patrolRoot;
                }

                navSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ConfigureCityDangerZone(
            Scene scene,
            Transform world,
            Transform player,
            GameEventHub hub,
            DayNightCycleManager dayNight,
            RuntimeNavMeshBootstrap navBootstrap,
            GameObject cityBrutePrefab,
            GameObject bossPrefab)
        {
            Terrain terrain = Object.FindFirstObjectByType<Terrain>();

            if (cityBrutePrefab != null)
            {
                GameObject spawnerGo = GetOrCreateChild(world, "ZombieSpawner_CityDistrict");
                SpawnerZone spawner = GetOrAdd<SpawnerZone>(spawnerGo);
                Vector3[] points =
                {
                    new Vector3(404f, 0f, 98f),
                    new Vector3(450f, 0f, 106f),
                    new Vector3(520f, 0f, 104f),
                    new Vector3(562f, 0f, 142f),
                    new Vector3(540f, 0f, 202f),
                    new Vector3(486f, 0f, 226f),
                    new Vector3(428f, 0f, 222f),
                    new Vector3(396f, 0f, 178f),
                    new Vector3(500f, 0f, 166f),
                    new Vector3(458f, 0f, 176f)
                };

                List<Transform> spawnPoints = new List<Transform>();
                for (int i = 0; i < points.Length; i++)
                {
                    GameObject point = GetOrCreateChild(spawnerGo.transform, $"CitySpawnPoint_{i + 1:00}");
                    point.transform.position = ToSurface(terrain, points[i], 0.25f);
                    WorldSpawnPoint spawnPoint = GetOrAdd<WorldSpawnPoint>(point);
                    SerializedObject spawnSo = new SerializedObject(spawnPoint);
                    SetInt(spawnSo, "kind", (int)SpawnPointKind.Zombie);
                    spawnSo.ApplyModifiedPropertiesWithoutUndo();
                    spawnPoints.Add(point.transform);
                }

                SerializedObject so = new SerializedObject(spawner);
                SetObject(so, "zombiePrefab", cityBrutePrefab.GetComponent<ZombieAI>());
                SerializedProperty pointArray = so.FindProperty("spawnPoints");
                if (pointArray != null)
                {
                    pointArray.arraySize = spawnPoints.Count;
                    for (int i = 0; i < spawnPoints.Count; i++)
                    {
                        pointArray.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
                    }
                }

                SetInt(so, "maxAliveDay", 16);
                SetInt(so, "maxAliveNight", 30);
                SetFloat(so, "nightSpawnMultiplier", 2.0f);
                SetFloat(so, "respawnInterval", 3.25f);
                SetFloat(so, "respawnIntervalJitter", 0.8f);
                SetInt(so, "initialSpawnCount", 10);
                SetFloat(so, "minSpawnDistanceFromTarget", 28f);
                SetFloat(so, "spawnPositionJitter", 5.5f);
                SetInt(so, "nightInitialSpawnBonus", 8);
                SetObject(so, "target", player);
                SetObject(so, "eventHub", hub);
                SetObject(so, "dayNight", dayNight);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject cityBossRoot = GetOrCreateChild(world, "CityBosses");
            for (int i = cityBossRoot.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(cityBossRoot.transform.GetChild(i).gameObject);
            }

            if (bossPrefab != null)
            {
                Vector3[] bossPositions =
                {
                    new Vector3(516f, 0f, 166f),
                    new Vector3(448f, 0f, 214f)
                };

                for (int i = 0; i < bossPositions.Length; i++)
                {
                    GameObject boss = PrefabUtility.InstantiatePrefab(bossPrefab, scene) as GameObject;
                    if (boss == null)
                    {
                        boss = Object.Instantiate(bossPrefab);
                        SceneManager.MoveGameObjectToScene(boss, scene);
                    }

                    boss.name = $"CityBoss_{i + 1:00}";
                    boss.transform.SetParent(cityBossRoot.transform, true);
                    boss.transform.position = ToSurface(terrain, bossPositions[i], 0.1f);
                    boss.transform.rotation = Quaternion.Euler(0f, i == 0 ? 225f : 70f, 0f);
                    boss.transform.localScale = Vector3.one * 0.92f;
                    SetEnemyLayerAndTag(boss);

                    HealthComponent health = boss.GetComponent<HealthComponent>();
                    if (health != null)
                    {
                        SerializedObject healthSo = new SerializedObject(health);
                        SetFloat(healthSo, "maxHealth", 520f + i * 95f);
                        SetBool(healthSo, "destroyOnDeath", false);
                        SetBool(healthSo, "disableGameObjectOnDeath", false);
                        healthSo.ApplyModifiedPropertiesWithoutUndo();
                    }

                    BossAI bossAi = boss.GetComponent<BossAI>();
                    if (bossAi != null)
                    {
                        SerializedObject bossSo = new SerializedObject(bossAi);
                        SetString(bossSo, "bossId", $"boss.city.{i + 1:00}");
                        SetFloat(bossSo, "moveSpeed", 3.35f + i * 0.15f);
                        SetFloat(bossSo, "chaseRange", 54f);
                        SetFloat(bossSo, "meleeDamage", 24f + i * 4f);
                        SetFloat(bossSo, "spitDamage", 28f + i * 5f);
                        SetFloat(bossSo, "spitRange", 27f);
                        SetObject(bossSo, "target", player);
                        SetObject(bossSo, "eventHub", hub);
                        SetObject(bossSo, "dayNight", dayNight);
                        bossSo.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            cityBossRoot.SetActive(false);
            SetRuntimeNavMeshActivations(navBootstrap, world);
        }

        private static void ConfigureBoss(Scene scene, Transform app, Transform world, Transform player, GameEventHub hub, DayNightCycleManager dayNight, GameObject bossPrefab)
        {
            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            GameObject bossSpawn = GetOrCreateChild(world, "BossSpawnPoint");
            bossSpawn.transform.position = ToSurface(terrain, new Vector3(500f, 0f, 500f), 0.2f);
            WorldSpawnPoint worldSpawn = GetOrAdd<WorldSpawnPoint>(bossSpawn);
            SerializedObject spawnSo = new SerializedObject(worldSpawn);
            SetInt(spawnSo, "kind", (int)SpawnPointKind.Boss);
            spawnSo.ApplyModifiedPropertiesWithoutUndo();

            BossSpawnManager manager = GetOrAdd<BossSpawnManager>(GetOrCreateChild(app, "BossSpawnManager"));
            SerializedObject so = new SerializedObject(manager);
            SetObject(so, "bossPrefab", bossPrefab != null ? bossPrefab.GetComponent<BossAI>() : null);
            SetObject(so, "spawnPoint", bossSpawn.transform);
            SetBool(so, "spawnAtNightOnly", true);
            SetObject(so, "target", player);
            SetObject(so, "eventHub", hub);
            SetObject(so, "dayNight", dayNight);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureBurst(ParticleSystem ps, float duration, float lifetime, float size, int burstCount)
        {
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.duration = duration;
            main.startLifetime = lifetime;
            main.startSpeed = 0.2f;
            main.startSize = size;
            main.startColor = new Color(1f, 0.78f, 0.35f, 0.9f);

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.02f;
        }

        private static Transform FindPlayer(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == "Player" || transforms[j].CompareTag("Player"))
                    {
                        return transforms[j];
                    }
                }
            }

            return null;
        }

        private static Camera FindMainCamera(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Camera cam = roots[i].GetComponentInChildren<Camera>(true);
                if (cam != null && (cam.name == "Main Camera" || cam.CompareTag("MainCamera")))
                {
                    return cam;
                }
            }

            return Camera.main;
        }

        private static Light FindDirectionalLight(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Light light = roots[i].GetComponent<Light>();
                if (light != null && light.type == LightType.Directional)
                {
                    return light;
                }
            }

            return null;
        }

        private static GameObject GetOrCreateRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == name)
                {
                    return roots[i];
                }
            }

            return new GameObject(name);
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        private static void SetEnemyLayerAndTag(GameObject go)
        {
            if (ProjectDoctorRunner.HasTag("Enemy"))
            {
                go.tag = "Enemy";
            }

            int layer = LayerMask.NameToLayer("Enemy");
            if (layer >= 0)
            {
                go.layer = layer;
            }
        }

        private static Vector3 ToSurface(Terrain terrain, Vector3 point, float offset)
        {
            if (terrain == null)
            {
                point.y += offset;
                return point;
            }

            point.y = terrain.SampleHeight(point) + terrain.transform.position.y + offset;
            return point;
        }

        private static void SetRuntimeNavMeshActivations(RuntimeNavMeshBootstrap navBootstrap, Transform world)
        {
            if (navBootstrap == null || world == null)
            {
                return;
            }

            List<GameObject> targets = new List<GameObject>();
            AddActivationTarget(targets, world.Find("EnemyPatrols"));
            AddActivationTarget(targets, world.Find("CityBosses"));

            SerializedObject navSo = new SerializedObject(navBootstrap);
            SerializedProperty activateAfterBuild = navSo.FindProperty("activateAfterBuild");
            if (activateAfterBuild != null)
            {
                activateAfterBuild.arraySize = targets.Count;
                for (int i = 0; i < targets.Count; i++)
                {
                    activateAfterBuild.GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
                }
            }

            navSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddActivationTarget(List<GameObject> targets, Transform target)
        {
            if (target == null || targets.Contains(target.gameObject))
            {
                return;
            }

            targets.Add(target.gameObject);
        }

        private static void SetString(SerializedObject so, string propertyName, string value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetFloat(SerializedObject so, string propertyName, float value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetInt(SerializedObject so, string propertyName, int value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetBool(SerializedObject so, string propertyName, bool value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetObject(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void EnsureDirectory(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
#endif
