using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// A soft dark spot on the ground under a car, so it looks like it stands on the road even when real shadows are off
    /// ( the low quality levels have no shadows ) or far from the camera. Follows the car, flat on the ground
    /// </summary>
    public class ECCCarShadow : MonoBehaviour
    {
        static Material material;
        static Mesh quad;

        Transform car;
        float height;

        public static ECCCarShadow Create(ECCCar target)
        {
            if (target == null) return null;

            // The footprint from the car's collider ( already sized for the chase )
            Vector2 footprint = new Vector2(1.2f, 2.2f);

            BoxCollider box = target.GetComponent<BoxCollider>();
            if (box) footprint = new Vector2(box.size.x, box.size.z);

            return Create(target.transform, footprint);
        }

        /// <summary>
        /// A contact shadow under any moving object ( traffic cars ), footprint in world units ( width, length )
        /// </summary>
        public static ECCCarShadow Create(Transform target, Vector2 footprint)
        {
            if (target == null || target.GetComponentInChildren<ECCCarShadow>()) return null;

            GameObject spot = new GameObject("ContactShadow", typeof(MeshFilter), typeof(MeshRenderer));
            spot.transform.SetParent(target.transform, false);
            spot.transform.localPosition = Vector3.up * 0.03f;
            spot.transform.localRotation = Quaternion.Euler(90, 0, 0);
            spot.transform.localScale = new Vector3(footprint.x * 1.45f, footprint.y * 1.25f, 1);

            spot.GetComponent<MeshFilter>().sharedMesh = Quad;

            MeshRenderer renderer = spot.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            ECCCarShadow shadow = spot.AddComponent<ECCCarShadow>();
            shadow.car = target;
            shadow.height = 0.03f;

            return shadow;
        }

        void LateUpdate()
        {
            if (car == null) return;

            // Stay flat on the ground while the car leans, drifts or is flipped by a crash
            transform.position = new Vector3(car.position.x, height, car.position.z);
            transform.rotation = Quaternion.Euler(90, car.eulerAngles.y, 0);
        }

        static Mesh Quad
        {
            get
            {
                if (quad) return quad;

                quad = new Mesh { name = "ContactShadowQuad" };
                quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) };
                quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
                quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
                quad.RecalculateBounds();

                return quad;
            }
        }

        /// <summary>
        /// A see-through material with a soft round dark texture, drawn from code
        /// </summary>
        static Material Material
        {
            get
            {
                if (material) return material;

                const int size = 64;

                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.wrapMode = TextureWrapMode.Clamp;

                Color[] pixels = new Color[size * size];

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f) / size * 2 - 1;
                        float dy = (y + 0.5f) / size * 2 - 1;

                        float alpha = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy));

                        pixels[y * size + x] = new Color(0, 0, 0, alpha * alpha * 0.55f);
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply();

                material = new Material(Shader.Find("Sprites/Default"));
                material.mainTexture = texture;
                material.renderQueue = 3000;

                return material;
            }
        }
    }
}
