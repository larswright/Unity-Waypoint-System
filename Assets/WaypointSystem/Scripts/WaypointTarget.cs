using UnityEngine;
using System;

namespace WrightAngle.Waypoint
{
    /// <summary>
    /// Marks a GameObject as a target for the Waypoint System. Attach this component
    /// to any object in your scene that you want a waypoint marker to point towards.
    /// Is automatically registered by the WaypointUIManager based on the 'ActivateOnStart' setting,
    /// or can be controlled manually via script.
    /// </summary>
    [AddComponentMenu("WrightAngle/Waypoint Target")]
    public class WaypointTarget : MonoBehaviour
    {
        [Tooltip("An optional name for this waypoint, primarily for identification in the editor or scripts.")]
        public string DisplayName = "";

        [Tooltip("If checked, this waypoint target will be automatically registered by the WaypointUIManager when the scene starts (requires a WaypointUIManager in the scene). Uncheck to control activation manually using the ActivateWaypoint() method.")]
        public bool ActivateOnStart = true;

        [Tooltip("Offset applied to the target's position in world space. Use this to make the marker track slightly above or beside the actual transform position (e.g., above a character's head).")]
        public Vector3 WorldOffset = Vector3.zero;

        [Header("Appearance")]
        [Tooltip("Optional preset defining this waypoint's visual style. If null, uses the default prefab appearance.")]
        [SerializeField] private WaypointPreset preset;

        /// <summary>
        /// Fired when the preset is changed at runtime via SetPreset().
        /// The WaypointUIManager listens to this to update marker visuals.
        /// </summary>
        public event System.Action<WaypointTarget> OnPresetChanged;

        /// <summary>
        /// Gets the current visual preset for this waypoint. May be null for default appearance.
        /// </summary>
        public WaypointPreset Preset => preset;

        /// <summary>
        /// Sets the visual preset at runtime and notifies the system to update marker visuals.
        /// </summary>
        /// <param name="newPreset">The new preset to apply, or null for default appearance.</param>
        public void SetPreset(WaypointPreset newPreset)
        {
            if (preset == newPreset) return;
            preset = newPreset;
            OnPresetChanged?.Invoke(this);
        }

        /// <summary>
        /// Returns the target's world position with the offset applied.
        /// This is the position that the waypoint marker will track.
        /// </summary>
        public Vector3 TargetPosition => transform.position + WorldOffset;

        /// <summary>
        /// Indicates whether this target is currently registered and being tracked by the WaypointUIManager. (Read-Only)
        /// This state is owned by the WaypointUIManager and should not be set by user code.
        /// </summary>
        public bool IsRegistered { get; private set; } = false;

        /// <summary>
        /// Internal hook used by the WaypointUIManager to keep the target's read-only state in sync
        /// with the actual tracking collections.
        /// </summary>
        internal void SetRegisteredByManager(bool isRegistered)
        {
            IsRegistered = isRegistered;
        }

        // --- Static Events for Communication with WaypointUIManager ---
        // These events allow the target to notify the manager when its state changes,
        // decoupling the components.
        // NOTE: These are marked obsolete to encourage using the new explicit API.
        // They are still functional for backwards compatibility.

        /// <summary> Fired when this target should become active and tracked by the manager. </summary>
        [Obsolete("Use WaypointUIManager.Register() directly for more predictable behavior. Static events will be removed in a future version.")]
        public static event Action<WaypointTarget> OnTargetEnabled;
        /// <summary> Fired when this target should become inactive and untracked by the manager. </summary>
        [Obsolete("Use WaypointUIManager.Unregister() directly for more predictable behavior. Static events will be removed in a future version.")]
        public static event Action<WaypointTarget> OnTargetDisabled;

        // --- Unity Lifecycle Callbacks ---

        // OnEnable: Automatic registration is handled by WaypointUIManager during its Start phase
        // to avoid script execution order issues. Manual activation via ActivateWaypoint()
        // can still be called after OnEnable.

        private void OnDisable()
        {
            // Ensure the manager stops tracking this target if the component or its GameObject is disabled.
            ProcessDeactivation();
        }

        // --- Public API ---

        /// <summary>
        /// Requests that this waypoint target becomes tracked by the system.
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
#pragma warning disable CS0618 // Obsolete warning suppressed - event still fires for backwards compatibility
            OnTargetEnabled?.Invoke(this);
#pragma warning restore CS0618
        }

        /// <summary>
        /// Requests that this waypoint target stops being tracked by the system, hiding its marker.
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
        /// Contains the shared logic for untracking the target and notifying listeners via the
        /// OnTargetDisabled event. Prevents multiple notifications.
        /// </summary>
        private void ProcessDeactivation()
        {
            // Only proceed if the target was actually registered.
            if (!IsRegistered) return;

            // Notify the WaypointUIManager (or other listeners) to stop tracking this target.
#pragma warning disable CS0618 // Obsolete warning suppressed - event still fires for backwards compatibility
            OnTargetDisabled?.Invoke(this);
#pragma warning restore CS0618
        }


        // --- Editor Visualization ---
        // Provides visual feedback in the Scene view when the object is selected.
        private void OnDrawGizmosSelected()
        {
            // Draw a wire sphere gizmo at the actual tracked position (with offset applied).
            // Color changes based on whether it's currently registered with the manager.
            Gizmos.color = IsRegistered ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(TargetPosition, 0.5f);
            
            // If offset is applied, draw a line from transform to offset position for clarity.
            if (WorldOffset.sqrMagnitude > 0.0001f)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, TargetPosition);
                // Draw a small sphere at the actual transform position.
                Gizmos.DrawWireSphere(transform.position, 0.15f);
            }
#if UNITY_EDITOR
            // Display a helpful label above the target in the Scene view.
            string label = $"Waypoint: {gameObject.name}";
            if (!ActivateOnStart) label += " (Manual Activation)";
            UnityEditor.Handles.Label(TargetPosition + Vector3.up * 0.7f, label);
#endif
        }
    } // End Class
} // End Namespace
