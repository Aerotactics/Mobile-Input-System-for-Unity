using UnityEngine;

namespace AInput
{
    [CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/AInputConfiguration", order = 1)]
    public class InputConfiguration : ScriptableObject
    {
        [Tooltip("Toggle using mouse for inputs, or device for inputs. Cannot use both at the same time.")]
        public bool useMouseForInputs = false;

        [Tooltip("Display debug info on screen.")]
        public bool showDebugUI = false;

        [Tooltip("When set to true, only the begin and end of a touch raycasts for collisions (great for simple inputs, but the collision won't be updated between presses).\nWhen set to false, every touch raycasts for collisions every frame (for when every collision matters)")]
        public bool minimalRaycasting = true;

        [Tooltip("The maximum amount of fingers on the screen")]
        public int maxTouches = 2;

        [Tooltip("The maximum time for a tap to be considered a tap rather than a press (in seconds)")]
        public float maxTapTime = 0.5f;

        [Tooltip("The maximum time between the end of a tap and the start of another (in seconds). If this value is 0, it disables double and multitaps, but the tap event will no longer be delayed.")]
        public float maxConsecutiveTouchTime = 0.5f;

        [Tooltip("The maximum time to be considered inhuman behaviour (in seconds)")]
        public float maxSuspiciousTime = 0.03f;

        [Tooltip("The maximum distance for a stationary touch (in pixels)")]
        public int maxTouchDistance = 3;

        [Tooltip("The maximum time for a slide to be considered a quicker \"swipe\" (in seconds)")]
        public float maxSwipeTime = 0.5f;

        [Tooltip("The minimum distance that is also considered for a slide to be a swipe (in pixels)")]
        public int minSwipeDistance = 20;

    }
}
