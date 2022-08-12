using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    [SerializeField] private LayerMask platformLayerMask;
    private PlayerInput playerInput;
    private CharacterController characterController;
    private Animator animator;

    private int isWalkingHash;
    private int isRunningHash;
    private int isFallingHash;

    private float movementSpeed = 2.5f;
    private float runMultiplier = 2f;
    private Vector2 currentMovementInput;
    private Vector3 currentMovement;
    private Vector3 currentRunMovement;
    private Vector3 appliedMovement;
    private bool isMovementPressed;
    private bool isRunPressed;
    private float rotationFactorPerFrame = 15.0f;

    // gravity
    private float gravity = -9.8f;
    private float groundedGravity = -0.05f;

    // jumping
    private bool isJumpPressed = false;
    private float initialJumpVelocity;
    private float maxJumpHeight = 1.20f;
    private float maxJumpTime = 0.85f;
    private bool isJumping = false;
    private int isJumpingHash;
    private int jumpCountHash;
    private bool requiredNewJumpPress = false;
    private int jumpCount = 0;
    Dictionary<int, float> initialJumpVelocities = new Dictionary<int, float>();
    Dictionary<int, float> jumpGravities = new Dictionary<int, float>(); 
    private Coroutine currentJumpResetCoroutine = null;
    
    // State variables
    PlayerBaseState _currentState;
    PlayerStateFactory _states;

    // getters and setters
    public PlayerBaseState CurrentState { get { return _currentState; } set { _currentState = value; } }
    public bool IsJumpPressed { get { return isJumpPressed; } }
    public Animator Animator { get { return animator; } }
    public Coroutine CurrentJumpResetCoroutine { get { return currentJumpResetCoroutine; } set { currentJumpResetCoroutine = value; } }
    public Dictionary<int, float> InitialJumpVelocities { get { return initialJumpVelocities; } }
    public int JumpCount { get { return jumpCount; } set { jumpCount = value;} }
    public int IsJumpingHash { get { return isJumpingHash; } }
    public int JumpCountHash { get { return jumpCountHash; } }
    public bool RequiredNewJumpPress { get { return requiredNewJumpPress; } set { requiredNewJumpPress = value; } }
    public bool IsJumping { set { isJumping = value; } }
    public float CurrentMovementY { get { return currentMovement.y; } set { currentMovement.y = value; } }
    public float AppliedMovementX { get { return appliedMovement.x; } set { appliedMovement.x = value; } }
    public float AppliedMovementY { get { return appliedMovement.y; } set { appliedMovement.y = value; } }
    public float AppliedMovementZ { get { return appliedMovement.z; } set { appliedMovement.z = value; } }
    public CharacterController CharacterController { get { return characterController; } }
    // public float GroundedGravity { get { return groundedGravity; } }
    public float Gravity { get { return gravity; } }
    public Dictionary<int, float> JumpGravities { get { return jumpGravities; } }
    public bool IsMovementPressed { get { return isMovementPressed; } }
    public bool IsRunPressed { get { return isRunPressed; } }
    public int IsWalkingHash { get { return isWalkingHash; } }
    public int IsRunningHash { get { return isRunningHash; } }
    public int IsFallingHash { get { return isFallingHash; } }
    public Vector2 CurrentMovementInput { get { return currentMovementInput; } }
    public float RunMultiplier { get { return runMultiplier; } }
    public bool IsGrounded { get { return IsPlayerGrounded(); } }

    private void Awake()
    {
        playerInput = new PlayerInput();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // setup state
        _states = new PlayerStateFactory(this);
        _currentState = _states.Grounded();
        _currentState.EnterState();

        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
        isJumpingHash = Animator.StringToHash("isJumping");
        jumpCountHash = Animator.StringToHash("jumpCount");
        isFallingHash = Animator.StringToHash("isFalling");

        playerInput.CharacterControls.Move.started += OnMovementInput;
        playerInput.CharacterControls.Move.canceled += OnMovementInput;
        playerInput.CharacterControls.Move.performed += OnMovementInput;
        
        playerInput.CharacterControls.Run.started += OnRun;
        playerInput.CharacterControls.Run.canceled += OnRun;

        playerInput.CharacterControls.Jump.started += OnJump;
        playerInput.CharacterControls.Jump.canceled += OnJump;

        SetupJumpVariables();
    }

    private void Start()
    {
        characterController.Move(appliedMovement * Time.deltaTime);
    } 

    private void OnEnable() 
    {
        playerInput.CharacterControls.Enable();
    }

    private void OnDisable() 
    {
        playerInput.CharacterControls.Disable();
    }

    private void SetupJumpVariables()
    {
        // just copied from tutorial -- look up more about the math/science behind this:
        // https://www.youtube.com/watch?v=hG9SzQxaCm8
        float timeToApex = maxJumpTime / 2;
        float initialGravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;

        float secondJumpGravity = (-2 * (maxJumpHeight + 1)) / Mathf.Pow((timeToApex * 1.25f), 2);
        float secondJumpInitialVelocity = (2 * (maxJumpHeight + 1)) / (timeToApex * 1.25f);
        float thirdJumpGravity = (-2 * (maxJumpHeight + 3)) / Mathf.Pow((timeToApex * 1.75f), 2);
        float thirdJumpInitialVelocity = (2 * (maxJumpHeight + 3)) / (timeToApex * 1.75f);

        initialJumpVelocities.Add(1, initialJumpVelocity);
        initialJumpVelocities.Add(2, secondJumpInitialVelocity);
        initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

        jumpGravities.Add(0, initialGravity);
        jumpGravities.Add(1, initialGravity);
        jumpGravities.Add(2, secondJumpGravity);
        jumpGravities.Add(3, thirdJumpGravity);
    }

    private void OnMovementInput(InputAction.CallbackContext context)
    {
        Debug.Log($"currentMovementInput: {currentMovementInput}");
        currentMovementInput = context.ReadValue<Vector2>();
        currentMovement.x = currentMovementInput.x;
        currentMovement.z = currentMovementInput.y;
        currentRunMovement.x = currentMovementInput.x * runMultiplier;
        currentRunMovement.z = currentMovementInput.y * runMultiplier;
        isMovementPressed = currentMovementInput.x != 0 || currentMovementInput.y != 0;
    }

    private void OnRun(InputAction.CallbackContext context)
    {
        isRunPressed = context.ReadValueAsButton();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        isJumpPressed = context.ReadValueAsButton();
        requiredNewJumpPress = false;
    }

    private void HandleRotation()
    {
        Vector3 positionToLookAt;

        // The change in position our character should point to
        positionToLookAt.x = currentMovement.x;
        positionToLookAt.y = 0f;
        positionToLookAt.z = currentMovement.z;

        // The current rotation of our character
        Quaternion currentRotation = transform.rotation;

        if (isMovementPressed)
        {
            // Creates a new rotation based on where the player is currently pressing
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFactorPerFrame * Time.deltaTime);
        }
    }

    private bool IsPlayerGrounded()
    {
        var center = characterController.transform.position + characterController.center;
        var distance = characterController.bounds.extents.y + 0.1f;
        return Physics.Raycast(center, Vector3.down, distance, platformLayerMask);
    }

    private void Update()
    {
        HandleRotation();
        _currentState.UpdateStates();
        characterController.Move(appliedMovement * Time.deltaTime * movementSpeed);
    }

}
