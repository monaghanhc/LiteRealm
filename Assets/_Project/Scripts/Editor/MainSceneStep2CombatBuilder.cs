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
using UnityEngine.SceneManagement;

namespace LiteRealm.EditorTools
{
    public static class MainSceneStep2CombatBuilder
    {
        private const string ZombiePrefabPath = "Assets/_Project/Prefabs/Enemies/Zombie.prefab";
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

            ItemDefinition bossToken = GetOrCreateBossToken();
            GameObject projectilePrefab = GetOrCreateProjectilePrefab();
            GameObject zombiePrefab = GetOrCreateZombiePrefab();
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
            ConfigureBoss(scene, app.transform, world.transform, player, hub, dayNight, bossPrefab);

            GameObject canvasRoot = GetOrCreateRoot(scene, "UI Canvas");
            MainSceneStep3SurvivalBuilder.EnsureCanvasComponents(canvasRoot);
            MainSceneStep3SurvivalBuilder.EnsureReticle(canvasRoot);
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
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BloodImpactPrefabPath);
            if (prefab != null)
            {
                return prefab;
            }

            GameObject root = new GameObject("BloodImpact");
            ParticleSystem ps = root.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 4f;
            main.startSize = 0.12f;
            main.startColor = new Color(0.75f, 0.05f, 0.05f);
            main.gravityModifier = 0.6f;
            main.maxParticles = 24;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12, 18) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.15f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            prefab = PrefabUtility.SaveAsPrefabAsset(root, BloodImpactPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject GetOrCreateRiflePrefab(GameObject bloodImpactPrefab)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RiflePrefabPath);
            if (prefab != null)
            {
                if (bloodImpactPrefab != null)
                {
                    HitscanRifle rifleComponent = prefab.GetComponent<HitscanRifle>();
                    if (rifleComponent != null)
                    {
                        SerializedObject rifleSo = new SerializedObject(rifleComponent);
                        SetObject(rifleSo, "bloodImpactPrefab", bloodImpactPrefab);
                        rifleSo.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                return prefab;
            }

            GameObject root = new GameObject("Rifle");
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0f, 0.2f);
            body.transform.localScale = new Vector3(0.12f, 0.12f, 0.68f);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            Transform muzzle = new GameObject("MuzzlePoint").transform;
            muzzle.SetParent(root.transform, false);
            muzzle.localPosition = new Vector3(0f, 0f, 0.76f);

            ParticleSystem muzzleFx = muzzle.gameObject.AddComponent<ParticleSystem>();
            ConfigureBurst(muzzleFx, 0.08f, 0.05f, 0.2f, 9);

            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;

            HitscanRifle rifle = root.AddComponent<HitscanRifle>();
            SerializedObject so = new SerializedObject(rifle);
            SetString(so, "weaponId", "weapon.rifle.basic");
            SetString(so, "weaponDisplayName", "Ranger Rifle");
            SetFloat(so, "damage", 28f);
            SetFloat(so, "fireRate", 8.5f);
            SetFloat(so, "range", 140f);
            SetFloat(so, "spreadDegrees", 1.6f);
            SetInt(so, "magazineSize", 30);
            SetFloat(so, "reloadDuration", 1.5f);
            SetFloat(so, "recoilPitch", 1.15f);
            SetFloat(so, "recoilYaw", 0.32f);
            SetObject(so, "muzzlePoint", muzzle);
            SetObject(so, "muzzleFlash", muzzleFx);
            SetObject(so, "shootAudioSource", audioSource);
            if (bloodImpactPrefab != null)
            {
                SetObject(so, "bloodImpactPrefab", bloodImpactPrefab);
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            prefab = PrefabUtility.SaveAsPrefabAsset(root, RiflePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject GetOrCreateZombiePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);
            if (prefab != null)
            {
                return prefab;
            }

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "Zombie";
            SetEnemyLayerAndTag(root);
            UnityEngine.AI.NavMeshAgent agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 2.8f;
            agent.stoppingDistance = 1.4f;
            agent.radius = 0.45f;
            agent.height = 1.8f;

            HealthComponent health = root.AddComponent<HealthComponent>();
            SerializedObject healthSo = new SerializedObject(health);
            SetFloat(healthSo, "maxHealth", 90f);
            SetBool(healthSo, "destroyOnDeath", false);
            SetBool(healthSo, "disableGameObjectOnDeath", true);
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            AudioSource groanSource = root.AddComponent<AudioSource>();
            groanSource.playOnAwake = false;
            groanSource.spatialBlend = 1f;

            ZombieAI zombie = root.AddComponent<ZombieAI>();
            SerializedObject zombieSo = new SerializedObject(zombie);
            SetObject(zombieSo, "audioSource", groanSource);
            zombieSo.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<LootDropper>();
            prefab = PrefabUtility.SaveAsPrefabAsset(root, ZombiePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject GetOrCreateProjectilePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            if (prefab != null)
            {
                return prefab;
            }

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "BossProjectile";
            root.transform.localScale = Vector3.one * 0.45f;
            SphereCollider collider = root.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            root.AddComponent<BossProjectile>();
            prefab = PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject GetOrCreateBossPrefab(GameObject projectilePrefab, ItemDefinition bossToken)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
            if (prefab != null)
            {
                return prefab;
            }

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "Boss";
            root.transform.localScale = new Vector3(2f, 1.6f, 2f);
            SetEnemyLayerAndTag(root);

            UnityEngine.AI.NavMeshAgent agent = root.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 3.4f;
            agent.stoppingDistance = 2.1f;
            agent.radius = 0.8f;
            agent.height = 2.7f;

            HealthComponent health = root.AddComponent<HealthComponent>();
            SerializedObject healthSo = new SerializedObject(health);
            SetFloat(healthSo, "maxHealth", 650f);
            SetBool(healthSo, "destroyOnDeath", false);
            SetBool(healthSo, "disableGameObjectOnDeath", true);
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            Transform spitSpawn = new GameObject("SpitSpawnPoint").transform;
            spitSpawn.SetParent(root.transform, false);
            spitSpawn.localPosition = new Vector3(0f, 1.8f, 1.1f);

            BossAI boss = root.AddComponent<BossAI>();
            SerializedObject bossSo = new SerializedObject(boss);
            SetObject(bossSo, "spitProjectilePrefab", projectilePrefab != null ? projectilePrefab.GetComponent<BossProjectile>() : null);
            SetObject(bossSo, "spitSpawnPoint", spitSpawn);
            bossSo.ApplyModifiedPropertiesWithoutUndo();

            LootDropper dropper = root.AddComponent<LootDropper>();
            SerializedObject dropSo = new SerializedObject(dropper);
            SetInt(dropSo, "rollCount", 0);
            SerializedProperty guaranteed = dropSo.FindProperty("guaranteedDrops");
            if (guaranteed != null)
            {
                guaranteed.arraySize = 1;
                SerializedProperty entry = guaranteed.GetArrayElementAtIndex(0);
                SerializedProperty itemProp = entry.FindPropertyRelative("Item");
                SerializedProperty amountProp = entry.FindPropertyRelative("Amount");
                if (itemProp != null)
                {
                    itemProp.objectReferenceValue = bossToken;
                }

                if (amountProp != null)
                {
                    amountProp.intValue = 1;
                }
            }

            dropSo.ApplyModifiedPropertiesWithoutUndo();

            prefab = PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
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
            Terrain terrain = Object.FindObjectOfType<Terrain>();
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

        private static void ConfigureBoss(Scene scene, Transform app, Transform world, Transform player, GameEventHub hub, DayNightCycleManager dayNight, GameObject bossPrefab)
        {
            Terrain terrain = Object.FindObjectOfType<Terrain>();
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
