using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// Builds simple 3D objects from Unity primitives ( markers, power-ups, spike strips ), so they don't need prefabs
    /// </summary>
    public static class ECCRuntimeVisuals
    {
        static Shader shader;

        /// <summary>
        /// A colored material. Emission makes it glow so it's easy to spot
        /// </summary>
        public static Material CreateMaterial(Color color, float emission = 0.4f)
        {
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            Material material = new Material(shader);
            material.color = color;

            if (emission > 0 && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
            }

            return material;
        }

        /// <summary>
        /// Creates a primitive shape without a collider ( the game checks distances instead of physics for these objects )
        /// </summary>
        public static Transform CreatePart(PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);

            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider) Object.Destroy(partCollider);

            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            Renderer partRenderer = part.GetComponent<Renderer>();
            partRenderer.sharedMaterial = material;
            partRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            return part.transform;
        }

        /// <summary>
        /// Finds the ground height under a position
        /// </summary>
        public static Vector3 SnapToGround(Vector3 position, LayerMask groundLayer)
        {
            // In the city, spawns go onto the streets, never inside a block; on the highway, inside the barriers
            position = ECCCityWorld.SnapToRoad(position);
            position = ECCHighwayWorld.SnapToRoad(position);

            RaycastHit hit;

            if (Physics.Raycast(position + Vector3.up * 20, Vector3.down, out hit, 100, groundLayer)) position.y = hit.point.y;

            return position;
        }

        /// <summary>
        /// A ground position along a direction from a center ( at least minDistance away ) that the camera can't see, so a
        /// car placed there drives into view instead of popping up on screen. Steps outward until the spot ( after snapping
        /// to the street ) is out of view with some margin for the car's size
        /// </summary>
        public static Vector3 OutOfView(Vector3 center, Vector3 direction, float minDistance, LayerMask groundLayer, float maxDistance = 150)
        {
            direction.y = 0;

            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;

            // On the highway only ahead and behind are out of view ( the sides are barriers )
            if (ECCHighwayWorld.IsActive) direction = new Vector3(0, 0, direction.z < 0 ? -1 : 1);

            direction.Normalize();

            Camera view = Camera.main;

            float distance = minDistance;

            Vector3 position = SnapToGround(center + direction * distance, groundLayer);

            while (view && distance < maxDistance && IsInView(view, position))
            {
                distance += 3;

                position = SnapToGround(center + direction * distance, groundLayer);
            }

            return position;
        }

        /// <summary>
        /// True if a car standing at this position would be ( even partly ) on screen. Checks the ground point and a point
        /// above it ( the roof of an enlarged car ), with a margin around the screen edges
        /// </summary>
        public static bool IsInView(Camera view, Vector3 position, float margin = 0.12f, float height = 4)
        {
            for (int index = 0; index < 2; index++)
            {
                Vector3 point = view.WorldToViewportPoint(position + Vector3.up * height * index);

                if (point.z > 0 && point.x > -margin && point.x < 1 + margin && point.y > -margin && point.y < 1 + margin) return true;
            }

            return false;
        }

        /// <summary>
        /// Distance on the ground plane, ignoring height
        /// </summary>
        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0;

            return Vector3.Distance(a, b);
        }
    }
}
