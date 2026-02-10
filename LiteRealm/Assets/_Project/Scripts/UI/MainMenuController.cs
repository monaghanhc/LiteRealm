using LiteRealm.Saving;
using UnityEngine;
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
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text saveStatusText;

        [Header("Settings")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private SettingsMenuController settingsController;

        [Header("Scene")]
        [SerializeField] private string gameplaySceneName = "Main";

        private void Awake()
        {
            ResolveFallbackReferences();

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

            if (settingsController != null)
            {
                settingsController.RefreshFromSettings();
            }
        }

        private void OnNewGameClicked()
        {
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

            SaveSystem.RequestResumeOnNextSceneLoad();
            SceneManager.LoadScene(gameplaySceneName);
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

            if (newGameButton == null)
            {
                newGameButton = FindChildButton("NewGameButton") ?? FindChildButton("PlayButton");
            }

            if (resumeButton == null)
            {
                resumeButton = FindChildButton("ResumeButton");
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
    }
}
