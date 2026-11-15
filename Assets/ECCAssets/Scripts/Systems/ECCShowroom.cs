using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The 3D car dealership around the garage's spinning car ( instead of the chase ground ): a glossy tiled floor, a
    /// back wall with big windows framed in neon, an orange stripe, a lit sign with a dollar emblem and strings of
    /// colored pennants, side walls, a dark turntable with light stripes and a glowing orange ring that turns with the
    /// car, a warm glow on the floor under the car, gold sparkles rising around it, two potted plants and a warm spot
    /// light from above.
    /// Built from primitives at runtime; shown only while the garage is open.
    /// </summary>
    public class ECCShowroom : MonoBehaviour
    {
        const float HalfWidth = 12;
        const float BackWall = 9;
        const float WallHeight = 12;

        Light spot;
        Transform turntable;
        float spinSpeed;

        /// <summary>
        /// The showroom around a center; spinSpeed is the garage car's spin ( degrees per second ), the turntable turns
        /// with it
        /// </summary>
        public static ECCShowroom Create(Vector3 center, float spinSpeed)
        {
            ECCShowroom showroom = new GameObject("Showroom").AddComponent<ECCShowroom>();
            showroom.transform.position = center;
            showroom.spinSpeed = spinSpeed;
            showroom.Build();
            showroom.gameObject.SetActive(false);

            return showroom;
        }

        void Build()
        {
            Transform root = transform;

            // Floor: light tiles, a little glossy. Slightly above the chase ground so they don't flicker
            Material floor = ECCRuntimeVisuals.CreateMaterial(Color.white, 0);
            floor.mainTexture = CreateTileTexture();
            floor.mainTextureScale = new Vector2(HalfWidth, HalfWidth);
            SetGloss(floor, 0.75f);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, new Vector3(0, 0.01f, 0), new Vector3(HalfWidth * 4, 0.02f, HalfWidth * 4), floor);

            // Walls
            Material wall = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0xF4EFE6), 0);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, new Vector3(0, WallHeight * 0.5f, BackWall), new Vector3(HalfWidth * 2, WallHeight, 0.4f), wall);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, new Vector3(-HalfWidth, WallHeight * 0.5f, 0), new Vector3(0.4f, WallHeight, HalfWidth * 4), wall);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, new Vector3(HalfWidth, WallHeight * 0.5f, 0), new Vector3(0.4f, WallHeight, HalfWidth * 4), wall);

            // Skirting and an orange stripe along the back wall
            Material dark = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0x2B2D42), 0);
            Material orange = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.CandyOrange, 0.35f);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, new Vector3(0, 0.2f, BackWall - 0.22f), new Vector3(HalfWidth * 2, 0.4f, 0.06f), dark);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, new Vector3(0, 4.6f, BackWall - 0.22f), new Vector3(HalfWidth * 2, 0.35f, 0.06f), orange);

            // Big windows with a sky-blue glow and dark frames
            Material glass = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0x8FD3FF), 0.55f);
            SetGloss(glass, 0.9f);

            for (int index = -2; index <= 2; index++)
            {
                if (index == 0) continue;

                Vector3 position = new Vector3(index * 4.4f, 2.3f, BackWall - 0.22f);

                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, position, new Vector3(3.8f, 3.2f, 0.05f), dark);
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, position + new Vector3(0, 0, -0.03f), new Vector3(3.5f, 2.9f, 0.05f), glass);
            }

            // The lit sign in the middle of the back wall
            Material signBoard = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0x8E5CF7), 0.5f);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, new Vector3(0, 2.5f, BackWall - 0.3f), new Vector3(3.6f, 1.6f, 0.12f), signBoard);
            CreateEmblem(new Vector3(0, 2.5f, BackWall - 0.38f));

            // Turntable under the car, with a glowing ring and light stripes, turning with the car
            turntable = new GameObject("Turntable").transform;
            turntable.SetParent(root, false);

            Material ring = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.CandyOrange, 0.9f);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, turntable, new Vector3(0, 0.025f, 0), new Vector3(7.4f, 0.025f, 7.4f), ring);

            Material table = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0x3A3F4E), 0);
            SetGloss(table, 0.85f);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, turntable, new Vector3(0, 0.03f, 0), new Vector3(7, 0.03f, 7), table);

            Material stripe = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0xFFD27A), 0.7f);

            for (int index = 0; index < 8; index++)
            {
                Transform line = ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, turntable, Vector3.zero, new Vector3(0.12f, 0.02f, 0.9f), stripe);
                line.localRotation = Quaternion.Euler(0, index * 45, 0);
                line.localPosition = line.localRotation * new Vector3(0, 0.065f, 2.85f);
            }

            CreateFloorGlow(root);
            CreateNeonAndPennants(root);
            CreateSparkles(root);

            // Potted plants by the back wall
            Material pot = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0xE8E2D6), 0);
            Material leaves = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0x3FA34D), 0.1f);

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 position = new Vector3(side * 6.6f, 0, BackWall - 1.2f);

                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, root, position + new Vector3(0, 0.4f, 0), new Vector3(0.9f, 0.4f, 0.9f), pot);
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Sphere, root, position + new Vector3(0, 1.5f, 0), new Vector3(1.4f, 1.8f, 1.4f), leaves);
            }

            // A warm spot light over the car
            spot = new GameObject("Spot").AddComponent<Light>();
            spot.transform.SetParent(root, false);
            spot.transform.localPosition = new Vector3(0, 9, -1.5f);
            spot.transform.localRotation = Quaternion.Euler(80, 0, 0);
            spot.type = LightType.Spot;
            spot.spotAngle = 70;
            spot.range = 25;
            spot.intensity = 2.2f;
            spot.color = ECCCarTheme.Hex(0xFFF1DC);
            spot.shadows = LightShadows.None;
        }

        /// <summary>
        /// The dealership's dollar emblem on the sign board. Uses the city's Fade material ( Standard, transparent, depth
        /// tested, and included in builds through Resources ); skipped if it's missing
        /// </summary>
        void CreateEmblem(Vector3 position)
        {
            Material fade = Resources.Load<Material>("KenneyCity/KenneyFade");
            Texture2D dollar = Resources.Load<Texture2D>("ECCUI/DollarIcon");

            if (fade == null || dollar == null) return;

            Material emblem = new Material(fade);
            emblem.color = Color.white;
            emblem.mainTexture = dollar;

            if (emblem.HasProperty("_EmissionColor"))
            {
                emblem.EnableKeyword("_EMISSION");
                emblem.SetColor("_EmissionColor", new Color(0.35f, 0.35f, 0.35f));
                emblem.SetTexture("_EmissionMap", dollar);
            }

            // A quad faces the camera of the garage ( which looks along +Z )
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Quad, transform, position, new Vector3(1.3f, 1.3f, 1), emblem);
        }

        void Update()
        {
            if (turntable) turntable.Rotate(Vector3.up * spinSpeed * Time.deltaTime, Space.World);
        }

        /// <summary>
        /// A warm soft glow on the floor around the turntable ( the Fade material, with a soft round texture )
        /// </summary>
        void CreateFloorGlow(Transform root)
        {
            Material fade = Resources.Load<Material>("KenneyCity/KenneyFade");

            if (fade == null) return;

            Material glow = new Material(fade);
            glow.mainTexture = SoftDot(64);
            glow.color = new Color(1, 0.8f, 0.45f, 0.55f);

            if (glow.HasProperty("_EmissionColor"))
            {
                glow.EnableKeyword("_EMISSION");
                glow.SetColor("_EmissionColor", new Color(0.5f, 0.35f, 0.15f));
            }

            Transform quad = ECCRuntimeVisuals.CreatePart(PrimitiveType.Quad, root, new Vector3(0, 0.022f, 0), new Vector3(12, 12, 1), glow);
            quad.localRotation = Quaternion.Euler(90, 0, 0);
        }

        /// <summary>
        /// Neon frames around the windows and strings of colored pennants across the back wall
        /// </summary>
        void CreateNeonAndPennants(Transform root)
        {
            Material neonPink = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0xFF5FA2), 1.2f);
            Material neonCyan = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0x3FE0F5), 1.2f);

            for (int index = -2; index <= 2; index++)
            {
                if (index == 0) continue;

                Material neon = Mathf.Abs(index) == 1 ? neonPink : neonCyan;
                Vector3 center = new Vector3(index * 4.4f, 2.3f, BackWall - 0.3f);

                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, center + new Vector3(0, 1.72f, 0), new Vector3(4, 0.09f, 0.05f), neon);
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, center + new Vector3(0, -1.72f, 0), new Vector3(4, 0.09f, 0.05f), neon);
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, center + new Vector3(1.97f, 0, 0), new Vector3(0.09f, 3.5f, 0.05f), neon);
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, center + new Vector3(-1.97f, 0, 0), new Vector3(0.09f, 3.5f, 0.05f), neon);
            }

            // Pennants: little diamonds hanging from a drooping line, in the candy colors
            Color[] colors = { ECCCarTheme.CandyRed, ECCCarTheme.CandyOrange, ECCCarTheme.Hex(0xFFC83D), ECCCarTheme.CandyGreen, ECCCarTheme.CandyBlue, ECCCarTheme.Hex(0x8E5CF7) };
            Material[] flags = new Material[colors.Length];

            for (int index = 0; index < colors.Length; index++) flags[index] = ECCRuntimeVisuals.CreateMaterial(colors[index], 0.25f);

            Material cord = ECCRuntimeVisuals.CreateMaterial(ECCCarTheme.Hex(0x2B2D42), 0);

            for (int side = -1; side <= 1; side += 2)
            {
                const int count = 9;

                for (int index = 0; index < count; index++)
                {
                    float t = index / (float)(count - 1);
                    float x = side * Mathf.Lerp(1.9f, HalfWidth - 0.6f, t);
                    float y = 4.25f - Mathf.Sin(t * Mathf.PI) * 0.45f;

                    Transform flag = ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, new Vector3(x, y - 0.22f, BackWall - 0.35f), new Vector3(0.36f, 0.36f, 0.03f), flags[(index + (side > 0 ? 0 : 3)) % flags.Length]);
                    flag.localRotation = Quaternion.Euler(0, 0, 45);
                }

                // The cord, as a few short straight pieces along the droop
                for (int index = 0; index < count - 1; index++)
                {
                    float t0 = index / (float)(count - 1);
                    float t1 = (index + 1) / (float)(count - 1);

                    Vector3 a = new Vector3(side * Mathf.Lerp(1.9f, HalfWidth - 0.6f, t0), 4.25f - Mathf.Sin(t0 * Mathf.PI) * 0.45f, BackWall - 0.34f);
                    Vector3 b = new Vector3(side * Mathf.Lerp(1.9f, HalfWidth - 0.6f, t1), 4.25f - Mathf.Sin(t1 * Mathf.PI) * 0.45f, BackWall - 0.34f);

                    Transform piece = ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, root, (a + b) * 0.5f, new Vector3(Vector3.Distance(a, b), 0.03f, 0.03f), cord);
                    piece.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
                }
            }
        }

        /// <summary>
        /// Gold sparkles rising slowly around the car
        /// </summary>
        void CreateSparkles(Transform root)
        {
            Material material = ECCRainWeather.CreateParticleMaterial(SoftDot(32), null);

            if (material == null) return;

            GameObject holder = new GameObject("Sparkles");
            holder.transform.SetParent(root, false);
            holder.transform.localPosition = new Vector3(0, 0.1f, 0);

            ParticleSystem sparkles = holder.AddComponent<ParticleSystem>();
            sparkles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = sparkles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1, 0.85f, 0.35f, 0.9f), new Color(1, 1, 0.8f, 0.9f));
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = sparkles.emission;
            emission.rateOverTime = 22;

            ParticleSystem.ShapeModule shape = sparkles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 3.4f;
            shape.radiusThickness = 0.2f;
            shape.rotation = new Vector3(-90, 0, 0);

            ParticleSystem.ColorOverLifetimeModule color = sparkles.colorOverLifetime;
            color.enabled = true;

            Gradient fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(0, 1) });
            color.color = fade;

            ParticleSystemRenderer renderer = holder.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            sparkles.Play();
        }

        static Texture2D SoftDot(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = new Vector2(x + 0.5f - size * 0.5f, y + 0.5f - size * 0.5f).magnitude / (size * 0.5f);
                    float alpha = Mathf.Clamp01(1 - distance);

                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha * alpha));
                }
            }

            texture.Apply();

            return texture;
        }

        static void SetGloss(Material material, float gloss)
        {
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", gloss);
        }

        /// <summary>
        /// Two by two light tiles with thin gray joints ( repeats every two meters with the floor's scale )
        /// </summary>
        static Texture2D CreateTileTexture()
        {
            const int size = 128;

            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.anisoLevel = 4;

            Color tileA = ECCCarTheme.Hex(0xE9EDF3);
            Color tileB = ECCCarTheme.Hex(0xDDE3EC);
            Color joint = ECCCarTheme.Hex(0xB9C1CE);

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int half = size / 2;
                    bool isJoint = x % half < 2 || y % half < 2;
                    bool checker = (x / half + y / half) % 2 == 0;

                    pixels[y * size + x] = isJoint ? joint : (checker ? tileA : tileB);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(true);

            return texture;
        }
    }
}
