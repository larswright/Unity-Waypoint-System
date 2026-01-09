using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace WrightAngle.Waypoint.Editor
{
    public static class WaypointDocsShortcutOpener
    {
        [OnOpenAsset(1)]
        public static bool OpenDocsShortcut(int instanceID, int line)
        {
            var assetPath = AssetDatabase.GetAssetPath(instanceID);
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            var shortcut = AssetDatabase.LoadAssetAtPath<WaypointDocsShortcut>(assetPath);
            if (shortcut == null)
            {
                return false;
            }

            var url = shortcut.Url;
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning("WaypointDocsShortcut: URL is empty.");
                return true;
            }

            Application.OpenURL(url);
            return true;
        }
    }
}
