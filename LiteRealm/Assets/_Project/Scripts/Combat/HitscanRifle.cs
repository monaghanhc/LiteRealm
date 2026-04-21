using LiteRealm.Core;
using UnityEngine;

namespace LiteRealm.Combat
{
    public class HitscanRifle : WeaponBase
    {
        private bool _hadHitResult;
        private bool _lastHitKilled;

        public bool TryConsumeLastHitResult(out bool killed)
        {
            if (!_hadHitResult)
            {
                killed = false;
                return false;
            }

            killed = _lastHitKilled;
            _hadHitResult = false;
            _lastHitKilled = false;
            return true;
        }

        protected override void ExecuteShot(WeaponFireContext context)
        {
            _hadHitResult = false;
            _lastHitKilled = false;

            if (context.AimCamera == null)
            {
                return;
            }

            Ray centerRay = context.AimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float spread = GetSpreadForAim(context.IsAiming);
            Vector3 shootDirection = ApplySpread(centerRay.direction, context.AimCamera.transform, spread);
            Ray shotRay = new Ray(centerRay.origin, shootDirection);

            if (!Physics.Raycast(shotRay, out RaycastHit hit, Range, context.HitMask, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            IDamageable damageable = FindDamageable(hit.collider);
            if (damageable != null)
            {
                bool wasDead = damageable.IsDead;
                damageable.ApplyDamage(new DamageInfo(
                    Damage,
                    hit.point,
                    hit.normal,
                    shootDirection,
                    context.Instigator,
                    WeaponId));
                _hadHitResult = true;
                _lastHitKilled = !wasDead && damageable.IsDead;
                SpawnBloodImpact(hit.point, hit.normal);
            }
            else
            {
                SpawnImpact(hit.point, hit.normal);
            }
        }

        private Vector3 ApplySpread(Vector3 forward, Transform cameraTransform, float spread)
        {
            if (spread <= 0f)
            {
                return forward.normalized;
            }

            Vector2 spreadOffset = Random.insideUnitCircle * spread;
            Vector3 up = cameraTransform != null ? cameraTransform.up : Vector3.up;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            Quaternion spreadRotation = Quaternion.AngleAxis(spreadOffset.x, up)
                                      * Quaternion.AngleAxis(spreadOffset.y, right);
            return (spreadRotation * forward).normalized;
        }
    }
}
