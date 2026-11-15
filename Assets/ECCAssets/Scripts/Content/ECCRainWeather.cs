using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The rainy night road: the city streets at night in heavy rain.
    /// - night: a dim blue moon light, dark blue ambient light and fog, a dark sky
    /// - rain streaks falling around the camera and small splashes on the street, both following the chase
    /// - lightning now and then ( a short double flash of the whole scene )
    /// - headlights on the player's car, and red / blue flashing lights on the nearest police cars
    /// The wet road ( cars slide ) is ECCCar.grip, set by ECCThemes.
    /// Added to the game controller by ECCThemes for the rain theme.
    /// </summary>
    public class ECCRainWeather : MonoBehaviour
    {
        [Tooltip("Rain drops per second")]
        public float rainRate = 900;

        [Tooltip("Size of the rainy area around the player ( meters )")]
        public float rainArea = 70;

        [Tooltip("Seconds between lightning flashes ( random in this range )")]
        public Vector2 lightningInterval = new Vector2(7, 15);

        [Tooltip("The most police cars with flashing lights at once ( realtime lights cost on mobile )")]
        public int maxSirenLights = 3;

        static readonly Color MoonColor = new Color(0.55f, 0.63f, 0.9f);
        static readonly Color NightAmbient = new Color(0.2f, 0.23f, 0.34f);
        static readonly Color NightFog = new Color(0.1f, 0.12f, 0.18f);

        ECCGameController gameController;
        readonly List<Light> moonLights = new List<Light>();
        readonly List<float> moonIntensities = new List<float>();
        readonly List<ECCSirenLight> sirens = new List<ECCSirenLight>();

        Transform rainRoot;
        Light headlight;
        float nextLightning;
        float flash = 0;

        void Start()
        {
            gameController = GetComponent<ECCGameController>();

            SetNight();

            Transform follow = gameController && gameController.cameraHolder ? gameController.cameraHolder : (Camera.main ? Camera.main.transform : transform);

            rainRoot = new GameObject("Rain").transform;
            rainRoot.SetParent(follow, false);

            Material material = CreateParticleMaterial();

            if (material)
            {
                CreateRain(material);
                CreateSplashes(material);
            }

            nextLightning = Time.time + Random.Range(lightningInterval.x, lightningInterval.y);

            ECCSpawnAroundObject.ObjectSpawned += OnObjectSpawned;

            // The city moves the camera back in its own Start, so the fog is set a frame later
            StartCoroutine(SetFogDistance());
        }

        void OnDestroy()
        {
            ECCSpawnAroundObject.ObjectSpawned -= OnObjectSpawned;
        }

        void SetNight()
        {
            foreach (Light sceneLight in FindObjectsOfType<Light>())
            {
                if (sceneLight.type != LightType.Directional) continue;

                sceneLight.color = MoonColor;
                sceneLight.intensity = Mathf.Min(sceneLight.intensity, 0.45f);
                sceneLight.shadowStrength = 0.4f;

                moonLights.Add(sceneLight);
                moonIntensities.Add(sceneLight.intensity);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = NightAmbient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = NightFog;

            Camera view = Camera.main;

            if (view)
            {
                view.clearFlags = CameraClearFlags.SolidColor;
                view.backgroundColor = NightFog;
            }
        }

        IEnumerator SetFogDistance()
        {
            yield return null;

            Camera view = Camera.main;

            // The fog starts a little behind the player, so the chase stays clear and the far streets fade into the dark
            float cameraDistance = view && gameController && gameController.cameraHolder ? Vector3.Distance(view.transform.position, gameController.cameraHolder.position) : 30;

            RenderSettings.fogStartDistance = cameraDistance + 10;
            RenderSettings.fogEndDistance = cameraDistance + 75;
        }

        // ----- Rain -----

        /// <summary>
        /// A soft white streak, for the rain drops and the splashes
        /// </summary>
        Material CreateParticleMaterial()
        {
            const int width = 8, height = 32;

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float across = 1 - Mathf.Abs((x + 0.5f) / width * 2 - 1);
                    float along = Mathf.Sin((y + 0.5f) / height * Mathf.PI);

                    texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(across * 1.4f) * along));
                }
            }

            texture.Apply();

            return CreateParticleMaterial(texture, gameController);
        }

        /// <summary>
        /// An alpha blended particle material with a texture. The particle shader comes from the game's own effects
        /// ( the player car's explosion as a fallback ), so it's in the build. Null if no particle shader is found
        /// </summary>
        public static Material CreateParticleMaterial(Texture2D texture, ECCGameController gameController)
        {
            Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

            if (shader == null && gameController && gameController.playerObject && gameController.playerObject.deathEffect)
            {
                ParticleSystemRenderer effect = gameController.playerObject.deathEffect.GetComponentInChildren<ParticleSystemRenderer>(true);

                if (effect && effect.sharedMaterial) shader = effect.sharedMaterial.shader;
            }

            if (shader == null) return null;

            Material material = new Material(shader);
            material.mainTexture = texture;

            if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));

            return material;
        }

        void CreateRain(Material material)
        {
            ParticleSystem rain = CreateParticles("Drops", material);

            ParticleSystem.MainModule main = rain.main;
            main.startLifetime = 1.1f;
            main.startSpeed = 0;
            main.startSize = 0.07f;
            main.startColor = new Color(0.78f, 0.84f, 1, 0.55f);
            main.maxParticles = Mathf.CeilToInt(rainRate * 1.3f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = rain.emission;
            emission.rateOverTime = rainRate;

            ParticleSystem.ShapeModule shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(rainArea, 1, rainArea);
            shape.position = new Vector3(0, 26, 0);

            ParticleSystem.VelocityOverLifetimeModule velocity = rain.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-3);
            velocity.y = new ParticleSystem.MinMaxCurve(-26);
            velocity.z = new ParticleSystem.MinMaxCurve(-1);

            ParticleSystemRenderer renderer = rain.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.07f;
            renderer.lengthScale = 1;

            rain.Play();
        }

        void CreateSplashes(Material material)
        {
            ParticleSystem splashes = CreateParticles("Splashes", material);

            ParticleSystem.MainModule main = splashes.main;
            main.startLifetime = 0.28f;
            main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startColor = new Color(0.8f, 0.86f, 1, 0.45f);
            main.maxParticles = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = splashes.emission;
            emission.rateOverTime = rainRate * 0.3f;

            ParticleSystem.ShapeModule shape = splashes.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(rainArea * 0.6f, 0.1f, rainArea * 0.6f);
            shape.position = new Vector3(0, 0.15f, 0);

            // Small rings that grow and fade
            ParticleSystem.SizeOverLifetimeModule size = splashes.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0.4f, 1, 1.4f));

            ParticleSystem.ColorOverLifetimeModule color = splashes.colorOverLifetime;
            color.enabled = true;

            Gradient fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0, 1) });
            color.color = fade;

            ParticleSystemRenderer renderer = splashes.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;

            splashes.Play();
        }

        ParticleSystem CreateParticles(string name, Material material)
        {
            GameObject particles = new GameObject(name);
            particles.transform.SetParent(rainRoot, false);

            ParticleSystem system = particles.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return system;
        }

        // ----- Lights -----

        void Update()
        {
            if (gameController == null) return;

            // Headlights on the player's car, once it's in the scene
            if (headlight == null && gameController.gameStarted && gameController.playerObject && gameController.playerObject.gameObject.scene.IsValid())
            {
                headlight = CreateHeadlight(gameController.playerObject.transform);
            }

            UpdateLightning();
        }

        Light CreateHeadlight(Transform car)
        {
            Light light = new GameObject("Headlights").AddComponent<Light>();
            light.transform.SetParent(car, false);
            light.transform.localPosition = new Vector3(0, 2.2f, 0.5f);
            light.transform.localRotation = Quaternion.Euler(18, 0, 0);
            light.type = LightType.Spot;
            light.spotAngle = 75;
            light.range = 32;
            light.intensity = 3.2f;
            light.color = new Color(1, 0.95f, 0.82f);
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;

            return light;
        }

        void UpdateLightning()
        {
            if (Time.time >= nextLightning)
            {
                nextLightning = Time.time + Random.Range(lightningInterval.x, lightningInterval.y);
                flash = 1;
            }

            if (flash <= 0) return;

            flash = Mathf.Max(0, flash - Time.deltaTime * 2.2f);

            // Two quick flashes, then darkness again
            float elapsed = 1 - flash;
            float strength = elapsed < 0.08f || (elapsed > 0.16f && elapsed < 0.26f) ? 1 : 0;

            for (int index = 0; index < moonLights.Count; index++)
            {
                if (moonLights[index]) moonLights[index].intensity = moonIntensities[index] + strength * 1.6f;
            }

            RenderSettings.ambientLight = Color.Lerp(NightAmbient, new Color(0.7f, 0.75f, 0.9f), strength * 0.6f);
        }

        /// <summary>
        /// Police cars get flashing red / blue lights, a few at a time ( the oldest ones give theirs up )
        /// </summary>
        void OnObjectSpawned(Transform spawned)
        {
            if (spawned == null || spawned.GetComponent<ECCCar>() == null) return;

            sirens.RemoveAll(siren => siren == null);

            if (sirens.Count >= maxSirenLights)
            {
                Destroy(sirens[0].gameObject);
                sirens.RemoveAt(0);
            }

            ECCSirenLight light = new GameObject("SirenLight").AddComponent<ECCSirenLight>();
            light.transform.SetParent(spawned, false);
            light.transform.localPosition = new Vector3(0, 3.2f, 0);

            sirens.Add(light);
        }
    }

    /// <summary>
    /// A point light over a police car that flashes red and blue
    /// </summary>
    public class ECCSirenLight : MonoBehaviour
    {
        Light siren;
        float offset;

        void Start()
        {
            siren = gameObject.AddComponent<Light>();
            siren.type = LightType.Point;
            siren.range = 9;
            siren.intensity = 2.5f;
            siren.shadows = LightShadows.None;

            offset = Random.value;
        }

        void Update()
        {
            if (siren == null) return;

            bool red = Mathf.Repeat(Time.time * 2.5f + offset, 1) < 0.5f;

            siren.color = red ? new Color(1, 0.15f, 0.15f) : new Color(0.2f, 0.4f, 1);
        }
    }
}
