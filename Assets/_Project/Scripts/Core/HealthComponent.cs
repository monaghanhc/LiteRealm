using System;
using UnityEngine;

namespace LiteRealm.Core
{
    public class HealthComponent : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool destroyOnDeath;
        [SerializeField] private bool disableGameObjectOnDeath = true;

        public event Action<DamageInfo> Damaged;
        public event Action Died;
        public event Action<float, float> HealthChanged;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead { get; private set; }
        public Transform DamageTransform => transform;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void ApplyDamage(DamageInfo damageInfo)
        {
            if (IsDead || damageInfo.Amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageInfo.Amount);
            Damaged?.Invoke(damageInfo);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);

            if (CurrentHealth <= 0f)
            {
                IsDead = true;
                Died?.Invoke();

                if (disableGameObjectOnDeath)
                {
                    gameObject.SetActive(false);
                }

                if (destroyOnDeath)
                {
                    Destroy(gameObject);
                }
            }
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void Revive(float healthPercent = 1f)
        {
            healthPercent = Mathf.Clamp01(healthPercent);
            IsDead = false;
            CurrentHealth = maxHealth * healthPercent;
            gameObject.SetActive(true);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }
    }
}
