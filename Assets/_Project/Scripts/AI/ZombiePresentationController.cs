using LiteRealm.Core;
using UnityEngine;

namespace LiteRealm.AI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HealthComponent))]
    public class ZombiePresentationController : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] private Animator animator;
        [SerializeField] private string isMovingParameter = "isMoving";
        [SerializeField] private string isAttackingParameter = "isAttacking";
        [SerializeField] private string takeHitTrigger = "takeHit";
        [SerializeField] private string dieTrigger = "die";
        [SerializeField] private string speedParameter = "speed";
        [SerializeField] private float hitTriggerCooldown = 0.16f;

        [Header("Procedural Fallback")]
        [SerializeField] private bool animateModelWhenNoAnimatorController = true;
        [SerializeField] private Transform proceduralModelRoot;
        [SerializeField] private float walkBobHeight = 0.055f;
        [SerializeField] private float walkSwayDegrees = 5f;
        [SerializeField] private float attackLungeDistance = 0.18f;
        [SerializeField] private float hitRecoilDistance = 0.12f;
        [SerializeField] private float deathFallDegrees = 82f;
        [SerializeField] private float fallbackBlendSpeed = 8f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] attackClips;
        [SerializeField] private AudioClip[] hitClips;
        [SerializeField] private AudioClip[] deathClips;
        [SerializeField] [Range(0f, 1f)] private float attackVolume = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float hitVolume = 0.75f;
        [SerializeField] [Range(0f, 1f)] private float deathVolume = 0.9f;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);
        [SerializeField] private float attackAudioCooldown = 0.35f;
        [SerializeField] private float hitAudioCooldown = 0.12f;
        [SerializeField] private bool playAttackSoundOnAttackStart = true;
        [SerializeField] private bool createProceduralFallbackClips = true;

        private HealthComponent health;
        private AudioClip runtimeAttackClip;
        private AudioClip runtimeHitClip;
        private AudioClip runtimeDeathClip;
        private Vector3 proceduralBaseLocalPosition;
        private Quaternion proceduralBaseLocalRotation;
        private bool proceduralBaseCaptured;
        private bool moving;
        private bool attacking;
        private bool deathPlayed;
        private float currentSpeed;
        private float hitPulse;
        private float attackPulse;
        private float deathBlend;
        private float nextHitTriggerTime;
        private float nextAttackAudioTime;
        private float nextHitAudioTime;

        private int isMovingHash;
        private int isAttackingHash;
        private int takeHitHash;
        private int dieHash;
        private int speedHash;
        private bool canDriveAnimator;
        private bool hasIsMovingParameter;
        private bool hasIsAttackingParameter;
        private bool hasTakeHitTrigger;
        private bool hasDieTrigger;
        private bool hasSpeedParameter;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (health == null)
            {
                health = GetComponent<HealthComponent>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;

            isMovingHash = Animator.StringToHash(isMovingParameter);
            isAttackingHash = Animator.StringToHash(isAttackingParameter);
            takeHitHash = Animator.StringToHash(takeHitTrigger);
            dieHash = Animator.StringToHash(dieTrigger);
            speedHash = Animator.StringToHash(speedParameter);
            CacheAnimatorParameters();

            ResolveProceduralRoot();
            CreateFallbackClipsIfNeeded();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += OnDamaged;
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
            }
        }

        private void OnDestroy()
        {
            DestroyRuntimeClip(runtimeAttackClip);
            DestroyRuntimeClip(runtimeHitClip);
            DestroyRuntimeClip(runtimeDeathClip);
        }

        private void LateUpdate()
        {
            TickProceduralFallback(Time.deltaTime);
        }

        public void SetMoving(bool isMoving, float speed)
        {
            moving = isMoving && !deathPlayed;
            currentSpeed = Mathf.Max(0f, speed);

            if (!canDriveAnimator)
            {
                return;
            }

            if (hasIsMovingParameter)
            {
                animator.SetBool(isMovingHash, moving);
            }

            if (hasSpeedParameter)
            {
                animator.SetFloat(speedHash, currentSpeed);
            }
        }

        public void SetAttacking(bool isAttacking)
        {
            bool next = isAttacking && !deathPlayed;
            if (attacking == next)
            {
                return;
            }

            attacking = next;
            if (attacking)
            {
                attackPulse = 1f;
                if (playAttackSoundOnAttackStart)
                {
                    PlayAttackSound();
                }
            }

            if (canDriveAnimator && hasIsAttackingParameter)
            {
                animator.SetBool(isAttackingHash, attacking);
            }
        }

        public void PlayAttackSound()
        {
            if (Time.time < nextAttackAudioTime)
            {
                return;
            }

            nextAttackAudioTime = Time.time + Mathf.Max(0f, attackAudioCooldown);
            PlayRandomOneShot(attackClips, runtimeAttackClip, attackVolume);
        }

        public void PlayHitReaction(DamageInfo damageInfo)
        {
            if (deathPlayed || (health != null && (health.IsDead || health.CurrentHealth <= 0f)) || damageInfo.Amount <= 0f)
            {
                return;
            }

            hitPulse = 1f;
            if (canDriveAnimator && hasTakeHitTrigger && Time.time >= nextHitTriggerTime)
            {
                nextHitTriggerTime = Time.time + Mathf.Max(0f, hitTriggerCooldown);
                animator.ResetTrigger(takeHitHash);
                animator.SetTrigger(takeHitHash);
            }

            if (Time.time >= nextHitAudioTime)
            {
                nextHitAudioTime = Time.time + Mathf.Max(0f, hitAudioCooldown);
                PlayRandomOneShot(hitClips, runtimeHitClip, hitVolume);
            }
        }

        public void PlayDeath()
        {
            if (deathPlayed)
            {
                return;
            }

            deathPlayed = true;
            moving = false;
            attacking = false;

            if (canDriveAnimator)
            {
                if (hasIsMovingParameter)
                {
                    animator.SetBool(isMovingHash, false);
                }

                if (hasIsAttackingParameter)
                {
                    animator.SetBool(isAttackingHash, false);
                }

                if (hasTakeHitTrigger)
                {
                    animator.ResetTrigger(takeHitHash);
                }

                if (hasDieTrigger)
                {
                    animator.SetTrigger(dieHash);
                }
            }

            PlayRandomOneShot(deathClips, runtimeDeathClip, deathVolume);
        }

        private void OnDamaged(DamageInfo damageInfo)
        {
            PlayHitReaction(damageInfo);
        }

        private void OnDied()
        {
            PlayDeath();
        }

        private void ResolveProceduralRoot()
        {
            if (proceduralModelRoot == null)
            {
                Transform model = transform.Find("Model");
                if (model != null)
                {
                    proceduralModelRoot = model;
                }
            }

            if (proceduralModelRoot == null && transform.childCount > 0)
            {
                proceduralModelRoot = transform.GetChild(0);
            }

            if (proceduralModelRoot != null)
            {
                proceduralBaseLocalPosition = proceduralModelRoot.localPosition;
                proceduralBaseLocalRotation = proceduralModelRoot.localRotation;
                proceduralBaseCaptured = true;
            }
        }

        private void CacheAnimatorParameters()
        {
            canDriveAnimator = animator != null && animator.runtimeAnimatorController != null;
            if (!canDriveAnimator)
            {
                return;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.nameHash == isMovingHash && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    hasIsMovingParameter = true;
                }
                else if (parameter.nameHash == isAttackingHash && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    hasIsAttackingParameter = true;
                }
                else if (parameter.nameHash == takeHitHash && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    hasTakeHitTrigger = true;
                }
                else if (parameter.nameHash == dieHash && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    hasDieTrigger = true;
                }
                else if (parameter.nameHash == speedHash && parameter.type == AnimatorControllerParameterType.Float)
                {
                    hasSpeedParameter = true;
                }
            }
        }

        private void CreateFallbackClipsIfNeeded()
        {
            if (!createProceduralFallbackClips)
            {
                return;
            }

            if (attackClips == null || attackClips.Length == 0)
            {
                runtimeAttackClip = ProceduralAudio.CreateZombieAttackClip();
            }

            if (hitClips == null || hitClips.Length == 0)
            {
                runtimeHitClip = ProceduralAudio.CreateZombieHitClip();
            }

            if (deathClips == null || deathClips.Length == 0)
            {
                runtimeDeathClip = ProceduralAudio.CreateZombieDeathClip();
            }
        }

        private void TickProceduralFallback(float deltaTime)
        {
            if (!animateModelWhenNoAnimatorController || canDriveAnimator || !proceduralBaseCaptured || proceduralModelRoot == null)
            {
                return;
            }

            attackPulse = Mathf.MoveTowards(attackPulse, 0f, fallbackBlendSpeed * deltaTime);
            hitPulse = Mathf.MoveTowards(hitPulse, 0f, fallbackBlendSpeed * deltaTime);
            deathBlend = Mathf.MoveTowards(deathBlend, deathPlayed ? 1f : 0f, fallbackBlendSpeed * 0.5f * deltaTime);

            float bob = moving ? Mathf.Sin(Time.time * Mathf.Lerp(5f, 9f, Mathf.Clamp01(currentSpeed / 5f))) * walkBobHeight : 0f;
            float sway = moving ? Mathf.Sin(Time.time * 6.5f) * walkSwayDegrees : 0f;
            Vector3 targetPosition = proceduralBaseLocalPosition
                                     + Vector3.up * bob
                                     + Vector3.forward * (attackPulse * attackLungeDistance)
                                     - Vector3.forward * (hitPulse * hitRecoilDistance);
            Quaternion activeRotation = proceduralBaseLocalRotation
                                        * Quaternion.Euler(hitPulse * -8f, sway, attackPulse * -10f);
            Quaternion deathRotation = proceduralBaseLocalRotation * Quaternion.Euler(deathFallDegrees, 0f, -14f);

            proceduralModelRoot.localPosition = Vector3.Lerp(proceduralModelRoot.localPosition, targetPosition, 1f - Mathf.Exp(-fallbackBlendSpeed * deltaTime));
            proceduralModelRoot.localRotation = Quaternion.Slerp(activeRotation, deathRotation, deathBlend);
        }

        private void PlayRandomOneShot(AudioClip[] clips, AudioClip fallback, float volume)
        {
            if (audioSource == null)
            {
                return;
            }

            AudioClip clip = SelectClip(clips, fallback);
            if (clip == null)
            {
                return;
            }

            audioSource.pitch = Random.Range(
                Mathf.Min(pitchRange.x, pitchRange.y),
                Mathf.Max(pitchRange.x, pitchRange.y));
            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
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

        private static void DestroyRuntimeClip(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(clip);
            }
            else
            {
                DestroyImmediate(clip);
            }
        }
    }
}
