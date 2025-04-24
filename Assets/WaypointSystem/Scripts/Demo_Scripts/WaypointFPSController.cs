using UnityEngine;

namespace WrightAngle.Waypoint
{
    /// <summary>
    /// Provides basic first-person movement (WASD) and mouse-look camera control.
    /// Requires a CharacterController component on the same GameObject.
    /// Designed for simple integration, ideal for testing waypoint systems or basic prototypes.
    /// Ensure your main camera is a child of this GameObject.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [AddComponentMenu("WrightAngle/Waypoint FPS Controller")]
    public class WaypointFPSController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Controls the player's movement speed.")]
        [SerializeField] private float moveSpeed = 5.0f;

        [Header("Look Settings")]
        [Tooltip("Adjusts the sensitivity of the mouse look.")]
        [SerializeField] private float mouseSensitivity = 2.0f;
        [Tooltip("Clamps the minimum vertical camera angle (degrees).")]
        [SerializeField] private float verticalLookMin = -85.0f;
        [Tooltip("Clamps the maximum vertical camera angle (degrees).")]
        [SerializeField] private float verticalLookMax = 85.0f;

        [Header("References")]
        [Tooltip("Assign the Transform of the player's camera (usually a child object). If left empty, it will attempt to find a child Camera.")]
        [SerializeField] private Transform playerCameraTransform;

        // Cached component for efficient access
        private CharacterController characterController;

        // Internal variable to track vertical camera rotation for clamping
        private float verticalRotation = 0f;

        void Awake()
        {
            // Cache the required CharacterController component.
            characterController = GetComponent<CharacterController>();

            // Automatically find and assign the player camera if not set in the Inspector.
            if (playerCameraTransform == null)
            {
                Camera mainCam = GetComponentInChildren<Camera>();
                if (mainCam != null)
                {
                    playerCameraTransform = mainCam.transform;
                }

                // Log an error and disable the script if no camera is found.
                if (playerCameraTransform == null)
                {
                    Debug.LogError($"<b>[{gameObject.name}] WaypointFPSController Error:</b> Player Camera Transform is not assigned and a child camera could not be found. Please assign the camera transform or ensure a Camera component exists on a child GameObject.", this);
                    enabled = false; // Prevent runtime errors if camera setup is invalid.
                    return;
                }
            }

            // Lock the cursor to the game window and hide it for a standard FPS experience.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            // Process player input and update camera and position each frame.
            HandleLook();
            HandleMovement();
        }

        /// <summary>
        /// Manages camera rotation based on mouse input.
        /// Handles horizontal (player body yaw) and vertical (camera pitch) rotation.
        /// </summary>
        private void HandleLook()
        {
            // Get raw mouse input for immediate responsiveness.
            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            // Rotate the entire player controller horizontally around the Y-axis.
            transform.Rotate(Vector3.up * mouseX);

            // Calculate vertical rotation (pitch), inverting mouse Y input for standard FPS controls.
            verticalRotation -= mouseY;
            // Clamp the vertical rotation to the defined minimum and maximum angles.
            verticalRotation = Mathf.Clamp(verticalRotation, verticalLookMin, verticalLookMax);

            // Apply the clamped vertical rotation only to the camera's local X-axis rotation.
            playerCameraTransform.localEulerAngles = new Vector3(verticalRotation, 0f, 0f);
        }

        /// <summary>
        /// Manages player movement based on WASD or Arrow key input.
        /// Uses the CharacterController for smooth, collision-aware movement.
        /// </summary>
        private void HandleMovement()
        {
            // Get smoothed movement input axes.
            float horizontalInput = Input.GetAxis("Horizontal"); // Left/Right
            float verticalInput = Input.GetAxis("Vertical");     // Forward/Backward

            // Calculate movement direction relative to the player's current orientation.
            Vector3 moveDirection = (transform.forward * verticalInput + transform.right * horizontalInput).normalized;

            // Calculate the final velocity vector based on speed.
            Vector3 velocity = moveDirection * moveSpeed;

            // Apply movement using the CharacterController, scaled by delta time for frame-rate independence.
            characterController.Move(velocity * Time.deltaTime);

            // Note: This controller does not implement gravity or jumping.
            // You would typically add vertical velocity handling and ground checks for those features.
        }
    }
}