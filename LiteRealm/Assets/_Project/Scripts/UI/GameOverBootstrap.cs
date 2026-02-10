using LiteRealm.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    public static class GameOverBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureGameOverController()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            if (scene.name == "MainMenu")
            {
                return;
            }

            if (Object.FindObjectOfType<PlayerStats>() == null)
            {
                return;
            }

            if (FindInScene<GameOverController>(scene) != null)
            {
                return;
            }

            Canvas canvas = FindOrCreateCanvas();
            if (canvas == null)
            {
                Debug.LogError("GameOverBootstrap: Failed to find or create a Canvas for GameOverController.");
                return;
            }

            canvas.gameObject.AddComponent<GameOverController>();
        }

        private static Canvas FindOrCreateCanvas()
        {
            GameObject namedCanvas = GameObject.Find("UI Canvas");
            if (namedCanvas != null)
            {
                Canvas existing = namedCanvas.GetComponent<Canvas>();
                if (existing != null)
                {
                    return existing;
                }
            }

            Canvas anyCanvas = FindInScene<Canvas>(SceneManager.GetActiveScene());
            if (anyCanvas != null)
            {
                return anyCanvas;
            }

            GameObject go = new GameObject("UI Canvas", typeof(RectTransform));
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T found = roots[i].GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
