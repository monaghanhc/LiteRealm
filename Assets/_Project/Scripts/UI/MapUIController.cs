using System;
using System.Collections.Generic;
using LiteRealm.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    [Serializable]
    public class MapPoint
    {
        public string Label = "Point";
        public Vector3 WorldPosition;
        public Color Color = new Color(0.92f, 0.84f, 0.42f, 1f);
    }

    [DisallowMultipleComponent]
    public class MapUIController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private KeyCode toggleMapKey = KeyCode.M;
        [SerializeField] private ExplorationInput input;

        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform mapArea;
        [SerializeField] private RectTransform playerMarker;

        [Header("World Bounds")]
        [SerializeField] private Vector2 worldOrigin = Vector2.zero;
        [SerializeField] private Vector2 worldSize = new Vector2(600f, 600f);

        [Header("Map Points")]
        [SerializeField] private List<MapPoint> mapPoints = new List<MapPoint>();

        [Header("Behavior")]
        [SerializeField] private bool unlockCursorWhileOpen = true;
        [SerializeField] private bool createRuntimeUiIfMissing = true;
        [SerializeField] private Color playerMarkerColor = new Color(0.35f, 0.82f, 1f, 1f);

        private readonly List<RectTransform> runtimePointMarkers = new List<RectTransform>();
        private bool visible;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;

        public bool IsVisible => visible;

        private void Awake()
        {
            ResolveReferences();
            EnsureDefaultMapPoints();

            if (createRuntimeUiIfMissing && root == null)
            {
                BuildRuntimeUi();
            }

            SetVisible(false);
        }

        private void Update()
        {
            if (ReadTogglePressedThisFrame())
            {
                SetVisible(!visible);
            }

            if (visible)
            {
                UpdatePlayerMarker();
            }
        }

        public void Toggle()
        {
            SetVisible(!visible);
        }

        public void SetVisible(bool show)
        {
            bool wasVisible = visible;
            visible = show;
            if (root != null)
            {
                root.SetActive(show);
            }

            if (show)
            {
                previousCursorLock = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                if (unlockCursorWhileOpen)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                RefreshPointMarkers();
                UpdatePlayerMarker();
                return;
            }

            if (wasVisible && unlockCursorWhileOpen)
            {
                Cursor.lockState = previousCursorLock;
                Cursor.visible = previousCursorVisible;
            }
        }

        public Vector2 WorldToMapAnchoredPosition(Vector3 worldPosition, Vector2 mapSize)
        {
            Vector2 safeSize = new Vector2(Mathf.Max(1f, worldSize.x), Mathf.Max(1f, worldSize.y));
            float normalizedX = Mathf.InverseLerp(worldOrigin.x, worldOrigin.x + safeSize.x, worldPosition.x);
            float normalizedY = Mathf.InverseLerp(worldOrigin.y, worldOrigin.y + safeSize.y, worldPosition.z);
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedY = Mathf.Clamp01(normalizedY);
            return new Vector2((normalizedX - 0.5f) * mapSize.x, (normalizedY - 0.5f) * mapSize.y);
        }

        private void ResolveReferences()
        {
            if (input == null)
            {
                input = FindObjectOfType<ExplorationInput>();
            }

            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject == null)
                {
                    playerObject = GameObject.Find("Player");
                }

                if (playerObject != null)
                {
                    player = playerObject.transform;
                }
            }
        }

        private bool ReadTogglePressedThisFrame()
        {
            if (input != null && input.MapPressedThisFrame())
            {
                return true;
            }

            return Input.GetKeyDown(toggleMapKey);
        }

        private void UpdatePlayerMarker()
        {
            if (player == null || mapArea == null || playerMarker == null)
            {
                ResolveReferences();
                return;
            }

            playerMarker.anchoredPosition = WorldToMapAnchoredPosition(player.position, mapArea.rect.size);
        }

        private void EnsureDefaultMapPoints()
        {
            if (mapPoints.Count > 0)
            {
                return;
            }

            mapPoints.Add(new MapPoint { Label = "Cabin", WorldPosition = new Vector3(95f, 0f, 130f), Color = new Color(0.88f, 0.72f, 0.38f, 1f) });
            mapPoints.Add(new MapPoint { Label = "Camp", WorldPosition = new Vector3(230f, 0f, 210f), Color = new Color(0.52f, 0.9f, 0.48f, 1f) });
            mapPoints.Add(new MapPoint { Label = "Ruins", WorldPosition = new Vector3(360f, 0f, 290f), Color = new Color(0.74f, 0.74f, 0.78f, 1f) });
            mapPoints.Add(new MapPoint { Label = "Crown", WorldPosition = new Vector3(500f, 0f, 500f), Color = new Color(0.9f, 0.28f, 0.22f, 1f) });
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
                GameObject canvasObject = new GameObject("UI Canvas");
                canvas = canvasObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                canvasObject.AddComponent<GraphicRaycaster>();
                transform.SetParent(canvasObject.transform, false);
            }

            GameObject panel = new GameObject("MapPanel", typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
            root = panel;
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.015f, 0.018f, 0.02f, 0.86f);
            panelImage.raycastTarget = true;

            Text title = CreateText(panel.transform, "Title", "Field Map", 30, TextAnchor.MiddleCenter, Color.white);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(420f, 44f), new Vector2(0f, -42f));

            Text hint = CreateText(panel.transform, "Hint", "M: Close", 16, TextAnchor.MiddleRight, new Color(0.78f, 0.84f, 0.86f));
            SetRect(hint.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(240f, 28f), new Vector2(-34f, -34f));

            GameObject mapObject = new GameObject("MapArea", typeof(RectTransform));
            mapObject.transform.SetParent(panel.transform, false);
            mapArea = mapObject.GetComponent<RectTransform>();
            mapArea.anchorMin = new Vector2(0.5f, 0.5f);
            mapArea.anchorMax = new Vector2(0.5f, 0.5f);
            mapArea.pivot = new Vector2(0.5f, 0.5f);
            mapArea.sizeDelta = new Vector2(720f, 720f);
            mapArea.anchoredPosition = Vector2.zero;
            Image mapImage = mapObject.AddComponent<Image>();
            mapImage.color = new Color(0.09f, 0.15f, 0.14f, 0.96f);
            mapImage.raycastTarget = false;

            CreateGrid(mapArea);
            RefreshPointMarkers();

            GameObject playerObject = new GameObject("PlayerMarker", typeof(RectTransform));
            playerObject.transform.SetParent(mapArea, false);
            playerMarker = playerObject.GetComponent<RectTransform>();
            playerMarker.sizeDelta = new Vector2(20f, 20f);
            Image playerImage = playerObject.AddComponent<Image>();
            playerImage.color = playerMarkerColor;
            playerImage.raycastTarget = false;
        }

        private void RefreshPointMarkers()
        {
            if (mapArea == null)
            {
                return;
            }

            for (int i = 0; i < runtimePointMarkers.Count; i++)
            {
                if (runtimePointMarkers[i] != null)
                {
                    DestroyRuntimeObject(runtimePointMarkers[i].gameObject);
                }
            }

            runtimePointMarkers.Clear();
            for (int i = 0; i < mapPoints.Count; i++)
            {
                MapPoint point = mapPoints[i];
                if (point == null)
                {
                    continue;
                }

                GameObject marker = new GameObject("MapPoint_" + point.Label, typeof(RectTransform));
                marker.transform.SetParent(mapArea, false);
                RectTransform markerRect = marker.GetComponent<RectTransform>();
                markerRect.sizeDelta = new Vector2(14f, 14f);
                markerRect.anchoredPosition = WorldToMapAnchoredPosition(point.WorldPosition, mapArea.rect.size);
                Image markerImage = marker.AddComponent<Image>();
                markerImage.color = point.Color;
                markerImage.raycastTarget = false;
                runtimePointMarkers.Add(markerRect);

                Text label = CreateText(marker.transform, "Label", point.Label, 12, TextAnchor.MiddleLeft, Color.white);
                SetRect(label.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(110f, 20f), new Vector2(62f, 0f));
            }
        }

        private static void CreateGrid(RectTransform parent)
        {
            for (int i = 1; i < 4; i++)
            {
                float normalized = i / 4f;
                GameObject vertical = new GameObject("GridV_" + i, typeof(RectTransform));
                vertical.transform.SetParent(parent, false);
                RectTransform vRect = vertical.GetComponent<RectTransform>();
                vRect.anchorMin = new Vector2(normalized, 0f);
                vRect.anchorMax = new Vector2(normalized, 1f);
                vRect.sizeDelta = new Vector2(2f, 0f);
                vRect.anchoredPosition = Vector2.zero;
                vertical.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

                GameObject horizontal = new GameObject("GridH_" + i, typeof(RectTransform));
                horizontal.transform.SetParent(parent, false);
                RectTransform hRect = horizontal.GetComponent<RectTransform>();
                hRect.anchorMin = new Vector2(0f, normalized);
                hRect.anchorMax = new Vector2(1f, normalized);
                hRect.sizeDelta = new Vector2(0f, 2f);
                hRect.anchoredPosition = Vector2.zero;
                horizontal.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
            }
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureMapControllerExists();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureMapControllerExists();
        }

        private static void EnsureMapControllerExists()
        {
            if (FindObjectOfType<MapUIController>() != null)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                return;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            GameObject host = canvas != null ? canvas.gameObject : new GameObject("MapUIRuntime");
            MapUIController controller = host.AddComponent<MapUIController>();
            controller.player = player.transform;
            controller.input = player.GetComponent<ExplorationInput>();
        }

        private static void DestroyRuntimeObject(GameObject target)
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
    }
}
