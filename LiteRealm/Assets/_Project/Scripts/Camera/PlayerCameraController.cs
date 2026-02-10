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
        [SerializeField] [Range(0.1f, 1f)] private float aimSensitivityMultiplier = 0.72f;

        [Header("Aim Down Sights")]
        [SerializeField] private float defaultFov = 65f;
        [SerializeField] private float adsFov = 48f;
        [SerializeField] private float adsTransitionSpeed = 14f;
        [SerializeField] private Vector3 firstPersonAimOffset = new Vector3(0f, 0.02f, 0f);
        [SerializeField] private Vector3 thirdPersonAimShoulderOffset = new Vector3(0.3f, 0.15f, 0f);
        [SerializeField] private float thirdPersonAimDistance = 2.1f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private float collisionRadius = 0.2f;
        [SerializeField] private float minCameraDistance = 0.6f;

        [Header("Fallback Input (Legacy Input Manager)")]
        [SerializeField] private KeyCode toggleViewFallbackKey = KeyCode.V;
        [SerializeField] private KeyCode aimFallbackKey = KeyCode.Mouse1;

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
        private Camera cachedCamera;
        private bool aiming;
        private float aimBlend;

        public bool IsFirstPerson => firstPerson;
        public bool IsAiming => aiming;
        public float AimBlend => aimBlend;

        private void Awake()
        {
            cachedCamera = GetComponent<Camera>();
            if (cachedCamera != null)
            {
                defaultFov = cachedCamera.fieldOfView;
            }

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

            if (cachedCamera != null)
            {
                cachedCamera.fieldOfView = defaultFov;
            }
        }

        private void Update()
        {
            if (ReadToggleViewPressedThisFrame())
            {
                firstPerson = !firstPerson;
            }

            aiming = ReadAimHeld();
            Vector2 lookDelta = ReadLookDelta();
            float sensitivityScale = aiming ? aimSensitivityMultiplier : 1f;
            yaw += lookDelta.x * lookSensitivity.x * sensitivityScale;

            float yInput = invertLookY ? lookDelta.y : -lookDelta.y;
            pitch = Mathf.Clamp(pitch + yInput * lookSensitivity.y * sensitivityScale, minPitch, maxPitch);

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
            float targetAim = aiming ? 1f : 0f;
            aimBlend = Mathf.MoveTowards(aimBlend, targetAim, adsTransitionSpeed * Time.deltaTime);

            if (cachedCamera != null)
            {
                float targetFov = Mathf.Lerp(defaultFov, adsFov, aimBlend);
                cachedCamera.fieldOfView = Mathf.Lerp(cachedCamera.fieldOfView, targetFov, adsTransitionSpeed * Time.deltaTime);
            }

            if (firstPerson)
            {
                Vector3 firstPersonPivot = target.position + thirdPersonPivotOffset;
                Vector3 currentOffset = Vector3.Lerp(firstPersonOffset, firstPersonAimOffset, aimBlend);
                Vector3 desiredFirstPersonPosition = firstPersonPivot + lookRotation * currentOffset;
                transform.position = Vector3.SmoothDamp(transform.position, desiredFirstPersonPosition, ref currentVelocity, followSmoothTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSmooth * Time.deltaTime);
                return;
            }

            Vector3 pivot = target.position + thirdPersonPivotOffset;
            Vector3 shoulderOffset = Vector3.Lerp(thirdPersonShoulderOffset, thirdPersonAimShoulderOffset, aimBlend);
            float distanceTarget = Mathf.Lerp(thirdPersonDistance, thirdPersonAimDistance, aimBlend);
            Vector3 shoulderPivot = pivot + lookRotation * shoulderOffset;
            Vector3 desiredThirdPerson = shoulderPivot + lookRotation * (Vector3.back * distanceTarget);

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

        private bool ReadAimHeld()
        {
            if (input != null)
            {
                return input.AimHeld();
            }

            return Input.GetKey(aimFallbackKey);
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
