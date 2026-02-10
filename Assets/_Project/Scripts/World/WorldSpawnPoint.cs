using UnityEngine;

namespace LiteRealm.World
{
    public enum SpawnPointKind
    {
        Zombie,
        LootContainer,
        Npc,
        Boss
    }

    public class WorldSpawnPoint : MonoBehaviour
    {
        [SerializeField] private SpawnPointKind kind = SpawnPointKind.Zombie;
        [SerializeField] private float gizmoRadius = 0.4f;

        public SpawnPointKind Kind => kind;

        private void OnDrawGizmos()
        {
            Color color = kind switch
            {
                SpawnPointKind.Zombie => new Color(0.8f, 0.2f, 0.2f),
                SpawnPointKind.LootContainer => new Color(0.9f, 0.8f, 0.2f),
                SpawnPointKind.Npc => new Color(0.2f, 0.8f, 0.3f),
                SpawnPointKind.Boss => new Color(0.7f, 0.2f, 0.9f),
                _ => Color.white
            };

            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position, gizmoRadius);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
        }
    }
}
