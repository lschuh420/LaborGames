using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

// This script implements a basic third-person character controller for Unity.
// It handles movement, sprinting, jumping, camera zoom, and animation updates.
// Attach this script to your player GameObject and assign the required components in the inspector.
[RequireComponent(typeof(CharacterController))]
public class TP_Character_Controller : MonoBehaviour
{

    [Header("Movement Settings")]
    public float walkSpeed = 3f; // Speed when walking
    public float sprintSpeed = 7f; // Speed when sprinting
    public float rotationSpeed = 10f; // How fast the character rotates
    public float jumpHeight = 1.5f; // Height of the jump
    public float gravity = -20f; // Gravity applied to the character

    [Header("Mouse Settings")]
    public float zoomSpeed = 2f; // How fast the camera zooms in/out
    public float zoomLerpSpeed = 10f; // How smoothly the camera zooms
    public float minDistance = 3f; // Minimum camera distance
    public float maxDistance = 15f; // Maximum camera distance
    [Range(0.1f, 5f)]
    public float mouseSensitivity = 1f; // Mouse sensitivity for camera rotation
    
    // Private variables for internal logic
    private CharacterController _controller; // Reference to the CharacterController component
    private CinemachineCamera _camera; // Reference to the Cinemachine camera
    private CinemachineOrbitalFollow _orbit; // Reference to the camera's orbital follow script
    private CinemachineInputAxisController _camAxisController; // Controls camera axis input
    private Animator _animator; // Reference to the Animator component
    private Vector2 _moveInput; // Stores movement input from the player
    private Vector3 _velocity; // Stores current velocity (for gravity/jumping)
    private bool _isSprinting; // Is the player currently sprinting?
    private bool _isGrounded; // Is the player on the ground?
    private Vector2 scrollDelta; // Mouse scroll input for zoom
    private float targetZoom; // Target camera zoom distance
    private float currentZoom; // Current camera zoom distance

    // Animator Parameter Hashes (for performance)
    private readonly int _speedHash = Animator.StringToHash("Speed");
    private readonly int _jumpHash = Animator.StringToHash("Jump");
    private readonly int _freeFallHash = Animator.StringToHash("FreeFall");
    private readonly int _groundedHash = Animator.StringToHash("Grounded");

    void Start()
    {
        // Get references to required components
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        _camera = GetComponentInChildren<CinemachineCamera>();
        _orbit = GetComponentInChildren<CinemachineOrbitalFollow>();
        _camAxisController = GetComponentInChildren<CinemachineInputAxisController>();

        // Initialize camera zoom
        targetZoom = currentZoom = _orbit.Radius;

        // Lock the cursor to the game window
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Handle player movement and camera every frame
        HandleMovement();
        UpdateAnimator();
        ApplyMouseSensitivity();
    }

    // Updates the camera's mouse sensitivity based on the inspector value
    private void ApplyMouseSensitivity()
    {
        if(_camAxisController == null) return;

        foreach (var controller in _camAxisController.Controllers)
        {
            controller.Input.Gain = mouseSensitivity;
        }
    }

    // Public Input Methods (called by Unity's Input System)

    // Called when movement input is received (WASD/left stick)
    public void OnMove(InputAction.CallbackContext context) => _moveInput = context.ReadValue<Vector2>();

    // Called when jump input is pressed
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && _isGrounded)
        {
            // Calculate jump velocity using physics formula
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _animator.SetBool(_jumpHash, true);
        }
    }

    // Called when sprint input is pressed or released
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed) _isSprinting = true;
        else if (context.canceled) _isSprinting = false;
    }

    // Called when mouse scroll input is detected (for zoom)
    public void OnMouseZoom(InputAction.CallbackContext context)
    {
        if(context.performed) {
            HandleMouseScroll();
            scrollDelta = context.ReadValue<Vector2>();
        }
    }

    // Handles camera zooming in and out with the mouse scroll wheel
    private void HandleMouseScroll()
    {
        if(scrollDelta.y != 0)
        {
            if(_orbit != null)
            {
                // Adjust the target zoom based on scroll input
                targetZoom = Mathf.Clamp(_orbit.Radius - scrollDelta.y * zoomSpeed, minDistance, maxDistance);
                scrollDelta = Vector2.zero;
            }
        }

        // Smoothly interpolate the camera zoom
        currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime*zoomLerpSpeed);
        _orbit.Radius = currentZoom;
    }

    // Handles all character movement, jumping, and gravity
    private void HandleMovement()
    {
        // Check if the character is on the ground
        _isGrounded = _controller.isGrounded;
        
        if (_isGrounded && _velocity.y < 0) 
        {
            // Reset downward velocity when grounded
            _velocity.y = -2f;
            // Reset the Jump bool once we are back on the ground
            _animator.SetBool(_jumpHash, false);
        }

        // Calculate movement direction relative to the camera
        Transform cameraTransform = _camera.transform;
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Combine input with camera direction
        Vector3 moveDirection = forward * _moveInput.y + right * _moveInput.x;

        if (moveDirection.magnitude > 0.1f)
        {
            // Rotate the character to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // Move the character at the correct speed
            float targetSpeed = _isSprinting ? sprintSpeed : walkSpeed;
            _controller.Move(moveDirection * targetSpeed * Time.deltaTime);
        }

        // Apply gravity
        _velocity.y += gravity * Time.deltaTime;
        _controller.Move(_velocity * Time.deltaTime);
    }

    // Updates animator parameters for smooth transitions and correct animation states
    private void UpdateAnimator()
    {
        // Set the speed parameter for blend tree (walk/run)
        float currentSpeed = _moveInput.magnitude * (_isSprinting ? 1f : 0.5f);
        _animator.SetFloat(_speedHash, currentSpeed, 0.1f, Time.deltaTime);

        // Set grounded state for jump/land animations
        _animator.SetBool(_groundedHash, _isGrounded);

        // Set free fall state for falling animation
        bool isFalling = !_isGrounded && _velocity.y < -1f; 
        _animator.SetBool(_freeFallHash, isFalling);
    }
}
