using System.Collections;
using LiteRealm.Combat;
using LiteRealm.Player;
using LiteRealm.Saving;
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
        [SerializeField] private PlayerController playerController;
        [SerializeField] private WeaponManager weaponManager;
        [SerializeField] private PlayerInteractor playerInteractor;
        [SerializeField] private ExplorationInput explorationInput;

        [Header("Flow")]
        [SerializeField] private bool disablePlayerControlOnDeath = true;
        [SerializeField] private bool autoReturnToMainMenu = true;
        [SerializeField] private float autoReturnDelaySeconds = 4f;
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string gameOverMessage = "YOU DIED";

        private bool gameOverShown;
        private Coroutine autoReturnRoutine;

        private void Awake()
        {
            ResolveUiReferences();

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
            ResolvePlayerReferences();
            if (playerStats != null)
            {
                playerStats.Died += OnPlayerDied;
            }

            if (playerStats != null && playerStats.IsDead)
            {
                OnPlayerDied();
            }
        }

        private void Update()
        {
            if (!gameOverShown && playerStats != null && playerStats.IsDead)
            {
                OnPlayerDied();
            }
        }

        private void OnDestroy()
        {
            if (playerStats != null)
            {
                playerStats.Died -= OnPlayerDied;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
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

            if (disablePlayerControlOnDeath)
            {
                DisablePlayerControls();
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            if (gameOverText != null)
            {
                gameOverText.text = gameOverMessage;
            }

            if (autoReturnToMainMenu)
            {
                if (autoReturnRoutine != null)
                {
                    StopCoroutine(autoReturnRoutine);
                }

                autoReturnRoutine = StartCoroutine(AutoReturnToMenuRoutine());
            }
        }

        private void OnRestartClicked()
        {
            if (autoReturnRoutine != null)
            {
                StopCoroutine(autoReturnRoutine);
                autoReturnRoutine = null;
            }

            Time.timeScale = 1f;
            SaveSystem.ClearPendingResumeRequest();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnMainMenuClicked()
        {
            if (autoReturnRoutine != null)
            {
                StopCoroutine(autoReturnRoutine);
                autoReturnRoutine = null;
            }

            LoadMainMenu();
        }

        private IEnumerator AutoReturnToMenuRoutine()
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, autoReturnDelaySeconds));
            LoadMainMenu();
        }

        private void LoadMainMenu()
        {
            Time.timeScale = 1f;
            SaveSystem.ClearPendingResumeRequest();

            if (!string.IsNullOrWhiteSpace(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
                return;
            }

            if (SceneManager.sceneCountInBuildSettings > 0)
            {
                SceneManager.LoadScene(0);
                return;
            }

            Debug.LogError("GameOverController: Unable to load main menu scene. Check Build Settings.");
        }

        private void DisablePlayerControls()
        {
            ResolvePlayerReferences();

            if (playerController != null)
            {
                playerController.enabled = false;
            }

            if (weaponManager != null)
            {
                weaponManager.enabled = false;
            }

            if (playerInteractor != null)
            {
                playerInteractor.enabled = false;
            }

            if (explorationInput != null)
            {
                explorationInput.enabled = false;
            }
        }

        private void ResolvePlayerReferences()
        {
            if (playerStats != null
                && playerController != null
                && weaponManager != null
                && playerInteractor != null
                && explorationInput != null)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                PlayerStats foundStats = Object.FindObjectOfType<PlayerStats>();
                if (foundStats != null)
                {
                    player = foundStats.gameObject;
                }
            }

            if (player == null)
            {
                return;
            }

            if (playerStats == null)
            {
                playerStats = player.GetComponent<PlayerStats>();
            }

            if (playerController == null)
            {
                playerController = player.GetComponent<PlayerController>();
            }

            if (weaponManager == null)
            {
                weaponManager = player.GetComponent<WeaponManager>();
            }

            if (playerInteractor == null)
            {
                playerInteractor = player.GetComponent<PlayerInteractor>();
            }

            if (explorationInput == null)
            {
                explorationInput = player.GetComponent<ExplorationInput>();
            }
        }

        private void ResolveUiReferences()
        {
            if (gameOverPanel == null)
            {
                gameOverPanel = FindChildByName(transform, "GameOverPanel");
            }

            if (restartButton == null)
            {
                GameObject restart = FindChildByName(transform, "RestartButton");
                if (restart != null)
                {
                    restartButton = restart.GetComponent<Button>();
                }
            }

            if (mainMenuButton == null)
            {
                GameObject mainMenu = FindChildByName(transform, "MainMenuButton");
                if (mainMenu != null)
                {
                    mainMenuButton = mainMenu.GetComponent<Button>();
                }
            }

            if (gameOverText == null)
            {
                GameObject textObj = FindChildByName(transform, "GameOverText");
                if (textObj != null)
                {
                    gameOverText = textObj.GetComponent<Text>();
                }
            }

            if (gameOverPanel == null)
            {
                CreateRuntimeGameOverPanel();
            }
        }

        private void CreateRuntimeGameOverPanel()
        {
            GameObject panel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image bg = panel.GetComponent<Image>();
            bg.color = new Color(0.08f, 0f, 0f, 0.85f);
            bg.raycastTarget = true;

            GameObject textGo = new GameObject("GameOverText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panel.transform, false);
            RectTransform textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(720f, 120f);
            textRect.anchoredPosition = new Vector2(0f, 40f);

            Text title = textGo.GetComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 46;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(0.95f, 0.25f, 0.25f, 1f);
            title.text = gameOverMessage;

            GameObject subtextGo = new GameObject("GameOverSubtext", typeof(RectTransform), typeof(Text));
            subtextGo.transform.SetParent(panel.transform, false);
            RectTransform subtextRect = subtextGo.GetComponent<RectTransform>();
            subtextRect.anchorMin = new Vector2(0.5f, 0.5f);
            subtextRect.anchorMax = new Vector2(0.5f, 0.5f);
            subtextRect.pivot = new Vector2(0.5f, 0.5f);
            subtextRect.sizeDelta = new Vector2(720f, 60f);
            subtextRect.anchoredPosition = new Vector2(0f, -26f);

            Text subtext = subtextGo.GetComponent<Text>();
            subtext.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subtext.fontSize = 24;
            subtext.alignment = TextAnchor.MiddleCenter;
            subtext.color = new Color(0.92f, 0.92f, 0.92f, 1f);
            subtext.text = "Returning to Main Menu...";

            gameOverPanel = panel;
            gameOverText = title;
        }

        private static GameObject FindChildByName(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            if (root.name == childName)
            {
                return root.gameObject;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                GameObject found = FindChildByName(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
