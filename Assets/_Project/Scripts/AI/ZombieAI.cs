using System;
using LiteRealm.Core;
using LiteRealm.Player;
using LiteRealm.World;
using UnityEngine;
using UnityEngine.AI;

namespace LiteRealm.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(HealthComponent))]
    public class ZombieAI : MonoBehaviour
    {
        [SerializeField] private string enemyId = "zombie.basic";

        [Header("Movement")]
        [SerializeField] private float baseMoveSpeed = 2.7f;
        [SerializeField] private float wanderRadius = 15f;
        [SerializeField] private float wanderInterval = 4f;

        [Header("Perception")]
        [SerializeField] private float sightRange = 20f;
        [SerializeField] private float hearingRange = 16f;
        [SerializeField] private float loseTargetDelay = 4f;
        [SerializeField] private LayerMask sightBlockers = ~0;

        [Header("Combat")]
        [SerializeField] private float attackRange = 1.8f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackCooldown = 1.15f;
        [SerializeField] private float attackDamageDelay = 0.32f;
        [SerializeField] private float attackRecoveryDuration = 0.55f;
        [SerializeField] private float attackDamageRangePadding = 0.35f;

        [Header("Night Modifier")]
        [SerializeField] private float nightMoveSpeedMultiplier = 1.25f;
        [SerializeField] private float nightSenseMultiplier = 1.35f;
        [SerializeField] private float nightDamageMultiplier = 1.2f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip[] groanClips;
        [SerializeField] private float minGroanInterval = 6f;
        [SerializeField] private float maxGroanInterval = 12f;

        [Header("Death")]
        [SerializeField] private bool keepCorpseActiveForDeathAnimation = true;
        [SerializeField] private bool disableCollidersOnDeath = true;
        [SerializeField] private bool disableAgentOnDeath = true;

        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private GameEventHub eventHub;
        [SerializeField] private DayNightCycleManager dayNight;
        [SerializeField] private ZombiePresentationController presentation;

        public event Action<ZombieAI> ZombieDied;

        public string EnemyId => enemyId;
        public bool IsDead => health != null && health.IsDead;
        public float CurrentSenseMultiplier => dayNight != null && dayNight.IsNight ? nightSenseMultiplier : 1f;
        public float CurrentMoveSpeed => agent != null ? agent.speed : baseMoveSpeed;

        private NavMeshAgent agent;
        private HealthComponent health;
        private IDamageable targetDamageable;

        private Vector3 spawnOrigin;
        private Vector3? heardPosition;
        private float wanderTimer;
        private float attackTimer;
        private float loseSightTimer;
        private float groanTimer;
        private bool attackInProgress;
        private bool attackDamageApplied;
        private bool deathHandled;
        private Coroutine attackRoutine;
        private readonly RaycastHit[] sightHits = new RaycastHit[12];

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<HealthComponent>();
            presentation = presentation != null ? presentation : GetComponent<ZombiePresentationController>();
            if (presentation == null)
            {
                presentation = gameObject.AddComponent<ZombiePresentationController>();
            }

            if (health != null && keepCorpseActiveForDeathAnimation)
            {
                health.ConfigureDeathObjectHandling(false, false);
            }

            spawnOrigin = transform.position;
            ScheduleNextGroan();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDeath;
            }

            if (eventHub != null)
            {
                eventHub.WeaponFired += OnWeaponFired;
            }
        }

        private void Start()
        {
            if (target == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    target = playerObject.transform;
                }
            }

            ResolveTargetDamageable();
            UpdateNightModifiers();
        }

        private void OnDisable()
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            attackInProgress = false;
            presentation?.SetAttacking(false);
            presentation?.SetMoving(false, 0f);

            if (health != null)
            {
                health.Died -= OnDeath;
            }

            if (eventHub != null)
            {
                eventHub.WeaponFired -= OnWeaponFired;
            }
        }

        private void Update()
        {
            if (IsDead)
            {
                return;
            }

            UpdateNightModifiers();
            attackTimer -= Time.deltaTime;
            wanderTimer -= Time.deltaTime;
            TickGroans();

            if (target == null)
            {
                UpdatePresentationMovement();
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            bool canSeeTarget = distanceToTarget <= GetSightRange() && HasLineOfSight(target.position);
            bool heardRecently = heardPosition.HasValue && Vector3.Distance(transform.position, heardPosition.Value) <= GetHearingRange();

            if (canSeeTarget)
            {
                loseSightTimer = 0f;
                ChaseOrAttack(distanceToTarget);
                UpdatePresentationMovement();
                return;
            }

            loseSightTimer += Time.deltaTime;
            if (loseSightTimer <= loseTargetDelay)
            {
                ChaseLastKnownTarget();
                UpdatePresentationMovement();
                return;
            }

            if (heardRecently)
            {
                InvestigateHeardPoint();
                UpdatePresentationMovement();
                return;
            }

            Wander();
            UpdatePresentationMovement();
        }

        public void Initialize(Transform targetTransform, GameEventHub hub, DayNightCycleManager cycle)
        {
            target = targetTransform;
            ResolveTargetDamageable();

            if (eventHub != null)
            {
                eventHub.WeaponFired -= OnWeaponFired;
            }

            eventHub = hub;
            if (eventHub != null)
            {
                eventHub.WeaponFired += OnWeaponFired;
            }

            dayNight = cycle;
            UpdateNightModifiers();
        }

        private void ResolveTargetDamageable()
        {
            if (target == null)
            {
                targetDamageable = null;
                return;
            }

            MonoBehaviour[] components = target.GetComponentsInChildren<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is IDamageable damageable)
                {
                    targetDamageable = damageable;
                    return;
                }
            }

            targetDamageable = null;
        }

        private void ChaseOrAttack(float distanceToTarget)
        {
            if (distanceToTarget <= attackRange)
            {
                agent.isStopped = true;
                transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));
                TryAttack();
                return;
            }

            if (attackInProgress)
            {
                agent.isStopped = true;
                return;
            }

            presentation?.SetAttacking(false);
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }

        private void ChaseLastKnownTarget()
        {
            if (target == null)
            {
                return;
            }

            agent.isStopped = false;
            agent.SetDestination(target.position);
        }

        private void InvestigateHeardPoint()
        {
            if (!heardPosition.HasValue)
            {
                return;
            }

            agent.isStopped = false;
            agent.SetDestination(heardPosition.Value);
            if (Vector3.Distance(transform.position, heardPosition.Value) <= 1.5f)
            {
                heardPosition = null;
            }
        }

        private void Wander()
        {
            if (wanderTimer > 0f)
            {
                return;
            }

            wanderTimer = wanderInterval;
            Vector3 random = UnityEngine.Random.insideUnitSphere * wanderRadius;
            random.y = 0f;
            Vector3 destination = spawnOrigin + random;

            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
            }
        }

        private void TryAttack()
        {
            if (attackInProgress || attackTimer > 0f)
            {
                return;
            }

            if (targetDamageable == null)
            {
                ResolveTargetDamageable();
            }

            if (targetDamageable == null)
            {
                return;
            }

            attackTimer = attackCooldown;
            attackInProgress = true;
            attackDamageApplied = false;
            presentation?.SetAttacking(true);

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
            }

            attackRoutine = StartCoroutine(AttackRoutine());
        }

        public void AnimationEvent_DealAttackDamage()
        {
            ApplyAttackDamage();
        }

        public void AnimationEvent_PlayAttackSound()
        {
            presentation?.PlayAttackSound();
        }

        private void OnWeaponFired(WeaponFiredEvent data)
        {
            if (IsDead)
            {
                return;
            }

            float effectiveRange = GetHearingRange() + data.Loudness;
            float distance = Vector3.Distance(transform.position, data.Position);
            if (distance <= effectiveRange)
            {
                heardPosition = data.Position;
                loseSightTimer = loseTargetDelay;
            }
        }

        private bool HasLineOfSight(Vector3 targetPosition)
        {
            Vector3 origin = transform.position + Vector3.up * 1.4f;
            Vector3 destination = targetPosition + Vector3.up * 1.1f;
            Vector3 direction = destination - origin;
            float distance = direction.magnitude;

            if (distance <= 0.001f)
            {
                return true;
            }

            direction /= distance;

            int hitCount = Physics.RaycastNonAlloc(origin, direction, sightHits, distance, sightBlockers, QueryTriggerInteraction.Ignore);
            Transform nearestBlockingTransform = null;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = sightHits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                Transform hitTransform = hit.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform))
                {
                    continue;
                }

                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    nearestBlockingTransform = hitTransform;
                }
            }

            return nearestBlockingTransform == null
                || nearestBlockingTransform == target
                || nearestBlockingTransform.IsChildOf(target);
        }

        private float GetSightRange()
        {
            float multiplier = dayNight != null && dayNight.IsNight ? nightSenseMultiplier : 1f;
            return sightRange * multiplier;
        }

        private float GetHearingRange()
        {
            float multiplier = dayNight != null && dayNight.IsNight ? nightSenseMultiplier : 1f;
            return hearingRange * multiplier;
        }

        private void UpdateNightModifiers()
        {
            float multiplier = dayNight != null && dayNight.IsNight ? nightMoveSpeedMultiplier : 1f;
            agent.speed = baseMoveSpeed * multiplier;
        }

        private void TickGroans()
        {
            if (attackInProgress || audioSource == null || groanClips == null || groanClips.Length == 0)
            {
                return;
            }

            groanTimer -= Time.deltaTime;
            if (groanTimer > 0f)
            {
                return;
            }

            int index = UnityEngine.Random.Range(0, groanClips.Length);
            audioSource.PlayOneShot(groanClips[index]);
            ScheduleNextGroan();
        }

        private void ScheduleNextGroan()
        {
            groanTimer = UnityEngine.Random.Range(minGroanInterval, maxGroanInterval);
        }

        private System.Collections.IEnumerator AttackRoutine()
        {
            float damageDelay = Mathf.Max(0f, attackDamageDelay);
            if (damageDelay > 0f)
            {
                yield return new WaitForSeconds(damageDelay);
            }

            ApplyAttackDamage();

            float remainingRecovery = Mathf.Max(0f, attackRecoveryDuration - damageDelay);
            if (remainingRecovery > 0f)
            {
                yield return new WaitForSeconds(remainingRecovery);
            }

            attackInProgress = false;
            attackRoutine = null;
            presentation?.SetAttacking(false);
        }

        private void ApplyAttackDamage()
        {
            if (IsDead || attackDamageApplied || target == null)
            {
                return;
            }

            if (targetDamageable == null)
            {
                ResolveTargetDamageable();
            }

            if (targetDamageable == null)
            {
                return;
            }

            float maxDamageDistance = attackRange + Mathf.Max(0f, attackDamageRangePadding);
            if (Vector3.Distance(transform.position, target.position) > maxDamageDistance)
            {
                return;
            }

            attackDamageApplied = true;
            float damageAmount = attackDamage * (dayNight != null && dayNight.IsNight ? nightDamageMultiplier : 1f);
            Vector3 direction = (target.position - transform.position).normalized;
            targetDamageable.ApplyDamage(new DamageInfo(
                damageAmount,
                target.position,
                -direction,
                direction,
                gameObject,
                enemyId));
        }

        private void UpdatePresentationMovement()
        {
            if (presentation == null || agent == null)
            {
                return;
            }

            if (!agent.enabled)
            {
                presentation.SetMoving(false, 0f);
                return;
            }

            Vector3 velocity = agent.velocity;
            velocity.y = 0f;
            bool moving = !agent.isStopped && velocity.sqrMagnitude > 0.04f;
            presentation.SetMoving(moving, velocity.magnitude);
        }

        private void OnDeath()
        {
            if (deathHandled)
            {
                return;
            }

            deathHandled = true;
            attackInProgress = false;
            attackDamageApplied = true;
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            presentation?.SetAttacking(false);
            presentation?.SetMoving(false, 0f);
            presentation?.PlayDeath();

            if (agent != null)
            {
                if (agent.enabled)
                {
                    agent.isStopped = true;
                    agent.ResetPath();

                    if (disableAgentOnDeath)
                    {
                        agent.enabled = false;
                    }
                }
            }

            if (disableCollidersOnDeath)
            {
                Collider[] colliders = GetComponentsInChildren<Collider>();
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                    {
                        colliders[i].enabled = false;
                    }
                }
            }

            eventHub?.RaiseEnemyKilled(new EnemyKilledEvent
            {
                EnemyId = enemyId,
                EnemyObject = gameObject,
                Killer = null
            });

            ZombieDied?.Invoke(this);
        }
    }
}
