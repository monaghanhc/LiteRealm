using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace LiteRealm.World
{
    [DisallowMultipleComponent]
    public class RuntimeNavMeshBootstrap : MonoBehaviour
    {
        [Header("Build")]
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private bool skipBuildWhenNavMeshAlreadyPresent = true;

        [Header("Source Collection")]
        [SerializeField] private LayerMask includeLayers = ~0;
        [SerializeField] private NavMeshCollectGeometry collectGeometry = NavMeshCollectGeometry.PhysicsColliders;
        [SerializeField] private int defaultArea = 0;

        [Header("Bounds")]
        [SerializeField] private bool autoExpandToTerrain = true;
        [SerializeField] private Vector3 center = new Vector3(300f, 50f, 300f);
        [SerializeField] private Vector3 size = new Vector3(800f, 200f, 800f);

        private NavMeshData navMeshData;
        private NavMeshDataInstance navMeshDataInstance;

        public bool HasBuiltData => navMeshData != null && navMeshDataInstance.valid;

        private void Start()
        {
            if (buildOnStart)
            {
                BuildRuntimeNavMesh();
            }
        }

        [ContextMenu("Build Runtime NavMesh")]
        public void BuildRuntimeNavMesh()
        {
            if (skipBuildWhenNavMeshAlreadyPresent && HasAnyNavMeshData())
            {
                return;
            }

            ClearRuntimeNavMesh();

            if (NavMesh.GetSettingsCount() <= 0)
            {
                Debug.LogWarning("RuntimeNavMeshBootstrap: No NavMesh agent settings found.");
                return;
            }

            NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
            Bounds bounds = ResolveBuildBounds();

            List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
            List<NavMeshBuildMarkup> markups = new List<NavMeshBuildMarkup>();
            NavMeshBuilder.CollectSources(bounds, includeLayers, collectGeometry, defaultArea, markups, sources);

            if (sources.Count == 0)
            {
                Debug.LogWarning("RuntimeNavMeshBootstrap: No sources collected for runtime NavMesh build.");
                return;
            }

            navMeshData = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (navMeshData == null)
            {
                Debug.LogWarning("RuntimeNavMeshBootstrap: BuildNavMeshData returned null.");
                return;
            }

            navMeshDataInstance = NavMesh.AddNavMeshData(navMeshData);
        }

        [ContextMenu("Clear Runtime NavMesh")]
        public void ClearRuntimeNavMesh()
        {
            if (navMeshDataInstance.valid)
            {
                navMeshDataInstance.Remove();
            }

            navMeshDataInstance = default;
            navMeshData = null;
        }

        private void OnDestroy()
        {
            ClearRuntimeNavMesh();
        }

        private Bounds ResolveBuildBounds()
        {
            Bounds bounds = new Bounds(center, size);
            if (!autoExpandToTerrain)
            {
                return bounds;
            }

            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null || terrain.terrainData == null)
            {
                return bounds;
            }

            Vector3 terrainCenter = terrain.transform.position + (terrain.terrainData.size * 0.5f);
            Bounds terrainBounds = new Bounds(terrainCenter, terrain.terrainData.size + new Vector3(0f, 20f, 0f));
            bounds.Encapsulate(terrainBounds.min);
            bounds.Encapsulate(terrainBounds.max);
            return bounds;
        }

        private static bool HasAnyNavMeshData()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            return triangulation.vertices != null && triangulation.vertices.Length > 0;
        }

        private void OnDrawGizmosSelected()
        {
            Bounds bounds = ResolveBuildBounds();
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
