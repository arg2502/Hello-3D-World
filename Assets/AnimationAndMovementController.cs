using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimationAndMovementController : MonoBehaviour
{
    private PlayerInput playerInput;
    private CharacterController characterController;
    private Animator animator;

    private int isWalkingHash;
    private int isRunningHash;

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
    private bool isJumpAnimating = false;
    private int jumpCount = 0;
    Dictionary<int, float> initialJumpVelocities = new Dictionary<int, float>();
    Dictionary<int, float> jumpGravities = new Dictionary<int, float>(); 
    private Coroutine currentJumpResetCoroutine = null;
    
    [SerializeField] private float movementSpeed;
    [SerializeField] private float runMultiplier;

    private void Awake() 
    {
        playerInput = new PlayerInput();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
        isJumpingHash = Animator.StringToHash("isJumping");
        jumpCountHash = Animator.StringToHash("jumpCount");

        playerInput.CharacterControls.Move.started += OnMovementInput;
        playerInput.CharacterControls.Move.canceled += OnMovementInput;
        playerInput.CharacterControls.Move.performed += OnMovementInput;
        
        playerInput.CharacterControls.Run.started += OnRun;
        playerInput.CharacterControls.Run.canceled += OnRun;

        playerInput.CharacterControls.Jump.started += OnJump;
        playerInput.CharacterControls.Jump.canceled += OnJump;

        SetupJumpVariables();

    }

    private void OnEnable() 
    {
        playerInput.CharacterControls.Enable();
    }

    private void OnDisable() 
    {
        playerInput.CharacterControls.Disable();
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
    }

    private void SetupJumpVariables()
    {
        // just copied from tutorial -- look up more about the math/science behind this:
        // https://www.youtube.com/watch?v=hG9SzQxaCm8
        float timeToApex = maxJumpTime / 2;
        gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;

        float secondJumpGravity = (-2 * (maxJumpHeight + 1)) / Mathf.Pow((timeToApex * 1.25f), 2);
        float secondJumpInitialVelocity = (2 * (maxJumpHeight + 1)) / (timeToApex * 1.25f);
        float thirdJumpGravity = (-2 * (maxJumpHeight + 3)) / Mathf.Pow((timeToApex * 1.75f), 2);
        float thirdJumpInitialVelocity = (2 * (maxJumpHeight + 3)) / (timeToApex * 1.75f);

        initialJumpVelocities.Add(1, initialJumpVelocity);
        initialJumpVelocities.Add(2, secondJumpInitialVelocity);
        initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

        jumpGravities.Add(0, gravity);
        jumpGravities.Add(1, gravity);
        jumpGravities.Add(2, secondJumpGravity);
        jumpGravities.Add(3, thirdJumpGravity);
    }

    private void HandleJump()
    {
        // if (!isJumping && characterController.isGrounded && isJumpPressed)
        // {
        //     if (jumpCount < 3 && currentJumpResetCoroutine != null)
        //     {
        //         StopCoroutine(currentJumpResetCoroutine);
        //     }
        //     animator.SetBool(isJumpingHash, true);
        //     isJumpAnimating = true;
        //     isJumping = true;
        //     jumpCount += 1;
        //     animator.SetInteger(jumpCountHash, jumpCount);
        //     currentMovement.y = initialJumpVelocities[jumpCount];
        //     appliedMovement.y = initialJumpVelocities[jumpCount];
        // }
        // else if (!isJumpPressed && isJumping && characterController.isGrounded)
        // {
        //     isJumping = false;
        // }
    }

    private IEnumerator JumpResetCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        jumpCount = 0;
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

    private void HandleAnimation()
    {
        bool isWalking = animator.GetBool(isWalkingHash);
        bool isRunning = animator.GetBool(isRunningHash);

        if (isMovementPressed && !isWalking)
        {
            animator.SetBool(isWalkingHash, true);
        }
        else if (!isMovementPressed && isWalking)
        {
            animator.SetBool(isWalkingHash, false);
        }

        if ((isMovementPressed && isRunPressed) && !isRunning)
        {
            animator.SetBool(isRunningHash, true);
        }
        else if ((!isMovementPressed || !isRunPressed) && isRunning)
        {
            animator.SetBool(isRunningHash, false);
        }
    }

    private void HandleGravity()
    {
        // bool isFalling = currentMovement.y <= 0f || !isJumpPressed;
        // float fallMultiplier = 2f;
        // if (characterController.isGrounded)
        // {
        //     if (isJumpAnimating)
        //     {
        //         // animator.SetBool(isJumpingHash, false);
        //         // isJumpAnimating = false;
        //         // currentJumpResetCoroutine = StartCoroutine(JumpResetCoroutine());
        //         // if (jumpCount == 3)
        //         // {
        //         //     jumpCount = 0;
        //         //     animator.SetInteger(jumpCountHash, jumpCount);
        //         // }
        //     }
        //     // currentMovement.y = groundedGravity;
        //     // appliedMovement.y = groundedGravity;
        // }
        // else if (isFalling)
        // {
        //     float previousYVelocity = currentMovement.y;
        //     currentMovement.y = currentMovement.y + (jumpGravities[jumpCount] * fallMultiplier * Time.deltaTime);
        //     appliedMovement.y = Mathf.Max((previousYVelocity + currentMovement.y) * 0.5f, -20f);
        // }
        // else
        // {
        //     float previousYVelocity = currentMovement.y;
        //     currentMovement.y = currentMovement.y + (jumpGravities[jumpCount] * Time.deltaTime);
        //     appliedMovement.y = (previousYVelocity + currentMovement.y) * 0.5f;
        // }
    }

    private void Update() 
    {
        HandleRotation();
        HandleAnimation();

        if (isRunPressed)
        {
            appliedMovement.x = currentRunMovement.x;
            appliedMovement.z = currentRunMovement.z;
        }
        else
        {
            appliedMovement.x = currentMovement.x;
            appliedMovement.z = currentMovement.z;
        }

        characterController.Move(appliedMovement * Time.deltaTime * movementSpeed);
        
        HandleGravity();
        HandleJump();
    }
}
