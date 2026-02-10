using UnityEngine;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text promptText;

        private void Awake()
        {
            if (root == null)
            {
                root = gameObject;
            }

            Hide();
        }

        public void Show(string message)
        {
            if (promptText != null)
            {
                promptText.text = message;
            }

            if (root != null)
            {
                root.SetActive(true);
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }
}
