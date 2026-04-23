using LiteRealm.Core;
using UnityEngine;

namespace LiteRealm.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 4.75f;
        [SerializeField] private float sprintSpeed = 7.75f;
        [SerializeField] private float crouchSpeed = 2.65f;
        [SerializeField] private float jumpHeight = 1.22f;
        [SerializeField] private float gravity = -23f;

        [Header("Grounded Feel")]
        [SerializeField] private float groundAcceleration = 46f;
        [SerializeField] private float groundDeceleration = 58f;
        [SerializeField] private float airAcceleration = 12f;
        [SerializeField] private float directionChangeAccelerationMultiplier = 1.35f;
        [SerializeField] private float groundedStickForce = -5.5f;
        [SerializeField] private float terminalVelocity = -55f;
        [SerializeField] [Range(0f, 1f)] private float landingDamping = 0.26f;

        [Header("Controller Tuning")]
        [SerializeField] private float controllerRadius = 0.35f;
        [SerializeField] private float slopeLimit = 48f;
        [SerializeField] private float stepOffset = 0.40f;
        [SerializeField] private float skinWidth = 0.06f;

        [Header("Stamina")]
        [SerializeField] private float sprintStaminaCostPerSecond = 18f;

        [Header("Crouch")]
        [SerializeField] private bool crouchToggleMode;
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchingHeight = 1.1f;
        [SerializeField] private float crouchTransitionSpeed = 10f;

        [Header("Fallback Input (Legacy Input Manager)")]
        [SerializeField] private KeyCode jumpFallbackKey = KeyCode.Space;
        [SerializeField] private KeyCode sprintFallbackKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode crouchFallbackKey = KeyCode.LeftControl;

        [Header("Audio")]
        [SerializeField] private AudioSource footstepAudio;
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private float walkStepInterval = 0.43f;
        [SerializeField] private float sprintStepInterval = 0.29f;
        [SerializeField] private float crouchStepInterval = 0.65f;

        [Header("References")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private ExplorationInput explorationInput;

        private CharacterController characterController;
        private Vector3 horizontalVelocity;
        private Vector3 verticalVelocity;
        private bool crouched;
        private bool wasGrounded;
        private float footstepTimer;
        private AudioClip[] _runtimeFootstepClips;

        public bool IsSprinting { get; private set; }
        public bool IsCrouched => crouched;
        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public float PlanarSpeed => new Vector3(Velocity.x, 0f, Velocity.z).magnitude;
        public Vector3 Velocity => characterController != null ? characterController.velocity : Vector3.zero;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }

            if (explorationInput == null)
            {
                explorationInput = GetComponent<ExplorationInput>();
            }

            if (characterController != null)
            {
                characterController.height = standingHeight;
                characterController.radius = controllerRadius;
                characterController.center = new Vector3(0f, standingHeight * 0.5f, 0f);
                characterController.slopeLimit = slopeLimit;
                characterController.stepOffset = stepOffset;
                characterController.skinWidth = skinWidth;
                characterController.minMoveDistance = 0f;
                characterController.enableOverlapRecovery = true;
            }

            if (footstepAudio == null)
            {
                footstepAudio = GetComponent<AudioSource>();
                if (footstepAudio == null)
                {
                    footstepAudio = gameObject.AddComponent<AudioSource>();
                }
                footstepAudio.playOnAwake = false;
                footstepAudio.spatialBlend = 1f;
            }

            if ((footstepClips == null || footstepClips.Length == 0) && footstepAudio != null)
            {
                _runtimeFootstepClips = new[] { ProceduralAudio.CreateFootstepClip() };
            }
        }

        private void OnDestroy()
        {
            if (_runtimeFootstepClips != null)
            {
                for (int i = 0; i < _runtimeFootstepClips.Length; i++)
                {
                    if (_runtimeFootstepClips[i] != null)
                    {
                        Destroy(_runtimeFootstepClips[i]);
                    }
                }
            }
        }

        private void Update()
        {
            if (playerStats != null && playerStats.IsDead)
            {
                return;
            }

            HandleCrouchInput();
            HandleMovement(Time.deltaTime);
            HandleFootsteps(Time.deltaTime);
        }

        private void HandleMovement(float deltaTime)
        {
            if (characterController == null)
            {
                return;
            }

            if (playerStats != null && playerStats.IsDead)
            {
                return;
            }

            bool grounded = characterController.isGrounded;
            if (grounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = wasGrounded ? groundedStickForce : groundedStickForce * landingDamping;
            }

            Vector2 moveInput = ReadMoveInput();
            if (moveInput.sqrMagnitude > 1f)
            {
                moveInput.Normalize();
            }

            Vector3 moveDirection = (transform.right * moveInput.x + transform.forward * moveInput.y);
            bool hasMovementInput = moveDirection.sqrMagnitude > 0.0001f;

            bool tryingSprint = ReadSprintHeld() && !crouched && hasMovementInput;
            IsSprinting = false;
            float speed = crouched ? crouchSpeed : walkSpeed;

            if (tryingSprint)
            {
                bool canSprint = playerStats == null || playerStats.ConsumeStamina(sprintStaminaCostPerSecond * deltaTime);
                if (canSprint)
                {
                    IsSprinting = true;
                    speed = sprintSpeed;
                }
            }

            Vector3 desiredHorizontalVelocity = moveDirection * speed;
            float acceleration = hasMovementInput
                ? (grounded ? groundAcceleration : airAcceleration)
                : groundDeceleration;

            if (hasMovementInput && horizontalVelocity.sqrMagnitude > 0.1f)
            {
                float directionDot = Vector3.Dot(horizontalVelocity.normalized, desiredHorizontalVelocity.normalized);
                if (directionDot < -0.15f)
                {
                    acceleration *= directionChangeAccelerationMultiplier;
                }
            }

            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, desiredHorizontalVelocity, acceleration * deltaTime);
            if (grounded && !hasMovementInput && horizontalVelocity.sqrMagnitude < 0.01f)
            {
                horizontalVelocity = Vector3.zero;
            }

            characterController.Move(horizontalVelocity * deltaTime);

            if (ReadJumpPressedThisFrame() && grounded && !crouched)
            {
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity.y += gravity * deltaTime;
            verticalVelocity.y = Mathf.Max(verticalVelocity.y, terminalVelocity);
            characterController.Move(Vector3.up * verticalVelocity.y * deltaTime);

            float targetHeight = crouched ? crouchingHeight : standingHeight;
            characterController.height = Mathf.Lerp(characterController.height, targetHeight, crouchTransitionSpeed * deltaTime);
            characterController.center = new Vector3(0f, characterController.height * 0.5f, 0f);
            wasGrounded = grounded;
        }

        private void HandleCrouchInput()
        {
            if (crouchToggleMode)
            {
                if (ReadCrouchPressedThisFrame())
                {
                    crouched = !crouched;
                }

                return;
            }

            crouched = ReadCrouchHeld();
        }

        private void HandleFootsteps(float deltaTime)
        {
            AudioClip[] clips = (footstepClips != null && footstepClips.Length > 0) ? footstepClips : _runtimeFootstepClips;
            if (characterController == null || footstepAudio == null || clips == null || clips.Length == 0)
            {
                return;
            }

            Vector3 horizontalVelocity = characterController.velocity;
            horizontalVelocity.y = 0f;
            bool moving = horizontalVelocity.sqrMagnitude > 0.2f;

            if (!characterController.isGrounded || !moving)
            {
                footstepTimer = 0f;
                return;
            }

            float interval = walkStepInterval;
            if (IsSprinting)
            {
                interval = sprintStepInterval;
            }
            else if (crouched)
            {
                interval = crouchStepInterval;
            }

            footstepTimer += deltaTime;
            if (footstepTimer >= interval)
            {
                footstepTimer = 0f;
                int clipIndex = Random.Range(0, clips.Length);
                footstepAudio.PlayOneShot(clips[clipIndex]);
            }
        }

        private Vector2 ReadMoveInput()
        {
            if (explorationInput != null)
            {
                return explorationInput.ReadMove();
            }

            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }

        private bool ReadJumpPressedThisFrame()
        {
            if (explorationInput != null)
            {
                return explorationInput.JumpPressedThisFrame();
            }

            return Input.GetKeyDown(jumpFallbackKey);
        }

        private bool ReadSprintHeld()
        {
            if (explorationInput != null)
            {
                return explorationInput.SprintHeld();
            }

            return Input.GetKey(sprintFallbackKey);
        }

        private bool ReadCrouchHeld()
        {
            if (explorationInput != null)
            {
                return explorationInput.CrouchHeld();
            }

            return Input.GetKey(crouchFallbackKey);
        }

        private bool ReadCrouchPressedThisFrame()
        {
            if (explorationInput != null)
            {
                return explorationInput.CrouchPressedThisFrame();
            }

            return Input.GetKeyDown(crouchFallbackKey);
        }
    }
}
