#if UNITY_EDITOR
using LiteRealm.AI;
using LiteRealm.Combat;
using LiteRealm.Core;
using LiteRealm.Player;
using LiteRealm.UI;
using LiteRealm.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteRealm.EditorTools
{
    public static class MainSceneStep3SurvivalBuilder
    {
        private const string MainScenePath = "Assets/_Project/Scenes/Main.unity";

        [MenuItem("Tools/LiteRealm/Scenes/Apply Step 3 (Time + Survival Loop)")]
        public static void ApplyStep3()
        {
            MainSceneStep2CombatBuilder.ApplyStep2();

            Scene scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            GameObject appRoot = GetOrCreateRoot(scene, "__App");
            GameObject worldRoot = GetOrCreateRoot(scene, "World");
            GameObject canvasRoot = GetOrCreateRoot(scene, "UI Canvas");

            EnsureCanvasComponents(canvasRoot);
            EnsureDayNight(appRoot, scene);
            EnsureSurvivalHud(canvasRoot, scene);
            EnsureInteractionPrompt(canvasRoot, scene);
            EnsureGameOverPanel(canvasRoot, scene);
            EnsureSpawnerNightScaling(worldRoot, appRoot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Step 3 time/survival setup applied.");
        }

        private static void EnsureDayNight(GameObject appRoot, Scene scene)
        {
            GameObject hubGo = GetOrCreateChild(appRoot.transform, "GameEventHub");
            GameEventHub hub = GetOrAddComponent<GameEventHub>(hubGo);

            GameObject dayNightGo = GetOrCreateChild(appRoot.transform, "DayNightCycle");
            DayNightCycleManager dayNight = GetOrAddComponent<DayNightCycleManager>(dayNightGo);

            SerializedObject so = new SerializedObject(dayNight);
            SerializedProperty sunProp = so.FindProperty("sunLight");
            if (sunProp != null)
            {
                sunProp.objectReferenceValue = FindDirectionalLight(scene);
            }

            SerializedProperty hubProp = so.FindProperty("eventHub");
            if (hubProp != null)
            {
                hubProp.objectReferenceValue = hub;
            }

            SerializedProperty lengthProp = so.FindProperty("dayLengthMinutes");
            if (lengthProp != null && lengthProp.floatValue <= 0.1f)
            {
                lengthProp.floatValue = 24f;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureSurvivalHud(GameObject canvasRoot, Scene scene)
        {
            Transform hudRoot = GetOrCreateUiChild(canvasRoot.transform, "HUDRoot").transform;
            RectTransform hudRect = hudRoot.GetComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0f, 0f);
            hudRect.anchorMax = new Vector2(1f, 1f);
            hudRect.offsetMin = Vector2.zero;
            hudRect.offsetMax = Vector2.zero;

            Slider health = CreateOrGetBar(hudRoot, "HealthBar", "HP", new Vector2(220f, -32f), new Color(0.82f, 0.23f, 0.23f));
            Slider stamina = CreateOrGetBar(hudRoot, "StaminaBar", "Stamina", new Vector2(220f, -62f), new Color(0.26f, 0.73f, 0.34f));
            Slider hunger = CreateOrGetBar(hudRoot, "HungerBar", "Hunger", new Vector2(220f, -92f), new Color(0.84f, 0.67f, 0.22f));
            Slider thirst = CreateOrGetBar(hudRoot, "ThirstBar", "Thirst", new Vector2(220f, -122f), new Color(0.25f, 0.52f, 0.87f));

            Text timeText = CreateOrGetText(
                hudRoot,
                "TimeText",
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(260f, 30f),
                new Vector2(0f, -28f),
                18,
                TextAnchor.MiddleCenter,
                Color.white);

            Text quickSlotsText = CreateOrGetText(
                hudRoot,
                "QuickSlotsText",
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(260f, 110f),
                new Vector2(-200f, -36f),
                14,
                TextAnchor.UpperLeft,
                Color.white);

            Text ammoText = CreateOrGetText(
                hudRoot,
                "AmmoText",
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(220f, 28f),
                new Vector2(-170f, 32f),
                16,
                TextAnchor.MiddleRight,
                Color.white);

            SurvivalHUDController hud = GetOrAddComponent<SurvivalHUDController>(canvasRoot);
            GameObject player = FindPlayer(scene);
            PlayerStats playerStats = player != null ? player.GetComponent<PlayerStats>() : null;
            WeaponManager weaponManager = player != null ? player.GetComponent<WeaponManager>() : null;
            DayNightCycleManager dayNight = FindInScene<DayNightCycleManager>(scene);

            SerializedObject so = new SerializedObject(hud);
            SetObject(so, "healthBar", health);
            SetObject(so, "staminaBar", stamina);
            SetObject(so, "hungerBar", hunger);
            SetObject(so, "thirstBar", thirst);
            SetObject(so, "quickSlotsText", quickSlotsText);
            SetObject(so, "ammoText", ammoText);
            SetObject(so, "timeText", timeText);
            SetObject(so, "playerStats", playerStats);
            SetObject(so, "weaponManager", weaponManager);
            SetObject(so, "dayNight", dayNight);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void EnsureGameOverPanel(GameObject canvasRoot, Scene scene)
        {
            Transform panelRoot = GetOrCreateUiChild(canvasRoot.transform, "GameOverPanel").transform;
            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image bg = GetOrAddComponent<Image>(panelRoot.gameObject);
            bg.color = new Color(0.1f, 0f, 0f, 0.85f);
            bg.raycastTarget = true;

            Text titleText = CreateOrGetText(
                panelRoot,
                "GameOverText",
                new Vector2(0.5f, 0.6f),
                new Vector2(0.5f, 0.6f),
                new Vector2(400f, 60f),
                Vector2.zero,
                42,
                TextAnchor.MiddleCenter,
                Color.red);
            titleText.text = "GAME OVER";
            titleText.fontStyle = FontStyle.Bold;

            GameObject buttonGo = GetOrCreateUiChild(panelRoot, "RestartButton");
            RectTransform buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.35f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.35f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(200f, 44f);
            buttonRect.anchoredPosition = Vector2.zero;

            Image buttonImage = GetOrAddComponent<Image>(buttonGo);
            buttonImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            Button button = GetOrAddComponent<Button>(buttonGo);

            Text buttonText = CreateOrGetText(
                buttonGo.transform,
                "Text",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                22,
                TextAnchor.MiddleCenter,
                Color.white);
            buttonText.text = "Restart";

            GameObject mainMenuButtonGo = GetOrCreateUiChild(panelRoot, "MainMenuButton");
            RectTransform mainMenuButtonRect = mainMenuButtonGo.GetComponent<RectTransform>();
            mainMenuButtonRect.anchorMin = new Vector2(0.5f, 0.22f);
            mainMenuButtonRect.anchorMax = new Vector2(0.5f, 0.22f);
            mainMenuButtonRect.pivot = new Vector2(0.5f, 0.5f);
            mainMenuButtonRect.sizeDelta = new Vector2(200f, 44f);
            mainMenuButtonRect.anchoredPosition = Vector2.zero;
            GetOrAddComponent<Image>(mainMenuButtonGo).color = new Color(0.25f, 0.25f, 0.25f, 1f);
            Button mainMenuButton = GetOrAddComponent<Button>(mainMenuButtonGo);
            Text mainMenuButtonText = CreateOrGetText(
                mainMenuButtonGo.transform,
                "Text",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                22,
                TextAnchor.MiddleCenter,
                Color.white);
            mainMenuButtonText.text = "Main Menu";

            GameOverController gameOver = GetOrAddComponent<GameOverController>(canvasRoot);
            SerializedObject goSo = new SerializedObject(gameOver);
            SetObject(goSo, "gameOverPanel", panelRoot.gameObject);
            SetObject(goSo, "restartButton", button);
            SetObject(goSo, "mainMenuButton", mainMenuButton);
            SetObject(goSo, "gameOverText", titleText);
            GameObject player = FindPlayer(scene);
            if (player != null)
            {
                SetObject(goSo, "playerStats", player.GetComponent<PlayerStats>());
            }
            goSo.ApplyModifiedPropertiesWithoutUndo();

            panelRoot.gameObject.SetActive(false);
        }

        private static void EnsureSpawnerNightScaling(GameObject worldRoot, GameObject appRoot)
        {
            SpawnerZone[] spawners = worldRoot.GetComponentsInChildren<SpawnerZone>(true);
            DayNightCycleManager dayNight = appRoot.GetComponentInChildren<DayNightCycleManager>(true);

            for (int i = 0; i < spawners.Length; i++)
            {
                SpawnerZone spawner = spawners[i];
                if (spawner == null)
                {
                    continue;
                }

                SerializedObject so = new SerializedObject(spawner);
                SetObject(so, "dayNight", dayNight);

                SerializedProperty maxDay = so.FindProperty("maxAliveDay");
                SerializedProperty maxNight = so.FindProperty("maxAliveNight");
                SerializedProperty nightMultiplier = so.FindProperty("nightSpawnMultiplier");

                if (maxDay != null && maxDay.intValue < 4)
                {
                    maxDay.intValue = 6;
                }

                if (maxNight != null && maxNight.intValue < 8)
                {
                    maxNight.intValue = 12;
                }

                if (nightMultiplier != null && nightMultiplier.floatValue < 1.25f)
                {
                    nightMultiplier.floatValue = 1.8f;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureInteractionPrompt(GameObject canvasRoot, Scene scene)
        {
            InteractionPromptUI prompt = GetOrAddComponent<InteractionPromptUI>(canvasRoot);

            GameObject panel = GetOrCreateUiChild(canvasRoot.transform, "InteractionPromptPanel");
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f);
            panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(420f, 46f);
            panelRect.anchoredPosition = new Vector2(0f, 56f);

            Image bg = GetOrAddComponent<Image>(panel);
            bg.color = new Color(0f, 0f, 0f, 0.56f);

            Text promptText = CreateOrGetText(
                panel.transform,
                "PromptText",
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero,
                22,
                TextAnchor.MiddleCenter,
                Color.white);
            promptText.text = "Press E to interact";

            SerializedObject promptSo = new SerializedObject(prompt);
            SetObject(promptSo, "root", panel);
            SetObject(promptSo, "promptText", promptText);
            promptSo.ApplyModifiedPropertiesWithoutUndo();
            panel.SetActive(false);

            GameObject player = FindPlayer(scene);
            if (player == null)
            {
                return;
            }

            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            if (interactor == null)
            {
                return;
            }

            SerializedObject interactorSo = new SerializedObject(interactor);
            SetObject(interactorSo, "interactionPrompt", prompt);
            interactorSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Slider CreateOrGetBar(Transform parent, string name, string label, Vector2 anchoredPosition, Color fillColor)
        {
            GameObject root = GetOrCreateUiChild(parent, name);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(230f, 18f);
            rect.anchoredPosition = anchoredPosition;

            Text labelText = CreateOrGetText(
                root.transform,
                "Label",
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(78f, 18f),
                new Vector2(-80f, 0f),
                13,
                TextAnchor.MiddleRight,
                Color.white);
            labelText.text = label;

            Slider slider = GetOrAddComponent<Slider>(root);
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 100f;

            GameObject background = GetOrCreateUiChild(root.transform, "Background");
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = GetOrAddComponent<Image>(background);
            bgImage.color = new Color(0f, 0f, 0f, 0.55f);

            GameObject fillArea = GetOrCreateUiChild(root.transform, "Fill Area");
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(3f, 3f);
            fillAreaRect.offsetMax = new Vector2(-3f, -3f);

            GameObject fill = GetOrCreateUiChild(fillArea.transform, "Fill");
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = GetOrAddComponent<Image>(fill);
            fillImage.color = fillColor;

            slider.targetGraphic = fillImage;
            slider.fillRect = fillRect;
            slider.handleRect = null;
            return slider;
        }

        private static Text CreateOrGetText(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta,
            Vector2 anchoredPos,
            int fontSize,
            TextAnchor alignment,
            Color color)
        {
            GameObject go = GetOrCreateUiChild(parent, name);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPos;

            Text text = GetOrAddComponent<Text>(go);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            if (string.IsNullOrWhiteSpace(text.text))
            {
                text.text = name;
            }

            return text;
        }

        private static void EnsureCanvasComponents(GameObject canvasRoot)
        {
            Canvas canvas = GetOrAddComponent<Canvas>(canvasRoot);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvasRoot);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            GetOrAddComponent<GraphicRaycaster>(canvasRoot);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T found = roots[i].GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
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

        private static GameObject FindPlayer(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == "Player" || transforms[j].CompareTag("Player"))
                    {
                        return transforms[j].gameObject;
                    }
                }
            }

            return null;
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

        private static GameObject GetOrCreateUiChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                RectTransform existingRect = existing.GetComponent<RectTransform>();
                if (existingRect != null)
                {
                    return existing.gameObject;
                }

                existing.name = name + "_Legacy";
            }

            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
            {
                component = go.AddComponent<T>();
            }

            return component;
        }

        private static void SetObject(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }
    }
}
#endif
