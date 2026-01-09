using UnityEngine;

namespace WrightAngle.Waypoint
{
    /// <summary>
    /// Available measurement unit systems for distance display.
    /// </summary>
    public enum MeasurementSystem
    {
        Metric,     // Meters, Kilometers
        Imperial    // Feet, Yards, Miles
    }

    /// <summary>
    /// Configure your waypoint distance text appearance and behavior.
    /// Create instances via 'Assets -> Create -> WrightAngle -> Distance Text Settings'.
    /// This asset allows customization of unit systems, suffixes, formatting, and colors.
    /// </summary>
    [CreateAssetMenu(fileName = "DistanceTextSettings", menuName = "WrightAngle/Distance Text Settings", order = 2)]
    public class DistanceTextSettings : ScriptableObject
    {
        [Header("Measurement System")]
        [Tooltip("Select the measurement system to use for displaying distances.")]
        public MeasurementSystem UnitSystem = MeasurementSystem.Metric;

        [Header("Metric Configuration")]
        [Tooltip("Suffix appended to meter values (e.g., 'm', ' m', ' meters').")]
        public string MeterSuffix = "m";

        [Tooltip("Suffix appended to kilometer values (e.g., 'km', ' km', ' kilometers').")]
        public string KilometerSuffix = "km";

        [Tooltip("Distance threshold (in meters) at which display switches from meters to kilometers.")]
        [Min(1f)]
        public float MetersToKilometersThreshold = 1000f;

        [Header("Imperial Configuration")]
        [Tooltip("Suffix appended to feet values (e.g., 'ft', ' ft', ' feet').")]
        public string FeetSuffix = "ft";

        [Tooltip("Suffix appended to yard values (e.g., 'yd', ' yd', ' yards').")]
        public string YardSuffix = "yd";

        [Tooltip("Suffix appended to mile values (e.g., 'mi', ' mi', ' miles').")]
        public string MileSuffix = "mi";

        [Tooltip("Distance threshold (in feet) at which display switches from feet to yards.")]
        [Min(1f)]
        public float FeetToYardsThreshold = 300f;

        [Tooltip("Distance threshold (in yards) at which display switches from yards to miles.")]
        [Min(1f)]
        public float YardsToMilesThreshold = 1760f;

        [Tooltip("When enabled, uses feet instead of yards as an intermediate unit.")]
        public bool SkipYards = false;

        [Header("Formatting Options")]
        [Tooltip("Number format string for integer values (e.g., 'N0' for no decimals, 'N1' for one decimal).")]
        public string IntegerFormat = "N0";

        [Tooltip("Number format string for decimal values (e.g., 'N1' for one decimal, 'N2' for two decimals).")]
        public string DecimalFormat = "N1";

        [Tooltip("Threshold value below which decimal format is used instead of integer format. Useful for showing '0.5km' instead of '1km'.")]
        [Min(0f)]
        public float DecimalThreshold = 10f;

        [Tooltip("Minimum distance (in world units) below which the distance text is hidden.")]
        [Min(0f)]
        public float MinDisplayDistance = 0f;

        [Tooltip("When enabled, hides the distance text when marker is off-screen.")]
        public bool HideWhenOffScreen = false;

        [Header("Visual Appearance")]
        [Tooltip("Default color for the distance text.")]
        public Color TextColor = Color.white;

        [Tooltip("When enabled, the text color changes based on distance.")]
        public bool UseDistanceColors = false;

        [Tooltip("Color used when the target is close (at CloseDistanceThreshold or nearer).")]
        public Color CloseColor = Color.green;

        [Tooltip("Color used when the target is far (at FarDistanceThreshold or further).")]
        public Color FarColor = Color.red;

        [Tooltip("Distance (in world units) at which the 'close' color is applied.")]
        [Min(0f)]
        public float CloseDistanceThreshold = 10f;

        [Tooltip("Distance (in world units) at which the 'far' color is applied.")]
        [Min(0f)]
        public float FarDistanceThreshold = 100f;

        // --- Conversion Constants ---
        private const float METERS_TO_FEET = 3.28084f;
        private const float FEET_TO_YARDS = 0.333333f;
        private const float YARDS_TO_MILES = 0.000568182f;
        private const float METERS_TO_KILOMETERS = 0.001f;

        /// <summary>
        /// Formats a distance value (in world units/meters) into a human-readable string
        /// based on the current settings configuration.
        /// </summary>
        /// <param name="distanceInMeters">The raw distance value in world units (typically meters).</param>
        /// <returns>A formatted string representing the distance with appropriate suffix.</returns>
        public string FormatDistance(float distanceInMeters)
        {
            if (UnitSystem == MeasurementSystem.Metric)
            {
                return FormatMetric(distanceInMeters);
            }
            else
            {
                return FormatImperial(distanceInMeters);
            }
        }

        /// <summary>
        /// Formats distance using the metric system (meters/kilometers).
        /// </summary>
        private string FormatMetric(float meters)
        {
            if (meters >= MetersToKilometersThreshold)
            {
                float kilometers = meters * METERS_TO_KILOMETERS;
                string format = kilometers < DecimalThreshold ? DecimalFormat : IntegerFormat;
                return $"{kilometers.ToString(format)}{KilometerSuffix}";
            }
            else
            {
                string format = meters < DecimalThreshold ? DecimalFormat : IntegerFormat;
                return $"{meters.ToString(format)}{MeterSuffix}";
            }
        }

        /// <summary>
        /// Formats distance using the imperial system (feet/yards/miles).
        /// </summary>
        private string FormatImperial(float meters)
        {
            float feet = meters * METERS_TO_FEET;

            if (SkipYards)
            {
                // Use only feet and miles
                float milesThresholdInFeet = YardsToMilesThreshold / FEET_TO_YARDS; // Convert yards threshold to feet
                if (feet >= milesThresholdInFeet)
                {
                    float miles = feet * FEET_TO_YARDS * YARDS_TO_MILES;
                    string format = miles < DecimalThreshold ? DecimalFormat : IntegerFormat;
                    return $"{miles.ToString(format)}{MileSuffix}";
                }
                else
                {
                    string format = feet < DecimalThreshold ? DecimalFormat : IntegerFormat;
                    return $"{feet.ToString(format)}{FeetSuffix}";
                }
            }
            else
            {
                // Use feet, yards, and miles
                if (feet >= FeetToYardsThreshold)
                {
                    float yards = feet * FEET_TO_YARDS;
                    if (yards >= YardsToMilesThreshold)
                    {
                        float miles = yards * YARDS_TO_MILES;
                        string format = miles < DecimalThreshold ? DecimalFormat : IntegerFormat;
                        return $"{miles.ToString(format)}{MileSuffix}";
                    }
                    else
                    {
                        string format = yards < DecimalThreshold ? DecimalFormat : IntegerFormat;
                        return $"{yards.ToString(format)}{YardSuffix}";
                    }
                }
                else
                {
                    string format = feet < DecimalThreshold ? DecimalFormat : IntegerFormat;
                    return $"{feet.ToString(format)}{FeetSuffix}";
                }
            }
        }

        /// <summary>
        /// Gets the appropriate color for the distance text based on the current distance.
        /// Returns the default TextColor if UseDistanceColors is disabled.
        /// </summary>
        /// <param name="distance">The current distance value.</param>
        /// <returns>The color to apply to the distance text.</returns>
        public Color GetDistanceColor(float distance)
        {
            if (!UseDistanceColors)
            {
                return TextColor;
            }

            // Clamp and interpolate between close and far colors
            float range = FarDistanceThreshold - CloseDistanceThreshold;
            if (range <= 0.001f)
            {
                return distance <= CloseDistanceThreshold ? CloseColor : FarColor;
            }

            float t = Mathf.Clamp01((distance - CloseDistanceThreshold) / range);
            return Color.Lerp(CloseColor, FarColor, t);
        }

        /// <summary>
        /// Determines if the distance text should be visible based on the current distance
        /// and screen position.
        /// </summary>
        /// <param name="distance">The current distance value.</param>
        /// <param name="isOnScreen">Whether the waypoint is currently on screen.</param>
        /// <returns>True if the distance text should be displayed, false otherwise.</returns>
        public bool ShouldDisplayDistance(float distance, bool isOnScreen)
        {
            if (distance < MinDisplayDistance)
            {
                return false;
            }

            if (HideWhenOffScreen && !isOnScreen)
            {
                return false;
            }

            return true;
        }
    }
}
