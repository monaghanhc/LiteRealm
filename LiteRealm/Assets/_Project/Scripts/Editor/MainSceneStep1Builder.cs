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

        [MenuItem("Tools/LiteRealm/Scenes/Build Main Scene (Step 1 Exploration)")]
        public static void BuildMainScene()
        {
            ProjectDoctorRunner.EnsureRequiredFoldersExist();
            ProjectDoctorRunner.EnsureRequiredTagsAndLayers(out _);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Main";

            Terrain terrain = CreateTerrain();
            GameObject app = new GameObject("__App");
            GameObject world = new GameObject("World");

            CreateWater(world.transform);
            CreateRocks(world.transform, terrain);
            CreateCabin(world.transform, terrain, CabinPoi);
            CreateCampsite(world.transform, terrain, CampsitePoi);
            CreateRuins(world.transform, terrain, RuinsPoi);
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
                    heights[z, x] = Mathf.Clamp01(h);
                }
            }

            data.SetHeights(0, 0, heights);
            ApplyTrees(data);

            GameObject terrainGo = Terrain.CreateTerrainGameObject(data);
            terrainGo.name = "Terrain";
            Terrain terrain = terrainGo.GetComponent<Terrain>();
            terrain.drawInstanced = true;
            terrain.treeDistance = 300f;
            terrain.basemapDistance = 900f;
            return terrain;
        }

        private static void ApplyTrees(TerrainData data)
        {
            GameObject a = GetOrCreateTreePrefab(TreePrefabAPath, 1f, new Color(0.20f, 0.42f, 0.16f));
            GameObject b = GetOrCreateTreePrefab(TreePrefabBPath, 1.3f, new Color(0.16f, 0.36f, 0.14f));

            data.treePrototypes = new[]
            {
                new TreePrototype { prefab = a, bendFactor = 0.22f },
                new TreePrototype { prefab = b, bendFactor = 0.18f }
            };

            List<TreeInstance> trees = new List<TreeInstance>();
            for (int i = 0; i < 700; i++)
            {
                float nx = Random.Range(0.02f, 0.98f);
                float nz = Random.Range(0.02f, 0.98f);
                Vector3 p = new Vector3(nx * data.size.x, 0f, nz * data.size.z);
                if (NearPoi(p, 26f))
                {
                    continue;
                }

                float steepness = data.GetSteepness(nx, nz);
                if (steepness > 38f)
                {
                    continue;
                }

                float yNorm = data.GetInterpolatedHeight(nx, nz) / data.size.y;
                trees.Add(new TreeInstance
                {
                    position = new Vector3(nx, yNorm, nz),
                    prototypeIndex = Random.Range(0, 2),
                    widthScale = Random.Range(0.85f, 1.35f),
                    heightScale = Random.Range(0.9f, 1.45f),
                    color = Color.Lerp(new Color(0.85f, 0.95f, 0.85f), Color.white, Random.value),
                    lightmapColor = Color.white
                });
            }

            data.treeInstances = trees.ToArray();
        }

        private static GameObject GetOrCreateTreePrefab(string path, float trunkHeight, Color leaf)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                return prefab;
            }

            EnsureDirectory(Path.GetDirectoryName(path));
            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(path));
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(root.transform, false);
            trunk.transform.localPosition = new Vector3(0f, trunkHeight * 0.5f, 0f);
            trunk.transform.localScale = new Vector3(0.25f, trunkHeight * 0.5f, 0.25f);
            Object.DestroyImmediate(trunk.GetComponent<Collider>());
            trunk.GetComponent<Renderer>().sharedMaterial = NewColorMat(new Color(0.42f, 0.28f, 0.17f));

            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.transform.SetParent(root.transform, false);
            crown.transform.localPosition = new Vector3(0f, trunkHeight + 0.6f, 0f);
            crown.transform.localScale = Vector3.one * 1.3f;
            Object.DestroyImmediate(crown.GetComponent<Collider>());
            crown.GetComponent<Renderer>().sharedMaterial = NewColorMat(leaf);

            prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }
        private static void CreateWater(Transform parent)
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "WaterPlane";
            water.transform.SetParent(parent);
            water.transform.position = new Vector3(300f, 17f, 300f);
            water.transform.localScale = new Vector3(60f, 1f, 60f);
            water.GetComponent<Renderer>().sharedMaterial = NewColorMat(new Color(0.20f, 0.40f, 0.58f));
        }

        private static void CreateRocks(Transform parent, Terrain terrain)
        {
            Transform root = new GameObject("RockField").transform;
            root.SetParent(parent);
            Material rock = NewColorMat(new Color(0.48f, 0.48f, 0.46f));

            for (int i = 0; i < 90; i++)
            {
                Vector3 p = new Vector3(Random.Range(35f, 565f), 0f, Random.Range(35f, 565f));
                if (NearPoi(p, 18f))
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
            ExplorationInput input = player.AddComponent<ExplorationInput>();
            PlayerController playerController = player.AddComponent<PlayerController>();
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
            cam.fieldOfView = 72f;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 1000f;
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
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.98f, 0.95f);
            light.shadowStrength = 1f;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(45f, -26f, 0f);
            RenderSettings.sun = light;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.68f, 0.76f, 0.82f);
            RenderSettings.fogDensity = 0.0015f;
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
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPos;
            cube.transform.localScale = localScale;
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

        private static Material NewColorMat(Color color)
        {
            Material m = new Material(Shader.Find("Standard"));
            m.color = color;
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

        private static bool NearPoi(Vector3 p, float r)
        {
            return Vector3.Distance(p, CabinPoi) <= r
                || Vector3.Distance(p, CampsitePoi) <= r
                || Vector3.Distance(p, RuinsPoi) <= r
                || Vector3.Distance(p, BossPoi) <= r;
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
    }
}
#endif
