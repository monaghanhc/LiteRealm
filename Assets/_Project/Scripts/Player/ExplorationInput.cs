using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LiteRealm.Player
{
    [DisallowMultipleComponent]
    public class ExplorationInput : MonoBehaviour
    {
        [SerializeField] private bool useInputSystemWhenAvailable = true;

        [Header("Legacy Fallback Keys")]
        [SerializeField] private KeyCode jumpFallbackKey = KeyCode.Space;
        [SerializeField] private KeyCode sprintFallbackKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode crouchFallbackKey = KeyCode.LeftControl;
        [SerializeField] private KeyCode toggleViewFallbackKey = KeyCode.V;
        [SerializeField] private KeyCode interactFallbackKey = KeyCode.E;
        [SerializeField] private KeyCode reloadFallbackKey = KeyCode.R;
        [SerializeField] private KeyCode fireFallbackKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode aimFallbackKey = KeyCode.Mouse1;

        [Header("Optional")]
        [SerializeField] private PlayerStats playerStats;

        private bool initialized;
        private bool usingInputSystem;

#if ENABLE_INPUT_SYSTEM
        private InputActionMap actionMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction sprintAction;
        private InputAction crouchAction;
        private InputAction toggleViewAction;
        private InputAction interactAction;
        private InputAction fireAction;
        private InputAction reloadAction;
        private InputAction aimAction;
#endif

        public bool UsingInputSystem => usingInputSystem;

        private void Awake()
        {
            if (playerStats == null)
            {
                playerStats = GetComponent<PlayerStats>();
            }
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            EnableActions();
        }

        private void OnDisable()
        {
            DisableActions();
        }

        public Vector2 ReadMove()
        {
            if (playerStats != null && playerStats.IsDead)
            {
                return Vector2.zero;
            }
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && moveAction != null)
            {
                Vector2 v = moveAction.ReadValue<Vector2>();
                if (v.sqrMagnitude > 0.0001f)
                {
                    return v;
                }
            }
#endif
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }

        public Vector2 ReadLookDelta()
        {
            if (playerStats != null && playerStats.IsDead)
            {
                return Vector2.zero;
            }
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && lookAction != null)
            {
                Vector2 v = lookAction.ReadValue<Vector2>();
                if (v.sqrMagnitude > 0.0001f)
                {
                    return v;
                }
            }
#endif
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        }

        public bool JumpPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && jumpAction != null && jumpAction.WasPressedThisFrame())
            {
                return true;
            }
#endif
            return Input.GetKeyDown(jumpFallbackKey);
        }

        public bool SprintHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && sprintAction != null && sprintAction.IsPressed())
            {
                return true;
            }
#endif
            return Input.GetKey(sprintFallbackKey);
        }

        public bool CrouchHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && crouchAction != null && crouchAction.IsPressed())
            {
                return true;
            }
#endif
            return Input.GetKey(crouchFallbackKey);
        }

        public bool CrouchPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && crouchAction != null && crouchAction.WasPressedThisFrame())
            {
                return true;
            }
#endif
            return Input.GetKeyDown(crouchFallbackKey);
        }

        public bool ToggleViewPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && toggleViewAction != null && toggleViewAction.WasPressedThisFrame())
            {
                return true;
            }
#endif
            return Input.GetKeyDown(toggleViewFallbackKey);
        }

        public bool InteractPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && interactAction != null && interactAction.WasPressedThisFrame())
            {
                return true;
            }
#endif
            return Input.GetKeyDown(interactFallbackKey);
        }

        public bool FireHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && fireAction != null && fireAction.IsPressed())
            {
                return true;
            }
#endif
            return Input.GetKey(fireFallbackKey);
        }

        public bool ReloadPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && reloadAction != null && reloadAction.WasPressedThisFrame())
            {
                return true;
            }
#endif
            return Input.GetKeyDown(reloadFallbackKey);
        }

        public bool AimHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && aimAction != null && aimAction.IsPressed())
            {
                return true;
            }
#endif
            return Input.GetKey(aimFallbackKey);
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            usingInputSystem = useInputSystemWhenAvailable;
            if (usingInputSystem)
            {
                BuildActions();
            }
#else
            usingInputSystem = false;
#endif

            initialized = true;
        }

        private void EnableActions()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && actionMap != null)
            {
                actionMap.Enable();
            }
#endif
        }

        private void DisableActions()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && actionMap != null)
            {
                actionMap.Disable();
            }
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private void BuildActions()
        {
            if (actionMap != null)
            {
                return;
            }

            try
            {
                actionMap = new InputActionMap("Gameplay");

            moveAction = actionMap.AddAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddBinding("<Gamepad>/leftStick");

            lookAction = actionMap.AddAction("Look", InputActionType.Value);
            lookAction.AddBinding("<Mouse>/delta");
            lookAction.AddBinding("<Gamepad>/rightStick");

            jumpAction = actionMap.AddAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");

            sprintAction = actionMap.AddAction("Sprint", InputActionType.Button);
            sprintAction.AddBinding("<Keyboard>/leftShift");
            sprintAction.AddBinding("<Gamepad>/leftStickPress");

            crouchAction = actionMap.AddAction("Crouch", InputActionType.Button);
            crouchAction.AddBinding("<Keyboard>/leftCtrl");
            crouchAction.AddBinding("<Gamepad>/rightStickPress");

            toggleViewAction = actionMap.AddAction("ToggleView", InputActionType.Button);
            toggleViewAction.AddBinding("<Keyboard>/v");
            toggleViewAction.AddBinding("<Gamepad>/dpad/up");

            interactAction = actionMap.AddAction("Interact", InputActionType.Button);
            interactAction.AddBinding("<Keyboard>/e");
            interactAction.AddBinding("<Gamepad>/buttonWest");

            fireAction = actionMap.AddAction("Fire", InputActionType.Button);
            fireAction.AddBinding("<Mouse>/leftButton");
            fireAction.AddBinding("<Gamepad>/rightTrigger");

            reloadAction = actionMap.AddAction("Reload", InputActionType.Button);
            reloadAction.AddBinding("<Keyboard>/r");
            reloadAction.AddBinding("<Gamepad>/buttonNorth");

            aimAction = actionMap.AddAction("Aim", InputActionType.Button);
            aimAction.AddBinding("<Mouse>/rightButton");
            aimAction.AddBinding("<Gamepad>/leftTrigger");
            }
            catch (System.Exception)
            {
                usingInputSystem = false;
                actionMap = null;
            }
        }
#endif
    }
}
