using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteRealm.World
{
    [DefaultExecutionOrder(-250)]
    [DisallowMultipleComponent]
    public class PhysicsRealismController : MonoBehaviour
    {
        [SerializeField] private int solverIterations = 10;
        [SerializeField] private int solverVelocityIterations = 4;
        [SerializeField] private float fixedDeltaTime = 1f / 60f;
        [SerializeField] private float maxDepenetrationVelocity = 8f;
        [SerializeField] private float bounceThreshold = 1.4f;
        [SerializeField] private float contactOffset = 0.012f;

        private void Awake()
        {
            Apply();
        }

        public void Apply()
        {
            Physics.defaultSolverIterations = Mathf.Max(1, solverIterations);
            Physics.defaultSolverVelocityIterations = Mathf.Max(1, solverVelocityIterations);
            Physics.defaultMaxDepenetrationVelocity = Mathf.Max(1f, maxDepenetrationVelocity);
            Physics.bounceThreshold = Mathf.Max(0f, bounceThreshold);
            Physics.defaultContactOffset = Mathf.Max(0.001f, contactOffset);
            Physics.queriesHitTriggers = true;
            Time.fixedDeltaTime = Mathf.Clamp(fixedDeltaTime, 1f / 120f, 1f / 30f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureControllerExists();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureControllerExists();
        }

        private static void EnsureControllerExists()
        {
            if (FindObjectOfType<PhysicsRealismController>() != null)
            {
                return;
            }

            if (GameObject.FindGameObjectWithTag("Player") == null)
            {
                return;
            }

            GameObject host = new GameObject("PhysicsRealism");
            host.AddComponent<PhysicsRealismController>();
        }
    }
}
