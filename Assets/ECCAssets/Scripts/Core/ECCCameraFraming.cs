using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// One camera distance for every chapter and mode: the framing of chapter 1 ( the city ), where the gameplay camera
    /// sits 1.75 times farther back along its own angle. Applied once per run ( the scene reloads between runs ), so the
    /// city, the highway and the open-ground chapters all look the same distance away.
    /// </summary>
    public static class ECCCameraFraming
    {
        /// <summary>
        /// The chapter 1 camera distance ( multiplier of the scene's camera offset )
        /// </summary>
        public const float Distance = 1.75f;

        /// <summary>
        /// The gameplay camera's near clip plane
        /// </summary>
        public const float NearClip = 0.5f;

        /// <summary>
        /// Moves the gameplay camera back to the shared distance. Calling it again in the same run does nothing
        /// </summary>
        public static void Apply()
        {
            Camera view = Camera.main;

            if (view == null) return;

            // A marker on the camera: the scene's camera is new after every reload, so this runs once per run
            if (view.GetComponent<ECCCameraFramingMarker>() != null) return;

            view.gameObject.AddComponent<ECCCameraFramingMarker>();

            view.transform.localPosition *= Distance;
            view.farClipPlane = Mathf.Max(view.farClipPlane, 300);

            // The scene's near plane ( 0.01 ) wastes almost all the depth precision: with the far plane at 300 the road
            // and the ground under it flicker against each other ( z-fighting ). Nothing comes this close to a camera
            // that sits about 50 meters above and behind the player
            view.nearClipPlane = Mathf.Max(view.nearClipPlane, NearClip);
        }
    }

    /// <summary>
    /// Marks a camera that already has the shared framing
    /// </summary>
    public class ECCCameraFramingMarker : MonoBehaviour
    {
    }
}
