using LiteRealm.Quests;
using UnityEngine;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    public class QuestLogUIController : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text questLogText;
        [SerializeField] private KeyCode toggleKey = KeyCode.J;
        [SerializeField] private QuestManager questManager;

        private bool visible;

        private void OnEnable()
        {
            if (questManager != null)
            {
                questManager.QuestsChanged += Refresh;
            }

            SetVisible(false);
            Refresh();
        }

        private void OnDisable()
        {
            if (questManager != null)
            {
                questManager.QuestsChanged -= Refresh;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                SetVisible(!visible);
            }
        }

        private void SetVisible(bool shouldShow)
        {
            visible = shouldShow;
            if (root != null)
            {
                root.SetActive(visible);
            }

            if (visible)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (questLogText == null || questManager == null)
            {
                return;
            }

            questLogText.text = questManager.BuildQuestLogText();
        }
    }
}
