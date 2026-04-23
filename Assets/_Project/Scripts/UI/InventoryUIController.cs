using System.Text;
using LiteRealm.Inventory;
using LiteRealm.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    [DisallowMultipleComponent]
    public class InventoryUIController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private KeyCode toggleInventoryKey = KeyCode.I;
        [SerializeField] private ExplorationInput input;

        [Header("References")]
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text capacityText;
        [SerializeField] private Text itemsText;

        [Header("Behavior")]
        [SerializeField] private bool createRuntimeUiIfMissing = true;
        [SerializeField] private bool unlockCursorWhileOpen = true;
        [SerializeField] [Range(12, 24)] private int slotFontSize = 17;

        private bool visible;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;

        public bool IsVisible => visible;
        public string CurrentInventoryText => itemsText != null ? itemsText.text : string.Empty;

        private void Awake()
        {
            ResolveReferences();
            if (createRuntimeUiIfMissing && root == null)
            {
                BuildRuntimeUi();
            }

            SetVisible(false);
            Refresh();
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged += Refresh;
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= Refresh;
            }
        }

        private void Update()
        {
            if (ReadTogglePressedThisFrame())
            {
                SetVisible(!visible);
            }
        }

        public void Bind(InventoryComponent sourceInventory, ExplorationInput sourceInput)
        {
            if (inventory != null)
            {
                inventory.InventoryChanged -= Refresh;
            }

            inventory = sourceInventory;
            input = sourceInput;

            if (isActiveAndEnabled && inventory != null)
            {
                inventory.InventoryChanged += Refresh;
            }

            Refresh();
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

                Refresh();
                return;
            }

            if (wasVisible && unlockCursorWhileOpen)
            {
                Cursor.lockState = previousCursorLock;
                Cursor.visible = previousCursorVisible;
            }
        }

        public void Refresh()
        {
            ResolveReferences();

            if (titleText != null)
            {
                titleText.text = "Inventory";
            }

            if (capacityText != null)
            {
                capacityText.text = inventory != null
                    ? $"Capacity {CountUsedSlots()}/{inventory.SlotCount}"
                    : "Capacity 0/0";
            }

            if (itemsText != null)
            {
                itemsText.text = BuildInventorySummary();
            }
        }

        public string BuildInventorySummary()
        {
            if (inventory == null || inventory.Slots == null)
            {
                return "Inventory unavailable";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                InventorySlot slot = inventory.Slots[i];
                string slotLabel = (i + 1).ToString("00");
                string hotbarLabel = inventory.IsHotbarSlot(i) ? $"  [{i + 5}]" : "     ";

                if (slot == null || slot.IsEmpty)
                {
                    builder.AppendLine($"{slotLabel}{hotbarLabel} Empty");
                    continue;
                }

                builder.AppendLine($"{slotLabel}{hotbarLabel} {slot.Item.DisplayName} x{slot.Quantity}");
            }

            return builder.ToString();
        }

        private void ResolveReferences()
        {
            GameObject player = null;
            if (inventory == null || input == null)
            {
                player = GameObject.FindGameObjectWithTag("Player");
                if (player == null)
                {
                    player = GameObject.Find("Player");
                }
            }

            if (inventory == null && player != null)
            {
                inventory = player.GetComponent<InventoryComponent>();
            }

            if (input == null && player != null)
            {
                input = player.GetComponent<ExplorationInput>();
            }
        }

        private bool ReadTogglePressedThisFrame()
        {
            if (input != null && input.InventoryPressedThisFrame())
            {
                return true;
            }

            return Input.GetKeyDown(toggleInventoryKey);
        }

        private int CountUsedSlots()
        {
            if (inventory == null || inventory.Slots == null)
            {
                return 0;
            }

            int used = 0;
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                InventorySlot slot = inventory.Slots[i];
                if (slot != null && !slot.IsEmpty)
                {
                    used++;
                }
            }

            return used;
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

            GameObject panel = new GameObject("InventoryPanel", typeof(RectTransform));
            panel.transform.SetParent(canvas.transform, false);
            root = panel;

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            Image dim = panel.AddComponent<Image>();
            dim.color = new Color(0.015f, 0.017f, 0.018f, 0.78f);
            dim.raycastTarget = true;

            GameObject card = new GameObject("InventorySurface", typeof(RectTransform));
            card.transform.SetParent(panel.transform, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(680f, 760f);
            cardRect.anchoredPosition = Vector2.zero;
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(0.075f, 0.085f, 0.082f, 0.97f);
            cardImage.raycastTarget = true;

            titleText = CreateText(card.transform, "Title", 30, TextAnchor.MiddleLeft, Color.white);
            SetRect(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-48f, 48f), new Vector2(24f, -34f));
            titleText.fontStyle = FontStyle.Bold;

            capacityText = CreateText(card.transform, "Capacity", 16, TextAnchor.MiddleRight, new Color(0.72f, 0.78f, 0.77f, 1f));
            SetRect(capacityText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-48f, 34f), new Vector2(24f, -76f));

            itemsText = CreateText(card.transform, "Items", slotFontSize, TextAnchor.UpperLeft, new Color(0.9f, 0.93f, 0.9f, 1f));
            SetRect(itemsText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(-48f, -132f), new Vector2(24f, 20f));
            itemsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            itemsText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
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
            EnsureInventoryControllerExists();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureInventoryControllerExists();
        }

        private static void EnsureInventoryControllerExists()
        {
            if (FindObjectOfType<InventoryUIController>() != null)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                return;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            GameObject host = canvas != null ? canvas.gameObject : new GameObject("InventoryUIRuntime");
            InventoryUIController controller = host.AddComponent<InventoryUIController>();
            controller.inventory = player.GetComponent<InventoryComponent>();
            controller.input = player.GetComponent<ExplorationInput>();
        }
    }
}
