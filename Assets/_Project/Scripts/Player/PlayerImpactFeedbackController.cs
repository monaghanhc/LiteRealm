using LiteRealm.CameraSystem;
using LiteRealm.Combat;
using LiteRealm.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteRealm.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerImpactFeedbackController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerCameraController cameraController;
        [SerializeField] private WeaponManager weaponManager;
        [SerializeField] private GameEventHub eventHub;

        [Header("Camera Feedback")]
        [SerializeField] private float hurtShakeAmount = 0.16f;
        [SerializeField] private float hitConfirmShakeAmount = 0.025f;
        [SerializeField] private float killConfirmShakeAmount = 0.06f;
        [SerializeField] private float weaponFireShakeAmount = 0.006f;
        [SerializeField] private float hurtShakeCooldown = 0.12f;
        [SerializeField] private float hitConfirmShakeCooldown = 0.035f;
        [SerializeField] private float weaponFireShakeCooldown = 0.045f;

        private float nextAllowedHurtShakeTime;
        private float nextAllowedHitShakeTime;
        private float nextAllowedFireShakeTime;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void Start()
        {
            ResolveReferences();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveReferences()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (weaponManager == null)
            {
                weaponManager = GetComponent<WeaponManager>();
            }

            if (cameraController == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    cameraController = mainCamera.GetComponent<PlayerCameraController>();
                }
            }

            if (eventHub == null)
            {
                eventHub = FindObjectOfType<GameEventHub>();
            }
        }

        private void Subscribe()
        {
            if (playerStats != null)
            {
                playerStats.Damaged -= OnPlayerDamaged;
                playerStats.Damaged += OnPlayerDamaged;
            }

            if (weaponManager != null)
            {
                weaponManager.WeaponFired -= OnWeaponFired;
                weaponManager.WeaponFired += OnWeaponFired;
            }

            if (eventHub != null)
            {
                eventHub.DamageDealt -= OnDamageDealt;
                eventHub.DamageDealt += OnDamageDealt;
            }
        }

        private void Unsubscribe()
        {
            if (playerStats != null)
            {
                playerStats.Damaged -= OnPlayerDamaged;
            }

            if (weaponManager != null)
            {
                weaponManager.WeaponFired -= OnWeaponFired;
            }

            if (eventHub != null)
            {
                eventHub.DamageDealt -= OnDamageDealt;
            }
        }

        private void OnPlayerDamaged(DamageInfo damageInfo)
        {
            if (damageInfo.Amount <= 0f || Time.time < nextAllowedHurtShakeTime)
            {
                return;
            }

            nextAllowedHurtShakeTime = Time.time + Mathf.Max(0f, hurtShakeCooldown);
            AddCameraShake(hurtShakeAmount);
        }

        private void OnWeaponFired(WeaponBase weapon)
        {
            if (weaponFireShakeAmount <= 0f || Time.time < nextAllowedFireShakeTime)
            {
                return;
            }

            nextAllowedFireShakeTime = Time.time + Mathf.Max(0f, weaponFireShakeCooldown);
            AddCameraShake(weaponFireShakeAmount);
        }

        private void OnDamageDealt(DamageDealtEvent data)
        {
            if (data.Instigator != gameObject || data.Amount <= 0f || Time.time < nextAllowedHitShakeTime)
            {
                return;
            }

            nextAllowedHitShakeTime = Time.time + Mathf.Max(0f, hitConfirmShakeCooldown);
            AddCameraShake(data.Killed ? killConfirmShakeAmount : hitConfirmShakeAmount);
        }

        private void AddCameraShake(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            if (cameraController == null)
            {
                ResolveReferences();
            }

            if (cameraController != null)
            {
                cameraController.AddShake(amount);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallRuntimeHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureOnPlayer();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureOnPlayer();
        }

        private static void EnsureOnPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || player.GetComponent<PlayerStats>() == null)
            {
                return;
            }

            if (player.GetComponent<PlayerImpactFeedbackController>() == null)
            {
                player.AddComponent<PlayerImpactFeedbackController>();
            }
        }
    }
}
