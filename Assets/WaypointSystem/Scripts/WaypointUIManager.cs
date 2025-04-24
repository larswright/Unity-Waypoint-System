using UnityEngine;
using UnityEngine.Pool; // Required for efficient object pooling
using System.Collections.Generic;

namespace WrightAngle.Waypoint
{
    /// <summary>
    /// The core manager for the Waypoint System. Place one instance in your scene.
    /// Discovers active Waypoint Targets, manages a pool of Waypoint Marker UI elements,
    /// and orchestrates updates based on the assigned Camera and Waypoint Settings asset.
    /// Ensures efficient handling and display of waypoint markers.
    /// </summary>
    [AddComponentMenu("WrightAngle/Waypoint UI Manager")]
    [DisallowMultipleComponent] // Only one manager should exist per scene.
    public class WaypointUIManager : MonoBehaviour
    {
        [Header("Essential References")]
        [Tooltip("Assign the Waypoint Settings ScriptableObject asset here to configure the system.")]
        [SerializeField] private WaypointSettings settings;

        [Tooltip("Assign the primary Camera used in your gameplay scene.")]
        [SerializeField] private Camera waypointCamera;

        [Tooltip("Assign the UI Canvas's RectTransform that will serve as the parent for all instantiated waypoint markers.")]
        [SerializeField] private RectTransform markerParentCanvas;

        // --- Internal State ---
        private ObjectPool<WaypointMarkerUI> markerPool; // Efficiently reuses marker UI GameObjects.
        // Collections to manage active targets and their corresponding markers.
        private List<WaypointTarget> activeTargetList = new List<WaypointTarget>(); // Used for efficient iteration.
        private HashSet<WaypointTarget> activeTargetSet = new HashSet<WaypointTarget>(); // Used for fast checking of target existence.
        private Dictionary<WaypointTarget, WaypointMarkerUI> activeMarkers = new Dictionary<WaypointTarget, WaypointMarkerUI>(); // Maps a target to its active UI marker.

        private Camera _cachedWaypointCamera; // Cached camera reference for performance.
        private float lastUpdateTime = -1f;   // Used for update throttling based on UpdateFrequency.
        private bool isInitialized = false;   // Flag to prevent updates before successful initialization.

        // --- Unity Lifecycle ---

        private void Awake()
        {
            // Validate required references and setup.
            bool setupError = ValidateSetup();
            if (setupError)
            {
                enabled = false; // Disable component if setup fails.
                Debug.LogError($"<b>[{gameObject.name}] WaypointUIManager:</b> Component disabled due to setup errors. Check Inspector references.", this);
                return;
            }

            // Cache valid references.
            _cachedWaypointCamera = waypointCamera;
            // Set up the object pool for marker UI elements.
            InitializePool();

            // Subscribe to events from WaypointTarget components for dynamic registration/unregistration.
            WaypointTarget.OnTargetEnabled += HandleTargetEnabled;
            WaypointTarget.OnTargetDisabled += HandleTargetDisabled;

            isInitialized = true; // Mark initialization successful.
            Debug.Log($"<b>[{gameObject.name}] WaypointUIManager:</b> Initialized.", this);
        }

        private void Start()
        {
            // Start runs after all Awakes, ensuring targets can be found reliably.
            if (!isInitialized) return; // Don't proceed if initialization failed.
            // Find and register any targets in the scene configured to activate automatically.
            FindAndRegisterInitialTargets();
        }

        private void OnDestroy()
        {
            // --- Cleanup ---
            // Unsubscribe from events to prevent memory leaks when the manager is destroyed.
            WaypointTarget.OnTargetEnabled -= HandleTargetEnabled;
            WaypointTarget.OnTargetDisabled -= HandleTargetDisabled;

            // Clear and dispose of the object pool and internal tracking collections.
            markerPool?.Clear();
            markerPool?.Dispose();
            activeTargetList.Clear();
            activeTargetSet.Clear();
            activeMarkers.Clear();
        }

        /// <summary> Validates that all essential Inspector references are assigned correctly. Returns true if an error is found. </summary>
        private bool ValidateSetup()
        {
            bool error = false;
            if (waypointCamera == null) { Debug.LogError("WaypointUIManager Error: Waypoint Camera not assigned!", this); error = true; }
            if (settings == null) { Debug.LogError("WaypointUIManager Error: WaypointSettings not assigned!", this); error = true; }
            else if (settings.GetMarkerPrefab() == null) { Debug.LogError($"WaypointUIManager Error: Marker Prefab missing in WaypointSettings '{settings.name}'!", this); error = true; } // Check prefab validity.
            if (markerParentCanvas == null) { Debug.LogError("WaypointUIManager Error: Marker Parent Canvas not assigned!", this); error = true; }
            else if (markerParentCanvas.GetComponentInParent<Canvas>() == null) { Debug.LogError("WaypointUIManager Error: Marker Parent Canvas must be a child of a UI Canvas!", this); error = true; } // Ensure it's part of a valid UI hierarchy.
            return error;
        }


        private void Update()
        {
            // Exit if not initialized or essential components are missing.
            if (!isInitialized) return;

            // Throttle the update logic based on the frequency defined in settings.
            if (Time.time < lastUpdateTime + settings.UpdateFrequency) return;
            lastUpdateTime = Time.time;

            // Cache camera position for use within the loop.
            Vector3 cameraPosition = _cachedWaypointCamera.transform.position;
            float camPixelWidth = _cachedWaypointCamera.pixelWidth;
            float camPixelHeight = _cachedWaypointCamera.pixelHeight;

            // Iterate backwards through the list of active targets for safe removal during iteration.
            for (int i = activeTargetList.Count - 1; i >= 0; i--)
            {
                WaypointTarget target = activeTargetList[i];

                // --- Target Validity & Cleanup ---
                // Handle cases where the target might have been destroyed or deactivated unexpectedly.
                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    RemoveTargetCompletely(target, i); // Clean up tracking data.
                    continue; // Move to the next target.
                }

                // --- Core Waypoint Logic ---
                Transform targetTransform = target.transform;
                Vector3 targetWorldPos = targetTransform.position;

                // Calculate distance for visibility checks.
                float distance = CalculateDistance(cameraPosition, targetWorldPos);

                // Hide marker and skip further processing if beyond the maximum visible distance.
                if (distance > settings.MaxVisibleDistance)
                {
                    TryReleaseMarker(target); // Release marker back to the pool if it was active.
                    continue;
                }

                // Project the target's world position to screen space.
                Vector3 screenPos = _cachedWaypointCamera.WorldToScreenPoint(targetWorldPos);
                bool isBehindCamera = screenPos.z <= 0; // Check if target is behind the camera's near plane.
                // Check if the projected position is within the screen bounds (and not behind).
                bool isOnScreen = !isBehindCamera && screenPos.x > 0 && screenPos.x < camPixelWidth && screenPos.y > 0 && screenPos.y < camPixelHeight;

                // Determine if a marker should be displayed based on screen status and settings.
                bool shouldShowMarker = isOnScreen || (settings.UseOffScreenIndicators && !isOnScreen);

                if (shouldShowMarker)
                {
                    // --- Get or Activate Marker ---
                    // Try to get an existing marker; if none exists, retrieve one from the pool.
                    if (!activeMarkers.TryGetValue(target, out WaypointMarkerUI markerInstance))
                    {
                        markerInstance = markerPool.Get(); // Get from pool (activates the GameObject).
                        activeMarkers.Add(target, markerInstance); // Associate the new marker with the target.
                    }
                    // Ensure the marker's GameObject is active (could be inactive if just retrieved from pool).
                    if (!markerInstance.gameObject.activeSelf) markerInstance.gameObject.SetActive(true);

                    // --- Update Marker Visuals ---
                    // Call the marker's UpdateDisplay method to set its position and rotation.
                    markerInstance.UpdateDisplay(screenPos, isOnScreen, isBehindCamera, _cachedWaypointCamera, settings);
                }
                else // Marker should not be shown (e.g., off-screen and indicators disabled).
                {
                    TryReleaseMarker(target); // Release marker back to the pool if it was active.
                }
            }
        }

        // --- Calculation Helper ---

        /// <summary> Calculates distance between camera and target, optionally ignoring Z-axis in 2D mode. Used for MaxVisibleDistance check. </summary>
        private float CalculateDistance(Vector3 camPos, Vector3 targetPos)
        {
            if (settings.GameMode == WaypointSettings.ProjectionMode.Mode2D && settings.IgnoreZAxisForDistance2D)
            {
                // Calculate distance using only X and Y components.
                return Vector2.Distance(new Vector2(camPos.x, camPos.y), new Vector2(targetPos.x, targetPos.y));
            }
            else
            {
                // Calculate standard 3D distance.
                return Vector3.Distance(camPos, targetPos);
            }
        }

        // --- Target Management ---

        /// <summary> Scans the scene at startup for WaypointTargets configured to 'ActivateOnStart'. </summary>
        private void FindAndRegisterInitialTargets()
        {
            // Find all WaypointTarget components in the scene, including inactive ones initially.
            // Use FindObjectsByType for modern Unity versions and better performance options.
            WaypointTarget[] allTargets = FindObjectsByType<WaypointTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int activationCount = 0;
            foreach (WaypointTarget target in allTargets)
            {
                // Register only if ActivateOnStart is true AND the target's GameObject is currently active in the hierarchy.
                if (target.ActivateOnStart && target.gameObject.activeInHierarchy)
                {
                    RegisterTarget(target);
                    activationCount++;
                }
            }
            Debug.Log($"<b>[{gameObject.name}] WaypointUIManager:</b> Found {allTargets.Length} potential targets, activated {activationCount} marked 'ActivateOnStart'.");
        }

        /// <summary> Adds a target to the internal tracking collections if it's not already tracked. </summary>
        private void RegisterTarget(WaypointTarget target)
        {
            // Use HashSet.Add for an efficient way to add only if the target isn't already present.
            if (target != null && activeTargetSet.Add(target))
            {
                // If successfully added to the set, also add to the list used for iteration.
                activeTargetList.Add(target);
                // Note: The UI marker itself is only fetched from the pool when needed during the Update loop.
            }
        }

        /// <summary> Attempts to find the marker associated with a target and releases it back to the pool. </summary>
        private void TryReleaseMarker(WaypointTarget target)
        {
            // Check if the target is valid and if there's an active marker mapped to it.
            if (target != null && activeMarkers.TryGetValue(target, out WaypointMarkerUI markerToRelease))
            {
                markerPool.Release(markerToRelease); // Return the marker to the pool (deactivates the GameObject).
                activeMarkers.Remove(target);        // Remove the association from the dictionary.
            }
        }

        /// <summary> Removes a target completely from all tracking lists and ensures its marker is released. </summary>
        private void RemoveTargetCompletely(WaypointTarget target, int listIndex = -1)
        {
            // Ensure the marker is released back to the pool first.
            TryReleaseMarker(target);

            // Remove from the fast lookup set.
            if (target != null) activeTargetSet.Remove(target);

            // Efficiently remove from the list if the index is known and valid.
            if (listIndex >= 0 && listIndex < activeTargetList.Count && activeTargetList[listIndex] == target)
            {
                activeTargetList.RemoveAt(listIndex);
            }
            // Fallback: Search and remove from the list if index is unknown or invalid (less efficient).
            else if (target != null)
            {
                activeTargetList.Remove(target);
            }
            // Handle potential null entries that might occur if objects are destroyed improperly.
            else
            {
                activeTargetList.RemoveAll(item => item == null);
            }
        }

        // --- Pool Management Callbacks ---

        /// <summary> Sets up the Object Pool for creating and reusing WaypointMarkerUI instances. </summary>
        private void InitializePool()
        {
            // Get the prefab configured in settings (already validated in Awake).
            GameObject prefab = settings.GetMarkerPrefab();
            if (prefab == null) return; // Safety check.

            markerPool = new ObjectPool<WaypointMarkerUI>(
                createFunc: () => { // Defines how to create a new marker instance when the pool is empty.
                    GameObject go = Instantiate(prefab, markerParentCanvas);
                    WaypointMarkerUI ui = go.GetComponent<WaypointMarkerUI>();
                    // Add the script if missing on the prefab (for robustness).
                    if (ui == null)
                    {
                        ui = go.AddComponent<WaypointMarkerUI>();
                        Debug.LogWarning($"WaypointUIManager: Added missing WaypointMarkerUI script to '{prefab.name}' instance.", go);
                    }
                    go.SetActive(false); // Ensure new instances start inactive.
                    return ui;
                },
                actionOnGet: (marker) => marker.gameObject.SetActive(true),    // Action performed when an item is taken from the pool.
                actionOnRelease: (marker) => marker.gameObject.SetActive(false), // Action performed when an item is returned to the pool.
                actionOnDestroy: (marker) => { if (marker != null) Destroy(marker.gameObject); }, // Action performed when the pool destroys an item.
                collectionCheck: true, // Adds extra checks in editor builds to detect pool corruption issues.
                defaultCapacity: 10,   // Initial number of items the pool can hold.
                maxSize: 100         // Maximum number of items the pool will store.
            );
        }

        // --- Target Event Handlers ---

        /// <summary> Responds to the OnTargetEnabled event, registering the target. </summary>
        private void HandleTargetEnabled(WaypointTarget target) => RegisterTarget(target);

        /// <summary> Responds to the OnTargetDisabled event, removing the target completely. </summary>
        private void HandleTargetDisabled(WaypointTarget target)
        {
            // Find the target's index for potentially faster removal from the list.
            int index = activeTargetList.IndexOf(target);
            RemoveTargetCompletely(target, index);
        }

    } // End Class
} // End Namespace