using UnityEngine;

namespace WrightAngle.Waypoint
{
    /// <summary>
    /// Defines the visual appearance of a waypoint marker type.
    /// Create presets for different waypoint categories (Main Quest, Side Quest, POI, etc.)
    /// and assign them to WaypointTarget components for distinct visual styles.
    /// Create via: Assets → Create → WrightAngle → Waypoint Preset
    /// </summary>
    [CreateAssetMenu(fileName = "WaypointPreset", menuName = "WrightAngle/Waypoint Preset", order = 2)]
    public class WaypointPreset : ScriptableObject
    {
        [Header("Icon Appearance")]
        [Tooltip("The sprite to display for this waypoint type. If null, uses the prefab's default icon.")]
        public Sprite Icon;

        [Tooltip("Tint color applied to the marker icon.")]
        public Color IconColor = Color.white;

        [Header("Scale")]
        [Tooltip("Multiplier applied to the marker's base scale. Use to make certain waypoint types larger or smaller.")]
        [Range(0.1f, 3f)]
        public float ScaleMultiplier = 1f;

        [Header("Off-Screen Indicator")]
        [Tooltip("Optional separate icon to use when the waypoint is off-screen. If null, uses the main Icon.")]
        public Sprite OffScreenIcon;

        [Tooltip("Optional separate color for off-screen state. If alpha is 0, uses IconColor.")]
        public Color OffScreenColor = new Color(1f, 1f, 1f, 0f);

        /// <summary>
        /// Returns the appropriate icon for the current screen state.
        /// </summary>
        /// <param name="isOnScreen">Whether the waypoint is currently on-screen.</param>
        /// <returns>The icon sprite to use, or null if default should be used.</returns>
        public Sprite GetIcon(bool isOnScreen)
        {
            if (!isOnScreen && OffScreenIcon != null)
                return OffScreenIcon;
            return Icon;
        }

        /// <summary>
        /// Returns the appropriate color for the current screen state.
        /// </summary>
        /// <param name="isOnScreen">Whether the waypoint is currently on-screen.</param>
        /// <returns>The color to apply to the marker.</returns>
        public Color GetColor(bool isOnScreen)
        {
            if (!isOnScreen && OffScreenColor.a > 0.001f)
                return OffScreenColor;
            return IconColor;
        }
    }
}
