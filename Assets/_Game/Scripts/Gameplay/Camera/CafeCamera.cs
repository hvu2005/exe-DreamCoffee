using UnityEngine;

namespace DreamCafe.Gameplay.Camera
{
    /// <summary>
    /// Applies a 2.5D orthographic look to the attached Camera on Awake.
    /// Tilt the X angle for an isometric feel; sprites face the camera automatically.
    /// Attach to the main Camera object in the scene.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class CafeCamera : MonoBehaviour
    {
        [SerializeField] private float orthographicSize = 6f;
        [Tooltip("X-axis tilt in degrees. 10-20 gives a subtle isometric look.")]
        [SerializeField] private float tiltDegrees = 15f;
        [Tooltip("Camera Y world position — raise slightly so tables appear below center.")]
        [SerializeField] private float verticalOffset = 1.5f;

        private void Awake()
        {
            var cam = GetComponent<UnityEngine.Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = orthographicSize;
            var pos = transform.position;
            transform.SetPositionAndRotation(
                new Vector3(pos.x, verticalOffset, -10f),
                Quaternion.Euler(tiltDegrees, 0f, 0f));
        }
    }
}
