using LiteRealm.Inventory;
using UnityEngine;

namespace LiteRealm.Gameplay
{
    public class BuildSystemController : MonoBehaviour
    {
        private const string ScrapItemId = "item.material.scrap";

        [SerializeField] private Camera buildCamera;
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private LayerMask buildMask = ~0;
        [SerializeField] private KeyCode toggleBuildModeKey = KeyCode.B;

        [SerializeField] private int foundationScrapCost = 3;
        [SerializeField] private int wallScrapCost = 2;

        private BuildPieceType _selectedPiece = BuildPieceType.Foundation;
        private bool _buildModeActive;
        private GameObject _preview;

        private enum BuildPieceType
        {
            Foundation = 0,
            Wall = 1
        }

        private void Awake()
        {
            if (buildCamera == null)
            {
                buildCamera = Camera.main;
            }

            if (inventory == null)
            {
                inventory = GetComponent<InventoryComponent>();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleBuildModeKey))
            {
                _buildModeActive = !_buildModeActive;
                if (!_buildModeActive)
                {
                    HidePreview();
                }
            }

            if (!_buildModeActive)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _selectedPiece = BuildPieceType.Foundation;
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _selectedPiece = BuildPieceType.Wall;
            }

            UpdatePreview();

            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceCurrentPiece();
            }
        }

        public bool TryPlaceCurrentPieceForTests(Vector3 position, bool alignToGroundNormal = false)
        {
            return TryPlaceAtPosition(position, alignToGroundNormal ? Vector3.up : Vector3.up);
        }

        public bool TryPlaceCurrentPiece()
        {
            if (!TryGetBuildPoint(out RaycastHit hit))
            {
                return false;
            }

            Vector3 up = hit.normal.sqrMagnitude > 0.01f ? hit.normal : Vector3.up;
            return TryPlaceAtPosition(hit.point, up);
        }

        private bool TryPlaceAtPosition(Vector3 position, Vector3 up)
        {
            int cost = GetCost(_selectedPiece);
            if (inventory == null || inventory.GetItemCount(ScrapItemId) < cost)
            {
                return false;
            }

            GameObject piece = CreatePiece(_selectedPiece);
            piece.transform.position = GetSnappedPosition(position, _selectedPiece);
            piece.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);

            inventory.RemoveItem(ScrapItemId, cost);
            return true;
        }

        private void UpdatePreview()
        {
            if (!TryGetBuildPoint(out RaycastHit hit))
            {
                HidePreview();
                return;
            }

            if (_preview == null)
            {
                _preview = CreatePiece(_selectedPiece);
                SetPreviewMaterial(_preview);
            }

            _preview.transform.position = GetSnappedPosition(hit.point, _selectedPiece);
            _preview.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal.sqrMagnitude > 0.01f ? hit.normal : Vector3.up);
            _preview.SetActive(true);
        }

        private bool TryGetBuildPoint(out RaycastHit hit)
        {
            hit = default;
            if (buildCamera == null)
            {
                return false;
            }

            Ray ray = buildCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return Physics.Raycast(ray, out hit, 100f, buildMask, QueryTriggerInteraction.Ignore);
        }

        private static Vector3 GetSnappedPosition(Vector3 source, BuildPieceType pieceType)
        {
            float yOffset = pieceType == BuildPieceType.Foundation ? 0.15f : 1.5f;
            return new Vector3(
                Mathf.Round(source.x),
                Mathf.Round(source.y + yOffset * 2f) * 0.5f,
                Mathf.Round(source.z));
        }

        private static GameObject CreatePiece(BuildPieceType pieceType)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = pieceType == BuildPieceType.Foundation ? "Build_Foundation" : "Build_Wall";
            piece.transform.localScale = pieceType == BuildPieceType.Foundation
                ? new Vector3(3f, 0.3f, 3f)
                : new Vector3(3f, 3f, 0.3f);
            Renderer renderer = piece.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard"))
                {
                    color = pieceType == BuildPieceType.Foundation
                        ? new Color(0.35f, 0.35f, 0.38f)
                        : new Color(0.45f, 0.32f, 0.24f)
                };
            }

            return piece;
        }

        private static void SetPreviewMaterial(GameObject preview)
        {
            Collider collider = preview.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            Renderer renderer = preview.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Standard"))
                {
                    color = new Color(0.3f, 0.9f, 0.5f, 0.45f)
                };
            }
        }

        private void HidePreview()
        {
            if (_preview != null)
            {
                _preview.SetActive(false);
            }
        }

        private int GetCost(BuildPieceType pieceType)
        {
            return pieceType == BuildPieceType.Foundation ? Mathf.Max(1, foundationScrapCost) : Mathf.Max(1, wallScrapCost);
        }

        private void OnDisable()
        {
            if (_preview != null)
            {
                Destroy(_preview);
                _preview = null;
            }
        }
    }
}
