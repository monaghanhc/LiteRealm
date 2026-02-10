using UnityEngine;

namespace LiteRealm.Core
{
    public struct DamageInfo
    {
        public float Amount;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public Vector3 Direction;
        public GameObject Instigator;
        public string SourceId;
        public bool IsCritical;

        public DamageInfo(
            float amount,
            Vector3 hitPoint,
            Vector3 hitNormal,
            Vector3 direction,
            GameObject instigator,
            string sourceId = "",
            bool isCritical = false)
        {
            Amount = amount;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            Direction = direction;
            Instigator = instigator;
            SourceId = sourceId;
            IsCritical = isCritical;
        }
    }
}
