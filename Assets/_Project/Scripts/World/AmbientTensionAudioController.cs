using LiteRealm.Core;
using LiteRealm.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteRealm.World
{
    [DisallowMultipleComponent]
    public class AmbientTensionAudioController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private DayNightCycleManager dayNight;
        [SerializeField] private PlayerStats playerStats;

        [Header("Clips")]
        [SerializeField] private AudioClip ambientLoopClip;
        [SerializeField] private bool createProceduralFallback = true;

        [Header("Mix")]
        [SerializeField] [Range(0f, 1f)] private float dayVolume = 0.16f;
        [SerializeField] [Range(0f, 1f)] private float nightVolume = 0.28f;
        [SerializeField] [Range(0f, 1f)] private float lowHealthTensionBoost = 0.14f;
        [SerializeField] private float dayPitch = 0.92f;
        [SerializeField] private float nightPitch = 0.72f;
        [SerializeField] private float transitionSpeed = 1.2f;

        private AudioClip runtimeAmbientClip;

        private void Awake()
        {
            ResolveReferences();
            EnsureAudioSource();
        }

        private void Start()
        {
            ResolveReferences();
            EnsureAudioSource();
            StartLoopIfNeeded();
        }

        private void Update()
        {
            if (audioSource == null)
            {
                return;
            }

            float targetVolume = dayNight != null && dayNight.IsNight ? nightVolume : dayVolume;
            if (playerStats != null)
            {
                float health01 = playerStats.GetHealth01();
                targetVolume += Mathf.Lerp(lowHealthTensionBoost, 0f, Mathf.Clamp01(health01 * 2f));
            }

            float targetPitch = dayNight != null && dayNight.IsNight ? nightPitch : dayPitch;
            audioSource.volume = Mathf.MoveTowards(audioSource.volume, Mathf.Clamp01(targetVolume), transitionSpeed * Time.deltaTime);
            audioSource.pitch = Mathf.MoveTowards(audioSource.pitch, Mathf.Max(0.1f, targetPitch), transitionSpeed * Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (runtimeAmbientClip == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeAmbientClip);
            }
            else
            {
                DestroyImmediate(runtimeAmbientClip);
            }
        }

        private void ResolveReferences()
        {
            if (dayNight == null)
            {
                dayNight = FindObjectOfType<DayNightCycleManager>();
            }

            if (playerStats == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerStats = player.GetComponent<PlayerStats>();
                }
            }
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;

            if (ambientLoopClip == null && runtimeAmbientClip == null && createProceduralFallback)
            {
                runtimeAmbientClip = ProceduralAudio.CreateAmbientWindLoopClip();
            }
        }

        private void StartLoopIfNeeded()
        {
            if (audioSource == null)
            {
                return;
            }

            AudioClip clip = ambientLoopClip != null ? ambientLoopClip : runtimeAmbientClip;
            if (clip == null)
            {
                return;
            }

            if (audioSource.clip != clip)
            {
                audioSource.clip = clip;
            }

            if (!audioSource.isPlaying)
            {
                audioSource.volume = 0f;
                audioSource.Play();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallRuntimeHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureRuntimeAtmosphere();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureRuntimeAtmosphere();
        }

        private static void EnsureRuntimeAtmosphere()
        {
            if (FindObjectOfType<AmbientTensionAudioController>() != null)
            {
                return;
            }

            if (GameObject.FindGameObjectWithTag("Player") == null)
            {
                return;
            }

            GameObject go = new GameObject("AmbientTensionAudio");
            go.AddComponent<AmbientTensionAudioController>();
        }
    }
}
