using UnityEngine;
using System;

namespace WrightAngle.Waypoint
{
    /// <summary>
    /// Marks a GameObject as a target for the Waypoint System. Attach this component
    /// to any object in your scene that you want a waypoint marker to point towards.
    /// Automatically registers with the WaypointUIManager based on the 'ActivateOnStart' setting,
    /// or can be controlled manually via script.
    /// </summary>
    [AddComponentMenu("WrightAngle/Waypoint Target")]
    public class WaypointTarget : MonoBehaviour
    {
        [Tooltip("An optional name for this waypoint, primarily for identification in the editor or scripts.")]
        public string DisplayName = "";

        [Tooltip("If checked, this waypoint target will automatically register itself with the WaypointUIManager when the scene starts (requires a WaypointUIManager in the scene). Uncheck to control activation manually using the ActivateWaypoint() method.")]
        public bool ActivateOnStart = true;

        /// <summary>
        /// Indicates whether this target is currently registered and being tracked by the WaypointUIManager. (Read-Only)
        /// </summary>
        public bool IsRegistered { get; private set; } = false;

        // --- Static Events for Communication with WaypointUIManager ---
        // These events allow the target to notify the manager when its state changes,
        // decoupling the components.

        /// <summary> Fired when this target should become active and tracked by the manager. </summary>
        public static event Action<WaypointTarget> OnTargetEnabled;
        /// <summary> Fired when this target should become inactive and untracked by the manager. </summary>
        public static event Action<WaypointTarget> OnTargetDisabled;

        // --- Unity Lifecycle Callbacks ---

        // OnEnable: Automatic registration is handled by WaypointUIManager during its Start phase
        // to avoid script execution order issues. Manual activation via ActivateWaypoint()
        // can still be called after OnEnable.

        private void OnDisable()
        {
            // Ensures the target is always unregistered if the component or its GameObject is disabled.
            ProcessDeactivation();
        }

        // --- Public API ---

        /// <summary>
        /// Manually registers this waypoint target with the system, making it eligible for tracking.
        /// Use this if 'ActivateOnStart' is disabled. Has no effect if already registered or inactive.
        /// </summary>
        public void ActivateWaypoint()
        {
            // Only proceed if the GameObject is active and the target isn't already registered.
            if (!gameObject.activeInHierarchy || IsRegistered)
            {
                return;
            }

            // Notify the WaypointUIManager (or other listeners) to start tracking this target.
            OnTargetEnabled?.Invoke(this);
            IsRegistered = true; // Update internal state.
        }

        /// <summary>
        /// Manually unregisters this waypoint target from the system, hiding its marker.
        /// This allows hiding a marker without disabling the target GameObject itself.
        /// Has no effect if not currently registered.
        /// </summary>
        public void DeactivateWaypoint()
        {
            // Use the shared internal logic for deactivation.
            ProcessDeactivation();
        }

        // --- Internal Logic ---

        /// <summary>
        /// Contains the shared logic for unregistering the target. Updates the internal state
        /// and notifies listeners via the OnTargetDisabled event. Prevents multiple notifications.
        /// </summary>
        private void ProcessDeactivation()
        {
            // Only proceed if the target was actually registered.
            if (!IsRegistered) return;

            // Notify the WaypointUIManager (or other listeners) to stop tracking this target.
            OnTargetDisabled?.Invoke(this);
            IsRegistered = false; // Update internal state.
        }


        // --- Editor Visualization ---
        // Provides visual feedback in the Scene view when the object is selected.
        private void OnDrawGizmosSelected()
        {
            // Draw a wire sphere gizmo around the target's position.
            // Color changes based on whether it's currently registered with the manager.
            Gizmos.color = IsRegistered ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
#if UNITY_EDITOR
            // Display a helpful label above the target in the Scene view.
            string label = $"Waypoint: {gameObject.name}";
            if (!ActivateOnStart) label += " (Manual Activation)";
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, label);
#endif
        }
    } // End Class
} // End Namespace