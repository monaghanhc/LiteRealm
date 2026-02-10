using LiteRealm.Player;
using UnityEngine;

namespace LiteRealm.Core
{
    public interface IDamageable
    {
        bool IsDead { get; }
        Transform DamageTransform { get; }
        void ApplyDamage(DamageInfo damageInfo);
    }

    public interface IInteractable
    {
        string GetInteractionPrompt(PlayerInteractor interactor);
        void Interact(PlayerInteractor interactor);
    }
}
