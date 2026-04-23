using LiteRealm.Player;
using UnityEngine;

namespace LiteRealm.CameraSystem
{
    public class PlayerCameraController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform target;
        [SerializeField] private ExplorationInput input;
        [SerializeField] private PlayerController playerController;

        [Header("View Modes")]
        [SerializeField] private bool startFirstPerson;
        [SerializeField] private Vector3 firstPersonOffset = new Vector3(0f, 0.08f, 0f);
        [SerializeField] private Vector3 thirdPersonPivotOffset = new Vector3(0f, 1.58f, 0f);
        [SerializeField] private Vector3 thirdPersonShoulderOffset = new Vector3(0.46f, 0.18f, 0f);
        [SerializeField] private float thirdPersonDistance = 3.35f;

        [Header("Look")]
        [SerializeField] private Vector2 lookSensitivity = new Vector2(0.09f, 0.09f);
        [SerializeField] private bool invertLookY;
        [SerializeField] private float minPitch = -70f;
        [SerializeField] private float maxPitch = 80f;
        [SerializeField] private float followSmoothTime = 0.034f;
        [SerializeField] private float rotationSmooth = 22f;
        [SerializeField] [Range(0.1f, 1f)] private float aimSensitivityMultiplier = 0.72f;

        [Header("Aim Down Sights")]
        [SerializeField] private bool doubleClickRightMouseToToggleAds = true;
        [SerializeField] private bool holdRightMouseToAimWhenDoubleClickDisabled = true;
        [SerializeField] private float adsDoubleClickWindow = 0.32f;
        [SerializeField] private float defaultFov = 68f;
        [SerializeField] private float adsFov = 50f;
        [SerializeField] private float adsTransitionSpeed = 18f;
        [SerializeField] private float sprintFovBoost = 4.25f;
        [SerializeField] private Vector3 firstPersonAimOffset = new Vector3(0f, 0.02f, 0f);
        [SerializeField] private Vector3 thirdPersonAimShoulderOffset = new Vector3(0.28f, 0.14f, 0f);
        [SerializeField] private float thirdPersonAimDistance = 2.18f;

        [Header("Camera Motion")]
        [SerializeField] private bool enableHeadBob = true;
        [SerializeField] private float walkBobFrequency = 7.8f;
        [SerializeField] private float sprintBobFrequency = 10.2f;
        [SerializeField] private float crouchBobFrequency = 6f;
        [SerializeField] private Vector2 walkBobAmplitude = new Vector2(0.018f, 0.026f);
        [SerializeField] private Vector2 sprintBobAmplitude = new Vector2(0.032f, 0.045f);
        [SerializeField] private Vector2 crouchBobAmplitude = new Vector2(0.010f, 0.016f);
        [SerializeField] private float bobSmoothing = 17f;
        [SerializeField] private float movementLeanDegrees = 1.15f;
        [SerializeField] private float sprintLeanMultiplier = 1.35f;
        [SerializeField] private float leanSmoothing = 12f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private float collisionRadius = 0.24f;
        [SerializeField] private float minCameraDistance = 0.55f;

        [Header("Fallback Input (Legacy Input Manager)")]
        [SerializeField] private KeyCode toggleViewFallbackKey = KeyCode.V;
        [SerializeField] private KeyCode aimFallbackKey = KeyCode.Mouse1;

        [Header("Cursor")]
        [SerializeField] private bool lockCursor = true;

        [Header("Recoil")]
        [SerializeField] private float recoilReturnSpeed = 16f;

        [Header("Impact Shake")]
        [SerializeField] private float shakeDamping = 12f;
        [SerializeField] private float shakeFrequency = 31f;
        [SerializeField] private float maxShakeOffset = 0.09f;

        private bool firstPerson;
        private float yaw;
        private float pitch;
        private float recoilPitch;
        private float recoilYaw;
        private Vector3 currentVelocity;
        private Camera cachedCamera;
        private bool aiming;
        private bool adsToggleActive;
        private float lastAimPressTime = -999f;
        private float aimBlend;
        private float bobTimer;
        private Vector3 currentBobOffset;
        private float shakeAmplitude;
        private float shakeSeed;
        private float currentLean;

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

            shakeSeed = Random.value * 100f;

            if (input == null && target != null)
            {
                input = target.GetComponent<ExplorationInput>();
            }

            if (playerController == null && target != null)
            {
                playerController = target.GetComponent<PlayerController>();
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

                    if (playerController == null)
                    {
                        playerController = player.GetComponent<PlayerController>();
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

            UpdateAimState();
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

            float targetAim = aiming ? 1f : 0f;
            aimBlend = Mathf.MoveTowards(aimBlend, targetAim, adsTransitionSpeed * Time.deltaTime);
            Quaternion lookRotation = Quaternion.Euler(pitch + recoilPitch, yaw + recoilYaw, 0f);
            Quaternion cameraRotation = Quaternion.Euler(pitch + recoilPitch, yaw + recoilYaw, CalculateMovementLean(Time.deltaTime));
            target.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (cachedCamera != null)
            {
                float sprintBoost = playerController != null && playerController.IsSprinting && !aiming ? sprintFovBoost : 0f;
                float targetFov = Mathf.Lerp(defaultFov + sprintBoost, adsFov, aimBlend);
                cachedCamera.fieldOfView = Mathf.Lerp(cachedCamera.fieldOfView, targetFov, adsTransitionSpeed * Time.deltaTime);
            }

            Vector3 cameraMotionOffset = CalculateCameraMotionOffset(Time.deltaTime);
            Vector3 shakeOffset = CalculateShakeOffset(Time.deltaTime, lookRotation);

            if (firstPerson)
            {
                Vector3 firstPersonPivot = target.position + thirdPersonPivotOffset;
                Vector3 currentOffset = Vector3.Lerp(firstPersonOffset, firstPersonAimOffset, aimBlend);
                Vector3 desiredFirstPersonPosition = firstPersonPivot + lookRotation * (currentOffset + cameraMotionOffset) + shakeOffset;
                transform.position = Vector3.SmoothDamp(transform.position, desiredFirstPersonPosition, ref currentVelocity, followSmoothTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, cameraRotation, rotationSmooth * Time.deltaTime);
                return;
            }

            Vector3 pivot = target.position + thirdPersonPivotOffset + lookRotation * (cameraMotionOffset * 0.35f);
            Vector3 shoulderOffset = Vector3.Lerp(thirdPersonShoulderOffset, thirdPersonAimShoulderOffset, aimBlend);
            float distanceTarget = Mathf.Lerp(thirdPersonDistance, thirdPersonAimDistance, aimBlend);
            Vector3 shoulderPivot = pivot + lookRotation * shoulderOffset;
            Vector3 desiredThirdPerson = shoulderPivot + lookRotation * (Vector3.back * distanceTarget) + shakeOffset;

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
            transform.rotation = Quaternion.Slerp(transform.rotation, cameraRotation, rotationSmooth * Time.deltaTime);
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

        public void AddShake(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            shakeAmplitude = Mathf.Min(maxShakeOffset, shakeAmplitude + amount);
        }

        private Vector3 CalculateCameraMotionOffset(float deltaTime)
        {
            if (!enableHeadBob || playerController == null || !playerController.IsGrounded)
            {
                currentBobOffset = Vector3.Lerp(currentBobOffset, Vector3.zero, bobSmoothing * deltaTime);
                return currentBobOffset;
            }

            float planarSpeed = playerController.PlanarSpeed;
            if (planarSpeed < 0.15f)
            {
                currentBobOffset = Vector3.Lerp(currentBobOffset, Vector3.zero, bobSmoothing * deltaTime);
                return currentBobOffset;
            }

            float frequency = walkBobFrequency;
            Vector2 amplitude = walkBobAmplitude;
            if (playerController.IsSprinting)
            {
                frequency = sprintBobFrequency;
                amplitude = sprintBobAmplitude;
            }
            else if (playerController.IsCrouched)
            {
                frequency = crouchBobFrequency;
                amplitude = crouchBobAmplitude;
            }

            bobTimer += deltaTime * frequency;
            Vector3 targetOffset = new Vector3(
                Mathf.Sin(bobTimer * 0.5f) * amplitude.x,
                Mathf.Abs(Mathf.Sin(bobTimer)) * amplitude.y,
                0f);

            currentBobOffset = Vector3.Lerp(currentBobOffset, targetOffset, bobSmoothing * deltaTime);
            return currentBobOffset;
        }

        private float CalculateMovementLean(float deltaTime)
        {
            float targetLean = 0f;
            if (playerController != null && target != null && playerController.IsGrounded)
            {
                Vector3 localVelocity = target.InverseTransformDirection(playerController.Velocity);
                float lateral = Mathf.Clamp(localVelocity.x / 5f, -1f, 1f);
                float sprintScale = playerController.IsSprinting ? sprintLeanMultiplier : 1f;
                targetLean = -lateral * movementLeanDegrees * sprintScale;
                targetLean *= 1f - aimBlend * 0.7f;
            }

            currentLean = Mathf.Lerp(currentLean, targetLean, 1f - Mathf.Exp(-leanSmoothing * deltaTime));
            return currentLean;
        }

        private Vector3 CalculateShakeOffset(float deltaTime, Quaternion lookRotation)
        {
            if (shakeAmplitude <= 0.0001f)
            {
                shakeAmplitude = 0f;
                return Vector3.zero;
            }

            shakeAmplitude = Mathf.MoveTowards(shakeAmplitude, 0f, shakeDamping * deltaTime);
            float time = Time.time * shakeFrequency;
            float x = Mathf.PerlinNoise(shakeSeed, time) * 2f - 1f;
            float y = Mathf.PerlinNoise(shakeSeed + 31.7f, time) * 2f - 1f;
            return lookRotation * new Vector3(x, y, 0f) * Mathf.Min(maxShakeOffset, shakeAmplitude);
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

        private bool ReadAimPressedThisFrame()
        {
            if (input != null)
            {
                return input.AimPressedThisFrame();
            }

            return Input.GetKeyDown(aimFallbackKey);
        }

        private void UpdateAimState()
        {
            if (!doubleClickRightMouseToToggleAds)
            {
                adsToggleActive = false;
                aiming = holdRightMouseToAimWhenDoubleClickDisabled && ReadAimHeld();
                return;
            }

            if (ReadAimPressedThisFrame())
            {
                float now = Time.unscaledTime;
                if (now - lastAimPressTime <= Mathf.Max(0.05f, adsDoubleClickWindow))
                {
                    adsToggleActive = !adsToggleActive;
                    lastAimPressTime = -999f;
                }
                else
                {
                    lastAimPressTime = now;
                }
            }

            aiming = adsToggleActive;
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
