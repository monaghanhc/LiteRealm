using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject settingsPanel;

        [Header("Main menu buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitToMainMenuButton;
        [SerializeField] private Button exitDesktopButton;

        [Header("Settings")]
        [SerializeField] private SettingsMenuController settingsController;
        [SerializeField] private Button settingsBackButton;

        private bool _isPaused;

        private void Awake()
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(OnResume);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (exitToMainMenuButton != null)
            {
                exitToMainMenuButton.onClick.AddListener(OnExitToMainMenu);
            }

            if (exitDesktopButton != null)
            {
                exitDesktopButton.onClick.AddListener(OnExitDesktop);
            }

            if (settingsBackButton != null)
            {
                settingsBackButton.onClick.AddListener(OnSettingsBack);
            }

            if (settingsController != null)
            {
                settingsController.OnBackClicked += OnSettingsBack;
            }
        }

        private void OnDestroy()
        {
            if (settingsController != null)
            {
                settingsController.OnBackClicked -= OnSettingsBack;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_isPaused)
                {
                    if (settingsPanel != null && settingsPanel.activeSelf)
                    {
                        OnSettingsBack();
                    }
                    else
                    {
                        OnResume();
                    }
                }
                else
                {
                    OpenPauseMenu();
                }
            }
        }

        private void OpenPauseMenu()
        {
            _isPaused = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (pausePanel != null)
            {
                pausePanel.SetActive(true);
            }

            if (mainMenu != null)
            {
                mainMenu.SetActive(true);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void ClosePauseMenu()
        {
            _isPaused = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (pausePanel != null)
            {
                pausePanel.SetActive(false);
            }
        }

        private void OnResume()
        {
            ClosePauseMenu();
        }

        private void OnSettingsClicked()
        {
            if (mainMenu != null)
            {
                mainMenu.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
                if (settingsController != null)
                {
                    settingsController.RefreshFromSettings();
                }
            }
        }

        private void OnSettingsBack()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            if (mainMenu != null)
            {
                mainMenu.SetActive(true);
            }

            if (settingsController != null)
            {
                GameSettings.Save();
            }
        }

        private void OnExitToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        private void OnExitDesktop()
        {
            Application.Quit();
        }
    }
}
