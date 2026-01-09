using UnityEngine;
using UnityEngine.UI; // Required for Image component

namespace WrightAngle.Waypoint
{
    /// <summary>
    /// Controls the visual state of a single waypoint marker instance on the UI Canvas.
    /// Attach this script to your waypoint marker prefab. It handles positioning the marker
    /// correctly on-screen or clamping it to the screen edge as an off-screen indicator,
    /// including rotation to point towards the target.
    /// </summary>
    [AddComponentMenu("WrightAngle/Waypoint Marker UI")]
    [RequireComponent(typeof(RectTransform))]
    public class WaypointMarkerUI : MonoBehaviour
    {
        [Header("UI Element References")]
        [Tooltip("The core visual element of your marker (e.g., an arrow, dot, or custom icon). Must have an Image component. If left unassigned, the system will attempt to auto-detect an Image component in children (may pick the wrong one if multiple Images exist).")]
        [SerializeField] private Image markerIcon;

        // Cached components for performance
        private RectTransform rectTransform;

        /// <summary>
        /// Checks if the markerIcon is assigned. Used by WaypointUIManager for prefab validation.
        /// </summary>
        /// <returns>True if markerIcon is valid, false otherwise.</returns>
        public bool HasValidMarkerIcon() => markerIcon != null;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();

            // If markerIcon is not assigned, attempt to auto-detect from children
            if (markerIcon == null)
            {
                markerIcon = GetComponentInChildren<Image>();
                if (markerIcon != null)
                {
                    // Auto-detection succeeded, but warn about potential issues
                    Debug.LogWarning($"<b>[{gameObject.name}] WaypointMarkerUI:</b> 'Marker Icon' was not assigned. Auto-detected Image component '{markerIcon.name}'. If your prefab has multiple Image components, the wrong one may have been selected. Assign the correct Image in the Inspector to avoid this warning.", this);
                }
                else
                {
                    // No Image found at all, this is a critical error
                    Debug.LogError($"<b>[{gameObject.name}] WaypointMarkerUI Error:</b> 'Marker Icon' is not assigned and no Image component was found in children. Add an Image component to your prefab and assign it to the 'Marker Icon' field.", this);
                    enabled = false;
                    return;
                }
            }

            // Optimize performance by disabling raycast target for the icon (markers are typically non-interactive)
            markerIcon.raycastTarget = false;
        }

        /// <summary>
        /// Updates the marker's position and rotation based on the target's screen-space information.
        /// Called frequently by the WaypointUIManager.
        /// </summary>
        /// <param name="screenPosition">Target's projected position on the screen (can be off-screen).</param>
        /// <param name="isOnScreen">Indicates if the target is currently within the camera's viewport.</param>
        /// <param name="isBehindCamera">Indicates if the target is located behind the camera.</param>
        /// <param name="cam">The reference camera used for calculations.</param>
        /// <param name="settings">The active WaypointSettings asset providing configuration.</param>
        public void UpdateDisplay(Vector3 screenPosition, bool isOnScreen, bool isBehindCamera, Camera cam, WaypointSettings settings)
        {
            // Safety checks for required components and settings
            if (settings == null || rectTransform == null || cam == null || markerIcon == null)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false); // Hide if setup is invalid
                return;
            }

            if (isOnScreen)
            {
                // --- Target ON Screen ---
                // Position the marker directly at the target's screen position.
                rectTransform.position = screenPosition;
                // Ensure no rotation is applied for on-screen markers.
                rectTransform.rotation = Quaternion.identity;
                if (!gameObject.activeSelf) gameObject.SetActive(true); // Ensure marker is visible
            }
            else // --- Target OFF Screen ---
            {
                // If off-screen indicators are disabled in settings, hide the marker.
                if (!settings.UseOffScreenIndicators)
                {
                    if (gameObject.activeSelf) gameObject.SetActive(false);
                    return;
                }

                if (!gameObject.activeSelf) gameObject.SetActive(true); // Ensure marker is visible

                // --- Calculate Off-Screen Position and Rotation ---
                float margin = settings.ScreenEdgeMargin;
                Vector2 screenCenter = new Vector2(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f);
                // Define the clamping boundaries based on screen size and margin.
                Rect screenBounds = new Rect(margin, margin, cam.pixelWidth - (margin * 2f), cam.pixelHeight - (margin * 2f));

                Vector3 positionToClamp; // The position used for boundary intersection calculation.
                Vector2 directionForRotation; // Direction the marker icon should point towards.

                if (isBehindCamera)
                {
                    // --- Target is BEHIND Camera ---
                    // Calculate direction vector pointing away from the screen center, adjusted for being behind.
                    Vector2 screenPos2D = new Vector2(screenPosition.x, screenPosition.y);
                    Vector2 directionFromCenter = screenPos2D - screenCenter;
                    directionFromCenter.x *= -1; // Invert horizontal component.
                    directionFromCenter.y = -Mathf.Abs(directionFromCenter.y); // Force downwards.

                    // Handle edge case where target is exactly behind center.
                    if (directionFromCenter.sqrMagnitude < 0.001f) directionFromCenter = Vector2.down;
                    directionFromCenter.Normalize();

                    // Project a point far outside the screen in the calculated direction to ensure it intersects the clamping bounds.
                    float farDistance = cam.pixelWidth + cam.pixelHeight;
                    positionToClamp = new Vector3(screenCenter.x + directionFromCenter.x * farDistance,
                                                  screenCenter.y + directionFromCenter.y * farDistance, 0);
                    directionForRotation = directionFromCenter;
                }
                else
                {
                    // --- Target is IN FRONT but OFF-SCREEN ---
                    // Use the target's actual (off-screen) projection for clamping.
                    positionToClamp = screenPosition;
                    // Calculate the direction from the screen center towards the off-screen position.
                    directionForRotation = (new Vector2(screenPosition.x, screenPosition.y) - screenCenter).normalized;
                }

                // --- Clamping to Screen Edge ---
                // Calculate the precise intersection point with the screen bounds rectangle.
                Vector2 clampedPosition = IntersectWithScreenBounds(screenCenter, positionToClamp, screenBounds);
                // Apply the clamped position to the marker.
                rectTransform.position = new Vector3(clampedPosition.x, clampedPosition.y, 0f);

                // --- Rotation ---
                // Rotate the marker icon to point towards the target's direction.
                if (directionForRotation.sqrMagnitude > 0.001f) // Avoid issues with zero direction.
                {
                    // Calculate the angle relative to the screen's right direction.
                    float angle = Vector2.SignedAngle(Vector2.right, directionForRotation);
                    // Determine flip adjustment based on settings.
                    float flipAngle = settings.FlipOffScreenMarkerY ? 180f : 0f;
                    // Apply rotation, assuming the icon points 'up' by default (-90 degrees offset). Adjust offset if your icon points differently.
                    rectTransform.rotation = Quaternion.Euler(0, 0, angle + flipAngle - 90f);
                }
                else // Handle case where direction is zero (e.g., exactly behind center).
                {
                    float flipAngle = settings.FlipOffScreenMarkerY ? 180f : 0f;
                    // Default rotation points down, adjust based on flip setting.
                    rectTransform.rotation = Quaternion.Euler(0, 0, -180f + flipAngle);
                }
            }
        }

        /// <summary>
        /// Calculates the exact intersection point of a line (from screen center towards a target point)
        /// with the edges of a rectangular boundary. Ensures accurate clamping to the screen edge.
        /// </summary>
        private Vector2 IntersectWithScreenBounds(Vector2 center, Vector2 targetPoint, Rect bounds)
        {
            Vector2 direction = (targetPoint - center).normalized;
            // Handle zero direction vector edge case.
            if (direction.sqrMagnitude < 0.0001f) return new Vector2(bounds.center.x, bounds.yMin);

            // Calculate potential intersection distances ('t' values) along the direction vector for each edge.
            float tXMin = (direction.x != 0) ? (bounds.xMin - center.x) / direction.x : Mathf.Infinity;
            float tXMax = (direction.x != 0) ? (bounds.xMax - center.x) / direction.x : Mathf.Infinity;
            float tYMin = (direction.y != 0) ? (bounds.yMin - center.y) / direction.y : Mathf.Infinity;
            float tYMax = (direction.y != 0) ? (bounds.yMax - center.y) / direction.y : Mathf.Infinity;

            // Find the smallest positive 't' value that corresponds to an intersection point *within* the bounds of the *other* axis.
            float minT = Mathf.Infinity;
            if (tXMin > 0 && center.y + tXMin * direction.y >= bounds.yMin && center.y + tXMin * direction.y <= bounds.yMax) minT = Mathf.Min(minT, tXMin);
            if (tXMax > 0 && center.y + tXMax * direction.y >= bounds.yMin && center.y + tXMax * direction.y <= bounds.yMax) minT = Mathf.Min(minT, tXMax);
            if (tYMin > 0 && center.x + tYMin * direction.x >= bounds.xMin && center.x + tYMin * direction.x <= bounds.xMax) minT = Mathf.Min(minT, tYMin);
            if (tYMax > 0 && center.x + tYMax * direction.x >= bounds.xMin && center.x + tYMax * direction.x <= bounds.xMax) minT = Mathf.Min(minT, tYMax);

            // Fallback if no valid intersection is found (should be rare with correct inputs).
            if (float.IsInfinity(minT))
            {
                Debug.LogWarning("WaypointMarkerUI: Could not find screen bounds intersection. Using fallback clamping.", this);
                return new Vector2(Mathf.Clamp(targetPoint.x, bounds.xMin, bounds.xMax),
                                   Mathf.Clamp(targetPoint.y, bounds.yMin, bounds.yMax));
            }

            // Calculate the precise intersection point using the smallest valid 't'.
            return center + direction * minT;
        }

    } // End Class
} // End Namespace