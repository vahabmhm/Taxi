using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// Cheap light effects without realtime lights: soft glowing discs on the ground ( headlight pools, the helicopter's
    /// spotlight circle, a car's neon underglow, shadows ) and see-through light beams. They use the alpha blended
    /// particle material of the game's own effects ( ECCRainWeather.CreateParticleMaterial ), so the shader is in the build.
    /// Materials are shared per color.
    /// </summary>
    public static class ECCGlow
    {
        static Texture2D radialTexture;
        static Texture2D beamTexture;
        static Texture2D coneTexture;
        static Mesh coneMesh;

        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

        /// <summary>
        /// A white disc that fades out to the edge
        /// </summary>
        static Texture2D RadialTexture
        {
            get
            {
                if (radialTexture) return radialTexture;

                const int size = 64;

                radialTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                radialTexture.wrapMode = TextureWrapMode.Clamp;

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f) / size * 2 - 1;
                        float dy = (y + 0.5f) / size * 2 - 1;
                        float distance = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(1 - distance);

                        radialTexture.SetPixel(x, y, new Color(1, 1, 1, alpha * alpha * (3 - 2 * alpha)));
                    }
                }

                radialTexture.Apply();

                return radialTexture;
            }
        }

        /// <summary>
        /// A headlight beam on the ground: bright at the car ( bottom ), widening and fading ahead ( top )
        /// </summary>
        static Texture2D BeamTexture
        {
            get
            {
                if (beamTexture) return beamTexture;

                const int width = 32, height = 64;

                beamTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                beamTexture.wrapMode = TextureWrapMode.Clamp;

                for (int y = 0; y < height; y++)
                {
                    float along = (y + 0.5f) / height;

                    // The beam widens from 30% of the width at the car to the full width ahead
                    float halfWidth = Mathf.Lerp(0.15f, 0.5f, along);

                    for (int x = 0; x < width; x++)
                    {
                        float across = Mathf.Abs((x + 0.5f) / width - 0.5f) / halfWidth;
                        float alpha = Mathf.Clamp01(1 - across) * Mathf.Clamp01(1 - along) * Mathf.Clamp01(along * 6);

                        beamTexture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                }

                beamTexture.Apply();

                return beamTexture;
            }
        }

        /// <summary>
        /// A light cone seen from the side: brightest near the lamp ( v = 0 ), fading towards the ground
        /// </summary>
        static Texture2D ConeTexture
        {
            get
            {
                if (coneTexture) return coneTexture;

                const int height = 32;

                coneTexture = new Texture2D(4, height, TextureFormat.RGBA32, false);
                coneTexture.wrapMode = TextureWrapMode.Clamp;

                for (int y = 0; y < height; y++)
                {
                    float alpha = Mathf.Lerp(1, 0.25f, (y + 0.5f) / height);

                    for (int x = 0; x < 4; x++) coneTexture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }

                coneTexture.Apply();

                return coneTexture;
            }
        }

        static Material GetMaterial(string kind, Texture2D texture, Color color)
        {
            string key = kind + ColorUtility.ToHtmlStringRGBA(color);

            Material material;

            if (materials.TryGetValue(key, out material) && material) return material;

            material = ECCRainWeather.CreateParticleMaterial(texture, ECCGameController.Instance);

            if (material == null) return null;

            // The particle shader shows its tint at double strength: half the color gives the color itself
            if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, color.a * 0.5f));

            materials[key] = material;

            return material;
        }

        public static Material RadialMaterial(Color color)
        {
            return GetMaterial("radial", RadialTexture, color);
        }

        public static Material BeamMaterial(Color color)
        {
            return GetMaterial("beam", BeamTexture, color);
        }

        public static Material ConeMaterial(Color color)
        {
            return GetMaterial("cone", ConeTexture, color);
        }

        /// <summary>
        /// A flat quad lying on the ground under a parent, a little above it so it doesn't flicker with the road
        /// </summary>
        public static Transform CreateFlat(string name, Transform parent, Vector3 localPosition, Vector2 size, Material material)
        {
            if (material == null) return null;

            Transform quad = ECCRuntimeVisuals.CreatePart(PrimitiveType.Quad, parent, localPosition, new Vector3(size.x, size.y, 1), material);
            quad.name = name;
            quad.localRotation = Quaternion.Euler(90, 0, 0);

            Renderer renderer = quad.GetComponent<Renderer>();
            renderer.receiveShadows = false;

            return quad;
        }

        /// <summary>
        /// An open cone ( apex at the origin, pointing down -Y, 1 unit tall and 1 unit radius at the base ), seen from inside
        /// and outside. Scale it to the beam's length and width
        /// </summary>
        public static Mesh ConeMesh
        {
            get
            {
                if (coneMesh) return coneMesh;

                const int sides = 16;

                Vector3[] vertices = new Vector3[(sides + 1) * 2];
                Vector2[] uv = new Vector2[vertices.Length];
                int[] triangles = new int[sides * 6];

                for (int index = 0; index <= sides; index++)
                {
                    float angle = index / (float)sides * Mathf.PI * 2;

                    vertices[index * 2] = Vector3.zero;
                    vertices[index * 2 + 1] = new Vector3(Mathf.Cos(angle), -1, Mathf.Sin(angle));

                    uv[index * 2] = new Vector2(index / (float)sides, 0);
                    uv[index * 2 + 1] = new Vector2(index / (float)sides, 1);
                }

                for (int index = 0; index < sides; index++)
                {
                    int apex = index * 2, baseA = index * 2 + 1, baseB = index * 2 + 3;

                    triangles[index * 6] = apex;
                    triangles[index * 6 + 1] = baseB;
                    triangles[index * 6 + 2] = baseA;

                    // The back faces, for shaders that cull them
                    triangles[index * 6 + 3] = apex;
                    triangles[index * 6 + 4] = baseA;
                    triangles[index * 6 + 5] = baseB;
                }

                coneMesh = new Mesh();
                coneMesh.name = "LightCone";
                coneMesh.vertices = vertices;
                coneMesh.uv = uv;
                coneMesh.triangles = triangles;
                coneMesh.RecalculateBounds();

                return coneMesh;
            }
        }

        /// <summary>
        /// A see-through light cone object
        /// </summary>
        public static Transform CreateCone(string name, Transform parent, Material material)
        {
            if (material == null) return null;

            GameObject cone = new GameObject(name);
            cone.transform.SetParent(parent, false);

            cone.AddComponent<MeshFilter>().sharedMesh = ConeMesh;

            MeshRenderer renderer = cone.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return cone.transform;
        }
    }
}
