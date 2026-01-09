using UnityEngine;

namespace WrightAngle.Waypoint
{
    [CreateAssetMenu(fileName = "WaypointDocsShortcut", menuName = "WrightAngle/Waypoint Docs Shortcut", order = 100)]
    public sealed class WaypointDocsShortcut : ScriptableObject
    {
        [SerializeField]
        private string url = "https://wrightangle.dev/docs-waypoint.html";

        public string Url => url;
    }
}
