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
#endif

        public bool UsingInputSystem => usingInputSystem;

        private void Awake()
        {
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
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && moveAction != null)
            {
                return moveAction.ReadValue<Vector2>();
            }
#endif
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }

        public Vector2 ReadLookDelta()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && lookAction != null)
            {
                return lookAction.ReadValue<Vector2>();
            }
#endif
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        }

        public bool JumpPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && jumpAction != null)
            {
                return jumpAction.WasPressedThisFrame();
            }
#endif
            return Input.GetKeyDown(jumpFallbackKey);
        }

        public bool SprintHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && sprintAction != null)
            {
                return sprintAction.IsPressed();
            }
#endif
            return Input.GetKey(sprintFallbackKey);
        }

        public bool CrouchHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && crouchAction != null)
            {
                return crouchAction.IsPressed();
            }
#endif
            return Input.GetKey(crouchFallbackKey);
        }

        public bool CrouchPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && crouchAction != null)
            {
                return crouchAction.WasPressedThisFrame();
            }
#endif
            return Input.GetKeyDown(crouchFallbackKey);
        }

        public bool ToggleViewPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && toggleViewAction != null)
            {
                return toggleViewAction.WasPressedThisFrame();
            }
#endif
            return Input.GetKeyDown(toggleViewFallbackKey);
        }

        public bool InteractPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && interactAction != null)
            {
                return interactAction.WasPressedThisFrame();
            }
#endif
            return Input.GetKeyDown(interactFallbackKey);
        }

        public bool FireHeld()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && fireAction != null)
            {
                return fireAction.IsPressed();
            }
#endif
            return Input.GetKey(fireFallbackKey);
        }

        public bool ReloadPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (usingInputSystem && reloadAction != null)
            {
                return reloadAction.WasPressedThisFrame();
            }
#endif
            return Input.GetKeyDown(reloadFallbackKey);
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
        }
#endif
    }
}
