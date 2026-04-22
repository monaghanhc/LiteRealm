using LiteRealm.Saving;
using LiteRealm.Multiplayer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Main panel")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button lanButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text saveStatusText;

        [Header("LAN Co-op")]
        [SerializeField] private GameObject lanPanel;
        [SerializeField] private Button hostLanButton;
        [SerializeField] private Button joinLanButton;
        [SerializeField] private Button lanBackButton;
        [SerializeField] private InputField playerNameInput;
        [SerializeField] private InputField hostAddressInput;
        [SerializeField] private Text lanStatusText;

        [Header("Settings")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private SettingsMenuController settingsController;

        [Header("Scene")]
        [SerializeField] private string gameplaySceneName = "Main";

        private void Awake()
        {
            EnsureEventSystemExists();
            ResolveFallbackReferences();
            EnsureLanUiExists();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            if (newGameButton != null)
            {
                newGameButton.onClick.AddListener(OnNewGameClicked);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (lanButton != null)
            {
                lanButton.onClick.AddListener(OnLanClicked);
            }

            if (hostLanButton != null)
            {
                hostLanButton.onClick.AddListener(OnHostLanClicked);
            }

            if (joinLanButton != null)
            {
                joinLanButton.onClick.AddListener(OnJoinLanClicked);
            }

            if (lanBackButton != null)
            {
                lanBackButton.onClick.AddListener(OnLanBackClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }

            if (settingsController != null)
            {
                settingsController.OnBackClicked += OnSettingsBackClicked;
            }

            ShowMainPanel();
            RefreshSaveState();
        }

        private void OnEnable()
        {
            RefreshSaveState();
        }

        private void OnDestroy()
        {
            if (settingsController != null)
            {
                settingsController.OnBackClicked -= OnSettingsBackClicked;
            }
        }

        private void ShowMainPanel()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(true);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            if (lanPanel != null)
            {
                lanPanel.SetActive(false);
            }
        }

        private void ShowSettingsPanel()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }

            if (lanPanel != null)
            {
                lanPanel.SetActive(false);
            }

            if (settingsController != null)
            {
                settingsController.RefreshFromSettings();
            }
        }

        private void ShowLanPanel()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            if (lanPanel != null)
            {
                lanPanel.SetActive(true);
            }

            RefreshLanPanel();
        }

        private void OnNewGameClicked()
        {
            LanSessionLauncher.ClearPendingSession();
            SaveSystem.ClearPendingResumeRequest();
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void OnResumeClicked()
        {
            if (!SaveSystem.HasSaveFile())
            {
                RefreshSaveState();
                return;
            }

            LanSessionLauncher.ClearPendingSession();
            SaveSystem.RequestResumeOnNextSceneLoad();
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void OnLanClicked()
        {
            ShowLanPanel();
        }

        private void OnHostLanClicked()
        {
            SaveSystem.ClearPendingResumeRequest();
            LanSessionLauncher.StartHost(ReadPlayerName());
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void OnJoinLanClicked()
        {
            string hostAddress = hostAddressInput != null ? hostAddressInput.text.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(hostAddress))
            {
                if (lanStatusText != null)
                {
                    lanStatusText.text = "Enter the host IPv4 address shown on the host screen.";
                }

                return;
            }

            SaveSystem.ClearPendingResumeRequest();
            LanSessionLauncher.StartClient(hostAddress, ReadPlayerName());
            SceneManager.LoadScene(gameplaySceneName);
        }

        private void OnLanBackClicked()
        {
            ShowMainPanel();
        }

        private void OnSettingsClicked()
        {
            ShowSettingsPanel();
        }

        private void OnSettingsBackClicked()
        {
            ShowMainPanel();
        }

        private void OnQuitClicked()
        {
            Application.Quit();
        }

        private void RefreshSaveState()
        {
            bool hasSave = SaveSystem.HasSaveFile();

            if (resumeButton != null)
            {
                resumeButton.interactable = hasSave;
            }

            if (saveStatusText != null)
            {
                saveStatusText.text = hasSave ? "Save found: Resume available" : "No save found: Start a New Game";
            }
        }

        private void RefreshLanPanel()
        {
            if (playerNameInput != null && string.IsNullOrWhiteSpace(playerNameInput.text))
            {
                playerNameInput.text = "Player";
            }

            if (hostAddressInput != null && string.IsNullOrWhiteSpace(hostAddressInput.text))
            {
                hostAddressInput.text = "127.0.0.1";
            }

            if (lanStatusText != null)
            {
                lanStatusText.text = "Host creates a 4-player local Wi-Fi session. Join uses the host IPv4 address.";
            }
        }

        private string ReadPlayerName()
        {
            return playerNameInput != null ? LanProtocol.SanitizePlayerName(playerNameInput.text) : "Player";
        }

        private void ResolveFallbackReferences()
        {
            if (mainPanel == null)
            {
                mainPanel = FindChildGameObject("MainPanel");
            }

            if (settingsPanel == null)
            {
                settingsPanel = FindChildGameObject("SettingsPanel");
            }

            if (lanPanel == null)
            {
                lanPanel = FindChildGameObject("LanPanel");
            }

            if (newGameButton == null)
            {
                newGameButton = FindChildButton("NewGameButton") ?? FindChildButton("PlayButton");
            }

            if (resumeButton == null)
            {
                resumeButton = FindChildButton("ResumeButton");
            }

            if (lanButton == null)
            {
                lanButton = FindChildButton("LanButton");
            }

            if (hostLanButton == null)
            {
                hostLanButton = FindChildButton("HostLanButton");
            }

            if (joinLanButton == null)
            {
                joinLanButton = FindChildButton("JoinLanButton");
            }

            if (lanBackButton == null)
            {
                lanBackButton = FindChildButton("LanBackButton");
            }

            if (playerNameInput == null)
            {
                playerNameInput = FindChildInputField("PlayerNameInput");
            }

            if (hostAddressInput == null)
            {
                hostAddressInput = FindChildInputField("HostAddressInput");
            }

            if (lanStatusText == null)
            {
                lanStatusText = FindChildText("LanStatusText");
            }

            if (settingsButton == null)
            {
                settingsButton = FindChildButton("SettingsButton");
            }

            if (quitButton == null)
            {
                quitButton = FindChildButton("QuitButton");
            }

            if (saveStatusText == null)
            {
                saveStatusText = FindChildText("SaveStatusText");
            }

            if (settingsController == null && settingsPanel != null)
            {
                settingsController = settingsPanel.GetComponent<SettingsMenuController>();
            }
        }

        private void EnsureLanUiExists()
        {
            if (lanButton == null && mainPanel != null)
            {
                SetButtonAnchor(newGameButton, 0.50f);
                SetButtonAnchor(resumeButton, 0.40f);
                SetButtonAnchor(settingsButton, 0.20f);
                SetButtonAnchor(quitButton, 0.10f);
                lanButton = CreateRuntimeButton(mainPanel.transform, "LanButton", "LAN Co-op", 0.30f);
            }

            if (lanPanel != null)
            {
                return;
            }

            Transform panelParent = mainPanel != null && mainPanel.transform.parent != null ? mainPanel.transform.parent : transform;
            lanPanel = CreateRuntimePanel(panelParent, "LanPanel", new Color(0.045f, 0.06f, 0.07f, 0.98f));
            lanPanel.SetActive(false);

            Text title = CreateRuntimeText(lanPanel.transform, "LanTitle", "LAN Co-op", 42, Color.white);
            title.fontStyle = FontStyle.Bold;
            SetRect(title.rectTransform, new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.84f), new Vector2(520f, 60f), Vector2.zero);

            lanStatusText = CreateRuntimeText(
                lanPanel.transform,
                "LanStatusText",
                "Host creates a 4-player local Wi-Fi session. Join uses the host IPv4 address.",
                17,
                new Color(0.78f, 0.86f, 0.88f));
            SetRect(lanStatusText.rectTransform, new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.74f), new Vector2(620f, 46f), Vector2.zero);

            playerNameInput = CreateRuntimeLabeledInput(lanPanel.transform, "PlayerNameInput", "Name", "Player", 0.61f, 24);
            hostAddressInput = CreateRuntimeLabeledInput(lanPanel.transform, "HostAddressInput", "Host IP", "127.0.0.1", 0.49f, 64);
            hostLanButton = CreateRuntimeButton(lanPanel.transform, "HostLanButton", "Host LAN", 0.34f);
            joinLanButton = CreateRuntimeButton(lanPanel.transform, "JoinLanButton", "Join LAN", 0.24f);
            lanBackButton = CreateRuntimeButton(lanPanel.transform, "LanBackButton", "Back", 0.10f);
        }

        private static void SetButtonAnchor(Button button, float anchorY)
        {
            if (button == null)
            {
                return;
            }

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = new Vector2(0.5f, anchorY);
            rect.anchorMax = new Vector2(0.5f, anchorY);
            rect.anchoredPosition = Vector2.zero;
        }

        private static GameObject CreateRuntimePanel(Transform parent, string name, Color color)
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

        private static Text CreateRuntimeText(Transform parent, string name, string content, int fontSize, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = content;
            return text;
        }

        private static Button CreateRuntimeButton(Transform parent, string name, string label, float anchorY)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, anchorY);
            rect.anchorMax = new Vector2(0.5f, anchorY);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(220f, 44f);
            rect.anchoredPosition = Vector2.zero;

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.22f, 0.22f, 0.28f, 1f);
            Button button = go.AddComponent<Button>();

            Text text = CreateRuntimeText(go.transform, "Text", label, 24, Color.white);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static InputField CreateRuntimeLabeledInput(Transform parent, string name, string label, string value, float anchorY, int characterLimit)
        {
            GameObject row = new GameObject(name + "Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, anchorY);
            rowRect.anchorMax = new Vector2(0.5f, anchorY);
            rowRect.pivot = new Vector2(0.5f, 0.5f);
            rowRect.sizeDelta = new Vector2(520f, 42f);
            rowRect.anchoredPosition = Vector2.zero;

            Text labelText = CreateRuntimeText(row.transform, "Label", label, 18, Color.white);
            labelText.alignment = TextAnchor.MiddleLeft;
            SetRect(labelText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(110f, 30f), Vector2.zero, new Vector2(0f, 0.5f));

            GameObject inputGo = new GameObject(name, typeof(RectTransform));
            inputGo.transform.SetParent(row.transform, false);
            RectTransform inputRect = inputGo.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0f, 0.5f);
            inputRect.anchorMax = new Vector2(1f, 0.5f);
            inputRect.pivot = new Vector2(0.5f, 0.5f);
            inputRect.offsetMin = new Vector2(128f, -20f);
            inputRect.offsetMax = new Vector2(0f, 20f);

            Image background = inputGo.AddComponent<Image>();
            background.color = new Color(0.12f, 0.15f, 0.16f, 1f);
            InputField input = inputGo.AddComponent<InputField>();
            input.characterLimit = characterLimit;

            Text inputText = CreateRuntimeText(inputGo.transform, "Text", value, 18, Color.white);
            inputText.alignment = TextAnchor.MiddleLeft;
            SetRect(inputText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            inputText.rectTransform.offsetMin = new Vector2(10f, 4f);
            inputText.rectTransform.offsetMax = new Vector2(-10f, -4f);

            Text placeholderText = CreateRuntimeText(inputGo.transform, "Placeholder", value, 18, new Color(0.55f, 0.62f, 0.64f));
            placeholderText.alignment = TextAnchor.MiddleLeft;
            SetRect(placeholderText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            placeholderText.rectTransform.offsetMin = new Vector2(10f, 4f);
            placeholderText.rectTransform.offsetMax = new Vector2(-10f, -4f);

            input.textComponent = inputText;
            input.placeholder = placeholderText;
            input.text = value;
            return input;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            SetRect(rect, anchorMin, anchorMax, sizeDelta, anchoredPosition, new Vector2(0.5f, 0.5f));
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta, Vector2 anchoredPosition, Vector2 pivot)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
        }

        private GameObject FindChildGameObject(string childName)
        {
            Transform found = FindDeepChild(transform, childName);
            return found != null ? found.gameObject : null;
        }

        private Button FindChildButton(string childName)
        {
            Transform found = FindDeepChild(transform, childName);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private InputField FindChildInputField(string childName)
        {
            Transform found = FindDeepChild(transform, childName);
            return found != null ? found.GetComponent<InputField>() : null;
        }

        private Text FindChildText(string childName)
        {
            Transform found = FindDeepChild(transform, childName);
            return found != null ? found.GetComponent<Text>() : null;
        }

        private static Transform FindDeepChild(Transform parent, string targetName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            if (parent.name == targetName)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                Transform found = FindDeepChild(child, targetName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void EnsureEventSystemExists()
        {
            EventSystem eventSystem = Object.FindObjectOfType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemGo = new GameObject("EventSystem");
                eventSystem = eventSystemGo.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent("UnityEngine.InputSystem.UI.InputSystemUIInputModule") == null
                && eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                System.Type inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputSystemModuleType != null)
                {
                    eventSystem.gameObject.AddComponent(inputSystemModuleType);
                }
                else
                {
                    eventSystem.gameObject.AddComponent<StandaloneInputModule>();
                }
            }
        }
    }
}
