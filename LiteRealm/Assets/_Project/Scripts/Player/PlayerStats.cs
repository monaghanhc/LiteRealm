using System;
using LiteRealm.Core;
using UnityEngine;

namespace LiteRealm.Player
{
    [Serializable]
    public struct PlayerStatsSnapshot
    {
        public float Health;
        public float MaxHealth;
        public float Stamina;
        public float MaxStamina;
        public float Hunger;
        public float MaxHunger;
        public float Thirst;
        public float MaxThirst;
        public bool GodMode;
    }

    [Serializable]
    public struct PlayerStatsState
    {
        public float Health;
        public float Stamina;
        public float Hunger;
        public float Thirst;
        public bool GodMode;
    }

    public class PlayerStats : MonoBehaviour, IDamageable
    {
        [Header("Maximums")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float maxThirst = 100f;

        [Header("Drain")]
        [SerializeField] private float hungerDrainPerSecond = 0.25f;
        [SerializeField] private float thirstDrainPerSecond = 0.35f;

        [Header("Stamina")]
        [SerializeField] private float baseStaminaRegenPerSecond = 12f;
        [SerializeField] [Range(0f, 1f)] private float lowResourceThreshold = 0.25f;
        [SerializeField] [Range(0f, 1f)] private float lowResourceRegenMultiplier = 0.35f;
        [SerializeField] private float regenDelayAfterUse = 0.2f;

        public event Action<PlayerStatsSnapshot> StatsChanged;
        public event Action Died;

        public float CurrentHealth { get; private set; }
        public float CurrentStamina { get; private set; }
        public float CurrentHunger { get; private set; }
        public float CurrentThirst { get; private set; }
        public bool GodMode { get; private set; }

        public bool IsDead => CurrentHealth <= 0f;
        public Transform DamageTransform => transform;

        private float lastStaminaUseTime;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            CurrentStamina = maxStamina;
            CurrentHunger = maxHunger;
            CurrentThirst = maxThirst;
            NotifyChanged();
        }

        private void Update()
        {
            TickSurvival(Time.deltaTime);
        }

        public void ApplyDamage(DamageInfo damageInfo)
        {
            if (GodMode || IsDead || damageInfo.Amount <= 0f)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageInfo.Amount);
            NotifyChanged();

            if (CurrentHealth <= 0f)
            {
                Died?.Invoke();
            }
        }

        public void RestoreFromConsumable(float health, float stamina, float hunger, float thirst)
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth + Mathf.Max(0f, health), 0f, maxHealth);
            CurrentStamina = Mathf.Clamp(CurrentStamina + Mathf.Max(0f, stamina), 0f, maxStamina);
            CurrentHunger = Mathf.Clamp(CurrentHunger + Mathf.Max(0f, hunger), 0f, maxHunger);
            CurrentThirst = Mathf.Clamp(CurrentThirst + Mathf.Max(0f, thirst), 0f, maxThirst);
            NotifyChanged();
        }

        public bool ConsumeStamina(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }

            if (CurrentStamina <= 0f)
            {
                return false;
            }

            CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
            lastStaminaUseTime = Time.time;
            NotifyChanged();
            return CurrentStamina > 0f;
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDead)
            {
                return;
            }

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            NotifyChanged();
        }

        public void SetGodMode(bool enabled)
        {
            GodMode = enabled;
            NotifyChanged();
        }

        public void ToggleGodMode()
        {
            SetGodMode(!GodMode);
        }

        public PlayerStatsSnapshot GetSnapshot()
        {
            return new PlayerStatsSnapshot
            {
                Health = CurrentHealth,
                MaxHealth = maxHealth,
                Stamina = CurrentStamina,
                MaxStamina = maxStamina,
                Hunger = CurrentHunger,
                MaxHunger = maxHunger,
                Thirst = CurrentThirst,
                MaxThirst = maxThirst,
                GodMode = GodMode
            };
        }

        public PlayerStatsState CaptureState()
        {
            return new PlayerStatsState
            {
                Health = CurrentHealth,
                Stamina = CurrentStamina,
                Hunger = CurrentHunger,
                Thirst = CurrentThirst,
                GodMode = GodMode
            };
        }

        public void RestoreState(PlayerStatsState state)
        {
            CurrentHealth = Mathf.Clamp(state.Health, 0f, maxHealth);
            CurrentStamina = Mathf.Clamp(state.Stamina, 0f, maxStamina);
            CurrentHunger = Mathf.Clamp(state.Hunger, 0f, maxHunger);
            CurrentThirst = Mathf.Clamp(state.Thirst, 0f, maxThirst);
            GodMode = state.GodMode;
            NotifyChanged();
        }

        public float GetHealth01()
        {
            return maxHealth <= 0f ? 0f : CurrentHealth / maxHealth;
        }

        public float GetStamina01()
        {
            return maxStamina <= 0f ? 0f : CurrentStamina / maxStamina;
        }

        public float GetHunger01()
        {
            return maxHunger <= 0f ? 0f : CurrentHunger / maxHunger;
        }

        public float GetThirst01()
        {
            return maxThirst <= 0f ? 0f : CurrentThirst / maxThirst;
        }

        private void TickSurvival(float deltaTime)
        {
            if (deltaTime <= 0f || IsDead)
            {
                return;
            }

            float previousHunger = CurrentHunger;
            float previousThirst = CurrentThirst;
            float previousStamina = CurrentStamina;

            CurrentHunger = Mathf.Max(0f, CurrentHunger - hungerDrainPerSecond * deltaTime);
            CurrentThirst = Mathf.Max(0f, CurrentThirst - thirstDrainPerSecond * deltaTime);

            bool canRegenStamina = (Time.time - lastStaminaUseTime) >= regenDelayAfterUse;
            if (canRegenStamina && CurrentStamina < maxStamina)
            {
                float regenMultiplier = 1f;
                if (GetHunger01() <= lowResourceThreshold || GetThirst01() <= lowResourceThreshold)
                {
                    regenMultiplier = lowResourceRegenMultiplier;
                }

                CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + baseStaminaRegenPerSecond * regenMultiplier * deltaTime);
            }

            if (!Mathf.Approximately(previousHunger, CurrentHunger)
                || !Mathf.Approximately(previousThirst, CurrentThirst)
                || !Mathf.Approximately(previousStamina, CurrentStamina))
            {
                NotifyChanged();
            }
        }

        private void NotifyChanged()
        {
            StatsChanged?.Invoke(GetSnapshot());
        }
    }
}
