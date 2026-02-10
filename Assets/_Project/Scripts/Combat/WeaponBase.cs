using System;
using System.Collections;
using LiteRealm.Core;
using UnityEngine;

namespace LiteRealm.Combat
{
    public struct WeaponFireContext
    {
        public Camera AimCamera;
        public LayerMask HitMask;
        public GameObject Instigator;
        public GameEventHub EventHub;
    }

    public abstract class WeaponBase : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string weaponId = "weapon.rifle";
        [SerializeField] private string weaponDisplayName = "Rifle";

        [Header("Stats")]
        [SerializeField] private float damage = 22f;
        [SerializeField] private float fireRate = 8f;
        [SerializeField] private float range = 140f;
        [SerializeField] private float spreadDegrees = 1.4f;
        [SerializeField] private int magazineSize = 30;
        [SerializeField] private float reloadDuration = 1.5f;
        [SerializeField] private float recoilPitch = 1f;
        [SerializeField] private float recoilYaw = 0.3f;
        [SerializeField] private float loudness = 18f;

        [Header("Presentation")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private ParticleSystem muzzleFlash;
        [SerializeField] private AudioSource shootAudioSource;
        [SerializeField] private AudioClip shootClip;
        [SerializeField] private GameObject impactEffectPrefab;

        public event Action<WeaponBase> Fired;
        public event Action<WeaponBase> AmmoChanged;

        public string WeaponId => weaponId;
        public string WeaponDisplayName => weaponDisplayName;
        public float Damage => damage;
        public float FireRate => fireRate;
        public float Range => range;
        public float SpreadDegrees => spreadDegrees;
        public int MagazineSize => Mathf.Max(1, magazineSize);
        public int CurrentAmmo { get; private set; }
        public float ReloadDuration => Mathf.Max(0.1f, reloadDuration);
        public float RecoilPitch => recoilPitch;
        public float RecoilYaw => recoilYaw;
        public float Loudness => loudness;
        public Transform MuzzlePoint => muzzlePoint != null ? muzzlePoint : transform;
        public GameObject ImpactEffectPrefab => impactEffectPrefab;
        public bool IsReloading { get; private set; }

        private float nextAllowedShotTime;

        protected virtual void Awake()
        {
            CurrentAmmo = MagazineSize;
            nextAllowedShotTime = 0f;
        }

        public bool TryFire(WeaponFireContext context)
        {
            if (IsReloading)
            {
                return false;
            }

            if (Time.time < nextAllowedShotTime)
            {
                return false;
            }

            if (CurrentAmmo <= 0)
            {
                return false;
            }

            nextAllowedShotTime = Time.time + (1f / Mathf.Max(0.01f, FireRate));
            CurrentAmmo--;

            PlayFirePresentation();
            ExecuteShot(context);

            context.EventHub?.RaiseWeaponFired(new WeaponFiredEvent
            {
                Position = MuzzlePoint.position,
                Loudness = Loudness,
                Instigator = context.Instigator
            });

            AmmoChanged?.Invoke(this);
            Fired?.Invoke(this);
            return true;
        }

        public bool TryStartReload(MonoBehaviour runner)
        {
            if (runner == null || IsReloading || CurrentAmmo >= MagazineSize)
            {
                return false;
            }

            runner.StartCoroutine(ReloadRoutine());
            return true;
        }

        public void ForceSetAmmo(int ammo)
        {
            CurrentAmmo = Mathf.Clamp(ammo, 0, MagazineSize);
            AmmoChanged?.Invoke(this);
        }

        protected abstract void ExecuteShot(WeaponFireContext context);

        protected IDamageable FindDamageable(Collider collider)
        {
            MonoBehaviour[] components = collider.GetComponentsInParent<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] is IDamageable damageable)
                {
                    return damageable;
                }
            }

            return null;
        }

        protected void SpawnImpact(Vector3 position, Vector3 normal)
        {
            if (ImpactEffectPrefab == null)
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(normal);
            GameObject effect = Instantiate(ImpactEffectPrefab, position, rotation);
            Destroy(effect, 2f);
        }

        private IEnumerator ReloadRoutine()
        {
            IsReloading = true;
            yield return new WaitForSeconds(ReloadDuration);
            CurrentAmmo = MagazineSize;
            IsReloading = false;
            AmmoChanged?.Invoke(this);
        }

        private void PlayFirePresentation()
        {
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            if (shootAudioSource != null && shootClip != null)
            {
                shootAudioSource.PlayOneShot(shootClip);
            }
        }
    }
}
