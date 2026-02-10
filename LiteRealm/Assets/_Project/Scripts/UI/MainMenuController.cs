using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Main panel")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Settings")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private SettingsMenuController settingsController;

        private void Awake()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
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

        private void OnPlayClicked()
        {
            SceneManager.LoadScene("Main");
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
    }
}
