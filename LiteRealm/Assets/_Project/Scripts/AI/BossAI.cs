using System;
using LiteRealm.Core;
using LiteRealm.World;
using UnityEngine;
using UnityEngine.AI;

namespace LiteRealm.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(HealthComponent))]
    public class BossAI : MonoBehaviour
    {
        [SerializeField] private string bossId = "boss.alpha";

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float chaseRange = 35f;

        [Header("Melee")]
        [SerializeField] private float meleeRange = 2.6f;
        [SerializeField] private float meleeDamage = 18f;
        [SerializeField] private float meleeCooldown = 1.2f;

        [Header("Spit Attack")]
        [SerializeField] private float spitRange = 20f;
        [SerializeField] private float spitDamage = 25f;
        [SerializeField] private float spitCooldown = 5f;
        [SerializeField] private BossProjectile spitProjectilePrefab;
        [SerializeField] private Transform spitSpawnPoint;

        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private GameEventHub eventHub;
        [SerializeField] private DayNightCycleManager dayNight;

        public event Action<BossAI> BossDied;

        public string BossId => bossId;
        public bool IsDead => health != null && health.IsDead;

        private NavMeshAgent agent;
        private HealthComponent health;
        private IDamageable targetDamageable;
        private float meleeTimer;
        private float spitTimer;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDeath;
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
            agent.speed = moveSpeed;
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDeath;
            }
        }

        private void Update()
        {
            if (IsDead || target == null)
            {
                return;
            }

            meleeTimer -= Time.deltaTime;
            spitTimer -= Time.deltaTime;

            float distance = Vector3.Distance(transform.position, target.position);
            if (distance > chaseRange)
            {
                agent.isStopped = true;
                return;
            }

            transform.LookAt(new Vector3(target.position.x, transform.position.y, target.position.z));

            if (distance <= meleeRange)
            {
                agent.isStopped = true;
                TryMeleeAttack();
                return;
            }

            if (distance <= spitRange)
            {
                TrySpitAttack();
            }

            agent.isStopped = false;
            agent.SetDestination(target.position);
        }

        public void Initialize(Transform targetTransform, GameEventHub hub, DayNightCycleManager cycle)
        {
            target = targetTransform;
            eventHub = hub;
            dayNight = cycle;
            ResolveTargetDamageable();
            agent.speed = moveSpeed;
        }

        private void ResolveTargetDamageable()
        {
            targetDamageable = null;
            if (target == null)
            {
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
        }

        private void TryMeleeAttack()
        {
            if (meleeTimer > 0f)
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

            meleeTimer = meleeCooldown;
            float damage = dayNight != null && dayNight.IsNight ? meleeDamage * 1.2f : meleeDamage;

            Vector3 direction = (target.position - transform.position).normalized;
            targetDamageable.ApplyDamage(new DamageInfo(
                damage,
                target.position,
                -direction,
                direction,
                gameObject,
                bossId + ".melee"));
        }

        private void TrySpitAttack()
        {
            if (spitTimer > 0f || spitProjectilePrefab == null)
            {
                return;
            }

            spitTimer = spitCooldown;
            Transform spawn = spitSpawnPoint != null ? spitSpawnPoint : transform;
            Vector3 spawnPos = spawn.position + transform.forward * 1f + Vector3.up * 1.2f;
            Quaternion rotation = Quaternion.LookRotation((target.position + Vector3.up * 1f) - spawnPos);

            BossProjectile projectile = Instantiate(spitProjectilePrefab, spawnPos, rotation);
            float damage = dayNight != null && dayNight.IsNight ? spitDamage * 1.15f : spitDamage;
            projectile.Initialize(gameObject, damage);
        }

        private void OnDeath()
        {
            if (agent != null)
            {
                agent.isStopped = true;
            }

            eventHub?.RaiseBossKilled(new BossKilledEvent
            {
                BossId = bossId,
                BossObject = gameObject,
                Killer = null
            });

            BossDied?.Invoke(this);
        }
    }
}
