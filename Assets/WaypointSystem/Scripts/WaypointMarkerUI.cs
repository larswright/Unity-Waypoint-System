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

        [Header("Distance Text (Optional)")]
        [Tooltip("Optional DistanceTextUI component for displaying distance to the target. If assigned, it will be automatically updated during marker display.")]
        [SerializeField] private DistanceTextUI distanceTextUI;

        // Cached components for performance
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup; // Used for efficient alpha fading
        private bool hasDistanceText; // Cached flag to avoid null checks every frame
        private Vector3 baseScale; // Original scale for reference

        /// <summary>
        /// Checks if the markerIcon is assigned. Used by WaypointUIManager for prefab validation.
        /// </summary>
        /// <returns>True if markerIcon is valid, false otherwise.</returns>
        public bool HasValidMarkerIcon() => markerIcon != null;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            baseScale = rectTransform.localScale;
            
            // Cache distance text availability
            hasDistanceText = distanceTextUI != null;
            
            // Get or add CanvasGroup for efficient alpha control
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

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
        /// Uses RectTransformUtility for proper handling of all Canvas render modes (Screen Space - Overlay,
        /// Screen Space - Camera, World Space) and CanvasScaler configurations.
        /// </summary>
        /// <param name="screenPosition">Target's projected position on the screen (can be off-screen).</param>
        /// <param name="isOnScreen">Indicates if the target is currently within the camera's viewport.</param>
        /// <param name="isBehindCamera">Indicates if the target is located behind the camera.</param>
        /// <param name="cam">The reference camera used for calculations.</param>
        /// <param name="settings">The active WaypointSettings asset providing configuration.</param>
        /// <param name="parentRect">The parent RectTransform used for local coordinate conversion and clamping.</param>
        /// <param name="uiCamera">The camera rendering the UI Canvas (null for Screen Space - Overlay).</param>
        public void UpdateDisplay(Vector3 screenPosition, bool isOnScreen, bool isBehindCamera, Camera cam, WaypointSettings settings, RectTransform parentRect, Camera uiCamera, float distance)
        {
            // Safety checks for required components and settings
            if (settings == null || rectTransform == null || cam == null || markerIcon == null || parentRect == null)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false); // Hide if setup is invalid
                return;
            }

            // Get parent rectangle bounds for clamping (in local coordinates)
            Rect parentBounds = parentRect.rect;

            if (isOnScreen)
            {
                // --- Target ON Screen ---
                // Convert screen position to local position within the parent RectTransform.
                // This properly handles all Canvas render modes and CanvasScaler configurations.
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, uiCamera, out Vector2 localPoint))
                {
                    rectTransform.anchoredPosition = localPoint;
                }
                // Ensure no rotation is applied for on-screen markers (local to canvas).
                rectTransform.localRotation = Quaternion.identity;
                
                // Apply distance-based scaling and fading for on-screen markers
                UpdateScaleAndFade(settings, distance);
                
                // Update distance text if available
                if (hasDistanceText)
                {
                    distanceTextUI.UpdateDistance(distance, true);
                }
                
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
                // Convert margin from pixels to local units using parent rect scale
                // This ensures consistent margins regardless of CanvasScaler settings
                float pixelToLocalScale = parentBounds.width / cam.pixelWidth;
                float localMargin = settings.ScreenEdgeMargin * pixelToLocalScale;
                
                // Screen center in pixels for direction calculations
                Vector2 screenCenter = new Vector2(cam.pixelWidth * 0.5f, cam.pixelHeight * 0.5f);
                
                // Define clamping boundaries in local coordinates (relative to parent center)
                // parentBounds.xMin/yMin are typically negative, xMax/yMax positive when anchored at center
                Rect localBounds = new Rect(
                    parentBounds.xMin + localMargin,
                    parentBounds.yMin + localMargin,
                    parentBounds.width - (localMargin * 2f),
                    parentBounds.height - (localMargin * 2f)
                );

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
                // First convert the position to clamp into local coordinates
                Vector2 localPositionToClamp;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, positionToClamp, uiCamera, out localPositionToClamp))
                {
                    // Get local center point for intersection calculation
                    Vector2 localCenter = Vector2.zero; // Parent center in local coords is (0,0) when using anchoredPosition
                    
                    // Calculate the precise intersection point with the local bounds rectangle.
                    Vector2 clampedLocalPosition = IntersectWithLocalBounds(localCenter, localPositionToClamp, localBounds);
                    // Apply the clamped position to the marker using anchoredPosition.
                    rectTransform.anchoredPosition = clampedLocalPosition;
                }
                else
                {
                    // Fallback: simple clamp if conversion fails
                    rectTransform.anchoredPosition = new Vector2(
                        Mathf.Clamp(0, localBounds.xMin, localBounds.xMax),
                        Mathf.Clamp(0, localBounds.yMin, localBounds.yMax)
                    );
                }

                // --- Rotation ---
                // Rotate the marker icon to point towards the target's direction.
                if (directionForRotation.sqrMagnitude > 0.001f) // Avoid issues with zero direction.
                {
                    // Calculate the angle relative to the screen's right direction.
                    float angle = Vector2.SignedAngle(Vector2.right, directionForRotation);
                    // Determine flip adjustment based on settings.
                    float flipAngle = settings.FlipOffScreenMarkerY ? 180f : 0f;
                    // Apply rotation, assuming the icon points 'up' by default (-90 degrees offset). Adjust offset if your icon points differently.
                    // Use localRotation to stay upright relative to the Canvas (important for Screen Space - Camera).
                    rectTransform.localRotation = Quaternion.Euler(0, 0, angle + flipAngle - 90f);
                }
                else // Handle case where direction is zero (e.g., exactly behind center).
                {
                    float flipAngle = settings.FlipOffScreenMarkerY ? 180f : 0f;
                    // Default rotation points down, adjust based on flip setting.
                    // Use localRotation to stay upright relative to the Canvas.
                    rectTransform.localRotation = Quaternion.Euler(0, 0, -180f + flipAngle);
                }
                // Apply distance-based scaling for off-screen markers if enabled
                if (settings.ScaleOffScreenMarkers)
                {
                    UpdateScaleAndFade(settings, distance);
                }
                else
                {
                    // Keep full scale and alpha for off-screen visibility
                    rectTransform.localScale = baseScale;
                    canvasGroup.alpha = 1f;
                }
                
                // Update distance text for off-screen markers if available
                if (hasDistanceText)
                {
                    distanceTextUI.UpdateDistance(distance, false);
                }
            }
        }
        
        /// <summary>
        /// Applies distance-based scaling and optional fading to the marker.
        /// Uses efficient calculations with no allocations.
        /// </summary>
        private void UpdateScaleAndFade(WaypointSettings settings, float distance)
        {
            if (!settings.UseDistanceScaling)
            {
                // Reset to base scale and full alpha when scaling is disabled
                rectTransform.localScale = baseScale;
                canvasGroup.alpha = 1f;
                return;
            }
            
            // Pre-calculate range to avoid repeated division
            float scaleRange = settings.MaxScaleDistance - settings.MinScaleDistance;
            
            // Prevent division by zero
            if (scaleRange <= 0.001f)
            {
                rectTransform.localScale = baseScale;
                canvasGroup.alpha = 1f;
                return;
            }
            
            // Calculate normalized distance (0 at MinScaleDistance, 1 at MaxScaleDistance)
            // Clamp to [0, 1] to handle distances outside the range
            float normalizedDistance = Mathf.Clamp01((distance - settings.MinScaleDistance) / scaleRange);
            
            // Calculate scale: full size when close (low normalizedDistance), smaller when far
            float scaleFactor = Mathf.Lerp(1f, settings.MinScaleFactor, normalizedDistance);
            rectTransform.localScale = baseScale * scaleFactor;
            
            // Calculate alpha/fade when approaching maximum distance (far away)
            float alpha = 1f;
            if (settings.UseFadeAtMinDistance && distance > settings.MaxScaleDistance - settings.FadeRange)
            {
                // Fade from 1 at (MaxScaleDistance - FadeRange) to 0 at MaxScaleDistance
                alpha = Mathf.Clamp01((settings.MaxScaleDistance - distance) / settings.FadeRange);
            }
            canvasGroup.alpha = alpha;
        }

        /// <summary>
        /// Calculates the exact intersection point of a line (from center towards a target point)
        /// with the edges of a rectangular boundary in local coordinates.
        /// Ensures accurate clamping to the parent rect edge.
        /// </summary>
        private Vector2 IntersectWithLocalBounds(Vector2 center, Vector2 targetPoint, Rect bounds)
        {
            Vector2 direction = (targetPoint - center).normalized;
            // Handle zero direction vector edge case - return bottom center of bounds
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
                Debug.LogWarning("WaypointMarkerUI: Could not find local bounds intersection. Using fallback clamping.", this);
                return new Vector2(Mathf.Clamp(targetPoint.x, bounds.xMin, bounds.xMax),
                                   Mathf.Clamp(targetPoint.y, bounds.yMin, bounds.yMax));
            }

            // Calculate the precise intersection point using the smallest valid 't'.
            return center + direction * minT;
        }

    } // End Class
} // End Namespace