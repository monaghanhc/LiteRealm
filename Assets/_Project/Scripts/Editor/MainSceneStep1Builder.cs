#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using LiteRealm.CameraSystem;
using LiteRealm.Player;
using LiteRealm.UI;
using LiteRealm.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteRealm.EditorTools
{
    public static class MainSceneStep1Builder
    {
        private const string TerrainDataPath = "Assets/_Project/Scenes/Main_TerrainData.asset";
        private const string TreePrefabAPath = "Assets/_Project/Prefabs/Environment/TreePrototype_A.prefab";
        private const string TreePrefabBPath = "Assets/_Project/Prefabs/Environment/TreePrototype_B.prefab";

        private static readonly Vector3 CabinPoi = new Vector3(95f, 0f, 130f);
        private static readonly Vector3 CampsitePoi = new Vector3(230f, 0f, 210f);
        private static readonly Vector3 RuinsPoi = new Vector3(360f, 0f, 290f);
        private static readonly Vector3 BossPoi = new Vector3(500f, 0f, 500f);
        private static readonly Vector3 CityCenterPoi = new Vector3(485f, 0f, 165f);
        private static readonly Vector3 WoodlandVillagePoi = new Vector3(175f, 0f, 155f);

        private static readonly Vector3[] MainRoadPoints =
        {
            new Vector3(70f, 0f, 88f),
            new Vector3(118f, 0f, 122f),
            new Vector3(190f, 0f, 158f),
            new Vector3(280f, 0f, 205f),
            new Vector3(372f, 0f, 216f),
            new Vector3(438f, 0f, 178f),
            new Vector3(535f, 0f, 154f)
        };

        private static readonly Vector3[] BossRoadPoints =
        {
            new Vector3(372f, 0f, 216f),
            new Vector3(420f, 0f, 310f),
            new Vector3(474f, 0f, 408f),
            new Vector3(500f, 0f, 500f)
        };

        [MenuItem("Tools/LiteRealm/Scenes/Build Main Scene (Step 1 Exploration)")]
        public static void BuildMainScene()
        {
            ProjectDoctorRunner.EnsureRequiredFoldersExist();
            ProjectDoctorRunner.EnsureRequiredTagsAndLayers(out _);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Main";

            Terrain terrain = CreateTerrain();
            GameObject app = new GameObject("__App");
            app.AddComponent<ImmersiveVisualQualityController>();
            TryAddComponentByTypeName(app, "LiteRealm.World.PhysicsRealismController");
            GameObject world = new GameObject("World");

            CreateWater(world.transform);
            CreateCloudBank(world.transform);
            CreateRoadNetwork(world.transform, terrain);
            CreateRocks(world.transform, terrain);
            CreateCabin(world.transform, terrain, CabinPoi);
            CreateWoodlandVillage(world.transform, terrain);
            CreateCampsite(world.transform, terrain, CampsitePoi);
            CreateRuins(world.transform, terrain, RuinsPoi);
            CreateCityDistrict(world.transform, terrain);
            CreateBossClearing(world.transform, terrain, BossPoi);

            GameObject spawn = new GameObject("PlayerSpawn");
            spawn.transform.SetParent(world.transform);
            spawn.transform.position = ToSurface(terrain, new Vector3(80f, 0f, 90f), 2f);

            GameObject player = CreatePlayer(spawn.transform.position);
            Camera cam = CreateCamera(player.transform);
            CreateSun();
            CreateGlobalVolume(app);
            GameObject gameplay = new GameObject("Gameplay");
            player.transform.SetParent(gameplay.transform);
            cam.transform.SetParent(gameplay.transform);
            InteractionPromptUI prompt = CreatePromptCanvas();
            CreateEventSystem();
            ConfigureInteraction(player, cam, prompt);

            PlaceSigns(world.transform, terrain);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ProjectDoctorConstants.MainScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Main scene generated for Step 1 exploration.");
        }

        [InitializeOnLoadMethod]
        private static void AutoBuildIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                if (File.Exists(ProjectDoctorConstants.MainScenePath))
                {
                    return;
                }

                Scene active = SceneManager.GetActiveScene();
                if (active.IsValid() && active.isDirty)
                {
                    return;
                }

                BuildMainScene();
            };
        }

        private static Terrain CreateTerrain()
        {
            TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
            if (data == null)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, TerrainDataPath);
            }

            data.heightmapResolution = 513;
            data.size = new Vector3(600f, 85f, 600f);

            ApplyTerrainHeightLayout(data);
            ProceduralArtKit.ApplyTerrainSurface(data);
            ApplyTrees(data);

            GameObject terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.name = "Terrain";
            Terrain terrain = terrainGo.GetComponent<Terrain>();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 3f;
            terrain.detailObjectDistance = 92f;
            terrain.detailObjectDensity = 0.82f;
            terrain.treeDistance = 430f;
            terrain.treeBillboardDistance = 96f;
            terrain.treeCrossFadeLength = 24f;
            terrain.basemapDistance = 1200f;
            return terrain;
        }

        public static void ApplyExpandedWorldLayout(Scene scene)
        {
            if (!scene.IsValid())
            {
                return;
            }

            GameObject worldRoot = GetOrCreateRoot(scene, "World");
            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain != null && terrain.terrainData != null)
            {
                ApplyTerrainHeightLayout(terrain.terrainData);
                ProceduralArtKit.ApplyTerrainSurface(terrain.terrainData);
                ApplyTrees(terrain.terrainData);
                terrain.drawInstanced = true;
                terrain.heightmapPixelError = 3f;
                terrain.detailObjectDistance = 96f;
                terrain.detailObjectDensity = 0.88f;
                terrain.treeDistance = 520f;
                terrain.treeBillboardDistance = 120f;
                terrain.treeCrossFadeLength = 28f;
                terrain.basemapDistance = 1300f;
                EditorUtility.SetDirty(terrain);
                EditorUtility.SetDirty(terrain.terrainData);
            }

            ClearWorldChild(worldRoot.transform, "RoadNetwork");
            ClearWorldChild(worldRoot.transform, "POI_WoodlandHomesteads");
            ClearWorldChild(worldRoot.transform, "POI_CityDistrict");
            ClearWorldChild(worldRoot.transform, "WoodlandLake");
            ClearWorldChild(worldRoot.transform, "WaterPlane");

            CreateWater(worldRoot.transform);
            if (terrain != null)
            {
                CreateRoadNetwork(worldRoot.transform, terrain);
                CreateWoodlandVillage(worldRoot.transform, terrain);
                CreateCityDistrict(worldRoot.transform, terrain);
            }
        }

        private static void ApplyTerrainHeightLayout(TerrainData data)
        {
            if (data == null)
            {
                return;
            }

            int r = data.heightmapResolution;
            float[,] heights = new float[r, r];
            for (int z = 0; z < r; z++)
            {
                for (int x = 0; x < r; x++)
                {
                    float nx = x / (float)(r - 1);
                    float nz = z / (float)(r - 1);
                    float h = 0.03f;
                    h += Mathf.PerlinNoise(nx * 3.2f, nz * 3.2f) * 0.11f;
                    h += Mathf.PerlinNoise(nx * 11.5f + 0.17f, nz * 11.5f + 0.37f) * 0.03f;
                    h += Gaussian(nx, nz, 0.23f, 0.34f, 0.09f) * 0.19f;
                    h += Gaussian(nx, nz, 0.62f, 0.44f, 0.07f) * 0.15f;
                    h -= Gaussian(nx, nz, 0.42f, 0.66f, 0.12f) * 0.08f;

                    Vector3 world = new Vector3(nx * data.size.x, 0f, nz * data.size.z);
                    float roadFlatten = RoadMask01(world, 9f, 29f);
                    float cityFlatten = CityBlockMask01(world, 120f, 100f);
                    float settlementFlatten = WoodlandSettlementMask01(world, 54f);
                    float flatten = Mathf.Clamp01(Mathf.Max(roadFlatten * 0.82f, Mathf.Max(cityFlatten * 0.95f, settlementFlatten * 0.72f)));
                    float plannedHeight = Mathf.Lerp(0.118f, 0.132f, Mathf.PerlinNoise(nx * 4.1f + 8.3f, nz * 4.1f + 3.7f));
                    h = Mathf.Lerp(h, plannedHeight, flatten);

                    heights[z, x] = Mathf.Clamp01(h);
                }
            }

            data.SetHeights(0, 0, heights);
        }

        private static void ApplyTrees(TerrainData data)
        {
            GameObject a = ProceduralArtKit.EnsureTreePrefab(TreePrefabAPath, ProceduralTreeStyle.Pine);
            GameObject b = ProceduralArtKit.EnsureTreePrefab(TreePrefabBPath, ProceduralTreeStyle.Broadleaf);

            data.treePrototypes = new[]
            {
                new TreePrototype { prefab = a, bendFactor = 0.22f },
                new TreePrototype { prefab = b, bendFactor = 0.18f }
            };

            List<TreeInstance> trees = new List<TreeInstance>();
            Random.State randomState = Random.state;
            Random.InitState(2701);
            int attempts = 0;
            while (trees.Count < 2800 && attempts < 7600)
            {
                attempts++;
                float nx = Random.Range(0.02f, 0.98f);
                float nz = Random.Range(0.02f, 0.98f);
                Vector3 p = new Vector3(nx * data.size.x, 0f, nz * data.size.z);
                if (ShouldClearTrees(p))
                {
                    continue;
                }

                float steepness = data.GetSteepness(nx, nz);
                if (steepness > 38f)
                {
                    continue;
                }

                float woodlandWeight = Mathf.Clamp01(0.48f + Mathf.PerlinNoise(nx * 7.8f + 11f, nz * 7.8f + 4f) * 0.62f);
                if (p.x > 382f && p.z < 255f)
                {
                    woodlandWeight *= 0.22f;
                }

                if (Random.value > woodlandWeight)
                {
                    continue;
                }

                float yNorm = data.GetInterpolatedHeight(nx, nz) / data.size.y;
                trees.Add(new TreeInstance
                {
                    position = new Vector3(nx, yNorm, nz),
                    prototypeIndex = Random.Range(0, 2),
                    widthScale = Random.Range(0.72f, 1.28f),
                    heightScale = Random.Range(0.88f, 1.5f),
                    color = Color.Lerp(new Color(0.85f, 0.95f, 0.85f), Color.white, Random.value),
                    lightmapColor = Color.white
                });
            }

            data.treeInstances = trees.ToArray();
            Random.state = randomState;
        }

        private static void CreateWater(Transform parent)
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "WoodlandLake";
            water.transform.SetParent(parent);
            water.transform.position = new Vector3(150f, 9.2f, 430f);
            water.transform.localScale = new Vector3(12f, 1f, 8.5f);
            water.GetComponent<Renderer>().sharedMaterial = ProceduralArtKit.EnsureStandardMaterial(
                ProceduralArtKit.WaterMaterialPath,
                new Color(0.17f, 0.42f, 0.62f, 0.62f),
                0.72f,
                0f,
                false,
                default,
                true);
        }

        private static void CreateCloudBank(Transform parent)
        {
            ProceduralArtKit.EnsureCloudBank(parent);
        }

        private static void CreateRocks(Transform parent, Terrain terrain)
        {
            Transform root = new GameObject("RockField").transform;
            root.SetParent(parent);
            Material rock = ProceduralArtKit.EnsureStandardMaterial(
                ProceduralArtKit.RockMaterialPath,
                new Color(0.43f, 0.43f, 0.40f),
                0.32f,
                0f);

            for (int i = 0; i < 90; i++)
            {
                Vector3 p = new Vector3(Random.Range(35f, 565f), 0f, Random.Range(35f, 565f));
                if (NearPoi(p, 18f) || DistanceToRoad(p) < 12f || CityBlockMask01(p, 128f, 104f) > 0.05f)
                {
                    continue;
                }

                p = ToSurface(terrain, p, 0.15f);
                PrimitiveType type = Random.value > 0.55f ? PrimitiveType.Sphere : PrimitiveType.Cube;
                GameObject rockObj = GameObject.CreatePrimitive(type);
                rockObj.transform.SetParent(root);
                rockObj.transform.position = p;
                rockObj.transform.localScale = Vector3.one * Random.Range(1.2f, 4.5f);
                rockObj.transform.rotation = Random.rotation;
                rockObj.GetComponent<Renderer>().sharedMaterial = rock;
            }
        }

        private static void CreateRoadNetwork(Transform parent, Terrain terrain)
        {
            Transform root = new GameObject("RoadNetwork").transform;
            root.SetParent(parent);

            Material asphalt = NewColorMat(new Color(0.075f, 0.078f, 0.073f));
            Material dirt = NewColorMat(new Color(0.28f, 0.22f, 0.145f));
            Material shoulder = NewColorMat(new Color(0.18f, 0.16f, 0.115f));
            Material stripe = NewColorMat(new Color(0.74f, 0.70f, 0.55f));

            CreateRoadPolyline(root, terrain, MainRoadPoints, "MainRoad", 10.5f, shoulder, 0.035f);
            CreateRoadPolyline(root, terrain, MainRoadPoints, "MainRoad_Asphalt", 7.2f, asphalt, 0.075f);
            CreateRoadPolyline(root, terrain, MainRoadPoints, "MainRoad_CenterLine", 0.34f, stripe, 0.13f);

            CreateRoadPolyline(root, terrain, BossRoadPoints, "BossAccessTrail", 8.5f, dirt, 0.055f);

            Vector3[] cabinAccess =
            {
                new Vector3(118f, 0f, 122f),
                new Vector3(102f, 0f, 132f),
                new Vector3(95f, 0f, 130f)
            };
            CreateRoadPolyline(root, terrain, cabinAccess, "CabinAccessTrack", 5.6f, dirt, 0.06f);

            Vector3[] campsiteAccess =
            {
                new Vector3(250f, 0f, 190f),
                new Vector3(230f, 0f, 210f)
            };
            CreateRoadPolyline(root, terrain, campsiteAccess, "CampsiteTrack", 5.2f, dirt, 0.06f);

            float[] cityX = { 415f, 455f, 495f, 535f };
            float[] cityZ = { 102f, 138f, 176f, 214f };
            for (int i = 0; i < cityX.Length; i++)
            {
                CreateRoadSegment(root, terrain, $"City_NorthSouth_{i:00}", new Vector3(cityX[i], 0f, 82f), new Vector3(cityX[i], 0f, 238f), 8.2f, asphalt, 0.095f);
            }

            for (int i = 0; i < cityZ.Length; i++)
            {
                CreateRoadSegment(root, terrain, $"City_EastWest_{i:00}", new Vector3(392f, 0f, cityZ[i]), new Vector3(566f, 0f, cityZ[i]), 8.2f, asphalt, 0.095f);
            }

            CreateRoadSegment(root, terrain, "City_BlockadeRoad", new Vector3(438f, 0f, 238f), new Vector3(560f, 0f, 238f), 6.2f, dirt, 0.09f);
        }

        private static void CreateWoodlandVillage(Transform parent, Terrain terrain)
        {
            Transform root = new GameObject("POI_WoodlandHomesteads").transform;
            root.SetParent(parent);

            Material fence = NewColorMat(new Color(0.32f, 0.22f, 0.13f));
            CreateHouse(root, terrain, "HunterHouse_01", new Vector3(146f, 0f, 132f), 18f, new Vector2(8.5f, 6.5f), new Color(0.38f, 0.26f, 0.17f));
            CreateHouse(root, terrain, "HunterHouse_02", new Vector3(178f, 0f, 139f), -14f, new Vector2(7.2f, 6.0f), new Color(0.34f, 0.29f, 0.22f));
            CreateHouse(root, terrain, "HunterHouse_03", new Vector3(208f, 0f, 171f), 36f, new Vector2(8.0f, 6.5f), new Color(0.42f, 0.28f, 0.18f));
            CreateHouse(root, terrain, "SawmillCabin", new Vector3(142f, 0f, 181f), -32f, new Vector2(11f, 7f), new Color(0.30f, 0.22f, 0.16f));
            CreateHouse(root, terrain, "RangerShack", new Vector3(188f, 0f, 205f), 12f, new Vector2(7f, 5.4f), new Color(0.25f, 0.31f, 0.22f));

            CreateFenceLine(root, terrain, new Vector3(124f, 0f, 118f), new Vector3(222f, 0f, 118f), fence);
            CreateFenceLine(root, terrain, new Vector3(222f, 0f, 118f), new Vector3(232f, 0f, 198f), fence);
            CreateFenceLine(root, terrain, new Vector3(232f, 0f, 198f), new Vector3(158f, 0f, 226f), fence);

            Material logMat = NewColorMat(new Color(0.36f, 0.24f, 0.13f));
            for (int i = 0; i < 9; i++)
            {
                Vector3 pos = ToSurface(terrain, new Vector3(128f + i * 3.2f, 0f, 204f + (i % 2) * 1.1f), 0.34f);
                Cylinder(root, root.InverseTransformPoint(pos), new Vector3(0.28f, 1.7f, 0.28f), logMat, new Vector3(90f, 0f, 90f));
            }
        }

        private static void CreateCityDistrict(Transform parent, Terrain terrain)
        {
            Transform root = new GameObject("POI_CityDistrict").transform;
            root.SetParent(parent);

            Material concrete = NewColorMat(new Color(0.42f, 0.43f, 0.40f));
            Material brick = NewColorMat(new Color(0.42f, 0.22f, 0.18f));
            Material darkConcrete = NewColorMat(new Color(0.22f, 0.23f, 0.22f));
            Material roof = NewColorMat(new Color(0.10f, 0.11f, 0.12f));
            Material glass = NewColorMat(new Color(0.12f, 0.18f, 0.20f));
            Material hazard = NewColorMat(new Color(0.45f, 0.12f, 0.08f));

            CreateCityBuilding(root, terrain, "Apartments_A", new Vector3(410f, 0f, 120f), 4, new Vector3(20f, 0f, 18f), brick, roof, glass);
            CreateCityBuilding(root, terrain, "Apartments_B", new Vector3(462f, 0f, 118f), 5, new Vector3(23f, 0f, 16f), concrete, roof, glass);
            CreateCityBuilding(root, terrain, "MedicalClinic", new Vector3(520f, 0f, 118f), 3, new Vector3(26f, 0f, 18f), concrete, roof, glass);
            CreateCityBuilding(root, terrain, "Warehouse", new Vector3(550f, 0f, 168f), 2, new Vector3(32f, 0f, 28f), darkConcrete, roof, glass);
            CreateCityBuilding(root, terrain, "OfficeBlock", new Vector3(432f, 0f, 166f), 6, new Vector3(24f, 0f, 20f), concrete, roof, glass);
            CreateCityBuilding(root, terrain, "CornerMarket", new Vector3(490f, 0f, 168f), 2, new Vector3(22f, 0f, 18f), brick, roof, glass);
            CreateCityBuilding(root, terrain, "Tenement_A", new Vector3(410f, 0f, 214f), 4, new Vector3(22f, 0f, 20f), brick, roof, glass);
            CreateCityBuilding(root, terrain, "Tenement_B", new Vector3(468f, 0f, 214f), 5, new Vector3(25f, 0f, 18f), concrete, roof, glass);
            CreateCityBuilding(root, terrain, "MunicipalGarage", new Vector3(532f, 0f, 218f), 2, new Vector3(30f, 0f, 20f), darkConcrete, roof, glass);

            CreateCityPlaza(root, terrain, CityCenterPoi, concrete, hazard);
            CreateBarricade(root, terrain, new Vector3(392f, 0f, 238f), 18f, hazard);
            CreateBarricade(root, terrain, new Vector3(560f, 0f, 238f), -18f, hazard);

            for (int i = 0; i < 14; i++)
            {
                float x = 402f + (i % 7) * 25f;
                float z = i < 7 ? 92f : 226f;
                CreateStreetLight(root, terrain, new Vector3(x, 0f, z), i < 7 ? 180f : 0f);
            }
        }

        private static void CreateCabin(Transform parent, Terrain terrain, Vector3 poi)
        {
            Transform root = new GameObject("POI_Cabin").transform;
            root.SetParent(parent);
            root.position = ToSurface(terrain, poi, 0f);
            Material wood = NewColorMat(new Color(0.45f, 0.30f, 0.18f));
            Material roof = NewColorMat(new Color(0.22f, 0.16f, 0.12f));

            Block(root, new Vector3(0f, 0.15f, 0f), new Vector3(7f, 0.3f, 6f), wood);
            Block(root, new Vector3(0f, 1.45f, -2.85f), new Vector3(7f, 2.6f, 0.3f), wood);
            Block(root, new Vector3(-3.35f, 1.45f, 0f), new Vector3(0.3f, 2.6f, 5.6f), wood);
            Block(root, new Vector3(3.35f, 1.45f, 0f), new Vector3(0.3f, 2.6f, 5.6f), wood);
            Block(root, new Vector3(0f, 3.05f, 0f), new Vector3(7.4f, 0.35f, 6.3f), roof);
        }

        private static void CreateCampsite(Transform parent, Terrain terrain, Vector3 poi)
        {
            Transform root = new GameObject("POI_Campsite").transform;
            root.SetParent(parent);
            root.position = ToSurface(terrain, poi, 0f);
            Material cloth = NewColorMat(new Color(0.25f, 0.36f, 0.24f));
            Material logs = NewColorMat(new Color(0.36f, 0.25f, 0.15f));
            Material ember = NewColorMat(new Color(0.87f, 0.36f, 0.14f));

            GameObject tent = Block(root, new Vector3(0f, 1f, 0f), new Vector3(3.2f, 2f, 2.2f), cloth);
            tent.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            Cylinder(root, new Vector3(-1.5f, 0.35f, 1.8f), new Vector3(0.2f, 0.9f, 0.2f), logs, new Vector3(90f, 35f, 0f));
            Cylinder(root, new Vector3(-1.7f, 0.35f, 1.6f), new Vector3(0.2f, 0.9f, 0.2f), logs, new Vector3(90f, -25f, 0f));
            Cylinder(root, new Vector3(-1.6f, 0.2f, 1.7f), new Vector3(0.45f, 0.18f, 0.45f), ember, Vector3.zero);
        }

        private static void CreateRuins(Transform parent, Terrain terrain, Vector3 poi)
        {
            Transform root = new GameObject("POI_Ruins").transform;
            root.SetParent(parent);
            root.position = ToSurface(terrain, poi, 0f);
            Material stone = NewColorMat(new Color(0.52f, 0.50f, 0.48f));

            Block(root, new Vector3(-2.6f, 1.4f, -2.3f), new Vector3(0.7f, 2.8f, 5.3f), stone);
            Block(root, new Vector3(2.8f, 1.0f, 0.8f), new Vector3(0.7f, 2.0f, 4.0f), stone);
            Block(root, new Vector3(0f, 0.8f, -3.1f), new Vector3(4.8f, 1.6f, 0.7f), stone);
            Block(root, new Vector3(-0.8f, 0.35f, 1.9f), new Vector3(1.7f, 0.7f, 1.1f), stone);
        }

        private static void CreateBossClearing(Transform parent, Terrain terrain, Vector3 poi)
        {
            Transform root = new GameObject("POI_BossClearing").transform;
            root.SetParent(parent);
            Vector3 center = ToSurface(terrain, poi, 0f);
            root.position = center;
            Material marker = NewColorMat(new Color(0.36f, 0.15f, 0.15f));

            for (int i = 0; i < 16; i++)
            {
                float angle = (i / 16f) * Mathf.PI * 2f;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 12f;
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.transform.SetParent(root);
                pillar.transform.position = center + offset;
                pillar.transform.localScale = new Vector3(1.1f, Random.Range(0.7f, 1.6f), 1.1f);
                pillar.GetComponent<Renderer>().sharedMaterial = marker;
            }
        }

        private static GameObject CreatePlayer(Vector3 pos)
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = pos;
            if (ProjectDoctorRunner.HasTag("Player"))
            {
                player.tag = "Player";
            }

            int layer = LayerMask.NameToLayer("Player");
            if (layer >= 0)
            {
                player.layer = layer;
            }

            Object.DestroyImmediate(player.GetComponent<Collider>());
            CharacterController cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            player.AddComponent<PlayerStats>();
            player.AddComponent<PlayerDamageAudioController>();
            ExplorationInput input = player.AddComponent<ExplorationInput>();
            PlayerController playerController = player.AddComponent<PlayerController>();
            player.AddComponent<PlayerCharacterVisualController>();
            PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();

            AudioSource footstepSource = player.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.spatialBlend = 1f;
            SerializedObject pcSo = new SerializedObject(playerController);
            SerializedProperty footstepProp = pcSo.FindProperty("footstepAudio");
            if (footstepProp != null) footstepProp.objectReferenceValue = footstepSource;
            pcSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject so = new SerializedObject(interactor);
            SerializedProperty ip = so.FindProperty("input");
            if (ip != null)
            {
                ip.objectReferenceValue = input;
            }

            SerializedProperty maskProp = so.FindProperty("interactionMask");
            int interactLayer = LayerMask.NameToLayer("Interactable");
            if (maskProp != null)
            {
                maskProp.intValue = interactLayer >= 0 ? (1 << interactLayer) : ~0;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return player;
        }

        private static Camera CreateCamera(Transform player)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 68f;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 1000f;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            cam.useOcclusionCulling = true;
            camGo.AddComponent<AudioListener>();

            CameraController controller = camGo.AddComponent<CameraController>();
            SerializedObject so = new SerializedObject(controller);
            SerializedProperty target = so.FindProperty("target");
            if (target != null)
            {
                target.objectReferenceValue = player;
            }

            SerializedProperty input = so.FindProperty("input");
            if (input != null)
            {
                input.objectReferenceValue = player.GetComponent<ExplorationInput>();
            }

            int playerLayer = LayerMask.NameToLayer("Player");
            SerializedProperty mask = so.FindProperty("collisionMask");
            if (mask != null)
            {
                int m = ~0;
                if (playerLayer >= 0)
                {
                    m &= ~(1 << playerLayer);
                }

                mask.intValue = m;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            camGo.transform.position = player.position + new Vector3(0.8f, 1.8f, -3.8f);
            camGo.transform.rotation = Quaternion.Euler(12f, 28f, 0f);
            return cam;
        }

        private static void CreateSun()
        {
            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.42f;
            light.color = new Color(1f, 0.91f, 0.76f);
            light.shadowStrength = 0.92f;
            light.shadowBias = 0.035f;
            light.shadowNormalBias = 0.28f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(42f, -31f, 0f);
            ProceduralArtKit.ApplySkyRenderSettings(light);
        }

        private static void CreateGlobalVolume(GameObject appRoot)
        {
            string profilePath = "Assets/Settings/DefaultVolumeProfile.asset";
            Object profileAsset = AssetDatabase.LoadAssetAtPath<Object>(profilePath);
            if (profileAsset == null)
            {
                return;
            }

            System.Type volumeType = System.Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
            if (volumeType == null)
            {
                return;
            }

            GameObject volumeGo = new GameObject("Global Volume");
            volumeGo.transform.SetParent(appRoot.transform, false);
            Component volumeComponent = volumeGo.AddComponent(volumeType);

            SerializedObject volumeSo = new SerializedObject(volumeComponent);
            SerializedProperty profileProp = volumeSo.FindProperty("m_Profile");
            if (profileProp != null)
            {
                profileProp.objectReferenceValue = profileAsset;
                volumeSo.ApplyModifiedPropertiesWithoutUndo();
            }
            SerializedProperty isGlobalProp = volumeSo.FindProperty("m_IsGlobal");
            if (isGlobalProp != null) isGlobalProp.boolValue = true;
            SerializedProperty priorityProp = volumeSo.FindProperty("m_Priority");
            if (priorityProp != null) priorityProp.floatValue = 1f;
            SerializedProperty weightProp = volumeSo.FindProperty("m_Weight");
            if (weightProp != null) weightProp.floatValue = 1f;
            volumeSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static InteractionPromptUI CreatePromptCanvas()
        {
            GameObject canvasGo = new GameObject("UI Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            GameObject panel = new GameObject("InteractionPromptPanel");
            panel.transform.SetParent(canvasGo.transform, false);
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.56f);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(420f, 46f);
            panelRect.anchoredPosition = new Vector2(0f, 38f);

            GameObject textGo = new GameObject("PromptText");
            textGo.transform.SetParent(panel.transform, false);
            Text text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "Press E to interact";
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            InteractionPromptUI prompt = canvasGo.AddComponent<InteractionPromptUI>();
            SerializedObject so = new SerializedObject(prompt);
            SerializedProperty root = so.FindProperty("root");
            if (root != null)
            {
                root.objectReferenceValue = panel;
            }

            SerializedProperty promptText = so.FindProperty("promptText");
            if (promptText != null)
            {
                promptText.objectReferenceValue = text;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);
            return prompt;
        }

        private static void CreateEventSystem()
        {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static void ConfigureInteraction(GameObject player, Camera cam, InteractionPromptUI prompt)
        {
            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            if (interactor == null)
            {
                return;
            }

            SerializedObject so = new SerializedObject(interactor);
            SerializedProperty pCam = so.FindProperty("interactionCamera");
            if (pCam != null)
            {
                pCam.objectReferenceValue = cam;
            }

            SerializedProperty pPrompt = so.FindProperty("interactionPrompt");
            if (pPrompt != null)
            {
                pPrompt.objectReferenceValue = prompt;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PlaceSigns(Transform parent, Terrain terrain)
        {
            AddSign(parent, terrain, CabinPoi + new Vector3(5f, 0f, -6f), "Press E to inspect cabin notes", "The cabin journal mentions strange noises near the ruins.");
            AddSign(parent, terrain, CampsitePoi + new Vector3(4f, 0f, 4f), "Press E to inspect campsite", "Cold embers. Someone left in a hurry.");
            AddSign(parent, terrain, RuinsPoi + new Vector3(-4f, 0f, 2f), "Press E to inspect ruins", "Scratch marks lead toward the boss clearing.");
            AddSign(parent, terrain, BossPoi + new Vector3(-8f, 0f, -8f), "Press E to inspect arena", "This clearing feels dangerous at night.");
        }

        private static void AddSign(Transform parent, Terrain terrain, Vector3 p, string prompt, string message)
        {
            GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.transform.SetParent(parent);
            sign.transform.position = ToSurface(terrain, p, 0.9f);
            sign.transform.localScale = new Vector3(0.9f, 1.8f, 0.25f);
            sign.GetComponent<Renderer>().sharedMaterial = NewColorMat(new Color(0.30f, 0.22f, 0.12f));
            int layer = LayerMask.NameToLayer("Interactable");
            if (layer >= 0)
            {
                sign.layer = layer;
            }

            SimpleWorldInteractable i = sign.AddComponent<SimpleWorldInteractable>();
            SerializedObject so = new SerializedObject(i);
            SerializedProperty pp = so.FindProperty("promptText");
            if (pp != null)
            {
                pp.stringValue = prompt;
            }

            SerializedProperty pm = so.FindProperty("interactionMessage");
            if (pm != null)
            {
                pm.stringValue = message;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject Block(Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            return Block(parent, localPos, localScale, mat, Vector3.zero);
        }

        private static GameObject Block(Transform parent, Vector3 localPos, Vector3 localScale, Material mat, Vector3 euler)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPos;
            cube.transform.localScale = localScale;
            cube.transform.localRotation = Quaternion.Euler(euler);
            cube.GetComponent<Renderer>().sharedMaterial = mat;
            return cube;
        }

        private static void Cylinder(Transform parent, Vector3 localPos, Vector3 localScale, Material mat, Vector3 euler)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            c.transform.SetParent(parent, false);
            c.transform.localPosition = localPos;
            c.transform.localScale = localScale;
            c.transform.localRotation = Quaternion.Euler(euler);
            c.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateRoadPolyline(Transform parent, Terrain terrain, Vector3[] points, string name, float width, Material material, float yOffset)
        {
            if (points == null || points.Length < 2)
            {
                return;
            }

            for (int i = 0; i < points.Length - 1; i++)
            {
                CreateRoadSegment(parent, terrain, $"{name}_{i:00}", points[i], points[i + 1], width, material, yOffset);
            }
        }

        private static void CreateRoadSegment(Transform parent, Terrain terrain, string name, Vector3 start, Vector3 end, float width, Material material, float yOffset)
        {
            Vector3 a = ToSurface(terrain, start, yOffset);
            Vector3 b = ToSurface(terrain, end, yOffset);
            Vector3 flat = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float length = flat.magnitude;
            if (length <= 0.01f)
            {
                return;
            }

            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = name;
            segment.transform.SetParent(parent, true);
            segment.transform.position = (a + b) * 0.5f;
            segment.transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
            segment.transform.localScale = new Vector3(width, 0.12f, length);
            Collider collider = segment.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            segment.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateHouse(Transform parent, Terrain terrain, string name, Vector3 worldPosition, float yaw, Vector2 footprint, Color wallColor)
        {
            Transform root = new GameObject(name).transform;
            root.SetParent(parent);
            root.position = ToSurface(terrain, worldPosition, 0f);
            root.rotation = Quaternion.Euler(0f, yaw, 0f);

            Material wall = NewColorMat(wallColor);
            Material trim = NewColorMat(Color.Lerp(wallColor, Color.white, 0.18f));
            Material roof = NewColorMat(new Color(0.12f, 0.09f, 0.075f));
            Material glass = NewColorMat(new Color(0.10f, 0.16f, 0.18f));

            float width = Mathf.Max(4f, footprint.x);
            float depth = Mathf.Max(4f, footprint.y);
            Block(root, new Vector3(0f, 1.35f, 0f), new Vector3(width, 2.7f, depth), wall);
            Block(root, new Vector3(0f, 2.9f, 0f), new Vector3(width + 1.3f, 0.45f, depth + 1.1f), roof);
            Block(root, new Vector3(0f, 3.25f, -0.35f), new Vector3(width + 0.55f, 0.55f, 0.58f), roof, new Vector3(0f, 0f, 7f));
            Block(root, new Vector3(0f, 3.25f, 0.35f), new Vector3(width + 0.55f, 0.55f, 0.58f), roof, new Vector3(0f, 0f, -7f));
            Block(root, new Vector3(0f, 0.85f, depth * 0.51f), new Vector3(1.0f, 1.7f, 0.08f), trim);
            Block(root, new Vector3(-width * 0.28f, 1.65f, depth * 0.51f), new Vector3(0.8f, 0.62f, 0.08f), glass);
            Block(root, new Vector3(width * 0.28f, 1.65f, depth * 0.51f), new Vector3(0.8f, 0.62f, 0.08f), glass);
            Block(root, new Vector3(width * 0.38f, 3.65f, -depth * 0.18f), new Vector3(0.58f, 1.0f, 0.58f), NewColorMat(new Color(0.19f, 0.15f, 0.12f)));
        }

        private static void CreateCityBuilding(Transform parent, Terrain terrain, string name, Vector3 worldPosition, int floors, Vector3 footprint, Material wall, Material roof, Material glass)
        {
            Transform root = new GameObject(name).transform;
            root.SetParent(parent);
            root.position = ToSurface(terrain, worldPosition, 0f);

            float width = Mathf.Max(8f, footprint.x);
            float depth = Mathf.Max(8f, footprint.z);
            float height = Mathf.Max(2, floors) * 3.35f;
            Block(root, new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, depth), wall);
            Block(root, new Vector3(0f, height + 0.18f, 0f), new Vector3(width + 0.8f, 0.36f, depth + 0.8f), roof);

            int windowColumns = Mathf.Clamp(Mathf.RoundToInt(width / 4.4f), 2, 6);
            for (int floor = 0; floor < floors; floor++)
            {
                float y = 1.65f + floor * 3.15f;
                for (int col = 0; col < windowColumns; col++)
                {
                    float x = Mathf.Lerp(-width * 0.32f, width * 0.32f, windowColumns == 1 ? 0.5f : col / (float)(windowColumns - 1));
                    GameObject window = Block(root, new Vector3(x, y, depth * 0.505f), new Vector3(1.1f, 0.72f, 0.08f), glass);
                    Collider collider = window.GetComponent<Collider>();
                    if (collider != null) Object.DestroyImmediate(collider);
                }
            }

            Block(root, new Vector3(-width * 0.26f, 1.05f, depth * 0.515f), new Vector3(1.6f, 2.1f, 0.1f), roof);
        }

        private static void CreateCityPlaza(Transform parent, Terrain terrain, Vector3 center, Material concrete, Material hazard)
        {
            Vector3 p = ToSurface(terrain, center, 0.11f);
            GameObject plaza = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plaza.name = "CityCentralPlaza";
            plaza.transform.SetParent(parent, true);
            plaza.transform.position = p;
            plaza.transform.localScale = new Vector3(28f, 0.12f, 24f);
            Collider collider = plaza.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            plaza.GetComponent<Renderer>().sharedMaterial = concrete;

            Transform monument = new GameObject("CityWarningMonument").transform;
            monument.SetParent(parent);
            monument.position = ToSurface(terrain, center + new Vector3(0f, 0f, 2f), 0f);
            Cylinder(monument, new Vector3(0f, 1.4f, 0f), new Vector3(1.1f, 1.4f, 1.1f), hazard, Vector3.zero);
            Block(monument, new Vector3(0f, 2.95f, 0f), new Vector3(2.3f, 0.25f, 2.3f), hazard);
        }

        private static void CreateStreetLight(Transform parent, Terrain terrain, Vector3 worldPosition, float yaw)
        {
            Transform root = new GameObject("StreetLight").transform;
            root.SetParent(parent);
            root.position = ToSurface(terrain, worldPosition, 0f);
            root.rotation = Quaternion.Euler(0f, yaw, 0f);
            Material metal = NewColorMat(new Color(0.07f, 0.075f, 0.08f));
            Material lamp = NewColorMat(new Color(1f, 0.78f, 0.38f));
            Cylinder(root, new Vector3(0f, 2.35f, 0f), new Vector3(0.08f, 2.35f, 0.08f), metal, Vector3.zero);
            Block(root, new Vector3(0f, 4.78f, 0.55f), new Vector3(0.18f, 0.15f, 1.1f), metal);
            Block(root, new Vector3(0f, 4.62f, 1.12f), new Vector3(0.5f, 0.22f, 0.38f), lamp);
        }

        private static void CreateBarricade(Transform parent, Terrain terrain, Vector3 worldPosition, float yaw, Material material)
        {
            Transform root = new GameObject("CityBarricade").transform;
            root.SetParent(parent);
            root.position = ToSurface(terrain, worldPosition, 0f);
            root.rotation = Quaternion.Euler(0f, yaw, 0f);
            for (int i = 0; i < 5; i++)
            {
                Block(root, new Vector3((i - 2f) * 1.35f, 0.55f, 0f), new Vector3(1.0f, 1.1f, 0.65f), material);
            }
        }

        private static void CreateFenceLine(Transform parent, Terrain terrain, Vector3 start, Vector3 end, Material material)
        {
            Vector3 a = ToSurface(terrain, start, 0f);
            Vector3 b = ToSurface(terrain, end, 0f);
            Vector3 flat = new Vector3(b.x - a.x, 0f, b.z - a.z);
            float length = flat.magnitude;
            if (length <= 0.1f)
            {
                return;
            }

            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 7f));
            for (int i = 0; i <= posts; i++)
            {
                Vector3 pos = Vector3.Lerp(start, end, i / (float)posts);
                Transform post = new GameObject("FencePost").transform;
                post.SetParent(parent);
                post.position = ToSurface(terrain, pos, 0.75f);
                post.localScale = new Vector3(0.22f, 1.5f, 0.22f);
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(post, false);
                cube.transform.localScale = Vector3.one;
                cube.GetComponent<Renderer>().sharedMaterial = material;
            }

            CreateRoadSegment(parent, terrain, "FenceRail_Lower", start + Vector3.up * 0f, end + Vector3.up * 0f, 0.16f, material, 0.72f);
            CreateRoadSegment(parent, terrain, "FenceRail_Upper", start + Vector3.up * 0f, end + Vector3.up * 0f, 0.16f, material, 1.12f);
        }

        private static Material NewColorMat(Color color)
        {
            Material m = new Material(Shader.Find("Standard"));
            m.color = color;
            m.enableInstancing = true;
            if (m.HasProperty("_Glossiness"))
            {
                m.SetFloat("_Glossiness", 0.28f);
            }

            if (m.HasProperty("_Metallic"))
            {
                m.SetFloat("_Metallic", 0f);
            }
            return m;
        }

        private static Vector3 ToSurface(Terrain terrain, Vector3 p, float yOffset)
        {
            if (terrain == null)
            {
                p.y += yOffset;
                return p;
            }

            p.y = terrain.SampleHeight(p) + terrain.transform.position.y + yOffset;
            return p;
        }

        private static bool ShouldClearTrees(Vector3 p)
        {
            return NearPoi(p, 30f)
                || DistanceToRoad(p) < 17f
                || CityBlockMask01(p, 135f, 112f) > 0.02f
                || WoodlandSettlementMask01(p, 62f) > 0.08f;
        }

        private static bool NearPoi(Vector3 p, float r)
        {
            return Vector3.Distance(p, CabinPoi) <= r
                || Vector3.Distance(p, CampsitePoi) <= r
                || Vector3.Distance(p, RuinsPoi) <= r
                || Vector3.Distance(p, BossPoi) <= r
                || Vector3.Distance(p, CityCenterPoi) <= r
                || Vector3.Distance(p, WoodlandVillagePoi) <= r;
        }

        private static float RoadMask01(Vector3 p, float innerWidth, float outerWidth)
        {
            float distance = DistanceToRoad(p);
            return 1f - Mathf.SmoothStep(innerWidth, outerWidth, distance);
        }

        private static float CityBlockMask01(Vector3 p, float width, float depth)
        {
            float dx = Mathf.Abs(p.x - CityCenterPoi.x);
            float dz = Mathf.Abs(p.z - CityCenterPoi.z);
            float mx = 1f - Mathf.SmoothStep(width * 0.45f, width * 0.5f, dx);
            float mz = 1f - Mathf.SmoothStep(depth * 0.45f, depth * 0.5f, dz);
            return Mathf.Clamp01(Mathf.Min(mx, mz));
        }

        private static float WoodlandSettlementMask01(Vector3 p, float radius)
        {
            float distance = Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(WoodlandVillagePoi.x, 0f, WoodlandVillagePoi.z));
            return 1f - Mathf.SmoothStep(radius * 0.72f, radius, distance);
        }

        private static float DistanceToRoad(Vector3 p)
        {
            float distance = float.MaxValue;
            distance = Mathf.Min(distance, DistanceToPolyline(p, MainRoadPoints));
            distance = Mathf.Min(distance, DistanceToPolyline(p, BossRoadPoints));
            distance = Mathf.Min(distance, DistanceToSegmentXZ(p, new Vector3(118f, 0f, 122f), CabinPoi));
            distance = Mathf.Min(distance, DistanceToSegmentXZ(p, new Vector3(250f, 0f, 190f), CampsitePoi));

            float[] cityX = { 415f, 455f, 495f, 535f };
            float[] cityZ = { 102f, 138f, 176f, 214f, 238f };
            for (int i = 0; i < cityX.Length; i++)
            {
                distance = Mathf.Min(distance, DistanceToSegmentXZ(p, new Vector3(cityX[i], 0f, 82f), new Vector3(cityX[i], 0f, 238f)));
            }

            for (int i = 0; i < cityZ.Length; i++)
            {
                distance = Mathf.Min(distance, DistanceToSegmentXZ(p, new Vector3(392f, 0f, cityZ[i]), new Vector3(566f, 0f, cityZ[i])));
            }

            return distance;
        }

        private static float DistanceToPolyline(Vector3 p, Vector3[] points)
        {
            if (points == null || points.Length < 2)
            {
                return float.MaxValue;
            }

            float distance = float.MaxValue;
            for (int i = 0; i < points.Length - 1; i++)
            {
                distance = Mathf.Min(distance, DistanceToSegmentXZ(p, points[i], points[i + 1]));
            }

            return distance;
        }

        private static float DistanceToSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 pp = new Vector2(p.x, p.z);
            Vector2 aa = new Vector2(a.x, a.z);
            Vector2 bb = new Vector2(b.x, b.z);
            Vector2 ab = bb - aa;
            float t = Mathf.Clamp01(Vector2.Dot(pp - aa, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            return Vector2.Distance(pp, aa + ab * t);
        }

        private static float Gaussian(float x, float z, float cx, float cz, float scale)
        {
            float dx = x - cx;
            float dz = z - cz;
            return Mathf.Exp(-((dx * dx + dz * dz) / Mathf.Max(0.0001f, scale)));
        }

        private static void EnsureDirectory(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static GameObject GetOrCreateRoot(Scene scene, string name)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == name)
                {
                    return roots[i];
                }
            }

            GameObject go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }

        private static void ClearWorldChild(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName))
            {
                return;
            }

            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static Component TryAddComponentByTypeName(GameObject target, string typeName)
        {
            if (target == null || string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            System.Type type = System.Type.GetType(typeName + ", LiteRealm")
                               ?? System.Type.GetType(typeName + ", Assembly-CSharp")
                               ?? System.Type.GetType(typeName);

            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                return null;
            }

            Component existing = target.GetComponent(type);
            return existing != null ? existing : target.AddComponent(type);
        }
    }
}
#endif
