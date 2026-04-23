using System;
using LiteRealm.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    [DisallowMultipleComponent]
    public class CharacterCreationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject root;
        [SerializeField] private RawImage previewImage;
        [SerializeField] private Text skinLabel;
        [SerializeField] private Text hairStyleLabel;
        [SerializeField] private Text hairColorLabel;
        [SerializeField] private Text outfitLabel;
        [SerializeField] private Button skinPrevButton;
        [SerializeField] private Button skinNextButton;
        [SerializeField] private Button hairStylePrevButton;
        [SerializeField] private Button hairStyleNextButton;
        [SerializeField] private Button hairColorPrevButton;
        [SerializeField] private Button hairColorNextButton;
        [SerializeField] private Button outfitPrevButton;
        [SerializeField] private Button outfitNextButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button backButton;

        [Header("Behavior")]
        [SerializeField] private bool createRuntimeUiIfMissing = true;

        private CharacterCustomizationData workingData;
        private Action confirmedCallback;
        private RenderTexture previewTexture;
        private Camera previewCamera;
        private GameObject previewRoot;
        private PlayerCharacterVisualController previewVisual;

        private void Awake()
        {
            if (createRuntimeUiIfMissing && root == null)
            {
                BuildRuntimeUi();
            }

            WireButtons();
            workingData = CharacterCustomizationState.Current;
            Refresh();
            Close();
        }

        private void OnDestroy()
        {
            if (previewTexture != null)
            {
                previewTexture.Release();
                DestroyRuntime(previewTexture);
            }

            if (previewRoot != null)
            {
                DestroyRuntime(previewRoot);
            }

            if (previewCamera != null)
            {
                DestroyRuntime(previewCamera.gameObject);
            }
        }

        public void OpenForNewGame(Action onConfirmed)
        {
            confirmedCallback = onConfirmed;
            workingData = CharacterCustomizationState.Current;
            Refresh();
            if (root != null)
            {
                root.SetActive(true);
            }
        }

        public void Close()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void WireButtons()
        {
            AddListener(skinPrevButton, () => ChangeSkin(-1));
            AddListener(skinNextButton, () => ChangeSkin(1));
            AddListener(hairStylePrevButton, () => ChangeHairStyle(-1));
            AddListener(hairStyleNextButton, () => ChangeHairStyle(1));
            AddListener(hairColorPrevButton, () => ChangeHairColor(-1));
            AddListener(hairColorNextButton, () => ChangeHairColor(1));
            AddListener(outfitPrevButton, () => ChangeOutfit(-1));
            AddListener(outfitNextButton, () => ChangeOutfit(1));
            AddListener(confirmButton, Confirm);
            AddListener(backButton, Close);
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private void ChangeSkin(int delta)
        {
            workingData.SkinToneIndex = Wrap(workingData.SkinToneIndex + delta, CharacterCustomizationState.SkinTones.Length);
            Refresh();
        }

        private void ChangeHairStyle(int delta)
        {
            workingData.HairStyleIndex = Wrap(workingData.HairStyleIndex + delta, CharacterCustomizationState.HairStyleNames.Length);
            Refresh();
        }

        private void ChangeHairColor(int delta)
        {
            workingData.HairColorIndex = Wrap(workingData.HairColorIndex + delta, CharacterCustomizationState.HairColors.Length);
            Refresh();
        }

        private void ChangeOutfit(int delta)
        {
            workingData.OutfitColorIndex = Wrap(workingData.OutfitColorIndex + delta, CharacterCustomizationState.OutfitColors.Length);
            Refresh();
        }

        private void Confirm()
        {
            CharacterCustomizationState.Current = workingData;
            Close();
            confirmedCallback?.Invoke();
        }

        private void Refresh()
        {
            workingData = CharacterCustomizationState.Clamp(workingData);
            if (skinLabel != null)
            {
                skinLabel.text = $"Skin Tone {workingData.SkinToneIndex + 1}";
            }

            if (hairStyleLabel != null)
            {
                hairStyleLabel.text = $"Hair {CharacterCustomizationState.GetHairStyleName(workingData)}";
            }

            if (hairColorLabel != null)
            {
                hairColorLabel.text = $"Hair Color {workingData.HairColorIndex + 1}";
            }

            if (outfitLabel != null)
            {
                outfitLabel.text = $"Outfit {workingData.OutfitColorIndex + 1}";
            }

            if (previewVisual != null)
            {
                previewVisual.ApplyCustomization(workingData);
            }
        }

        private void BuildRuntimeUi()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
            }

            if (canvas == null)
            {
                GameObject canvasObject = new GameObject("Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
                transform.SetParent(canvasObject.transform, false);
            }

            root = CreatePanel(canvas.transform, "CharacterCreationPanel", new Color(0.025f, 0.031f, 0.034f, 0.97f));

            Text title = CreateText(root.transform, "Title", "Character Creation", 42, TextAnchor.MiddleCenter, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(620f, 62f), Vector2.zero);

            previewImage = CreatePreview(root.transform);
            SetRect(previewImage.rectTransform, new Vector2(0.33f, 0.49f), new Vector2(0.33f, 0.49f), new Vector2(300f, 430f), Vector2.zero);

            Transform controls = new GameObject("Controls", typeof(RectTransform)).transform;
            controls.SetParent(root.transform, false);
            RectTransform controlsRect = controls.GetComponent<RectTransform>();
            SetRect(controlsRect, new Vector2(0.66f, 0.50f), new Vector2(0.66f, 0.50f), new Vector2(430f, 430f), Vector2.zero);

            skinLabel = CreateChoiceRow(controls, "Skin", 138f, out skinPrevButton, out skinNextButton);
            hairStyleLabel = CreateChoiceRow(controls, "HairStyle", 52f, out hairStylePrevButton, out hairStyleNextButton);
            hairColorLabel = CreateChoiceRow(controls, "HairColor", -34f, out hairColorPrevButton, out hairColorNextButton);
            outfitLabel = CreateChoiceRow(controls, "Outfit", -120f, out outfitPrevButton, out outfitNextButton);

            confirmButton = CreateButton(root.transform, "ConfirmButton", "Start", new Vector2(0.66f, 0.18f), new Vector2(170f, 46f));
            backButton = CreateButton(root.transform, "BackButton", "Back", new Vector2(0.66f, 0.10f), new Vector2(170f, 42f));

            SetupPreviewScene();
        }

        private RawImage CreatePreview(Transform parent)
        {
            GameObject go = new GameObject("CharacterPreview", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RawImage image = go.AddComponent<RawImage>();
            image.color = Color.white;
            previewTexture = new RenderTexture(512, 768, 16, RenderTextureFormat.ARGB32);
            previewTexture.name = "CharacterPreviewTexture";
            image.texture = previewTexture;
            return image;
        }

        private void SetupPreviewScene()
        {
            previewRoot = new GameObject("CharacterCreationPreviewRoot");
            previewRoot.transform.position = new Vector3(2000f, 0f, 2000f);
            previewRoot.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            previewRoot.transform.localScale = Vector3.one * 1.15f;
            previewVisual = previewRoot.AddComponent<PlayerCharacterVisualController>();

            GameObject cameraObject = new GameObject("CharacterCreationPreviewCamera");
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.08f, 0.095f, 0.10f, 1f);
            previewCamera.fieldOfView = 28f;
            previewCamera.nearClipPlane = 0.03f;
            previewCamera.farClipPlane = 20f;
            previewCamera.targetTexture = previewTexture;
            previewCamera.transform.position = previewRoot.transform.position + new Vector3(0f, 1.25f, -5.1f);
            previewCamera.transform.LookAt(previewRoot.transform.position + Vector3.up * 1.05f);

            Light key = new GameObject("CharacterCreationPreviewKeyLight").AddComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.25f;
            key.color = new Color(1f, 0.95f, 0.86f, 1f);
            key.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
            key.transform.SetParent(previewRoot.transform, true);
        }

        private Text CreateChoiceRow(Transform parent, string name, float y, out Button prev, out Button next)
        {
            Transform row = new GameObject(name + "Row", typeof(RectTransform)).transform;
            row.SetParent(parent, false);
            SetRect(row.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(420f, 60f), new Vector2(0f, y));

            prev = CreateButton(row, "Prev", "<", new Vector2(0f, 0.5f), new Vector2(52f, 42f));
            next = CreateButton(row, "Next", ">", new Vector2(1f, 0.5f), new Vector2(52f, 42f));
            Text label = CreateText(row, "Label", name, 21, TextAnchor.MiddleCenter, Color.white);
            SetRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(280f, 42f), Vector2.zero);
            return label;
        }

        private static GameObject CreatePanel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = panel.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return panel;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            SetRect(rect, anchor, anchor, size, Vector2.zero);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.18f, 0.22f, 0.22f, 1f);
            Button button = go.AddComponent<Button>();

            Text text = CreateText(go.transform, "Text", label, 22, TextAnchor.MiddleCenter, Color.white);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.text = content;
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
        }

        private static int Wrap(int value, int length)
        {
            if (length <= 0)
            {
                return 0;
            }

            while (value < 0)
            {
                value += length;
            }

            return value % length;
        }

        private static void DestroyRuntime(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureControllerExists();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureControllerExists();
        }

        private static void EnsureControllerExists()
        {
            if (FindObjectOfType<CharacterCreationController>() != null)
            {
                return;
            }

            if (FindObjectOfType<MainMenuController>() == null)
            {
                return;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            canvas.gameObject.AddComponent<CharacterCreationController>();
        }
    }
}
