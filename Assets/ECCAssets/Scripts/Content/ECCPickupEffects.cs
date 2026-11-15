using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// Feedback effects for what the player car picks up, built at runtime ( no prefabs ):
    /// - Heal: green plus signs and sparkles rise from the car and a ring pulses on the road ( repair item, repair kit ).
    /// - Electric: crackling arcs around the car, arcs to the police it knocks out, sparks and a shock ring ( EMP ).
    /// - MoneyToBalance: the dollars of a cash pickup fly into the money balance at the top of the screen; each one adds
    ///   its share to the wallet when it lands ( the rest of the run's money is still banked at the end of the run ).
    /// Everything uses Sprites/Default, which every build includes.
    /// </summary>
    public static class ECCPickupEffects
    {
        static readonly Color HealColor = new Color(0.35f, 1, 0.45f, 1);
        static readonly Color SparkColor = new Color(1, 0.92f, 0.45f, 1);
        static readonly Color BoltColor = new Color(0.6f, 0.9f, 1, 1);

        static Texture2D softDot;
        static Texture2D plusSign;
        static Texture2D ring;

        public static void Heal(ECCCar car)
        {
            if (car == null) return;

            GameObject holder = new GameObject("HealEffect");
            holder.AddComponent<ECCHealEffect>().Build(car);
        }

        public static void Electric(ECCCar car, float radius)
        {
            if (car == null) return;

            GameObject holder = new GameObject("ElectricEffect");
            holder.AddComponent<ECCElectricEffect>().Build(car, radius);
        }

        /// <summary>
        /// Flies the dollars of a cash pickup from where it was on the road to the balance. Without a balance on screen
        /// the money is banked right away
        /// </summary>
        public static void MoneyToBalance(ECCGameController gameController, Vector3 worldPosition, int amount)
        {
            if (gameController == null || amount <= 0) return;

            RectTransform target = ECCCoinBalanceButton.FlyTarget;
            Camera view = Camera.main;

            if (target == null || view == null)
            {
                gameController.BankEarly(amount);
                return;
            }

            ECCMoneyFlight.Launch(gameController, view.WorldToScreenPoint(worldPosition), target, amount);
        }

        // ----- Shared pieces -----

        /// <summary>
        /// The car's box in the world ( from its collider, the renderers if it has none )
        /// </summary>
        public static Bounds CarBounds(ECCCar car)
        {
            Collider body = car.GetComponent<Collider>();

            if (body) return body.bounds;

            Bounds bounds = new Bounds(car.transform.position + Vector3.up * 0.6f, new Vector3(1.4f, 1.2f, 2.4f));

            foreach (MeshRenderer part in car.GetComponentsInChildren<MeshRenderer>()) bounds.Encapsulate(part.bounds);

            return bounds;
        }

        public static Material CreateMaterial(Texture2D texture, Color color)
        {
            Material material = new Material(Shader.Find("Sprites/Default"));
            if (texture) material.mainTexture = texture;
            material.color = color;

            return material;
        }

        /// <summary>
        /// A flat textured quad on the road
        /// </summary>
        public static Transform CreateFloorQuad(string name, Transform parent, Material material)
        {
            Transform quad = ECCRuntimeVisuals.CreatePart(PrimitiveType.Quad, parent, Vector3.zero, Vector3.one, material);
            quad.name = name;
            quad.localRotation = Quaternion.Euler(90, 0, 0);

            return quad;
        }

        public static ParticleSystem CreateParticles(string name, Transform parent, Texture2D texture, int maxParticles)
        {
            GameObject holder = new GameObject(name);
            holder.transform.SetParent(parent, false);

            ParticleSystem particles = holder.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 1;
            main.maxParticles = maxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0;

            ParticleSystemRenderer renderer = holder.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = CreateMaterial(texture, Color.white);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return particles;
        }

        /// <summary>
        /// Fades particles in quickly and out slowly
        /// </summary>
        public static void FadeOverLifetime(ParticleSystem particles)
        {
            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;

            Gradient fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) });
            color.color = fade;
        }

        public static void DestroyMaterials(GameObject root)
        {
            foreach (Renderer part in root.GetComponentsInChildren<Renderer>(true))
            {
                if (part.sharedMaterial) Object.Destroy(part.sharedMaterial);
            }
        }

        public static Color Sparks
        {
            get { return SparkColor; }
        }

        public static Color Bolt
        {
            get { return BoltColor; }
        }

        public static Color Healing
        {
            get { return HealColor; }
        }

        // ----- Textures ( made once ) -----

        public static Texture2D SoftDot
        {
            get
            {
                if (softDot == null) softDot = CreateTexture(32, delegate (float x, float y) { float alpha = Mathf.Clamp01(1 - new Vector2(x, y).magnitude); return alpha * alpha; });

                return softDot;
            }
        }

        /// <summary>
        /// A rounded plus sign with a soft edge
        /// </summary>
        public static Texture2D PlusSign
        {
            get
            {
                if (plusSign == null)
                {
                    plusSign = CreateTexture(48, delegate (float x, float y)
                    {
                        float arm = 0.24f;
                        float length = 0.82f;

                        float horizontal = Mathf.Max(Mathf.Abs(y) - arm, Mathf.Abs(x) - length);
                        float vertical = Mathf.Max(Mathf.Abs(x) - arm, Mathf.Abs(y) - length);

                        return Mathf.Clamp01(-Mathf.Min(horizontal, vertical) * 14 + 0.5f);
                    });
                }

                return plusSign;
            }
        }

        /// <summary>
        /// A soft ring, brightest at its edge
        /// </summary>
        public static Texture2D Ring
        {
            get
            {
                if (ring == null)
                {
                    ring = CreateTexture(128, delegate (float x, float y)
                    {
                        float distance = new Vector2(x, y).magnitude;

                        if (distance > 1) return 0;

                        float edge = Mathf.Clamp01(1 - Mathf.Abs(distance - 0.88f) / 0.1f);
                        float inside = Mathf.Clamp01(distance) * 0.25f;

                        return Mathf.Max(edge, inside * inside);
                    });
                }

                return ring;
            }
        }

        static Texture2D CreateTexture(int size, System.Func<float, float, float> alphaAt)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // -1 to 1 across the texture
                    float u = (x + 0.5f) / size * 2 - 1;
                    float v = (y + 0.5f) / size * 2 - 1;

                    pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(alphaAt(u, v)));
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return texture;
        }
    }

    /// <summary>
    /// The healing effect: follows the car for about two seconds, then removes itself
    /// </summary>
    public class ECCHealEffect : MonoBehaviour
    {
        const float Duration = 2;

        Transform car;
        Vector3 offset;
        float time = 0;
        float radius = 1.2f;

        readonly List<Transform> rings = new List<Transform>();
        readonly List<Material> ringMaterials = new List<Material>();
        Transform glow;
        Material glowMaterial;

        public void Build(ECCCar target)
        {
            car = target.transform;

            Bounds bounds = ECCPickupEffects.CarBounds(target);

            radius = Mathf.Max(0.8f, Mathf.Max(bounds.extents.x, bounds.extents.z));

            // The effect stands on the road under the car's middle
            offset = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) - car.position;
            transform.position = car.position + offset;

            Color heal = ECCPickupEffects.Healing;

            // A soft green glow on the road
            glowMaterial = ECCPickupEffects.CreateMaterial(ECCPickupEffects.SoftDot, new Color(heal.r, heal.g, heal.b, 0));
            glow = ECCPickupEffects.CreateFloorQuad("Glow", transform, glowMaterial);
            glow.localPosition = Vector3.up * 0.04f;
            glow.localScale = Vector3.one * radius * 3.4f;

            // Two rings that spread out, one after the other
            for (int index = 0; index < 2; index++)
            {
                Material material = ECCPickupEffects.CreateMaterial(ECCPickupEffects.Ring, new Color(heal.r, heal.g, heal.b, 0));
                Transform ringQuad = ECCPickupEffects.CreateFloorQuad("Ring", transform, material);
                ringQuad.localPosition = Vector3.up * (0.06f + index * 0.01f);

                rings.Add(ringQuad);
                ringMaterials.Add(material);
            }

            // Plus signs rising around the car
            ParticleSystem pluses = ECCPickupEffects.CreateParticles("Pluses", transform, ECCPickupEffects.PlusSign, 60);
            pluses.transform.localPosition = Vector3.up * 0.3f;
            pluses.transform.localRotation = Quaternion.Euler(90, 0, 0);

            ParticleSystem.MainModule main = pluses.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.3f);
            main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f * radius, 0.6f * radius);
            main.startColor = new ParticleSystem.MinMaxGradient(heal, new Color(0.75f, 1, 0.75f, 1));

            ParticleSystem.EmissionModule emission = pluses.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 12), new ParticleSystem.Burst(0.25f, 9), new ParticleSystem.Burst(0.5f, 7) });

            ParticleSystem.ShapeModule shape = pluses.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius * 0.95f;

            Rise(pluses, 2, 3.4f);

            ParticleSystem.SizeOverLifetimeModule size = pluses.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, 0.4f), new Keyframe(0.25f, 1.1f), new Keyframe(0.5f, 1), new Keyframe(1, 0.6f)));

            ECCPickupEffects.FadeOverLifetime(pluses);

            // Small sparkles swirling up through the car
            ParticleSystem sparkles = ECCPickupEffects.CreateParticles("Sparkles", transform, ECCPickupEffects.SoftDot, 60);
            sparkles.transform.localPosition = Vector3.up * 0.6f;

            ParticleSystem.MainModule sparkleMain = sparkles.main;
            sparkleMain.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            sparkleMain.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
            sparkleMain.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            sparkleMain.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(0.7f, 1, 0.75f, 1));

            ParticleSystem.EmissionModule sparkleEmission = sparkles.emission;
            sparkleEmission.SetBursts(new[] { new ParticleSystem.Burst(0, 26), new ParticleSystem.Burst(0.3f, 14) });

            ParticleSystem.ShapeModule sparkleShape = sparkles.shape;
            sparkleShape.shapeType = ParticleSystemShapeType.Sphere;
            sparkleShape.radius = radius;

            Rise(sparkles, 1, 2.2f);
            ECCPickupEffects.FadeOverLifetime(sparkles);

            pluses.Play();
            sparkles.Play();
        }

        /// <summary>
        /// Particles drift up in the world, whatever way the car faces
        /// </summary>
        static void Rise(ParticleSystem particles, float min, float max)
        {
            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0, 0);
            velocity.y = new ParticleSystem.MinMaxCurve(min, max);
            velocity.z = new ParticleSystem.MinMaxCurve(0, 0);
        }

        void LateUpdate()
        {
            time += Time.deltaTime;

            if (car) transform.position = car.position + offset;

            Color heal = ECCPickupEffects.Healing;

            for (int index = 0; index < rings.Count; index++)
            {
                float progress = Mathf.Clamp01((time - index * 0.3f) / 0.8f);
                float eased = 1 - Mathf.Pow(1 - progress, 2);

                rings[index].localScale = Vector3.one * radius * Mathf.Lerp(1.2f, 4, eased);
                ringMaterials[index].color = new Color(heal.r, heal.g, heal.b, progress <= 0 ? 0 : (1 - progress) * 0.9f);
            }

            // The glow swells in and fades out
            float glowAlpha = time < 0.25f ? time / 0.25f : Mathf.Clamp01(1 - (time - 0.25f) / 1);
            glowMaterial.color = new Color(heal.r, heal.g, heal.b, glowAlpha * 0.6f);

            if (time >= Duration) Destroy(gameObject);
        }

        void OnDestroy()
        {
            ECCPickupEffects.DestroyMaterials(gameObject);
        }
    }

    /// <summary>
    /// The EMP shock: arcs crackle around the car and jump to the police in range, sparks fly and a ring spreads out
    /// </summary>
    public class ECCElectricEffect : MonoBehaviour
    {
        const float Duration = 1.1f;
        const float CopArcTime = 0.4f;
        const int CageArcs = 7;

        class Arc
        {
            public LineRenderer line;
            public Transform target; // null for an arc around the car
            public Color color;
        }

        Transform car;
        Vector3 offset;
        Vector3 extents;
        float radius;
        float time = 0;
        float nextJitter = 0;

        readonly List<Arc> arcs = new List<Arc>();
        Transform shockRing;
        Material shockMaterial;
        Transform flash;
        Material flashMaterial;
        Material lineMaterial;

        public void Build(ECCCar target, float shockRadius)
        {
            car = target.transform;
            radius = Mathf.Max(1, shockRadius);

            Bounds bounds = ECCPickupEffects.CarBounds(target);

            extents = bounds.extents;
            offset = bounds.center - car.position;
            transform.position = bounds.center;

            lineMaterial = ECCPickupEffects.CreateMaterial(null, Color.white);

            for (int index = 0; index < CageArcs; index++) arcs.Add(CreateArc(null, index % 3 == 0 ? ECCPickupEffects.Sparks : ECCPickupEffects.Bolt, 8, 0.14f));

            // An arc to every police car the shock knocks out ( found before the blast )
            foreach (ECCCar other in ECCCar.ActiveCars)
            {
                if (other == null || other.IsPlayer || other.health <= 0) continue;

                if (ECCRuntimeVisuals.FlatDistance(other.transform.position, car.position) <= shockRadius) arcs.Add(CreateArc(other.transform, ECCPickupEffects.Bolt, 14, 0.2f));
            }

            // The ring on the road, out to the shock's reach
            Color spark = ECCPickupEffects.Sparks;

            shockMaterial = ECCPickupEffects.CreateMaterial(ECCPickupEffects.Ring, spark);
            shockRing = ECCPickupEffects.CreateFloorQuad("ShockRing", transform, shockMaterial);
            shockRing.position = new Vector3(bounds.center.x, bounds.min.y + 0.07f, bounds.center.z);

            // A short blue-white flash around the car
            flashMaterial = ECCPickupEffects.CreateMaterial(ECCPickupEffects.SoftDot, ECCPickupEffects.Bolt);
            flash = ECCRuntimeVisuals.CreatePart(PrimitiveType.Quad, transform, Vector3.zero, Vector3.one * Mathf.Max(extents.x, extents.z) * 5, flashMaterial);

            // Sparks bursting out of the car
            ParticleSystem sparks = ECCPickupEffects.CreateParticles("Sparks", transform, ECCPickupEffects.SoftDot, 60);

            ParticleSystem.MainModule main = sparks.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4, 10);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.26f);
            main.startColor = new ParticleSystem.MinMaxGradient(spark, Color.white);
            main.gravityModifier = 0.6f;

            ParticleSystem.EmissionModule emission = sparks.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 36), new ParticleSystem.Burst(0.35f, 16) });

            ParticleSystem.ShapeModule shape = sparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Mathf.Max(extents.x, extents.z) * 0.6f;

            ECCPickupEffects.FadeOverLifetime(sparks);

            sparks.Play();

            Jitter();
        }

        Arc CreateArc(Transform target, Color color, int points, float width)
        {
            GameObject holder = new GameObject(target ? "CopArc" : "Arc");
            holder.transform.SetParent(transform, false);

            LineRenderer line = holder.AddComponent<LineRenderer>();
            line.sharedMaterial = lineMaterial;
            line.useWorldSpace = true;
            line.positionCount = points;
            line.startWidth = width;
            line.endWidth = width * 0.4f;
            line.numCapVertices = 2;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            return new Arc { line = line, target = target, color = color };
        }

        /// <summary>
        /// New jagged shapes for every arc, a few times a second ( the crackle )
        /// </summary>
        void Jitter()
        {
            Vector3 center = car ? car.position + offset : transform.position;

            foreach (Arc arc in arcs)
            {
                Vector3 from;
                Vector3 to;
                float jag;

                if (arc.target)
                {
                    from = center;
                    to = arc.target.position + Vector3.up * extents.y;
                    jag = 0.6f;
                }
                else
                {
                    // Between two points of a shell a little bigger than the car
                    from = center + Vector3.Scale(Random.onUnitSphere, extents * 1.15f);
                    to = center + Vector3.Scale(Random.onUnitSphere, extents * 1.35f);
                    jag = 0.35f;
                }

                int count = arc.line.positionCount;
                Vector3 side = Vector3.Cross(to - from, Vector3.up).normalized;

                if (side.sqrMagnitude < 0.01f) side = Vector3.right;

                for (int index = 0; index < count; index++)
                {
                    float progress = index / (float)(count - 1);

                    // Ends stay put, the middle jumps around the most
                    float bend = Mathf.Sin(progress * Mathf.PI);
                    Vector3 point = Vector3.Lerp(from, to, progress) + (side * Random.Range(-1f, 1f) + Vector3.up * Random.Range(-0.6f, 0.6f)) * jag * bend;

                    arc.line.SetPosition(index, point);
                }

                // Some arcs blink off for a moment
                arc.line.enabled = arc.target || Random.value < 0.75f;
            }
        }

        void LateUpdate()
        {
            time += Time.deltaTime;

            if (car) transform.position = car.position + offset;

            // The effect is over once the time is up ( or right away if the car is gone )
            if (time >= Duration || car == null)
            {
                Destroy(gameObject);
                return;
            }

            if (time >= nextJitter)
            {
                nextJitter = time + 0.045f;
                Jitter();
            }

            float fade = Mathf.Clamp01((Duration - time) / 0.35f);

            foreach (Arc arc in arcs)
            {
                float alpha = fade;

                // The arcs to the police only last as long as the blast
                if (arc.target) alpha = arc.target.gameObject.activeInHierarchy ? Mathf.Clamp01((CopArcTime - time) / 0.15f) : 0;

                Color core = Color.Lerp(arc.color, Color.white, 0.35f);

                arc.line.startColor = new Color(core.r, core.g, core.b, alpha);
                arc.line.endColor = new Color(arc.color.r, arc.color.g, arc.color.b, alpha * 0.7f);
            }

            // The shock ring spreads out to the reach of the shock
            float progress = Mathf.Clamp01(time / 0.5f);
            float eased = 1 - Mathf.Pow(1 - progress, 3);

            shockRing.localScale = Vector3.one * Mathf.Lerp(1, radius * 2, eased);

            Color spark = ECCPickupEffects.Sparks;
            shockMaterial.color = new Color(spark.r, spark.g, spark.b, (1 - progress) * 0.85f);

            // The flash faces the camera and fades fast
            Camera view = Camera.main;

            if (view) flash.rotation = view.transform.rotation;

            Color bolt = ECCPickupEffects.Bolt;
            flashMaterial.color = new Color(bolt.r, bolt.g, bolt.b, Mathf.Clamp01(1 - time / 0.3f) * 0.75f);
        }

        void OnDestroy()
        {
            // The arcs share one material, destroyed here with the others
            ECCPickupEffects.DestroyMaterials(gameObject);
        }
    }

    /// <summary>
    /// Dollar icons that burst out of a cash pickup and fly to the balance on screen. Each landing adds its share to the
    /// wallet ( ECCGameController.BankEarly ), which counts up and bounces the balance. One per scene, on its own canvas
    /// </summary>
    public class ECCMoneyFlight : MonoBehaviour
    {
        const float BurstTime = 0.22f;
        const float FlyTime = 0.6f;
        const float IconSize = 46;

        class Coin
        {
            public RectTransform icon;
            public Vector2 start;
            public Vector2 burst;
            public float delay;
            public float time;
            public int value;
        }

        class Label
        {
            public Text text;
            public Vector2 start;
            public float time;
        }

        static ECCMoneyFlight instance;

        Canvas canvas;
        ECCGameController gameController;
        RectTransform target;
        readonly List<Coin> coins = new List<Coin>();
        readonly List<Label> labels = new List<Label>();

        public static void Launch(ECCGameController controller, Vector3 screenPosition, RectTransform balance, int amount)
        {
            if (instance == null)
            {
                Canvas canvas = ECCRuntimeUI.CreateCanvas("CanvasMoneyFlight", 85);
                canvas.GetComponent<GraphicRaycaster>().enabled = false;

                instance = canvas.gameObject.AddComponent<ECCMoneyFlight>();
                instance.canvas = canvas;
            }

            instance.gameController = controller;
            instance.target = balance;
            instance.Add(screenPosition, amount);
        }

        void Add(Vector2 start, int amount)
        {
            // More dollars for more money, two to six of them, sharing the amount
            int count = Mathf.Clamp(amount / 10, 2, 6);
            int share = amount / count;

            for (int index = 0; index < count; index++)
            {
                Image image = ECCRuntimeUI.CreateImage("Dollar", canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(IconSize, IconSize), Color.white);
                image.sprite = ECCCoinBalanceButton.DollarSprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                image.rectTransform.position = start;
                image.rectTransform.localScale = Vector3.zero;

                float pixels = Mathf.Min(Screen.width, Screen.height) * 0.09f;

                coins.Add(new Coin
                {
                    icon = image.rectTransform,
                    start = start,
                    burst = start + Random.insideUnitCircle.normalized * Random.Range(0.5f, 1) * pixels,
                    delay = index * 0.06f,
                    value = index == count - 1 ? amount - share * (count - 1) : share,
                });
            }

            // "+50" where the money was
            Text label = ECCRuntimeUI.CreateText("Amount", canvas.transform, "+" + ECCUIKit.PersianDigits(amount), 38, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 60), new Color(0.55f, 1, 0.45f, 1));
            label.font = ECCCarTheme.TitleFont;
            label.raycastTarget = false;
            label.rectTransform.position = start;

            Outline outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.6f);
            outline.effectDistance = new Vector2(2, -2);

            labels.Add(new Label { text = label, start = start });
        }

        Vector2 TargetPoint()
        {
            if (target == null) return new Vector2(Screen.width, Screen.height);

            Canvas targetCanvas = target.GetComponentInParent<Canvas>();

            if (targetCanvas && targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay) return RectTransformUtility.WorldToScreenPoint(targetCanvas.worldCamera, target.position);

            return target.position;
        }

        void Update()
        {
            float delta = Time.unscaledDeltaTime;
            Vector2 end = TargetPoint();

            for (int index = coins.Count - 1; index >= 0; index--)
            {
                Coin coin = coins[index];

                coin.time += delta;

                float time = coin.time - coin.delay;

                if (time < 0) continue;

                if (time < BurstTime)
                {
                    // Pop out of the pickup
                    float progress = time / BurstTime;
                    float eased = 1 - Mathf.Pow(1 - progress, 3);

                    coin.icon.position = Vector2.Lerp(coin.start, coin.burst, eased);
                    coin.icon.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.15f, eased);
                }
                else
                {
                    // Then curve up into the balance, speeding up and shrinking a little
                    float progress = Mathf.Clamp01((time - BurstTime) / FlyTime);
                    float eased = progress * progress;

                    Vector2 control = new Vector2(coin.burst.x, Mathf.Max(coin.burst.y, end.y) + Screen.height * 0.05f);
                    Vector2 a = Vector2.Lerp(coin.burst, control, eased);
                    Vector2 b = Vector2.Lerp(control, end, eased);

                    coin.icon.position = Vector2.Lerp(a, b, eased);
                    coin.icon.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.7f, eased);
                    coin.icon.Rotate(0, 0, 540 * delta);

                    if (progress >= 1)
                    {
                        if (gameController) gameController.BankEarly(coin.value);

                        Destroy(coin.icon.gameObject);
                        coins.RemoveAt(index);
                    }
                }
            }

            for (int index = labels.Count - 1; index >= 0; index--)
            {
                Label label = labels[index];

                label.time += delta;

                float progress = Mathf.Clamp01(label.time / 0.9f);

                label.text.rectTransform.position = label.start + Vector2.up * Screen.height * 0.08f * (1 - Mathf.Pow(1 - progress, 2));
                label.text.rectTransform.localScale = Vector3.one * (progress < 0.15f ? Mathf.Lerp(0.5f, 1.2f, progress / 0.15f) : 1);

                Color color = label.text.color;
                color.a = progress < 0.6f ? 1 : 1 - (progress - 0.6f) / 0.4f;
                label.text.color = color;

                if (progress >= 1)
                {
                    Destroy(label.text.gameObject);
                    labels.RemoveAt(index);
                }
            }
        }

        void OnDestroy()
        {
            // Dollars still in the air when the scene ends are banked with the rest of the run
            if (instance == this) instance = null;
        }
    }
}
