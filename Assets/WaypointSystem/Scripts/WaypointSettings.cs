using UnityEngine;

namespace WrightAngle.Waypoint
{
    /// <summary>
    /// Configure your waypoint system's appearance and behavior globally.
    /// Create instances via 'Assets -> Create -> WrightAngle -> Waypoint Settings'.
    /// This asset allows easy tweaking of performance, visuals, and core mechanics.
    /// </summary>
    [CreateAssetMenu(fileName = "WaypointSettings", menuName = "WrightAngle/Waypoint Settings", order = 1)]
    public class WaypointSettings : ScriptableObject
    {
        /// <summary> Specifies the camera projection type used in your scene. </summary>
        public enum ProjectionMode { Mode3D, Mode2D }

        [Header("Core Functionality")]
        [Tooltip("How often (in seconds) the waypoint system updates. Lower values increase responsiveness but may impact performance.")]
        [Range(0.01f, 1.0f)]
        public float UpdateFrequency = 0.1f;

        [Tooltip("Select Mode3D for perspective cameras or Mode2D for orthographic cameras to ensure correct calculations.")]
        public ProjectionMode GameMode = ProjectionMode.Mode3D;

        [Tooltip("Assign your custom waypoint marker prefab here. This UI element will represent your waypoints visually.")]
        public GameObject MarkerPrefab;

        [Tooltip("The maximum distance (in world units) from the camera at which a waypoint marker remains visible.")]
        public float MaxVisibleDistance = 1000f;

        [Tooltip("When using Mode2D, enable this to calculate the MaxVisibleDistance check using only X and Y axes, ignoring Z.")]
        public bool IgnoreZAxisForDistance2D = true;

        [Header("Off-Screen Indicator")]
        [Tooltip("Enable this to show markers clamped to the screen edges when their target is outside the camera view.")]
        public bool UseOffScreenIndicators = true;

        [Tooltip("Define the distance (in pixels) from the screen edges where off-screen indicators will be positioned.")]
        [Range(0f, 100f)]
        public float ScreenEdgeMargin = 50f;

        [Tooltip("Enable this to flip the off-screen marker's vertical orientation. Useful if your marker icon naturally points downwards.")]
        public bool FlipOffScreenMarkerY = false;

        // --- Helper Methods ---

        /// <summary>
        /// Retrieves the assigned marker prefab GameObject.
        /// Ensures a prefab is assigned before use.
        /// </summary>
        /// <returns>The assigned marker prefab, or null if none is set.</returns>
        public GameObject GetMarkerPrefab()
        {
            if (MarkerPrefab == null)
            {
                Debug.LogError("WaypointSettings: Marker Prefab is not assigned! Please assign a prefab in the Waypoint Settings asset.", this);
            }
            return MarkerPrefab;
        }
    } // End Class
} // End Namespace