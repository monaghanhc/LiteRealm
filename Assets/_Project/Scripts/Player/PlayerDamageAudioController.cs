using LiteRealm.Core;
using UnityEngine;

namespace LiteRealm.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerDamageAudioController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private AudioSource audioSource;

        [Header("Hurt Audio")]
        [SerializeField] private AudioClip[] hurtClips;
        [SerializeField] [Range(0f, 1f)] private float hurtVolume = 0.85f;
        [SerializeField] private Vector2 hurtPitchRange = new Vector2(0.92f, 1.08f);
        [SerializeField] private float hurtCooldown = 0.25f;
        [SerializeField] private bool createProceduralFallback = true;

        private AudioClip runtimeHurtClip;
        private float nextAllowedHurtTime;

        private void Awake()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            if (createProceduralFallback && (hurtClips == null || hurtClips.Length == 0))
            {
                runtimeHurtClip = ProceduralAudio.CreatePlayerHurtClip();
            }
        }

        private void OnEnable()
        {
            if (playerStats != null)
            {
                playerStats.Damaged += OnPlayerDamaged;
            }
        }

        private void OnDisable()
        {
            if (playerStats != null)
            {
                playerStats.Damaged -= OnPlayerDamaged;
            }
        }

        private void OnDestroy()
        {
            if (runtimeHurtClip != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(runtimeHurtClip);
                }
                else
                {
                    DestroyImmediate(runtimeHurtClip);
                }
            }
        }

        private void OnPlayerDamaged(DamageInfo damageInfo)
        {
            if (damageInfo.Amount <= 0f || Time.time < nextAllowedHurtTime)
            {
                return;
            }

            AudioClip clip = SelectClip(hurtClips, runtimeHurtClip);
            if (audioSource == null || clip == null)
            {
                return;
            }

            nextAllowedHurtTime = Time.time + Mathf.Max(0f, hurtCooldown);
            audioSource.pitch = Random.Range(
                Mathf.Min(hurtPitchRange.x, hurtPitchRange.y),
                Mathf.Max(hurtPitchRange.x, hurtPitchRange.y));
            audioSource.PlayOneShot(clip, hurtVolume);
        }

        private static AudioClip SelectClip(AudioClip[] clips, AudioClip fallback)
        {
            if (clips != null && clips.Length > 0)
            {
                AudioClip selected = clips[Random.Range(0, clips.Length)];
                if (selected != null)
                {
                    return selected;
                }
            }

            return fallback;
        }
    }
}
