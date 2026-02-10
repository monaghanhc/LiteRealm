using LiteRealm.Core;
using UnityEngine;

namespace LiteRealm.AI
{
    public class BossProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 14f;
        [SerializeField] private float lifeTime = 6f;
        [SerializeField] private float damage = 24f;

        private GameObject owner;

        private void Awake()
        {
            Destroy(gameObject, lifeTime);
        }

        public void Initialize(GameObject projectileOwner, float projectileDamage)
        {
            owner = projectileOwner;
            damage = projectileDamage;
        }

        private void Update()
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (owner != null && (other.gameObject == owner || other.transform.IsChildOf(owner.transform)))
            {
                return;
            }

            MonoBehaviour[] components = other.GetComponentsInParent<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is IDamageable damageable)
                {
                    damageable.ApplyDamage(new DamageInfo(
                        damage,
                        transform.position,
                        -transform.forward,
                        transform.forward,
                        owner,
                        "boss.spit"));
                    break;
                }
            }

            Destroy(gameObject);
        }
    }
}
