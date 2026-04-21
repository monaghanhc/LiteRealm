using LiteRealm.Core;
using LiteRealm.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    public class GameProgressionController : MonoBehaviour
    {
        [SerializeField] private GameEventHub eventHub;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private Text objectiveText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private bool _won;

        private void Awake()
        {
            if (eventHub == null)
            {
                eventHub = FindObjectOfType<GameEventHub>();
            }

            EnsureUi();
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }
            if (objectiveText != null)
            {
                objectiveText.text = "Objective: Survive and defeat the Alpha Boss";
            }
        }

        private void OnEnable()
        {
            if (eventHub != null)
            {
                eventHub.BossKilled += OnBossKilled;
            }
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(Restart);
            }
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(ReturnToMenu);
            }
        }

        private void OnDisable()
        {
            if (eventHub != null)
            {
                eventHub.BossKilled -= OnBossKilled;
            }
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(Restart);
            }
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(ReturnToMenu);
            }
        }

        public bool IsWinShownForTests => _won;

        private void OnBossKilled(BossKilledEvent _)
        {
            if (_won)
            {
                return;
            }

            _won = true;
            if (objectiveText != null)
            {
                objectiveText.text = "Objective Complete: Alpha Boss defeated!";
            }

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Restart()
        {
            Time.timeScale = 1f;
            SaveSystem.ClearPendingResumeRequest();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void ReturnToMenu()
        {
            Time.timeScale = 1f;
            SaveSystem.ClearPendingResumeRequest();
            if (!string.IsNullOrWhiteSpace(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }

        private void EnsureUi()
        {
            if (objectiveText == null)
            {
                objectiveText = CreateObjectiveText();
            }

            if (victoryPanel == null)
            {
                victoryPanel = CreateVictoryPanel();
            }
        }

        private Text CreateObjectiveText()
        {
            GameObject go = new GameObject("ObjectiveText", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(700f, 28f);
            rect.anchoredPosition = new Vector2(0f, -60f);
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.95f, 0.95f, 0.75f, 1f);
            return text;
        }

        private GameObject CreateVictoryPanel()
        {
            GameObject panel = new GameObject("VictoryPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0f, 0.08f, 0.03f, 0.88f);

            Text title = CreateCenteredText(panel.transform, "VictoryText", "VICTORY", new Vector2(0f, 70f), 48);
            title.color = new Color(0.45f, 1f, 0.6f, 1f);
            CreateCenteredText(panel.transform, "VictorySubText", "You survived and defeated the Alpha Boss", new Vector2(0f, 20f), 24);

            restartButton = CreateButton(panel.transform, "VictoryRestartButton", "Restart", new Vector2(0f, -50f));
            mainMenuButton = CreateButton(panel.transform, "VictoryMainMenuButton", "Main Menu", new Vector2(0f, -110f));
            return panel;
        }

        private static Text CreateCenteredText(Transform parent, string name, string content, Vector2 anchoredPos, int size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 60f);
            rect.anchoredPosition = anchoredPos;

            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = content;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(240f, 44f);
            rect.anchoredPosition = anchoredPos;
            go.GetComponent<Image>().color = new Color(0.2f, 0.32f, 0.22f, 1f);
            Button button = go.GetComponent<Button>();

            Text text = CreateCenteredText(go.transform, "Text", label, Vector2.zero, 24);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            return button;
        }
    }
}
