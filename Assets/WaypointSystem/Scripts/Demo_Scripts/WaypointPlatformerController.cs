using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace WrightAngle.Waypoint
{
    /// <summary>
    /// Provides extremely simple 2D side-scrolling movement (A/D or Left/Right Arrows).
    /// Requires a Rigidbody2D and a Collider2D component on the same GameObject.
    /// Designed for minimal setup, ideal for quick demos or testing basic horizontal movement.
    /// Includes direct camera following (no smoothing).
    /// Supports both the new Input System and legacy Input Manager automatically.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))] // e.g., BoxCollider2D or CapsuleCollider2D
    [AddComponentMenu("WrightAngle/Simple Platformer Controller (No Jump)")]
    public class WaypointPlatformerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Controls the player's horizontal movement speed.")]
        [SerializeField] private float moveSpeed = 5.0f;

        [Header("Camera Settings")]
        [Tooltip("Reference to the main camera transform for following.")]
        [SerializeField] private Transform mainCameraTransform;
        [Tooltip("Offset of the camera relative to the player.")]
        [SerializeField] private Vector3 cameraOffset = new Vector3(0, 1, -10); // Adjust Y offset as needed

        // Cached components
        private Rigidbody2D rb;

        // Internal variables
        private float horizontalInput;

        void Awake()
        {
            // Cache the required Rigidbody2D component.
            rb = GetComponent<Rigidbody2D>();

            // Automatically find and assign the main camera if not set in the Inspector.
            if (mainCameraTransform == null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    mainCameraTransform = mainCam.transform;
                }

                // Log an error and disable the script if no camera is found.
                if (mainCameraTransform == null)
                {
                    Debug.LogError($"<b>[{gameObject.name}] SimplePlatformerController Error:</b> Main Camera Transform is not assigned and Camera.main could not be found. Please assign the camera transform.", this);
                    enabled = false; // Prevent runtime errors if camera setup is invalid.
                    return;
                }
            }

            // Ensure gravity doesn't affect the player if you only want horizontal movement
            // If you *do* want gravity but no jump, set Gravity Scale back to 1 (or your desired value)
            // on the Rigidbody2D component in the Inspector.
            rb.gravityScale = 0;

            // Freeze rotation on the Z-axis to prevent the player from tipping over
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        void Update()
        {
            // Process player input each frame.
            HandleInput();
        }

        void FixedUpdate()
        {
            // Apply physics-based updates in FixedUpdate.
            HandleMovement();
            HandleCameraFollow(); // Camera updates can also be in LateUpdate
        }

        /// <summary>
        /// Reads horizontal input.
        /// </summary>
        private void HandleInput()
        {
#if ENABLE_INPUT_SYSTEM
            // New Input System: Read keyboard input directly
            if (Keyboard.current != null)
            {
                horizontalInput = 0f;

                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    horizontalInput -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    horizontalInput += 1f;
            }
            else
            {
                horizontalInput = 0f;
            }
#else
            // Legacy Input Manager
            horizontalInput = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right Arrow
#endif
        }

        /// <summary>
        /// Manages horizontal player movement based on input.
        /// Uses Rigidbody2D velocity for physics-based movement.
        /// </summary>
        private void HandleMovement()
        {
            // Set the horizontal velocity directly. Vertical velocity is ignored as gravity is off.
            // If gravity is enabled, use: rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, 0f);

            // Optional: Flip the player sprite based on movement direction
            // if (horizontalInput > 0.1f) // Use small thresholds to avoid flipping when standing still
            // {
            //     transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z); // Facing right
            // }
            // else if (horizontalInput < -0.1f)
            // {
            //     transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z); // Facing left
            // }
        }

        /// <summary>
        /// Makes the camera directly follow the player's position.
        /// </summary>
        private void HandleCameraFollow()
        {
            if (mainCameraTransform != null)
            {
                Vector3 targetPosition = transform.position + cameraOffset;
                // Ensure camera Z position remains constant or as defined in offset
                targetPosition.z = cameraOffset.z;
                mainCameraTransform.position = targetPosition; // Set position directly
            }
        }
    }
}