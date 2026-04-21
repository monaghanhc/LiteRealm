using LiteRealm.Combat;
using LiteRealm.Gameplay;
using LiteRealm.Player;
using LiteRealm.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteRealm.Core
{
    public static class PhaseOneRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallPhaseOneControllers()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureSceneControllers();
        }

        private static void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            EnsureSceneControllers();
        }

        private static void EnsureSceneControllers()
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                player = GameObject.Find("Player");
            }

            if (player != null)
            {
                if (player.GetComponent<BuildSystemController>() == null)
                {
                    player.AddComponent<BuildSystemController>();
                }
            }

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                return;
            }

            if (canvas.GetComponent<CombatFeedbackUIController>() == null)
            {
                canvas.gameObject.AddComponent<CombatFeedbackUIController>();
            }

            if (canvas.GetComponent<GameProgressionController>() == null)
            {
                canvas.gameObject.AddComponent<GameProgressionController>();
            }

            if (canvas.GetComponent<GameOverController>() == null)
            {
                canvas.gameObject.AddComponent<GameOverController>();
            }

            WeaponManager manager = player != null ? player.GetComponent<WeaponManager>() : null;
            CombatFeedbackUIController feedback = canvas.GetComponent<CombatFeedbackUIController>();
            if (manager != null && feedback != null)
            {
                WeaponBase active = manager.ActiveWeapon;
                if (active != null)
                {
                    active.Fired -= OnWeaponFired;
                    active.Fired += OnWeaponFired;
                }
            }
        }

        private static void OnWeaponFired(WeaponBase weapon)
        {
            if (!(weapon is HitscanRifle rifle))
            {
                return;
            }

            bool killed;
            if (!rifle.TryConsumeLastHitResult(out killed))
            {
                return;
            }

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            CombatFeedbackUIController feedback = canvas != null ? canvas.GetComponent<CombatFeedbackUIController>() : null;
            feedback?.ShowHitMarker(killed);
        }
    }
}
