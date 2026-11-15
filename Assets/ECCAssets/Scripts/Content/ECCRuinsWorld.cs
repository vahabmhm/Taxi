using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The ruins theme: the city streets after a disaster. Runs on top of ECCCityWorld ( whose buildings ECCThemes tints
    /// burnt and dusty through ECCCityWorld.buildingTint ):
    /// - a hazy orange-brown sky, warm low sun and dusty fog
    /// - burnt car wrecks on the streets ( the garage's PolygonApocalypse vehicles, stripped of their car scripts and
    ///   blackened ). They're obstacles: hitting one hurts and blows it apart, so the police can be led into them
    /// - rubble piles on the sidewalks, burning barrels and columns of smoke over the blocks
    /// Everything is kept around the player: new pieces appear out of view and far ones are removed.
    /// Added to the game controller by ECCThemes for the ruins theme.
    /// </summary>
    public class ECCRuinsWorld : MonoBehaviour
    {
        [Tooltip("Wrecks on the streets around the player")]
        public int wreckCount = 9;

        [Tooltip("Rubble piles and burning barrels around the player")]
        public int debrisCount = 12;

        [Tooltip("Columns of smoke over the blocks")]
        public int smokeCount = 4;

        [Tooltip("Distance range from the player where new pieces are placed")]
        public Vector2 placeDistance = new Vector2(35, 80);

        [Tooltip("Pieces farther than this are removed")]
        public float removeDistance = 100;

        [Tooltip("Length of a wreck ( meters )")]
        public float wreckLength = 3.8f;

        [Tooltip("Health a car loses when it hits a wreck")]
        public float wreckDamage = 1;

        static readonly Color HazeColor = new Color(0.56f, 0.43f, 0.32f);
        static readonly Color SunColor = new Color(1, 0.74f, 0.5f);
        static readonly Color AmbientColor = new Color(0.46f, 0.38f, 0.32f);

        /// <summary>
        /// The burnt and dusty color of the ruined city's buildings ( ECCCityWorld.buildingTint )
        /// </summary>
        public static readonly Color BuildingTint = new Color(0.62f, 0.55f, 0.5f);

        ECCGameController gameController;

        readonly List<GameObject> wreckTemplates = new List<GameObject>();
        readonly List<Vector3> wreckSizes = new List<Vector3>();
        readonly List<Transform> wrecks = new List<Transform>();
        readonly List<Transform> debris = new List<Transform>();
        readonly List<Transform> smokes = new List<Transform>();

        Transform templateRoot;
        Material concrete, rust, burnt;
        Material smokeMaterial, fireMaterial;
        float nextCheck = 0;
        bool filled = false;
        bool fogSet = false;

        void Start()
        {
            gameController = GetComponent<ECCGameController>();

            SetHaze();

            templateRoot = new GameObject("RuinsTemplates").transform;
            templateRoot.gameObject.SetActive(false);

            concrete = ECCRuntimeVisuals.CreateMaterial(new Color(0.6f, 0.57f, 0.53f), 0);
            rust = ECCRuntimeVisuals.CreateMaterial(new Color(0.55f, 0.27f, 0.13f), 0);
            burnt = ECCRuntimeVisuals.CreateMaterial(new Color(0.16f, 0.14f, 0.13f), 0);

            smokeMaterial = ECCRainWeather.CreateParticleMaterial(SoftDot(), gameController);
            fireMaterial = smokeMaterial;

            PrepareWrecks();
        }

        void OnDestroy()
        {
            if (templateRoot) Destroy(templateRoot.gameObject);
        }

        void SetHaze()
        {
            foreach (Light sceneLight in FindObjectsOfType<Light>())
            {
                if (sceneLight.type != LightType.Directional) continue;

                sceneLight.color = SunColor;
                sceneLight.intensity = Mathf.Min(sceneLight.intensity, 0.85f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = AmbientColor;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = HazeColor;

            Camera view = Camera.main;

            if (view)
            {
                view.clearFlags = CameraClearFlags.SolidColor;
                view.backgroundColor = HazeColor;
            }
        }

        /// <summary>
        /// Haze from a little behind the player, the far blocks fade into the dust. Set once the city has moved the
        /// camera back ( in its own Start )
        /// </summary>
        void SetFogDistance()
        {
            Camera view = Camera.main;

            float distance = view && gameController.cameraHolder ? Vector3.Distance(view.transform.position, gameController.cameraHolder.position) : 40;

            RenderSettings.fogStartDistance = distance + 15;
            RenderSettings.fogEndDistance = distance + 110;
        }

        // ----- Wreck models -----

        /// <summary>
        /// The garage's PolygonApocalypse vehicles ( SM_Veh_... ), copied without their car scripts, physics, effects
        /// and lights, and blackened
        /// </summary>
        void PrepareWrecks()
        {
            ECCShop shop = gameController ? gameController.shopMenu : null;

            if (shop == null) return;

            HashSet<Transform> used = new HashSet<Transform>();

            foreach (Types.ShopItem item in shop.items)
            {
                if (item == null || item.itemIcon == null || item.itemIcon.name.StartsWith("SM_Veh_") == false || used.Add(item.itemIcon) == false) continue;

                GameObject template = Instantiate(item.itemIcon.gameObject, templateRoot, false);
                template.name = "Wreck_" + item.itemIcon.name;

                // Scripts first ( the other components may be required by them ), then physics, effects, sound and lights
                foreach (MonoBehaviour script in template.GetComponentsInChildren<MonoBehaviour>(true)) DestroyImmediate(script);

                foreach (Component component in template.GetComponentsInChildren<Component>(true))
                {
                    if (component is Collider || component is Rigidbody || component is Animator || component is Animation || component is AudioSource || component is Light || component is TrailRenderer)
                    {
                        DestroyImmediate(component);
                    }
                }

                // Particle effects ( dust, smoke ) go with their objects
                foreach (ParticleSystem effect in template.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (effect) DestroyImmediate(effect.gameObject);
                }

                // Measure it and blacken it
                Bounds bounds = new Bounds();
                bool first = true;

                foreach (Renderer renderer in template.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] shared = renderer.sharedMaterials;

                    for (int index = 0; index < shared.Length; index++) shared[index] = BurntCopy(shared[index]);

                    renderer.sharedMaterials = shared;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                    MeshFilter filter = renderer.GetComponent<MeshFilter>();

                    if (filter == null || filter.sharedMesh == null) continue;

                    Matrix4x4 matrix = template.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                    Bounds mesh = filter.sharedMesh.bounds;

                    for (int corner = 0; corner < 8; corner++)
                    {
                        Vector3 point = matrix.MultiplyPoint3x4(new Vector3((corner & 1) == 0 ? mesh.min.x : mesh.max.x, (corner & 2) == 0 ? mesh.min.y : mesh.max.y, (corner & 4) == 0 ? mesh.min.z : mesh.max.z));

                        if (first) bounds = new Bounds(point, Vector3.zero);
                        else bounds.Encapsulate(point);

                        first = false;
                    }
                }

                if (first) continue;

                // Scaled to the wreck length, standing on the ground, centered on the holder
                float scale = wreckLength / Mathf.Max(0.01f, Mathf.Max(bounds.size.x, bounds.size.z));

                GameObject holder = new GameObject(template.name);
                holder.transform.SetParent(templateRoot, false);

                template.transform.SetParent(holder.transform, false);
                template.transform.localScale = template.transform.localScale * scale;
                template.transform.localPosition = new Vector3(-bounds.center.x * scale, -bounds.min.y * scale + 0.02f, -bounds.center.z * scale);

                wreckTemplates.Add(holder);
                wreckSizes.Add(bounds.size * scale);
            }
        }

        readonly Dictionary<Material, Material> burntCopies = new Dictionary<Material, Material>();

        Material BurntCopy(Material source)
        {
            if (source == null) return burnt;

            Material copy;

            if (burntCopies.TryGetValue(source, out copy)) return copy;

            copy = new Material(source);
            if (copy.HasProperty("_Color")) copy.color = new Color(0.3f, 0.26f, 0.24f);
            if (copy.HasProperty("_Glossiness")) copy.SetFloat("_Glossiness", 0.05f);
            if (copy.HasProperty("_EmissionColor")) copy.SetColor("_EmissionColor", Color.black);

            burntCopies[source] = copy;

            return copy;
        }

        // ----- Keeping the pieces around the player -----

        void Update()
        {
            if (gameController == null || gameController.playerObject == null || gameController.gameStarted == false) return;

            if (fogSet == false)
            {
                fogSet = true;
                SetFogDistance();
            }

            if (Time.time < nextCheck) return;

            nextCheck = Time.time + 0.5f;

            Vector3 player = gameController.playerObject.transform.position;

            // At the start the area is filled at once ( also in view, before the chase gets going )
            bool anywhere = filled == false;
            filled = true;

            RemoveFar(wrecks, player);
            RemoveFar(debris, player);
            RemoveFar(smokes, player);

            Camera view = Camera.main;

            for (int tries = 0; tries < 6 && wrecks.Count < wreckCount && wreckTemplates.Count > 0; tries++)
            {
                Vector3 position;

                if (FindSpot(player, view, anywhere, true, out position)) wrecks.Add(CreateWreck(position));
            }

            for (int tries = 0; tries < 6 && debris.Count < debrisCount; tries++)
            {
                Vector3 position;

                if (FindSpot(player, view, anywhere, false, out position)) debris.Add(Random.value < 0.35f ? CreateBarrel(position) : CreateRubble(position));
            }

            for (int tries = 0; tries < 3 && smokes.Count < smokeCount && smokeMaterial; tries++)
            {
                Vector3 position;

                if (FindBlockSpot(player, view, anywhere, out position)) smokes.Add(CreateSmoke(position));
            }
        }

        void RemoveFar(List<Transform> pieces, Vector3 player)
        {
            for (int index = pieces.Count - 1; index >= 0; index--)
            {
                if (pieces[index] == null)
                {
                    pieces.RemoveAt(index);
                }
                else if (ECCRuntimeVisuals.FlatDistance(pieces[index].position, player) > removeDistance)
                {
                    Destroy(pieces[index].gameObject);
                    pieces.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// A spot on a street ( onRoad: in the driving part, otherwise near the sidewalk ), at the place distance and out
        /// of view ( or anywhere at the start, but never right next to the player )
        /// </summary>
        bool FindSpot(Vector3 player, Camera view, bool anywhere, bool onRoad, out Vector3 position)
        {
            float spacing;

            ECCCityWorld.GetStreetSpacing(out spacing);

            float near = anywhere ? 18 : placeDistance.x;

            Vector3 direction = Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward;

            position = player + direction * Random.Range(near, placeDistance.y);

            if (spacing > 0)
            {
                // Onto the nearest street center line, then across it: within the lanes, or by the sidewalk
                float lineX = Mathf.Round(position.x / spacing) * spacing;
                float lineZ = Mathf.Round(position.z / spacing) * spacing;

                float across = onRoad ? Random.Range(-3f, 3f) : (Random.value < 0.5f ? -1 : 1) * Random.Range(4.6f, 5.4f);

                if (Mathf.Abs(position.x - lineX) < Mathf.Abs(position.z - lineZ)) position.x = lineX + across;
                else position.z = lineZ + across;
            }

            position.y = 0;

            if (ECCRuntimeVisuals.FlatDistance(position, player) < near) return false;

            if (anywhere == false && view && ECCRuntimeVisuals.IsInView(view, position)) return false;

            // Keep the pieces apart
            foreach (Transform wreck in wrecks)
            {
                if (wreck && ECCRuntimeVisuals.FlatDistance(wreck.position, position) < 9) return false;
            }

            return true;
        }

        /// <summary>
        /// A spot inside a block ( between the streets ), for the columns of smoke
        /// </summary>
        bool FindBlockSpot(Vector3 player, Camera view, bool anywhere, out Vector3 position)
        {
            float spacing;

            ECCCityWorld.GetStreetSpacing(out spacing);

            Vector3 direction = Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward;

            position = player + direction * Random.Range(anywhere ? 25 : placeDistance.x, placeDistance.y + 20);

            if (spacing > 0)
            {
                // The middle of the nearest block
                position.x = (Mathf.Floor(position.x / spacing) + 0.5f) * spacing;
                position.z = (Mathf.Floor(position.z / spacing) + 0.5f) * spacing;
            }

            position.y = 0;

            if (anywhere == false && view && ECCRuntimeVisuals.IsInView(view, position, 0.05f, 0)) return false;

            foreach (Transform smoke in smokes)
            {
                if (smoke && ECCRuntimeVisuals.FlatDistance(smoke.position, position) < 20) return false;
            }

            return true;
        }

        // ----- Pieces -----

        Transform CreateWreck(Vector3 position)
        {
            int index = Random.Range(0, wreckTemplates.Count);

            GameObject wreck = Instantiate(wreckTemplates[index]);
            wreck.name = "Wreck";
            wreck.transform.position = position;
            wreck.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            // A trigger obstacle: hitting it hurts the car and blows the wreck apart
            Vector3 size = wreckSizes[index];

            BoxCollider box = wreck.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(size.x * 0.9f, Mathf.Max(1.2f, size.y), size.z * 0.9f);
            box.center = new Vector3(0, box.size.y * 0.5f, 0);

            ECCObstacle obstacle = wreck.AddComponent<ECCObstacle>();
            obstacle.damage = wreckDamage;
            obstacle.removeOnHit = true;
            obstacle.randomRotation = 0;
            obstacle.hitEffect = gameController.playerObject ? gameController.playerObject.deathEffect : null;

            ECCCarShadow.Create(wreck.transform, new Vector2(size.x, size.z));

            // Some are still burning
            if (Random.value < 0.35f && fireMaterial) CreateFire(wreck.transform, new Vector3(0, size.y * 0.7f, 0), 0.8f);

            return wreck.transform;
        }

        Transform CreateRubble(Vector3 position)
        {
            Transform pile = new GameObject("Rubble").transform;
            pile.position = position;
            pile.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            int pieces = Random.Range(3, 6);

            for (int index = 0; index < pieces; index++)
            {
                Vector3 offset = new Vector3(Random.Range(-1.2f, 1.2f), 0, Random.Range(-1.2f, 1.2f));
                Vector3 size = new Vector3(Random.Range(0.6f, 1.6f), Random.Range(0.3f, 0.9f), Random.Range(0.6f, 1.4f));

                Transform piece = ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, pile, offset + Vector3.up * size.y * 0.4f, size, index == 0 && Random.value < 0.4f ? burnt : concrete);
                piece.localRotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
            }

            return pile;
        }

        Transform CreateBarrel(Vector3 position)
        {
            Transform barrel = new GameObject("FireBarrel").transform;
            barrel.position = position;

            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, barrel, new Vector3(0, 0.55f, 0), new Vector3(0.75f, 0.55f, 0.75f), rust);

            if (fireMaterial) CreateFire(barrel, new Vector3(0, 1.15f, 0), 0.6f);

            return barrel;
        }

        /// <summary>
        /// Flickering flames and a little smoke
        /// </summary>
        void CreateFire(Transform parent, Vector3 localPosition, float size)
        {
            ParticleSystem fire = CreateParticles("Fire", parent, localPosition, fireMaterial);

            ParticleSystem.MainModule main = fire.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f * size, 2.4f * size);
            main.startSize = new ParticleSystem.MinMaxCurve(0.7f * size, 1.3f * size);
            main.maxParticles = 40;

            ParticleSystem.EmissionModule emission = fire.emission;
            emission.rateOverTime = 28;

            ParticleSystem.ShapeModule shape = fire.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12;
            shape.radius = 0.3f * size;
            shape.rotation = new Vector3(-90, 0, 0);

            SetColorOverLifetime(fire, new Color(1, 0.85f, 0.4f, 0.95f), new Color(1, 0.35f, 0.05f, 0.8f), new Color(0.2f, 0.18f, 0.16f, 0));
            SetSizeOverLifetime(fire, 1, 0.4f);

            fire.Play();
        }

        Transform CreateSmoke(Vector3 position)
        {
            Transform root = new GameObject("SmokeColumn").transform;
            root.position = position;

            ParticleSystem smoke = CreateParticles("Smoke", root, new Vector3(0, 2, 0), smokeMaterial);

            ParticleSystem.MainModule main = smoke.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5, 7);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3, 4.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(5, 8);
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            main.maxParticles = 60;

            ParticleSystem.EmissionModule emission = smoke.emission;
            emission.rateOverTime = 8;

            ParticleSystem.ShapeModule shape = smoke.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8;
            shape.radius = 2;
            shape.rotation = new Vector3(-90, 0, 0);

            // Drifts with the wind as it rises
            ParticleSystem.VelocityOverLifetimeModule velocity = smoke.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(1.5f);
            velocity.y = new ParticleSystem.MinMaxCurve(0);
            velocity.z = new ParticleSystem.MinMaxCurve(0.6f);

            SetColorOverLifetime(smoke, new Color(0.25f, 0.2f, 0.17f, 0.75f), new Color(0.35f, 0.3f, 0.27f, 0.5f), new Color(0.5f, 0.45f, 0.4f, 0));
            SetSizeOverLifetime(smoke, 0.6f, 2.2f);

            smoke.Play();

            // A glow of fire at its foot
            if (fireMaterial) CreateFire(root, new Vector3(0, 0.6f, 0), 2.2f);

            return root;
        }

        static ParticleSystem CreateParticles(string name, Transform parent, Vector3 localPosition, Material material)
        {
            GameObject particles = new GameObject(name);
            particles.transform.SetParent(parent, false);
            particles.transform.localPosition = localPosition;

            ParticleSystem system = particles.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return system;
        }

        static void SetColorOverLifetime(ParticleSystem system, Color start, Color middle, Color end)
        {
            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(start, 0), new GradientColorKey(middle, 0.4f), new GradientColorKey(end, 1) },
                new[] { new GradientAlphaKey(start.a, 0), new GradientAlphaKey(middle.a, 0.4f), new GradientAlphaKey(end.a, 1) });

            color.color = gradient;
        }

        static void SetSizeOverLifetime(ParticleSystem system, float start, float end)
        {
            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, start, 1, end));
        }

        /// <summary>
        /// A soft round puff, for the flames and the smoke
        /// </summary>
        static Texture2D SoftDot()
        {
            const int size = 64;

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
    }
}
