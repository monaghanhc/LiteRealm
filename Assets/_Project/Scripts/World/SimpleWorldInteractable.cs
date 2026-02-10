using LiteRealm.Core;
using LiteRealm.Player;
using UnityEngine;

namespace LiteRealm.World
{
    public class SimpleWorldInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string promptText = "Press E to inspect";
        [SerializeField] [TextArea] private string interactionMessage = "You found something interesting.";

        public string GetInteractionPrompt(PlayerInteractor interactor)
        {
            return promptText;
        }

        public void Interact(PlayerInteractor interactor)
        {
            Debug.Log($"[Interactable] {interactionMessage}", this);
        }
    }
}
