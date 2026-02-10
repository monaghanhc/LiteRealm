using LiteRealm.Core;
using UnityEngine;

namespace LiteRealm.Combat
{
    public class HitscanRifle : WeaponBase
    {
        protected override void ExecuteShot(WeaponFireContext context)
        {
            if (context.AimCamera == null)
            {
                return;
            }

            Ray centerRay = context.AimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 shootDirection = ApplySpread(centerRay.direction, context.AimCamera.transform);
            Ray shotRay = new Ray(centerRay.origin, shootDirection);

            if (!Physics.Raycast(shotRay, out RaycastHit hit, Range, context.HitMask, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            IDamageable damageable = FindDamageable(hit.collider);
            if (damageable != null)
            {
                damageable.ApplyDamage(new DamageInfo(
                    Damage,
                    hit.point,
                    hit.normal,
                    shootDirection,
                    context.Instigator,
                    WeaponId));
                SpawnBloodImpact(hit.point, hit.normal);
            }
            else
            {
                SpawnImpact(hit.point, hit.normal);
            }
        }

        private Vector3 ApplySpread(Vector3 forward, Transform cameraTransform)
        {
            float spread = SpreadDegrees;
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
