using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The night theme: a dark blue sky and fog, a dim moon, the player's real headlights, glowing headlight beams on the
    /// road in front of every car ( police and traffic ) and the police cars' red / blue siren glow.
    /// Only the player's headlights and a few police sirens are realtime lights ( they cost on mobile ); the rest are soft
    /// see-through glows ( ECCGlow ).
    /// Levels marked night use it; the endless mode is at night when the phone's clock says it's night ( 19:00 to 6:00 ).
    /// </summary>
    public class ECCNightMode : MonoBehaviour
    {
        [Tooltip("The most police cars with a realtime flashing light at once")]
        public int maxSirenLights = 3;

        static readonly Color MoonColor = new Color(0.5f, 0.6f, 0.95f);
        static readonly Color NightAmbient = new Color(0.17f, 0.19f, 0.3f);
        static readonly Color NightSky = new Color(0.04f, 0.05f, 0.11f);
        static readonly Color HeadlightColor = new Color(1, 0.95f, 0.78f, 0.55f);
        static readonly Color TailColor = new Color(1, 0.1f, 0.1f, 0.5f);

        static ECCNightMode active;

        /// <summary>
        /// True while the current run is at night
        /// </summary>
        public static bool IsNight
        {
            get { return active != null; }
        }

        /// <summary>
        /// True if the phone's clock says it's night ( the endless mode follows it )
        /// </summary>
        public static bool IsNightTime
        {
            get
            {
                int hour = System.DateTime.Now.Hour;

                return hour >= 19 || hour < 6;
            }
        }

        public static ECCNightMode Enable(ECCGameController gameController)
        {
            if (gameController == null) return null;

            ECCNightMode night = gameController.GetComponent<ECCNightMode>();

            if (night == null) night = gameController.gameObject.AddComponent<ECCNightMode>();

            return night;
        }

        ECCGameController gameController;
        Light headlight;
        readonly HashSet<Transform> litCars = new HashSet<Transform>();
        readonly List<ECCSirenLight> sirens = new List<ECCSirenLight>();
        float scanTimer = 0;

        void Awake()
        {
            active = this;
        }

        void Start()
        {
            gameController = GetComponent<ECCGameController>();

            SetNight();

            StartCoroutine(SetFogDistance());
        }

        void OnDestroy()
        {
            if (active == this) active = null;
        }

        void SetNight()
        {
            foreach (Light sceneLight in FindObjectsOfType<Light>())
            {
                if (sceneLight.type != LightType.Directional) continue;

                sceneLight.color = MoonColor;
                sceneLight.intensity = Mathf.Min(sceneLight.intensity, 0.35f);
                sceneLight.shadowStrength = 0.3f;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = NightAmbient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = NightSky;

            Camera view = Camera.main;

            if (view)
            {
                view.clearFlags = CameraClearFlags.SolidColor;
                view.backgroundColor = NightSky;
            }
        }

        IEnumerator SetFogDistance()
        {
            // The camera distance is set when the chase starts
            yield return null;

            Camera view = Camera.main;

            float cameraDistance = view && gameController && gameController.cameraHolder ? Vector3.Distance(view.transform.position, gameController.cameraHolder.position) : 30;

            RenderSettings.fogStartDistance = cameraDistance + 15;
            RenderSettings.fogEndDistance = cameraDistance + 110;
        }

        void Update()
        {
            if (gameController == null) return;

            if (headlight == null && gameController.gameStarted && gameController.playerObject && gameController.playerObject.gameObject.scene.IsValid())
            {
                headlight = CreateHeadlight(gameController.playerObject.transform);
            }

            // New cars get their lights a few times a second ( the police, the boss, roadblocks and the traffic )
            scanTimer -= Time.deltaTime;

            if (scanTimer > 0) return;

            scanTimer = 0.3f;

            for (int index = 0; index < ECCCar.ActiveCars.Count; index++)
            {
                ECCCar car = ECCCar.ActiveCars[index];

                if (car == null || litCars.Contains(car.transform) || car.gameObject.scene.IsValid() == false) continue;

                litCars.Add(car.transform);

                bool police = car.IsPlayer == false;

                AddCarLights(car.transform, CarLength(car), police);

                if (police) AddSiren(car.transform);
            }

            ECCTraffic traffic = ECCTraffic.Instance;

            for (int index = 0; traffic && index < traffic.CarCount; index++)
            {
                Transform car = traffic.GetCar(index);

                if (car == null || litCars.Contains(car)) continue;

                litCars.Add(car);

                AddCarLights(car, 5, false);
            }

            litCars.RemoveWhere(car => car == null);
        }

        static float CarLength(ECCCar car)
        {
            Collider body = car.GetComponent<Collider>();

            if (body) return Mathf.Clamp(Mathf.Max(body.bounds.size.x, body.bounds.size.z), 3, 12);

            return 5;
        }

        Light CreateHeadlight(Transform car)
        {
            Light light = new GameObject("NightHeadlights").AddComponent<Light>();
            light.transform.SetParent(car, false);
            light.transform.localPosition = new Vector3(0, 2.2f, 0.5f);
            light.transform.localRotation = Quaternion.Euler(18, 0, 0);
            light.type = LightType.Spot;
            light.spotAngle = 80;
            light.range = 34;
            light.intensity = 3.5f;
            light.color = new Color(1, 0.95f, 0.82f);
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;

            return light;
        }

        /// <summary>
        /// Two glowing headlight beams on the road ahead of the car, a red glow behind it and, for police cars, a flashing
        /// red / blue glow around it. The holder cancels the car's scale, so the sizes are in meters
        /// </summary>
        public static void AddCarLights(Transform car, float length, bool police)
        {
            Transform holder = new GameObject("NightLights").transform;
            holder.SetParent(car, false);

            Vector3 scale = car.lossyScale;
            holder.localScale = new Vector3(1 / Mathf.Max(0.01f, scale.x), 1 / Mathf.Max(0.01f, scale.y), 1 / Mathf.Max(0.01f, scale.z));

            float beamLength = 16;
            float front = length * 0.5f;

            Material beam = ECCGlow.BeamMaterial(HeadlightColor);

            for (int side = -1; side <= 1; side += 2)
            {
                Transform quad = ECCGlow.CreateFlat("Beam", holder, new Vector3(side * length * 0.16f, 0.08f, front + beamLength * 0.5f - 0.6f), new Vector2(length * 0.75f, beamLength), beam);

                if (quad) quad.localRotation = Quaternion.Euler(90, side * 4, 0);
            }

            ECCGlow.CreateFlat("Tail", holder, new Vector3(0, 0.07f, -front - 0.4f), new Vector2(length * 0.6f, 2.2f), ECCGlow.RadialMaterial(TailColor));

            if (police)
            {
                Transform red = ECCGlow.CreateFlat("SirenRed", holder, new Vector3(-length * 0.25f, 0.09f, 0), new Vector2(length * 1.3f, length * 1.3f), ECCGlow.RadialMaterial(new Color(1, 0.1f, 0.1f, 0.6f)));
                Transform blue = ECCGlow.CreateFlat("SirenBlue", holder, new Vector3(length * 0.25f, 0.09f, 0), new Vector2(length * 1.3f, length * 1.3f), ECCGlow.RadialMaterial(new Color(0.15f, 0.35f, 1, 0.6f)));

                if (red && blue)
                {
                    ECCSirenGlow flash = holder.gameObject.AddComponent<ECCSirenGlow>();
                    flash.red = red.gameObject;
                    flash.blue = blue.gameObject;
                }
            }
        }

        /// <summary>
        /// A realtime flashing light over a few police cars ( the oldest gives its light to a new one )
        /// </summary>
        void AddSiren(Transform car)
        {
            sirens.RemoveAll(siren => siren == null);

            if (sirens.Count >= maxSirenLights)
            {
                Destroy(sirens[0].gameObject);
                sirens.RemoveAt(0);
            }

            ECCSirenLight light = new GameObject("SirenLight").AddComponent<ECCSirenLight>();
            light.transform.SetParent(car, false);
            light.transform.localPosition = new Vector3(0, 3.2f, 0);

            sirens.Add(light);
        }
    }

    /// <summary>
    /// Switches between the red and the blue siren glows
    /// </summary>
    public class ECCSirenGlow : MonoBehaviour
    {
        public GameObject red;
        public GameObject blue;

        float offset;

        void Start()
        {
            offset = Random.value;
        }

        void Update()
        {
            bool redOn = Mathf.Repeat(Time.time * 2.5f + offset, 1) < 0.5f;

            if (red && red.activeSelf != redOn) red.SetActive(redOn);
            if (blue && blue.activeSelf == redOn) blue.SetActive(redOn == false);
        }
    }
}
