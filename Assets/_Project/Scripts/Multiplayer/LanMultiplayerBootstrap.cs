using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteRealm.Multiplayer
{
    public static class LanMultiplayerBootstrap
    {
        private static bool registered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            if (registered)
            {
                return;
            }

            registered = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryCreateManager();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryCreateManager();
        }

        private static void TryCreateManager()
        {
            PendingLanSession session = LanSessionLauncher.PeekPendingSession();
            if (!session.IsActive || Object.FindObjectOfType<LanMultiplayerManager>() != null)
            {
                return;
            }

            if (FindPlayer() == null)
            {
                return;
            }

            GameObject manager = new GameObject("LanMultiplayerManager");
            manager.AddComponent<LanMultiplayerManager>();
        }

        private static GameObject FindPlayer()
        {
            try
            {
                return GameObject.FindGameObjectWithTag("Player");
            }
            catch (UnityException)
            {
                return null;
            }
        }
    }
}
