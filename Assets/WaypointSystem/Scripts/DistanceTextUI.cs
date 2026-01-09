using UnityEngine;
using TMPro;

namespace WrightAngle.Waypoint
{
    /// <summary>
    /// Displays and updates the distance text for a waypoint marker using TextMeshProUGUI.
    /// Attach this component anywhere within your waypoint marker prefab and assign the TextMeshProUGUI used for the distance label.
    /// The component automatically formats and colors the distance based on DistanceTextSettings.
    /// </summary>
    [AddComponentMenu("WrightAngle/Distance Text UI")]
    [RequireComponent(typeof(RectTransform))]
    public class DistanceTextUI : MonoBehaviour
    {
        [Header("UI Reference")]
        [Tooltip("The TextMeshProUGUI component used to display the distance. If not assigned, will attempt to find one on this GameObject.")]
        [SerializeField] private TextMeshProUGUI distanceText;

        [Header("Settings")]
        [Tooltip("The DistanceTextSettings asset that controls formatting and appearance. Can be overridden per-marker.")]
        [SerializeField] private DistanceTextSettings settings;

        [Header("Orientation")]
        [Tooltip("When enabled, the distance text will counter-rotate to remain upright, even when the marker rotates (e.g., off-screen indicators).")]
        [SerializeField] private bool keepUpright = true;

        // Cached components
        private RectTransform distanceTextRectTransform;
        private Transform uprightReferenceTransform;
        private Transform markerTransform;
        private bool isInitialized;

        // Performance: Cache last displayed value to avoid unnecessary updates
        private float lastDistance = -1f;
        private bool lastOnScreen = true;
        private const float ROTATION_DOT_EPSILON = 0.99999f;
        private Quaternion initialTextLocalRotation = Quaternion.identity;
        private Quaternion lastMarkerLocalRotation = Quaternion.identity;

        /// <summary>
        /// Gets or sets the settings asset. Allows runtime configuration changes.
        /// </summary>
        public DistanceTextSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        /// <summary>
        /// Checks if the distance text component is properly configured.
        /// Used by WaypointMarkerUI for validation.
        /// </summary>
        public bool IsValid => distanceText != null && settings != null;

        private void Awake()
        {
            // Auto-detect TextMeshProUGUI if not assigned
            if (distanceText == null)
            {
                distanceText = GetComponent<TextMeshProUGUI>();
                if (distanceText == null)
                {
                    distanceText = GetComponentInChildren<TextMeshProUGUI>();
                }

                if (distanceText != null)
                {
                    Debug.LogWarning($"<b>[{gameObject.name}] DistanceTextUI:</b> 'Distance Text' was not assigned. Auto-detected TextMeshProUGUI component '{distanceText.name}'. Assign it in the Inspector to avoid this warning.", this);
                }
                else
                {
                    Debug.LogError($"<b>[{gameObject.name}] DistanceTextUI Error:</b> 'Distance Text' is not assigned and no TextMeshProUGUI component was found. Add a TextMeshProUGUI component and assign it to the 'Distance Text' field.", this);
                    enabled = false;
                    return;
                }
            }

            // Optimize performance by disabling raycast target (distance text is typically non-interactive)
            distanceText.raycastTarget = false;

            distanceTextRectTransform = distanceText.rectTransform;
            initialTextLocalRotation = distanceTextRectTransform.localRotation;

            WaypointMarkerUI marker = GetComponentInParent<WaypointMarkerUI>();
            markerTransform = marker != null ? marker.transform : null;

            // Cache the Canvas transform so we can keep the text aligned with it (works across all Canvas render modes).
            Canvas canvas = distanceText.canvas;
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>();
            }
            uprightReferenceTransform = canvas != null ? canvas.transform : null;
            
            isInitialized = true;
        }

        private void LateUpdate()
        {
            // LateUpdate ensures this runs after the marker has been positioned/rotated.
            if (!isInitialized || !keepUpright || distanceTextRectTransform == null) return;

            // Preferred path: counter-rotate against the marker's local rotation.
            // This is the cheapest/most stable method and avoids dependence on hierarchy depth.
            if (markerTransform != null && markerTransform != distanceTextRectTransform)
            {
                Quaternion markerLocalRotation = markerTransform.localRotation;

                // Skip if the marker rotation hasn't changed (optimization).
                if (Mathf.Abs(Quaternion.Dot(markerLocalRotation, lastMarkerLocalRotation)) >= ROTATION_DOT_EPSILON) return;
                lastMarkerLocalRotation = markerLocalRotation;

                distanceTextRectTransform.localRotation = Quaternion.Inverse(markerLocalRotation) * initialTextLocalRotation;
                return;
            }

            // Fallback: force the text to match the Canvas orientation (handles uncommon setups gracefully).
            Quaternion desiredWorldRotation = uprightReferenceTransform != null ? uprightReferenceTransform.rotation : Quaternion.identity;
            if (Mathf.Abs(Quaternion.Dot(distanceTextRectTransform.rotation, desiredWorldRotation)) >= ROTATION_DOT_EPSILON) return;

            // Force the text to match the Canvas orientation so it never appears upside down.
            distanceTextRectTransform.rotation = desiredWorldRotation;
        }

        /// <summary>
        /// Updates the distance text display based on the current distance and screen state.
        /// Called by WaypointMarkerUI during its UpdateDisplay cycle.
        /// </summary>
        /// <param name="distance">The current distance to the waypoint target in world units.</param>
        /// <param name="isOnScreen">Whether the waypoint marker is currently visible on screen.</param>
        public void UpdateDistance(float distance, bool isOnScreen)
        {
            if (!isInitialized || distanceText == null) return;

            // Handle missing settings gracefully
            if (settings == null)
            {
                if (distanceText.enabled)
                {
                    distanceText.enabled = false;
                }
                return;
            }

            // Check if distance text should be displayed
            bool shouldDisplay = settings.ShouldDisplayDistance(distance, isOnScreen);
            
            if (!shouldDisplay)
            {
                if (distanceText.enabled)
                {
                    distanceText.enabled = false;
                    lastDistance = -1f; // Reset cache
                }
                return;
            }

            // Enable text if it was disabled
            if (!distanceText.enabled)
            {
                distanceText.enabled = true;
            }

            // Optimization: Only update if distance or screen state changed significantly
            // Using a small threshold to avoid constant updates from minor distance changes
            bool needsUpdate = Mathf.Abs(distance - lastDistance) > 0.1f || lastOnScreen != isOnScreen;
            
            if (!needsUpdate) return;

            // Update cached values
            lastDistance = distance;
            lastOnScreen = isOnScreen;

            // Format and set the distance text
            distanceText.text = settings.FormatDistance(distance);

            // Apply distance-based color if enabled
            distanceText.color = settings.GetDistanceColor(distance);
        }

        /// <summary>
        /// Resets the distance text to its default state.
        /// Called when the marker is returned to the pool.
        /// </summary>
        public void ResetDisplay()
        {
            lastDistance = -1f;
            lastOnScreen = true;
            lastMarkerLocalRotation = Quaternion.identity;
            
            if (distanceText != null)
            {
                distanceText.text = string.Empty;
                distanceText.enabled = false;
            }
            
            // Reset rotation
            if (distanceTextRectTransform != null)
            {
                distanceTextRectTransform.localRotation = initialTextLocalRotation;
            }
        }

        /// <summary>
        /// Sets the visibility of the distance text.
        /// </summary>
        /// <param name="visible">Whether the distance text should be visible.</param>
        public void SetVisible(bool visible)
        {
            if (distanceText != null)
            {
                distanceText.enabled = visible;
            }
        }

        /// <summary>
        /// Assigns new settings at runtime. Useful for dynamic waypoint configurations.
        /// </summary>
        /// <param name="newSettings">The new DistanceTextSettings to use.</param>
        public void AssignSettings(DistanceTextSettings newSettings)
        {
            settings = newSettings;
            lastDistance = -1f; // Force update on next cycle
        }

        /// <summary>
        /// Forces an immediate update of the distance display, bypassing the change detection.
        /// </summary>
        /// <param name="distance">The current distance to display.</param>
        /// <param name="isOnScreen">Whether the marker is on screen.</param>
        public void ForceUpdate(float distance, bool isOnScreen)
        {
            lastDistance = -1f; // Reset cache to force update
            UpdateDistance(distance, isOnScreen);
        }
    }
}
