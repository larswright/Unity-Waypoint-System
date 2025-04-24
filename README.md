# Unity Waypoint System

A flexible and efficient Waypoint System for Unity designed to display on-screen and off-screen markers for target GameObjects. Easily configurable via ScriptableObjects and supports both 2D and 3D environments.

## Features

* **On-Screen Markers:** Displays markers directly over targets within the camera view.
* **Off-Screen Indicators:** Shows clamped markers at the screen edge, pointing towards targets outside the camera view.
* **Configurable:** Uses `WaypointSettings` ScriptableObject for easy customization of appearance and behavior (update frequency, max distance, marker prefab, etc.).
* **Efficient:** Uses object pooling for managing UI markers, minimizing performance impact.
* **2D & 3D Support:** Works with both perspective and orthographic cameras.
* **Dynamic Targets:** Waypoints can be activated or deactivated during runtime.
* **Demo Controllers Included:** Basic FPS and 2D Platformer controllers for quick testing.

## Core Components

1.  **`WaypointUIManager.cs`:** The central manager. Place one instance in your scene. It discovers targets, manages the marker pool, and updates marker positions based on the camera and settings.
2.  **`WaypointTarget.cs`:** Attach this component to any GameObject you want a waypoint marker to point towards. Can be set to activate automatically on start or manually via script.
3.  **`WaypointMarkerUI.cs`:** Attach this script to your waypoint marker UI prefab (e.g., an Image). It handles positioning and rotating the marker correctly on the canvas, including clamping to screen edges.
4.  **`WaypointSettings.cs`:** A ScriptableObject asset used to define global settings for the waypoint system (e.g., marker prefab, update frequency, max visible distance, off-screen behavior). Create instances via `Assets -> Create -> WrightAngle -> Waypoint Settings`.

## Setup

1.  **Create Settings:** Create a `WaypointSettings` asset (Assets -> Create -> WrightAngle -> Waypoint Settings).
2.  **Configure Settings:** Assign your UI marker prefab (must have `WaypointMarkerUI` script) and adjust other settings like `MaxVisibleDistance`, `UpdateFrequency`, and `GameMode` (2D/3D) in the `WaypointSettings` asset.
3.  **Add Manager:** Add an empty GameObject to your scene and attach the `WaypointUIManager` script to it.
4.  **Assign References:** In the `WaypointUIManager` component inspector:
    * Assign the `WaypointSettings` asset you created.
    * Assign your main gameplay `Camera`.
    * Assign the `RectTransform` of the UI Canvas where markers should be placed.
5.  **Mark Targets:** Add the `WaypointTarget` component to any GameObject in your scene that should have a waypoint. Ensure `ActivateOnStart` is checked if you want them to appear automatically.
6.  **Create Marker Prefab:** Create a UI Image (or other UI element) on a Canvas. Attach the `WaypointMarkerUI` script to it. Assign the core visual element (e.g., the Image itself) to the `Marker Icon` field in the inspector. Make this UI element a prefab and assign it to the `MarkerPrefab` field in your `WaypointSettings` asset.

## Configuration (`WaypointSettings`)

* **`UpdateFrequency`:** How often the system updates positions (seconds). Lower is more responsive but potentially less performant.
* **`GameMode`:** Set to `Mode3D` for perspective cameras or `Mode2D` for orthographic.
* **`MarkerPrefab`:** The UI prefab used to represent waypoints.
* **`MaxVisibleDistance`:** How far away a target can be before its marker disappears.
* **`IgnoreZAxisForDistance2D`:** In 2D mode, calculate distance using only X/Y axes.
* **`UseOffScreenIndicators`:** Toggle showing markers clamped to the screen edge.
* **`ScreenEdgeMargin`:** Pixel distance from the screen edge for off-screen markers.
* **`FlipOffScreenMarkerY`:** Flips the off-screen marker vertically (useful if your icon points down).

## Demo Scripts

* **`WaypointFPSController.cs`:** Basic first-person movement (WASD) and mouse look. Requires a CharacterController.
* **`WaypointPlatformerController.cs`:** Simple 2D side-scrolling movement (A/D). Requires Rigidbody2D and Collider2D.
