#if UNITY_EDITOR
using System.IO;
using LiteRealm.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteRealm.EditorTools
{
    public static class MainMenuSceneBuilder
    {
        public const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";

        [MenuItem("Tools/LiteRealm/Scenes/Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject camera = GameObject.FindGameObjectWithTag("MainCamera");
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 0f, -10f);
                var cam = camera.GetComponent<Camera>();
                if (cam != null)
                {
                    cam.orthographic = true;
                    cam.orthographicSize = 5f;
                    cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
                    cam.clearFlags = CameraClearFlags.SolidColor;
                }
            }

            GameObject canvasRoot = new GameObject("Canvas");
            Canvas canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasRoot.AddComponent<CanvasScaler>();
            canvasRoot.AddComponent<GraphicRaycaster>();

            GameObject mainPanel = CreatePanel(canvasRoot.transform, "MainPanel", new Color(0.06f, 0.06f, 0.1f, 0.95f));
            Text titleText = CreateText(mainPanel.transform, "Title", "LiteRealm", 52, new Color(0.9f, 0.85f, 0.7f));
            RectTransform titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.72f);
            titleRect.anchorMax = new Vector2(0.5f, 0.72f);
            titleRect.anchoredPosition = Vector2.zero;
            titleText.fontStyle = FontStyle.Bold;

            Text saveStatusText = CreateText(mainPanel.transform, "SaveStatusText", "Checking saves...", 18, new Color(0.78f, 0.82f, 0.88f));
            RectTransform saveStatusRect = saveStatusText.GetComponent<RectTransform>();
            saveStatusRect.anchorMin = new Vector2(0.5f, 0.58f);
            saveStatusRect.anchorMax = new Vector2(0.5f, 0.58f);
            saveStatusRect.sizeDelta = new Vector2(520f, 32f);

            Button newGameButton = CreateButton(mainPanel.transform, "NewGameButton", "New Game", 0.46f);
            Button resumeButton = CreateButton(mainPanel.transform, "ResumeButton", "Resume", 0.36f);
            Button settingsButton = CreateButton(mainPanel.transform, "SettingsButton", "Settings", 0.26f);
            Button quitButton = CreateButton(mainPanel.transform, "QuitButton", "Quit", 0.16f);

            GameObject settingsPanel = CreatePanel(canvasRoot.transform, "SettingsPanel", new Color(0.06f, 0.06f, 0.12f, 0.98f));
            settingsPanel.SetActive(false);

            CreateText(settingsPanel.transform, "SettingsTitle", "Settings", 42, Color.white).fontStyle = FontStyle.Bold;
            RectTransform settingsTitleRect = settingsPanel.transform.Find("SettingsTitle").GetComponent<RectTransform>();
            settingsTitleRect.anchorMin = new Vector2(0.5f, 0.88f);
            settingsTitleRect.anchorMax = new Vector2(0.5f, 0.88f);
            settingsTitleRect.anchoredPosition = Vector2.zero;

            Slider masterSlider = CreateLabeledSlider(settingsPanel.transform, "MasterVolume", "Master Volume", new Vector2(0.5f, 0.72f));
            Slider musicSlider = CreateLabeledSlider(settingsPanel.transform, "MusicVolume", "Music", new Vector2(0.5f, 0.58f));
            Slider sfxSlider = CreateLabeledSlider(settingsPanel.transform, "SfxVolume", "SFX", new Vector2(0.5f, 0.44f));

            GameObject qualityRow = new GameObject("QualityRow", typeof(RectTransform));
            qualityRow.transform.SetParent(settingsPanel.transform, false);
            RectTransform qualityRowRect = qualityRow.GetComponent<RectTransform>();
            qualityRowRect.anchorMin = new Vector2(0.5f, 0.30f);
            qualityRowRect.anchorMax = new Vector2(0.5f, 0.30f);
            qualityRowRect.pivot = new Vector2(0.5f, 0.5f);
            qualityRowRect.sizeDelta = new Vector2(400f, 32f);
            qualityRowRect.anchoredPosition = Vector2.zero;
            Text qualityLabel = CreateText(qualityRow.transform, "Label", "Quality", 22, Color.white);
            qualityLabel.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
            qualityLabel.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0.5f);
            qualityLabel.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            qualityLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-200f, 0f);
            qualityLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 28f);
            GameObject dropdownGo = new GameObject("Dropdown", typeof(RectTransform));
            dropdownGo.transform.SetParent(qualityRow.transform, false);
            RectTransform dropdownRect = dropdownGo.GetComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(0f, 0.5f);
            dropdownRect.anchorMax = new Vector2(1f, 0.5f);
            dropdownRect.pivot = new Vector2(0.5f, 0.5f);
            dropdownRect.anchoredPosition = Vector2.zero;
            dropdownRect.sizeDelta = new Vector2(-140f, 28f);
            dropdownRect.offsetMin = new Vector2(130f, -14f);
            dropdownRect.offsetMax = new Vector2(-20f, 14f);
            Image dropdownBg = dropdownGo.AddComponent<Image>();
            dropdownBg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
            Dropdown qualityDropdown = dropdownGo.AddComponent<Dropdown>();

            GameObject captionGo = new GameObject("Caption", typeof(RectTransform));
            captionGo.transform.SetParent(dropdownGo.transform, false);
            RectTransform captionRect = captionGo.GetComponent<RectTransform>();
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(8f, 4f);
            captionRect.offsetMax = new Vector2(-24f, -4f);
            Text captionText = captionGo.AddComponent<Text>();
            captionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            captionText.fontSize = 18;
            captionText.color = Color.white;
            captionText.text = "Quality";
            qualityDropdown.captionText = captionText;

            GameObject templateGo = new GameObject("Template", typeof(RectTransform));
            templateGo.transform.SetParent(dropdownGo.transform, false);
            RectTransform templateRect = templateGo.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = Vector2.zero;
            templateRect.sizeDelta = new Vector2(0f, 28f);
            templateGo.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);
            ScrollRect templateScroll = templateGo.AddComponent<ScrollRect>();
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(templateGo.transform, false);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.white;
            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 28f);
            templateScroll.viewport = viewportRect;
            templateScroll.content = contentRect;
            templateScroll.horizontal = false;
            templateScroll.vertical = true;
            GameObject item = new GameObject("Item", typeof(RectTransform));
            item.transform.SetParent(content.transform, false);
            RectTransform itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, 28f);
            Toggle itemToggle = item.AddComponent<Toggle>();
            GameObject itemBg = new GameObject("Item Background", typeof(RectTransform));
            itemBg.transform.SetParent(item.transform, false);
            itemBg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            itemBg.GetComponent<RectTransform>().anchorMax = Vector2.one;
            itemBg.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            itemBg.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            itemBg.AddComponent<Image>().color = Color.clear;
            itemToggle.targetGraphic = itemBg.GetComponent<Image>();
            GameObject itemLabel = new GameObject("Item Label", typeof(RectTransform));
            itemLabel.transform.SetParent(item.transform, false);
            RectTransform itemLabelRect = itemLabel.GetComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(8f, 2f);
            itemLabelRect.offsetMax = new Vector2(-8f, -2f);
            Text itemText = itemLabel.AddComponent<Text>();
            itemText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemText.fontSize = 18;
            itemText.color = Color.white;
            qualityDropdown.itemText = itemText;
            qualityDropdown.template = templateRect;
            templateGo.SetActive(false);

            GameObject fullscreenRow = new GameObject("FullscreenRow", typeof(RectTransform));
            fullscreenRow.transform.SetParent(settingsPanel.transform, false);
            RectTransform fsRowRect = fullscreenRow.GetComponent<RectTransform>();
            fsRowRect.anchorMin = new Vector2(0.5f, 0.18f);
            fsRowRect.anchorMax = new Vector2(0.5f, 0.18f);
            fsRowRect.sizeDelta = new Vector2(400f, 32f);
            fsRowRect.anchoredPosition = Vector2.zero;
            Toggle fullscreenToggle = fullscreenRow.AddComponent<Toggle>();
            CreateText(fullscreenRow.transform, "Label", "Fullscreen", 22, Color.white);
            fullscreenRow.transform.Find("Label").GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
            fullscreenRow.transform.Find("Label").GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0.5f);
            fullscreenRow.transform.Find("Label").GetComponent<RectTransform>().anchoredPosition = new Vector2(-200f, 0f);
            fullscreenRow.transform.Find("Label").GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 28f);
            GameObject toggleBg = new GameObject("Background", typeof(RectTransform));
            toggleBg.transform.SetParent(fullscreenRow.transform, false);
            RectTransform toggleBgRect = toggleBg.GetComponent<RectTransform>();
            toggleBgRect.anchorMin = new Vector2(0f, 0.5f);
            toggleBgRect.anchorMax = new Vector2(0f, 0.5f);
            toggleBgRect.pivot = new Vector2(0f, 0.5f);
            toggleBgRect.anchoredPosition = new Vector2(130f, 0f);
            toggleBgRect.sizeDelta = new Vector2(36f, 36f);
            toggleBg.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);
            GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform));
            checkmark.transform.SetParent(toggleBg.transform, false);
            RectTransform checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(4f, 4f);
            checkRect.offsetMax = new Vector2(-4f, -4f);
            checkmark.AddComponent<Image>().color = new Color(0.3f, 0.7f, 0.4f, 1f);
            fullscreenToggle.graphic = checkmark.GetComponent<Image>();
            fullscreenToggle.targetGraphic = toggleBg.GetComponent<Image>();

            Button backButton = CreateButton(settingsPanel.transform, "BackButton", "Back", 0.06f);

            MainMenuController menuController = canvasRoot.AddComponent<MainMenuController>();
            SettingsMenuController settingsController = settingsPanel.AddComponent<SettingsMenuController>();

            SerializedObject menuSo = new SerializedObject(menuController);
            SetRef(menuSo, "mainPanel", mainPanel);
            SetRef(menuSo, "newGameButton", newGameButton);
            SetRef(menuSo, "resumeButton", resumeButton);
            SetRef(menuSo, "settingsButton", settingsButton);
            SetRef(menuSo, "quitButton", quitButton);
            SetRef(menuSo, "saveStatusText", saveStatusText);
            SetRef(menuSo, "settingsPanel", settingsPanel);
            SetRef(menuSo, "settingsController", settingsController);
            menuSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject settingsSo = new SerializedObject(settingsController);
            SetRef(settingsSo, "masterVolumeSlider", masterSlider);
            SetRef(settingsSo, "musicVolumeSlider", musicSlider);
            SetRef(settingsSo, "sfxVolumeSlider", sfxSlider);
            SetRef(settingsSo, "qualityDropdown", qualityDropdown);
            SetRef(settingsSo, "fullscreenToggle", fullscreenToggle);
            SetRef(settingsSo, "backButton", backButton);
            settingsSo.ApplyModifiedPropertiesWithoutUndo();

            bool saved = EditorSceneManager.SaveScene(scene, MainMenuScenePath, true);
            if (saved)
            {
                AssetDatabase.Refresh();
                EnsureBuildSettingsOrder();
                SetMainMenuAsPlayStartScene();
                Debug.Log("Main Menu scene built and saved to " + MainMenuScenePath);
            }
        }

        public static void EnsureMainMenuScene()
        {
            if (!File.Exists(MainMenuScenePath))
            {
                BuildMainMenuScene();
                return;
            }

            EnsureBuildSettingsOrder();
            SetMainMenuAsPlayStartScene();
        }

        [MenuItem("Tools/LiteRealm/Scenes/Set Main Menu as Play Start Scene")]
        public static void SetMainMenuAsPlayStartScene()
        {
            if (!File.Exists(MainMenuScenePath))
            {
                Debug.LogWarning("Main Menu scene not found. Run 'Tools > LiteRealm > Scenes > Build Main Menu Scene' first.");
                return;
            }

            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
            if (sceneAsset == null)
            {
                AssetDatabase.Refresh();
                sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
            }

            if (sceneAsset != null)
            {
                EditorSceneManager.playModeStartScene = sceneAsset;
                Debug.Log("Play will now start with the Main Menu scene.");
            }
            else
            {
                Debug.LogWarning("Could not load Main Menu scene asset at " + MainMenuScenePath);
            }
        }

        private static GameObject CreatePanel(Transform parent, string name, Color bgColor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image img = go.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = true;
            return go;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(400f, 60f);
            rect.anchoredPosition = Vector2.zero;
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = content;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, float anchorY)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, anchorY);
            rect.anchorMax = new Vector2(0.5f, anchorY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(220f, 44f);
            rect.anchoredPosition = Vector2.zero;
            Image img = go.AddComponent<Image>();
            img.color = new Color(0.22f, 0.22f, 0.28f, 1f);
            Button button = go.AddComponent<Button>();
            GameObject textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            Text text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            return button;
        }

        private static Slider CreateLabeledSlider(Transform parent, string name, string label, Vector2 anchorY)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, anchorY.x);
            rootRect.anchorMax = new Vector2(0.5f, anchorY.y);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(420f, 28f);
            rootRect.anchoredPosition = Vector2.zero;

            Text labelText = CreateText(root.transform, "Label", label, 20, Color.white);
            labelText.GetComponent<RectTransform>().anchorMin = new Vector2(0f, 0.5f);
            labelText.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 0.5f);
            labelText.GetComponent<RectTransform>().pivot = new Vector2(0f, 0.5f);
            labelText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-210f, 0f);
            labelText.GetComponent<RectTransform>().sizeDelta = new Vector2(140f, 24f);
            labelText.alignment = TextAnchor.MiddleLeft;

            GameObject sliderGo = new GameObject("Slider", typeof(RectTransform));
            sliderGo.transform.SetParent(root.transform, false);
            RectTransform sliderRect = sliderGo.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 0.5f);
            sliderRect.anchorMax = new Vector2(1f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = Vector2.zero;
            sliderRect.offsetMin = new Vector2(150f, -12f);
            sliderRect.offsetMax = new Vector2(-20f, 12f);
            Slider slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            GameObject bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(sliderGo.transform, false);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(4f, 4f);
            fillAreaRect.offsetMax = new Vector2(-4f, -4f);
            GameObject fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.35f, 0.5f, 0.7f, 1f);
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;
            return slider;
        }

        private static void SetRef(SerializedObject so, string propName, Object value)
        {
            SerializedProperty p = so.FindProperty(propName);
            if (p != null) p.objectReferenceValue = value;
        }

        private static void EnsureBuildSettingsOrder()
        {
            var buildScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            UpsertBuildScene(buildScenes, MainMenuScenePath, 0);

            if (File.Exists(ProjectDoctorConstants.MainScenePath))
            {
                UpsertBuildScene(buildScenes, ProjectDoctorConstants.MainScenePath, 1);
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        private static void UpsertBuildScene(System.Collections.Generic.List<EditorBuildSettingsScene> scenes, string path, int desiredIndex)
        {
            desiredIndex = Mathf.Clamp(desiredIndex, 0, scenes.Count);

            int existingIndex = -1;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == path)
                {
                    existingIndex = i;
                    break;
                }
            }

            EditorBuildSettingsScene sceneEntry = new EditorBuildSettingsScene(path, true);
            if (existingIndex >= 0)
            {
                scenes.RemoveAt(existingIndex);
            }

            if (desiredIndex >= scenes.Count)
            {
                scenes.Add(sceneEntry);
            }
            else
            {
                scenes.Insert(desiredIndex, sceneEntry);
            }
        }
    }
}
#endif
