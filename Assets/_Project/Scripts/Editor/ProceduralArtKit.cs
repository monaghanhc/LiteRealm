#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using LiteRealm.AI;
using LiteRealm.Combat;
using LiteRealm.Core;
using LiteRealm.Inventory;
using LiteRealm.Loot;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace LiteRealm.EditorTools
{
    public enum ProceduralTreeStyle
    {
        Pine,
        Broadleaf
    }

    public static class ProceduralArtKit
    {
        private const string MaterialFolder = "Assets/_Project/Materials";
        private const string TextureFolder = "Assets/_Project/Materials/GeneratedTextures";
        private const string MeshFolder = "Assets/_Project/Meshes";

        private const string GrassTexturePath = TextureFolder + "/Ground_Grass_Noise.asset";
        private const string DirtTexturePath = TextureFolder + "/Ground_Dirt_Noise.asset";
        private const string RockTexturePath = TextureFolder + "/Ground_Rock_Noise.asset";
        private const string GrassBladeTexturePath = TextureFolder + "/Grass_Blade_Alpha.asset";

        private const string GrassLayerPath = MaterialFolder + "/TerrainLayer_ForestGrass.terrainlayer";
        private const string DirtLayerPath = MaterialFolder + "/TerrainLayer_DampDirt.terrainlayer";
        private const string RockLayerPath = MaterialFolder + "/TerrainLayer_GraniteRock.terrainlayer";

        public const string BarkMaterialPath = MaterialFolder + "/M_Bark_Ridged.mat";
        public const string LeafDarkMaterialPath = MaterialFolder + "/M_Pine_DarkNeedles.mat";
        public const string LeafLightMaterialPath = MaterialFolder + "/M_Leaves_Summer.mat";
        public const string RockMaterialPath = MaterialFolder + "/M_Rock_Granite.mat";
        public const string WaterMaterialPath = MaterialFolder + "/M_Water_Lake.mat";
        public const string RifleMetalMaterialPath = MaterialFolder + "/M_Rifle_Gunmetal.mat";
        public const string RiflePolymerMaterialPath = MaterialFolder + "/M_Rifle_BlackPolymer.mat";
        public const string RifleBrassMaterialPath = MaterialFolder + "/M_Rifle_Brass.mat";
        public const string ZombieSkinMaterialPath = MaterialFolder + "/M_Zombie_SallowSkin.mat";
        public const string ZombieClothMaterialPath = MaterialFolder + "/M_Zombie_TornCloth.mat";
        public const string ZombieBloodMaterialPath = MaterialFolder + "/M_Zombie_DriedBlood.mat";
        public const string BossSkinMaterialPath = MaterialFolder + "/M_Boss_MutatedHide.mat";
        public const string BossGlowMaterialPath = MaterialFolder + "/M_Boss_GlowingCore.mat";
        public const string ProjectileMaterialPath = MaterialFolder + "/M_Boss_AcidProjectile.mat";
        public const string CloudMaterialPath = MaterialFolder + "/M_Sky_SoftCloud.mat";
        public const string SkyboxMaterialPath = MaterialFolder + "/M_Sky_ProceduralMorning.mat";

        [MenuItem("Tools/LiteRealm/Scenes/Apply Visual Upgrade")]
        public static void ApplyVisualUpgradeMenu()
        {
            MainSceneStep2CombatBuilder.ApplyStep2();
        }

        public static TerrainLayer[] EnsureTerrainLayers()
        {
            EnsureDirectory(MaterialFolder);
            EnsureDirectory(TextureFolder);

            Texture2D grass = EnsureNoiseTexture(
                GrassTexturePath,
                new Color(0.10f, 0.21f, 0.09f),
                new Color(0.30f, 0.46f, 0.20f),
                128,
                11,
                0.075f);
            Texture2D dirt = EnsureNoiseTexture(
                DirtTexturePath,
                new Color(0.19f, 0.14f, 0.095f),
                new Color(0.42f, 0.32f, 0.21f),
                128,
                29,
                0.09f);
            Texture2D rock = EnsureNoiseTexture(
                RockTexturePath,
                new Color(0.24f, 0.25f, 0.24f),
                new Color(0.58f, 0.58f, 0.54f),
                128,
                47,
                0.11f);

            return new[]
            {
                EnsureTerrainLayer(GrassLayerPath, grass, new Vector2(10f, 10f), 0.08f),
                EnsureTerrainLayer(DirtLayerPath, dirt, new Vector2(8f, 8f), 0.15f),
                EnsureTerrainLayer(RockLayerPath, rock, new Vector2(13f, 13f), 0.28f)
            };
        }

        public static void ApplyTerrainSurface(TerrainData data)
        {
            if (data == null)
            {
                return;
            }

            data.terrainLayers = EnsureTerrainLayers();
            ApplyTerrainAlphamaps(data);
            ApplyTerrainDetails(data);
            data.wavingGrassStrength = 0.32f;
            data.wavingGrassAmount = 0.42f;
            data.wavingGrassSpeed = 0.36f;
            data.wavingGrassTint = new Color(0.62f, 0.72f, 0.46f, 1f);
            EditorUtility.SetDirty(data);
        }

        public static GameObject EnsureTreePrefab(string path, ProceduralTreeStyle style)
        {
            EnsureDirectory(Path.GetDirectoryName(path));

            bool existed = File.Exists(path);
            GameObject root = existed
                ? PrefabUtility.LoadPrefabContents(path)
                : new GameObject(Path.GetFileNameWithoutExtension(path));

            root.name = Path.GetFileNameWithoutExtension(path);
            ClearChildren(root.transform);

            switch (style)
            {
                case ProceduralTreeStyle.Broadleaf:
                    BuildBroadleaf(root.transform);
                    break;
                default:
                    BuildPine(root.transform);
                    break;
            }

            BakeTreeChildrenIntoRootMesh(root, TreeMeshPath(path));

            if (existed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        public static Material EnsureSkyboxMaterial()
        {
            EnsureDirectory(MaterialFolder);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
            Shader shader = Shader.Find("Skybox/Procedural");
            if (material == null)
            {
                material = new Material(shader != null ? shader : Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, SkyboxMaterialPath);
            }

            if (shader != null)
            {
                material.shader = shader;
                SetFloat(material, "_SunDisk", 2f);
                SetFloat(material, "_SunSize", 0.032f);
                SetFloat(material, "_SunSizeConvergence", 5.5f);
                SetFloat(material, "_AtmosphereThickness", 1.28f);
                SetColor(material, "_SkyTint", new Color(0.48f, 0.65f, 0.90f));
                SetColor(material, "_GroundColor", new Color(0.31f, 0.34f, 0.36f));
                SetFloat(material, "_Exposure", 1.12f);
            }
            else
            {
                material.color = new Color(0.48f, 0.65f, 0.90f);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        public static void ApplySkyRenderSettings(Light sunLight)
        {
            RenderSettings.skybox = EnsureSkyboxMaterial();
            RenderSettings.sun = sunLight;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.72f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.43f, 0.39f);
            RenderSettings.ambientGroundColor = new Color(0.10f, 0.095f, 0.085f);
            RenderSettings.ambientIntensity = 0.95f;
            RenderSettings.reflectionIntensity = 0.68f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.50f, 0.60f, 0.67f);
            RenderSettings.fogDensity = 0.0021f;
        }

        public static void EnsureCloudBank(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            Transform existing = parent.Find("Sky_CloudBank");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            Transform root = new GameObject("Sky_CloudBank").transform;
            root.SetParent(parent);
            Material cloud = EnsureStandardMaterial(
                CloudMaterialPath,
                new Color(1f, 1f, 1f, 0.44f),
                0.1f,
                0f,
                false,
                default,
                true);

            Random.State state = Random.state;
            Random.InitState(9441);
            Vector3[] centers =
            {
                new Vector3(150f, 92f, 220f),
                new Vector3(285f, 104f, 140f),
                new Vector3(435f, 96f, 330f),
                new Vector3(520f, 112f, 150f),
                new Vector3(210f, 118f, 470f)
            };

            for (int i = 0; i < centers.Length; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    puff.name = $"CloudPuff_{i:00}_{j:00}";
                    puff.transform.SetParent(root);
                    puff.transform.position = centers[i] + new Vector3((j - 1.5f) * 14f, Random.Range(-3f, 3f), Random.Range(-6f, 6f));
                    puff.transform.localScale = new Vector3(Random.Range(18f, 30f), Random.Range(3f, 7f), Random.Range(9f, 18f));
                    puff.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    Object.DestroyImmediate(puff.GetComponent<Collider>());
                    puff.GetComponent<Renderer>().sharedMaterial = cloud;
                }
            }

            Random.state = state;
        }

        public static GameObject UpgradeBloodImpactPrefab(string path)
        {
            EnsureDirectory(Path.GetDirectoryName(path));
            bool existed = File.Exists(path);
            GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject("BloodImpact");
            root.name = "BloodImpact";
            ClearChildren(root.transform);

            ParticleSystem ps = GetOrAdd<ParticleSystem>(root);
            var main = ps.main;
            main.duration = 0.45f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.52f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 5.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.13f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.02f, 0.02f), new Color(0.28f, 0.01f, 0.01f));
            main.gravityModifier = 0.72f;
            main.maxParticles = 32;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16, 24) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.18f;

            SavePrefab(root, path, existed);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        public static GameObject UpgradeRiflePrefab(string path, GameObject bloodImpactPrefab)
        {
            EnsureDirectory(Path.GetDirectoryName(path));
            bool existed = File.Exists(path);
            GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject("Rifle");
            root.name = "Rifle";
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            ClearChildren(root.transform);
            RemoveIfPresent<Collider>(root);
            RemoveIfPresent<MeshRenderer>(root);
            RemoveIfPresent<MeshFilter>(root);

            Material metal = EnsureStandardMaterial(RifleMetalMaterialPath, new Color(0.08f, 0.085f, 0.09f), 0.42f, 0.65f);
            Material polymer = EnsureStandardMaterial(RiflePolymerMaterialPath, new Color(0.015f, 0.017f, 0.018f), 0.28f, 0.05f);
            Material brass = EnsureStandardMaterial(RifleBrassMaterialPath, new Color(0.72f, 0.52f, 0.24f), 0.36f, 0.55f);

            Primitive(root.transform, "Receiver", PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.12f), new Vector3(0.17f, 0.17f, 0.58f), metal);
            Primitive(root.transform, "Handguard", PrimitiveType.Cube, new Vector3(0f, -0.01f, 0.55f), new Vector3(0.13f, 0.13f, 0.46f), metal);
            Primitive(root.transform, "Stock", PrimitiveType.Cube, new Vector3(0f, -0.01f, -0.36f), new Vector3(0.19f, 0.18f, 0.36f), polymer);
            Primitive(root.transform, "ButtPad", PrimitiveType.Cube, new Vector3(0f, -0.01f, -0.58f), new Vector3(0.23f, 0.24f, 0.06f), polymer);
            Primitive(root.transform, "PistolGrip", PrimitiveType.Cube, new Vector3(0f, -0.24f, -0.02f), new Vector3(0.11f, 0.32f, 0.13f), polymer, new Vector3(-13f, 0f, 0f));
            Primitive(root.transform, "Magazine", PrimitiveType.Cube, new Vector3(0f, -0.27f, 0.2f), new Vector3(0.13f, 0.36f, 0.16f), polymer, new Vector3(7f, 0f, 0f));
            Primitive(root.transform, "Barrel", PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 0.93f), new Vector3(0.034f, 0.36f, 0.034f), metal, new Vector3(90f, 0f, 0f));
            Primitive(root.transform, "MuzzleBrake", PrimitiveType.Cylinder, new Vector3(0f, 0.015f, 1.29f), new Vector3(0.052f, 0.07f, 0.052f), metal, new Vector3(90f, 0f, 0f));
            Primitive(root.transform, "ScopeTube", PrimitiveType.Cylinder, new Vector3(0f, 0.18f, 0.2f), new Vector3(0.055f, 0.29f, 0.055f), metal, new Vector3(90f, 0f, 0f));
            Primitive(root.transform, "ScopeFrontLens", PrimitiveType.Cylinder, new Vector3(0f, 0.18f, 0.51f), new Vector3(0.061f, 0.012f, 0.061f), EnsureStandardMaterial(MaterialFolder + "/M_Rifle_ScopeGlass.mat", new Color(0.04f, 0.12f, 0.15f), 0.8f, 0.1f), new Vector3(90f, 0f, 0f));
            Primitive(root.transform, "ScopeMountA", PrimitiveType.Cube, new Vector3(0f, 0.095f, 0.04f), new Vector3(0.09f, 0.08f, 0.05f), metal);
            Primitive(root.transform, "ScopeMountB", PrimitiveType.Cube, new Vector3(0f, 0.095f, 0.34f), new Vector3(0.09f, 0.08f, 0.05f), metal);
            Primitive(root.transform, "BrassRound", PrimitiveType.Cylinder, new Vector3(0.095f, 0.005f, 0.18f), new Vector3(0.018f, 0.08f, 0.018f), brass, new Vector3(0f, 0f, 90f));

            Transform muzzle = new GameObject("MuzzlePoint").transform;
            muzzle.SetParent(root.transform, false);
            muzzle.localPosition = new Vector3(0f, 0.015f, 1.38f);
            ParticleSystem muzzleFx = muzzle.gameObject.AddComponent<ParticleSystem>();
            ConfigureMuzzleFlash(muzzleFx);

            AudioSource audioSource = GetOrAdd<AudioSource>(root);
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;

            HitscanRifle rifle = GetOrAdd<HitscanRifle>(root);
            SerializedObject so = new SerializedObject(rifle);
            SetString(so, "weaponId", "weapon.rifle.ranger");
            SetString(so, "weaponDisplayName", "Ranger Rifle");
            SetFloat(so, "damage", 34f);
            SetFloat(so, "fireRate", 9f);
            SetFloat(so, "range", 155f);
            SetFloat(so, "spreadDegrees", 1.25f);
            SetInt(so, "magazineSize", 30);
            SetFloat(so, "reloadDuration", 1.45f);
            SetFloat(so, "recoilPitch", 1.05f);
            SetFloat(so, "recoilYaw", 0.28f);
            SetObject(so, "muzzlePoint", muzzle);
            SetObject(so, "muzzleFlash", muzzleFx);
            SetObject(so, "shootAudioSource", audioSource);
            if (bloodImpactPrefab != null)
            {
                SetObject(so, "bloodImpactPrefab", bloodImpactPrefab);
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, path, existed);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        public static GameObject UpgradeZombiePrefab(string path)
        {
            EnsureDirectory(Path.GetDirectoryName(path));
            bool existed = File.Exists(path);
            GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject("Zombie");
            root.name = "Zombie";
            root.transform.localScale = Vector3.one;
            ClearChildren(root.transform);
            RemoveIfPresent<MeshRenderer>(root);
            RemoveIfPresent<MeshFilter>(root);

            SetEnemyLayerAndTag(root);
            CapsuleCollider collider = GetOrAdd<CapsuleCollider>(root);
            collider.radius = 0.38f;
            collider.height = 1.8f;
            collider.center = new Vector3(0f, 0.9f, 0f);

            NavMeshAgent agent = GetOrAdd<NavMeshAgent>(root);
            agent.speed = 2.8f;
            agent.stoppingDistance = 1.45f;
            agent.radius = 0.42f;
            agent.height = 1.8f;
            agent.baseOffset = 0f;

            HealthComponent health = GetOrAdd<HealthComponent>(root);
            SerializedObject healthSo = new SerializedObject(health);
            SetFloat(healthSo, "maxHealth", 90f);
            SetBool(healthSo, "destroyOnDeath", false);
            SetBool(healthSo, "disableGameObjectOnDeath", false);
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            AudioSource groanSource = GetOrAdd<AudioSource>(root);
            groanSource.playOnAwake = false;
            groanSource.spatialBlend = 1f;
            Animator animator = GetOrAdd<Animator>(root);
            ZombiePresentationController presentation = GetOrAdd<ZombiePresentationController>(root);

            ZombieAI zombie = GetOrAdd<ZombieAI>(root);
            SerializedObject zombieSo = new SerializedObject(zombie);
            SetFloat(zombieSo, "baseMoveSpeed", 2.85f);
            SetFloat(zombieSo, "sightRange", 24f);
            SetObject(zombieSo, "audioSource", groanSource);
            SetObject(zombieSo, "presentation", presentation);
            zombieSo.ApplyModifiedPropertiesWithoutUndo();

            GetOrAdd<LootDropper>(root);
            BuildZombieVisual(root.transform);

            Transform model = root.transform.Find("Model");
            SerializedObject presentationSo = new SerializedObject(presentation);
            SetObject(presentationSo, "animator", animator);
            SetObject(presentationSo, "audioSource", groanSource);
            SetObject(presentationSo, "proceduralModelRoot", model);
            presentationSo.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, path, existed);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        public static GameObject UpgradeProjectilePrefab(string path)
        {
            EnsureDirectory(Path.GetDirectoryName(path));
            bool existed = File.Exists(path);
            GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
            root.name = "BossProjectile";
            root.transform.localScale = Vector3.one * 0.45f;
            SetLayer(root, "Enemy");

            MeshRenderer renderer = GetOrAdd<MeshRenderer>(root);
            renderer.sharedMaterial = EnsureStandardMaterial(
                ProjectileMaterialPath,
                new Color(0.42f, 0.92f, 0.26f, 0.88f),
                0.6f,
                0f,
                true,
                new Color(0.20f, 1.2f, 0.12f),
                true);

            SphereCollider collider = GetOrAdd<SphereCollider>(root);
            collider.isTrigger = true;

            Rigidbody rb = GetOrAdd<Rigidbody>(root);
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            TrailRenderer trail = GetOrAdd<TrailRenderer>(root);
            trail.time = 0.35f;
            trail.startWidth = 0.2f;
            trail.endWidth = 0.02f;
            trail.sharedMaterial = EnsureStandardMaterial(
                MaterialFolder + "/M_Boss_AcidTrail.mat",
                new Color(0.32f, 1f, 0.12f, 0.35f),
                0.25f,
                0f,
                true,
                new Color(0.12f, 0.8f, 0.02f),
                true);

            GetOrAdd<BossProjectile>(root);
            SavePrefab(root, path, existed);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        public static GameObject UpgradeBossPrefab(string path, GameObject projectilePrefab, ItemDefinition bossToken)
        {
            EnsureDirectory(Path.GetDirectoryName(path));
            bool existed = File.Exists(path);
            GameObject root = existed ? PrefabUtility.LoadPrefabContents(path) : new GameObject("Boss");
            root.name = "Boss";
            root.transform.localScale = Vector3.one;
            ClearChildren(root.transform);
            RemoveIfPresent<MeshRenderer>(root);
            RemoveIfPresent<MeshFilter>(root);

            SetEnemyLayerAndTag(root);
            CapsuleCollider collider = GetOrAdd<CapsuleCollider>(root);
            collider.radius = 0.85f;
            collider.height = 2.8f;
            collider.center = new Vector3(0f, 1.4f, 0f);

            NavMeshAgent agent = GetOrAdd<NavMeshAgent>(root);
            agent.speed = 3.4f;
            agent.stoppingDistance = 2.2f;
            agent.radius = 0.8f;
            agent.height = 2.8f;
            agent.baseOffset = 0f;

            HealthComponent health = GetOrAdd<HealthComponent>(root);
            SerializedObject healthSo = new SerializedObject(health);
            SetFloat(healthSo, "maxHealth", 650f);
            SetBool(healthSo, "destroyOnDeath", false);
            SetBool(healthSo, "disableGameObjectOnDeath", true);
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            Transform spitSpawn = new GameObject("SpitSpawnPoint").transform;
            spitSpawn.SetParent(root.transform, false);
            spitSpawn.localPosition = new Vector3(0f, 2.25f, 1.05f);

            BossAI boss = GetOrAdd<BossAI>(root);
            SerializedObject bossSo = new SerializedObject(boss);
            SetFloat(bossSo, "moveSpeed", 3.35f);
            SetFloat(bossSo, "chaseRange", 42f);
            SetObject(bossSo, "spitProjectilePrefab", projectilePrefab != null ? projectilePrefab.GetComponent<BossProjectile>() : null);
            SetObject(bossSo, "spitSpawnPoint", spitSpawn);
            bossSo.ApplyModifiedPropertiesWithoutUndo();

            LootDropper dropper = GetOrAdd<LootDropper>(root);
            SerializedObject dropSo = new SerializedObject(dropper);
            SetInt(dropSo, "rollCount", 0);
            SerializedProperty guaranteed = dropSo.FindProperty("guaranteedDrops");
            if (guaranteed != null)
            {
                guaranteed.arraySize = bossToken != null ? 1 : 0;
                if (bossToken != null)
                {
                    SerializedProperty entry = guaranteed.GetArrayElementAtIndex(0);
                    SerializedProperty itemProp = entry.FindPropertyRelative("Item");
                    SerializedProperty amountProp = entry.FindPropertyRelative("Amount");
                    if (itemProp != null) itemProp.objectReferenceValue = bossToken;
                    if (amountProp != null) amountProp.intValue = 1;
                }
            }
            dropSo.ApplyModifiedPropertiesWithoutUndo();

            BuildBossVisual(root.transform);
            SavePrefab(root, path, existed);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        public static Material EnsureStandardMaterial(
            string path,
            Color color,
            float smoothness = 0.25f,
            float metallic = 0f,
            bool emissive = false,
            Color emission = default,
            bool transparent = false)
        {
            EnsureDirectory(Path.GetDirectoryName(path));
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            if (shader != null)
            {
                material.shader = shader;
            }

            material.color = color;
            material.enableInstancing = true;
            SetColor(material, "_Color", color);
            SetFloat(material, "_Glossiness", smoothness);
            SetFloat(material, "_Metallic", metallic);
            SetFloat(material, "_SpecularHighlights", 1f);
            SetFloat(material, "_GlossyReflections", 1f);
            SetColor(material, "_SpecColor", Color.Lerp(Color.black, Color.white, Mathf.Clamp01(smoothness * 0.35f)));

            if (transparent)
            {
                SetFloat(material, "_Mode", 3f);
                SetFloat(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
                SetFloat(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                SetFloat(material, "_ZWrite", 0f);
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                SetFloat(material, "_Mode", 0f);
                SetFloat(material, "_SrcBlend", (float)BlendMode.One);
                SetFloat(material, "_DstBlend", (float)BlendMode.Zero);
                SetFloat(material, "_ZWrite", 1f);
                material.DisableKeyword("_ALPHABLEND_ON");
                material.renderQueue = -1;
            }

            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                SetColor(material, "_EmissionColor", emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                SetColor(material, "_EmissionColor", Color.black);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyTerrainAlphamaps(TerrainData data)
        {
            const int layers = 3;
            int resolution = Mathf.Max(128, data.alphamapResolution);
            data.alphamapResolution = resolution;
            float[,,] maps = new float[resolution, resolution, layers];

            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float nx = x / (float)(resolution - 1);
                    float nz = z / (float)(resolution - 1);
                    float steep = Mathf.InverseLerp(14f, 42f, data.GetSteepness(nx, nz));
                    float height01 = data.GetInterpolatedHeight(nx, nz) / Mathf.Max(1f, data.size.y);
                    float noise = Mathf.PerlinNoise(nx * 18.7f + 0.31f, nz * 18.7f + 0.62f);
                    float trail = PathMask(nx, nz);

                    float rock = Mathf.Clamp01(steep * 1.25f + Mathf.InverseLerp(0.42f, 0.72f, height01) * 0.35f);
                    float dirt = Mathf.Clamp01(trail + (1f - steep) * Mathf.InverseLerp(0.48f, 0.92f, noise) * 0.38f);
                    float grass = Mathf.Clamp01(1f - rock * 0.86f - dirt * 0.55f);

                    float sum = Mathf.Max(0.0001f, grass + dirt + rock);
                    maps[z, x, 0] = grass / sum;
                    maps[z, x, 1] = dirt / sum;
                    maps[z, x, 2] = rock / sum;
                }
            }

            data.SetAlphamaps(0, 0, maps);
        }

        private static void ApplyTerrainDetails(TerrainData data)
        {
            Texture2D blade = EnsureGrassBladeTexture();
            DetailPrototype grass = new DetailPrototype
            {
                prototypeTexture = blade,
                renderMode = DetailRenderMode.GrassBillboard,
                healthyColor = new Color(0.34f, 0.49f, 0.18f),
                dryColor = new Color(0.46f, 0.38f, 0.18f),
                minWidth = 0.35f,
                maxWidth = 0.85f,
                minHeight = 0.45f,
                maxHeight = 1.15f,
                noiseSpread = 0.55f
            };

            int resolution = 512;
            data.SetDetailResolution(resolution, 16);
            data.detailPrototypes = new[] { grass };

            int[,] density = new int[resolution, resolution];
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float nx = x / (float)(resolution - 1);
                    float nz = z / (float)(resolution - 1);
                    float steep = data.GetSteepness(nx, nz);
                    float trail = PathMask(nx, nz);
                    float noise = Mathf.PerlinNoise(nx * 30f + 2.2f, nz * 30f + 7.4f);
                    bool canGrow = steep < 30f && trail < 0.35f;
                    density[z, x] = canGrow ? Mathf.RoundToInt(Mathf.Lerp(0, 10, noise)) : 0;
                }
            }

            data.SetDetailLayer(0, 0, 0, density);
        }

        private static TerrainLayer EnsureTerrainLayer(string path, Texture2D texture, Vector2 tileSize, float smoothness)
        {
            TerrainLayer layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }

            layer.diffuseTexture = texture;
            layer.tileSize = tileSize;
            layer.smoothness = smoothness;
            layer.specular = new Color(0.055f, 0.055f, 0.052f);
            EditorUtility.SetDirty(layer);
            return layer;
        }

        private static Texture2D EnsureNoiseTexture(string path, Color low, Color high, int size, int seed, float scale)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
                {
                    name = Path.GetFileNameWithoutExtension(path),
                    wrapMode = TextureWrapMode.Repeat,
                    filterMode = FilterMode.Bilinear
                };
                AssetDatabase.CreateAsset(texture, path);
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = Mathf.PerlinNoise((x + seed * 13f) * scale, (y + seed * 7f) * scale);
                    float fine = Mathf.PerlinNoise((x + seed * 3f) * scale * 3.4f, (y + seed * 5f) * scale * 3.4f);
                    texture.SetPixel(x, y, Color.Lerp(low, high, Mathf.Clamp01(n * 0.72f + fine * 0.28f)));
                }
            }

            texture.Apply(true, false);
            EditorUtility.SetDirty(texture);
            return texture;
        }

        private static Texture2D EnsureGrassBladeTexture()
        {
            const int size = 64;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassBladeTexturePath);
            if (texture == null)
            {
                texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
                {
                    name = "Grass_Blade_Alpha",
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                AssetDatabase.CreateAsset(texture, GrassBladeTexturePath);
            }

            Color clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            for (int blade = 0; blade < 5; blade++)
            {
                float center = 11f + blade * 10f;
                float lean = (blade - 2f) * 0.055f;
                for (int y = 0; y < size; y++)
                {
                    float t = y / (float)(size - 1);
                    float width = Mathf.Lerp(3.4f, 0.6f, t);
                    float xCenter = center + lean * y;
                    int min = Mathf.FloorToInt(xCenter - width);
                    int max = Mathf.CeilToInt(xCenter + width);
                    for (int x = min; x <= max; x++)
                    {
                        if (x < 0 || x >= size)
                        {
                            continue;
                        }

                        float alpha = Mathf.Clamp01(1f - Mathf.Abs(x - xCenter) / Mathf.Max(0.001f, width));
                        Color c = Color.Lerp(new Color(0.18f, 0.34f, 0.08f), new Color(0.48f, 0.62f, 0.22f), t);
                        c.a = alpha;
                        texture.SetPixel(x, y, c);
                    }
                }
            }

            texture.Apply(true, false);
            EditorUtility.SetDirty(texture);
            return texture;
        }

        private static void BuildPine(Transform root)
        {
            Material bark = EnsureStandardMaterial(BarkMaterialPath, new Color(0.29f, 0.18f, 0.10f), 0.18f, 0f);
            Material needles = EnsureStandardMaterial(LeafDarkMaterialPath, new Color(0.08f, 0.24f, 0.08f), 0.22f, 0f);
            Material needlesLight = EnsureStandardMaterial(LeafLightMaterialPath, new Color(0.13f, 0.35f, 0.11f), 0.25f, 0f);

            Primitive(root, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1.8f, 0f), new Vector3(0.18f, 1.8f, 0.18f), bark);
            for (int i = 0; i < 6; i++)
            {
                float y = 1.15f + i * 0.45f;
                float radius = Mathf.Lerp(1.35f, 0.45f, i / 5f);
                Primitive(root, $"NeedleCluster_{i:00}", PrimitiveType.Sphere, new Vector3(0f, y, 0f), new Vector3(radius, 0.28f, radius), i % 2 == 0 ? needles : needlesLight);
            }

            Primitive(root, "TopCluster", PrimitiveType.Sphere, new Vector3(0f, 4.0f, 0f), new Vector3(0.38f, 0.5f, 0.38f), needlesLight);
            for (int i = 0; i < 5; i++)
            {
                float angle = (i / 5f) * 360f;
                Vector3 pos = Quaternion.Euler(0f, angle, 0f) * new Vector3(0.34f, 2.15f, 0.18f);
                Primitive(root, $"Branch_{i:00}", PrimitiveType.Cylinder, pos, new Vector3(0.035f, 0.45f, 0.035f), bark, new Vector3(62f, angle, 0f));
            }
        }

        private static void BuildBroadleaf(Transform root)
        {
            Material bark = EnsureStandardMaterial(BarkMaterialPath, new Color(0.34f, 0.21f, 0.12f), 0.16f, 0f);
            Material leaves = EnsureStandardMaterial(LeafLightMaterialPath, new Color(0.18f, 0.39f, 0.14f), 0.25f, 0f);
            Material darkLeaves = EnsureStandardMaterial(LeafDarkMaterialPath, new Color(0.10f, 0.28f, 0.10f), 0.2f, 0f);

            Primitive(root, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1.45f, 0f), new Vector3(0.24f, 1.45f, 0.24f), bark);
            for (int i = 0; i < 6; i++)
            {
                float angle = (i / 6f) * 360f;
                Vector3 pos = Quaternion.Euler(0f, angle, 0f) * new Vector3(0.45f, 2.3f + (i % 2) * 0.2f, 0.2f);
                Primitive(root, $"Branch_{i:00}", PrimitiveType.Cylinder, pos, new Vector3(0.055f, 0.58f, 0.055f), bark, new Vector3(62f, angle, 0f));
            }

            Vector3[] crowns =
            {
                new Vector3(0f, 3.05f, 0f),
                new Vector3(0.62f, 2.85f, 0.18f),
                new Vector3(-0.54f, 2.78f, 0.12f),
                new Vector3(0.12f, 2.76f, 0.6f),
                new Vector3(0.04f, 2.88f, -0.58f)
            };

            for (int i = 0; i < crowns.Length; i++)
            {
                Primitive(root, $"Crown_{i:00}", PrimitiveType.Sphere, crowns[i], new Vector3(1.0f, 0.72f, 1.0f), i % 2 == 0 ? leaves : darkLeaves);
            }
        }

        private static void BuildZombieVisual(Transform root)
        {
            Transform model = new GameObject("Model").transform;
            model.SetParent(root, false);
            Material skin = EnsureStandardMaterial(ZombieSkinMaterialPath, new Color(0.42f, 0.48f, 0.35f), 0.18f, 0f);
            Material cloth = EnsureStandardMaterial(ZombieClothMaterialPath, new Color(0.12f, 0.16f, 0.20f), 0.22f, 0f);
            Material blood = EnsureStandardMaterial(ZombieBloodMaterialPath, new Color(0.28f, 0.015f, 0.012f), 0.08f, 0f);
            Material eye = EnsureStandardMaterial(MaterialFolder + "/M_Zombie_DullEye.mat", new Color(0.95f, 0.80f, 0.48f), 0.35f, 0f, true, new Color(0.6f, 0.35f, 0.08f));

            Primitive(model, "Torso", PrimitiveType.Capsule, new Vector3(0f, 1.0f, 0f), new Vector3(0.34f, 0.47f, 0.24f), cloth, new Vector3(0f, 0f, -5f));
            Primitive(model, "Head", PrimitiveType.Sphere, new Vector3(0.03f, 1.72f, 0.02f), new Vector3(0.27f, 0.31f, 0.27f), skin);
            Primitive(model, "Jaw", PrimitiveType.Cube, new Vector3(0.03f, 1.58f, 0.19f), new Vector3(0.18f, 0.08f, 0.08f), skin);
            Primitive(model, "LeftEye", PrimitiveType.Sphere, new Vector3(-0.075f, 1.75f, 0.25f), Vector3.one * 0.035f, eye);
            Primitive(model, "RightEye", PrimitiveType.Sphere, new Vector3(0.13f, 1.74f, 0.25f), Vector3.one * 0.035f, eye);
            Primitive(model, "BloodStain", PrimitiveType.Cube, new Vector3(0.05f, 1.14f, 0.245f), new Vector3(0.17f, 0.28f, 0.025f), blood, new Vector3(0f, 0f, -8f));
            Primitive(model, "LeftArm", PrimitiveType.Capsule, new Vector3(-0.39f, 1.05f, 0.08f), new Vector3(0.09f, 0.45f, 0.09f), skin, new Vector3(0f, 0f, -20f));
            Primitive(model, "RightArm", PrimitiveType.Capsule, new Vector3(0.42f, 1.0f, 0.11f), new Vector3(0.09f, 0.48f, 0.09f), skin, new Vector3(0f, 0f, 24f));
            Primitive(model, "LeftLeg", PrimitiveType.Capsule, new Vector3(-0.15f, 0.42f, 0f), new Vector3(0.11f, 0.42f, 0.11f), cloth, new Vector3(0f, 0f, 4f));
            Primitive(model, "RightLeg", PrimitiveType.Capsule, new Vector3(0.16f, 0.42f, 0.02f), new Vector3(0.11f, 0.42f, 0.11f), cloth, new Vector3(0f, 0f, -8f));
        }

        private static void BuildBossVisual(Transform root)
        {
            Transform model = new GameObject("Model").transform;
            model.SetParent(root, false);
            Material hide = EnsureStandardMaterial(BossSkinMaterialPath, new Color(0.31f, 0.21f, 0.18f), 0.16f, 0f);
            Material darkHide = EnsureStandardMaterial(MaterialFolder + "/M_Boss_DarkCarapace.mat", new Color(0.12f, 0.09f, 0.09f), 0.28f, 0.1f);
            Material glow = EnsureStandardMaterial(BossGlowMaterialPath, new Color(0.9f, 0.18f, 0.06f), 0.42f, 0f, true, new Color(1.4f, 0.18f, 0.04f));

            Primitive(model, "MassiveTorso", PrimitiveType.Capsule, new Vector3(0f, 1.42f, 0f), new Vector3(0.82f, 0.88f, 0.62f), hide);
            Primitive(model, "CarapaceBack", PrimitiveType.Cube, new Vector3(0f, 1.55f, -0.36f), new Vector3(1.15f, 1.45f, 0.18f), darkHide, new Vector3(7f, 0f, 0f));
            Primitive(model, "Head", PrimitiveType.Sphere, new Vector3(0f, 2.55f, 0.27f), new Vector3(0.48f, 0.4f, 0.45f), hide);
            Primitive(model, "Core", PrimitiveType.Sphere, new Vector3(0f, 1.52f, 0.64f), new Vector3(0.3f, 0.38f, 0.08f), glow);
            Primitive(model, "LeftEye", PrimitiveType.Sphere, new Vector3(-0.17f, 2.6f, 0.62f), Vector3.one * 0.06f, glow);
            Primitive(model, "RightEye", PrimitiveType.Sphere, new Vector3(0.17f, 2.6f, 0.62f), Vector3.one * 0.06f, glow);
            Primitive(model, "LeftArm", PrimitiveType.Capsule, new Vector3(-0.88f, 1.35f, 0.12f), new Vector3(0.16f, 0.75f, 0.16f), hide, new Vector3(0f, 0f, -26f));
            Primitive(model, "RightArm", PrimitiveType.Capsule, new Vector3(0.88f, 1.35f, 0.12f), new Vector3(0.16f, 0.75f, 0.16f), hide, new Vector3(0f, 0f, 26f));
            Primitive(model, "LeftClaw", PrimitiveType.Cube, new Vector3(-1.15f, 0.62f, 0.36f), new Vector3(0.38f, 0.12f, 0.28f), darkHide, new Vector3(0f, 28f, -14f));
            Primitive(model, "RightClaw", PrimitiveType.Cube, new Vector3(1.15f, 0.62f, 0.36f), new Vector3(0.38f, 0.12f, 0.28f), darkHide, new Vector3(0f, -28f, 14f));
            Primitive(model, "LeftLeg", PrimitiveType.Capsule, new Vector3(-0.35f, 0.58f, 0f), new Vector3(0.22f, 0.58f, 0.22f), hide, new Vector3(0f, 0f, 5f));
            Primitive(model, "RightLeg", PrimitiveType.Capsule, new Vector3(0.35f, 0.58f, 0f), new Vector3(0.22f, 0.58f, 0.22f), hide, new Vector3(0f, 0f, -5f));

            for (int i = 0; i < 5; i++)
            {
                Primitive(model, $"BackSpike_{i:00}", PrimitiveType.Cube, new Vector3(0f, 1.0f + i * 0.32f, -0.64f), new Vector3(0.12f, 0.38f, 0.12f), darkHide, new Vector3(38f, 45f, 0f));
            }
        }

        private static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material, Vector3 euler = default)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.transform.localRotation = Quaternion.Euler(euler);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return go;
        }

        private static void ConfigureMuzzleFlash(ParticleSystem ps)
        {
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.duration = 0.07f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.035f, 0.07f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.82f, 0.36f, 0.95f), new Color(1f, 0.28f, 0.08f, 0.72f));
            main.maxParticles = 18;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 9, 14) });

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.025f;
        }

        private static float PathMask(float nx, float nz)
        {
            Vector2 p = new Vector2(nx, nz);
            float a = SegmentMask(p, new Vector2(0.13f, 0.15f), new Vector2(0.16f, 0.22f));
            float b = SegmentMask(p, new Vector2(0.16f, 0.22f), new Vector2(0.38f, 0.35f));
            float c = SegmentMask(p, new Vector2(0.38f, 0.35f), new Vector2(0.60f, 0.48f));
            float d = SegmentMask(p, new Vector2(0.60f, 0.48f), new Vector2(0.83f, 0.83f));
            return Mathf.Clamp01(Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d)));
        }

        private static float SegmentMask(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            float distance = Vector2.Distance(p, a + ab * t);
            return 1f - Mathf.SmoothStep(0.006f, 0.025f, distance);
        }

        private static void SavePrefab(GameObject root, string path, bool existed)
        {
            if (existed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
            }
        }

        private static string TreeMeshPath(string prefabPath)
        {
            return MeshFolder + "/" + Path.GetFileNameWithoutExtension(prefabPath) + "_CombinedMesh.asset";
        }

        private static void BakeTreeChildrenIntoRootMesh(GameObject root, string meshPath)
        {
            EnsureDirectory(MeshFolder);

            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            List<CombineInstance> combines = new List<CombineInstance>();
            List<Material> materials = new List<Material>();
            Matrix4x4 rootMatrix = root.transform.worldToLocalMatrix;

            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter filter = filters[i];
                if (filter == null || filter.gameObject == root || filter.sharedMesh == null || filter.sharedMesh.subMeshCount <= 0)
                {
                    continue;
                }

                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                Material[] rendererMaterials = renderer != null ? renderer.sharedMaterials : null;
                int subMeshCount = Mathf.Max(1, filter.sharedMesh.subMeshCount);

                for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                {
                    combines.Add(new CombineInstance
                    {
                        mesh = filter.sharedMesh,
                        subMeshIndex = Mathf.Min(subMesh, filter.sharedMesh.subMeshCount - 1),
                        transform = rootMatrix * filter.transform.localToWorldMatrix
                    });

                    Material material = null;
                    if (rendererMaterials != null && rendererMaterials.Length > 0)
                    {
                        material = rendererMaterials[Mathf.Min(subMesh, rendererMaterials.Length - 1)];
                    }

                    materials.Add(EnsureTerrainTreeMaterial(material));
                }
            }

            if (combines.Count == 0)
            {
                return;
            }

            Mesh combinedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (combinedMesh == null)
            {
                combinedMesh = new Mesh
                {
                    name = root.name + "_CombinedMesh"
                };
                AssetDatabase.CreateAsset(combinedMesh, meshPath);
            }
            else
            {
                combinedMesh.Clear();
                combinedMesh.name = root.name + "_CombinedMesh";
            }

            combinedMesh.indexFormat = IndexFormat.UInt32;
            combinedMesh.CombineMeshes(combines.ToArray(), false, true, false);
            combinedMesh.RecalculateBounds();
            combinedMesh.RecalculateNormals();
            EditorUtility.SetDirty(combinedMesh);

            MeshFilter rootFilter = GetOrAdd<MeshFilter>(root);
            rootFilter.sharedMesh = combinedMesh;

            MeshRenderer rootRenderer = GetOrAdd<MeshRenderer>(root);
            rootRenderer.sharedMaterials = materials.ToArray();

            ClearChildren(root.transform);
        }

        private static Material EnsureTerrainTreeMaterial(Material source)
        {
            EnsureDirectory(MaterialFolder);

            string sourceName = source != null ? source.name : "Default";
            bool bark = sourceName.ToLowerInvariant().Contains("bark");
            string path = MaterialFolder + "/M_TerrainTree_" + SanitizeAssetName(sourceName) + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            Shader shader = Shader.Find(bark ? "Nature/Soft Occlusion Bark" : "Nature/Soft Occlusion Leaves");
            if (shader == null && source != null)
            {
                shader = source.shader;
            }

            if (material == null)
            {
                material = new Material(shader != null ? shader : Shader.Find("Standard"))
                {
                    name = Path.GetFileNameWithoutExtension(path)
                };
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            if (source != null && source.HasProperty("_Color") && material.HasProperty("_Color"))
            {
                material.color = source.color;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static string SanitizeAssetName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Default";
            }

            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }

        private static void ClearChildren(Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }

        private static void RemoveIfPresent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component != null)
            {
                Object.DestroyImmediate(component);
            }
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static void SetEnemyLayerAndTag(GameObject go)
        {
            if (ProjectDoctorRunner.HasTag("Enemy"))
            {
                go.tag = "Enemy";
            }

            SetLayer(go, "Enemy");
        }

        private static void SetLayer(GameObject go, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                go.layer = layer;
            }
        }

        private static void SetString(SerializedObject so, string propertyName, string value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null) property.stringValue = value;
        }

        private static void SetFloat(SerializedObject so, string propertyName, float value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null) property.floatValue = value;
        }

        private static void SetInt(SerializedObject so, string propertyName, int value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null) property.intValue = value;
        }

        private static void SetBool(SerializedObject so, string propertyName, bool value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null) property.boolValue = value;
        }

        private static void SetObject(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetFloat(Material material, string propertyName, float value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }

        private static void SetColor(Material material, string propertyName, Color value)
        {
            if (material != null && material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
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
