using LiteRealm.Player;
using UnityEngine;

namespace LiteRealm.CameraSystem
{
    public class PlayerCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private ExplorationInput input;

        [Header("View Modes")]
        [SerializeField] private bool startFirstPerson;
        [SerializeField] private Vector3 firstPersonOffset = new Vector3(0f, 0.08f, 0f);
        [SerializeField] private Vector3 thirdPersonPivotOffset = new Vector3(0f, 1.6f, 0f);
        [SerializeField] private Vector3 thirdPersonShoulderOffset = new Vector3(0.55f, 0.2f, 0f);
        [SerializeField] private float thirdPersonDistance = 3.7f;

        [Header("Look")]
        [SerializeField] private Vector2 lookSensitivity = new Vector2(0.09f, 0.09f);
        [SerializeField] private bool invertLookY;
        [SerializeField] private float minPitch = -70f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private float followSmoothTime = 0.045f;
        [SerializeField] private float rotationSmooth = 16f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private float collisionRadius = 0.2f;
        [SerializeField] private float minCameraDistance = 0.6f;

        [Header("Fallback Input (Legacy Input Manager)")]
        [SerializeField] private KeyCode toggleViewFallbackKey = KeyCode.V;

        [Header("Cursor")]
        [SerializeField] private bool lockCursor = true;

        [Header("Recoil")]
        [SerializeField] private float recoilReturnSpeed = 14f;

        private bool firstPerson;
        private float yaw;
        private float pitch;
        private float recoilPitch;
        private float recoilYaw;
        private Vector3 currentVelocity;

        public bool IsFirstPerson => firstPerson;

        private void Awake()
        {
            if (input == null && target != null)
            {
                input = target.GetComponent<ExplorationInput>();
            }

            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    target = player.transform;
                    if (input == null)
                    {
                        input = player.GetComponent<ExplorationInput>();
                    }
                }
            }
        }

        private void Start()
        {
            firstPerson = startFirstPerson;
            Vector3 euler = transform.eulerAngles;
            yaw = euler.y;
            pitch = NormalizePitch(euler.x);

            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            if (ReadToggleViewPressedThisFrame())
            {
                firstPerson = !firstPerson;
            }

            Vector2 lookDelta = ReadLookDelta();
            yaw += lookDelta.x * lookSensitivity.x;

            float yInput = invertLookY ? lookDelta.y : -lookDelta.y;
            pitch = Mathf.Clamp(pitch + yInput * lookSensitivity.y, minPitch, maxPitch);

            recoilPitch = Mathf.Lerp(recoilPitch, 0f, recoilReturnSpeed * Time.deltaTime);
            recoilYaw = Mathf.Lerp(recoilYaw, 0f, recoilReturnSpeed * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Quaternion lookRotation = Quaternion.Euler(pitch + recoilPitch, yaw + recoilYaw, 0f);
            target.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (firstPerson)
            {
                Vector3 firstPersonPivot = target.position + thirdPersonPivotOffset;
                Vector3 desiredFirstPersonPosition = firstPersonPivot + lookRotation * firstPersonOffset;
                transform.position = Vector3.SmoothDamp(transform.position, desiredFirstPersonPosition, ref currentVelocity, followSmoothTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSmooth * Time.deltaTime);
                return;
            }

            Vector3 pivot = target.position + thirdPersonPivotOffset;
            Vector3 shoulderPivot = pivot + lookRotation * thirdPersonShoulderOffset;
            Vector3 desiredThirdPerson = shoulderPivot + lookRotation * (Vector3.back * thirdPersonDistance);

            Vector3 toDesired = desiredThirdPerson - shoulderPivot;
            float distance = toDesired.magnitude;

            if (distance > 0.001f)
            {
                Vector3 direction = toDesired / distance;
                if (Physics.SphereCast(shoulderPivot, collisionRadius, direction, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    float adjustedDistance = Mathf.Max(minCameraDistance, hit.distance - collisionRadius);
                    desiredThirdPerson = shoulderPivot + direction * adjustedDistance;
                }
            }

            transform.position = Vector3.SmoothDamp(transform.position, desiredThirdPerson, ref currentVelocity, followSmoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSmooth * Time.deltaTime);
        }

        public bool ToggleViewModeForTests()
        {
            firstPerson = !firstPerson;
            return firstPerson;
        }

        public void AddRecoil(float verticalKick, float horizontalKick)
        {
            recoilPitch -= Mathf.Abs(verticalKick);
            recoilYaw += horizontalKick;
        }

        private Vector2 ReadLookDelta()
        {
            if (input != null)
            {
                return input.ReadLookDelta();
            }

            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        }

        private bool ReadToggleViewPressedThisFrame()
        {
            if (input != null)
            {
                return input.ToggleViewPressedThisFrame();
            }

            return Input.GetKeyDown(toggleViewFallbackKey);
        }

        private static float NormalizePitch(float angle)
        {
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return angle;
        }
    }
}
