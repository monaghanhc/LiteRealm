using LiteRealm.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    public class GameOverController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Text gameOverText;

        [Header("References")]
        [SerializeField] private PlayerStats playerStats;

        private bool gameOverShown;

        private void Awake()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        private void Start()
        {
            if (playerStats == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerStats = player.GetComponent<PlayerStats>();
                }
            }

            if (playerStats == null)
            {
                playerStats = Object.FindObjectOfType<PlayerStats>();
            }

            if (playerStats != null)
            {
                playerStats.Died += OnPlayerDied;
            }
        }

        private void OnDestroy()
        {
            if (playerStats != null)
            {
                playerStats.Died -= OnPlayerDied;
            }
        }

        private void OnPlayerDied()
        {
            if (gameOverShown)
            {
                return;
            }

            gameOverShown = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            if (gameOverText != null)
            {
                gameOverText.text = "GAME OVER";
            }
        }

        private void OnRestartClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnMainMenuClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }
}
