using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// Ordinary traffic ( Kenney Car Kit cars, CC0 ): civilian cars that drive by during the chase.
    /// - in the city they drive in the right lane of a street, straight on; on the open ground in straight lines
    /// - they appear out of view, ahead of or beside the player, and are removed when far away
    /// - crashing into one hurts the car that hit it ( the player or a police car ) and the traffic car explodes,
    ///   so they are obstacles to dodge, and a way to shake off the police
    /// Added to the game controller when the chase starts.
    /// </summary>
    public class ECCTraffic : MonoBehaviour
    {
        [Tooltip("The most traffic cars at the same time")]
        public int maxCars = 20;

        [Tooltip("Seconds between two new traffic cars")]
        public float spawnInterval = 0.5f;

        [Tooltip("Distance from the player at which traffic cars appear ( out of view )")]
        public Vector2 spawnDistance = new Vector2(55, 75);

        [Tooltip("Traffic cars farther than this from the player are removed")]
        public float removeDistance = 110;

        [Tooltip("Speed range of the traffic cars")]
        public Vector2 speedRange = new Vector2(6, 10);

        [Tooltip("On the highway: the most traffic cars, seconds between them and their speed range")]
        public int highwayMaxCars = 12;

        public float highwaySpawnInterval = 0.8f;

        public Vector2 highwaySpeedRange = new Vector2(9, 15);

        [Tooltip("Health a car loses when it crashes into a traffic car. Police cars lose double")]
        public float crashDamage = 1;

        [Tooltip("Length of a traffic car before the chase car size is applied")]
        public float carLength = 2.1f;

        [Tooltip("How far from the street's center line the lanes are")]
        public float laneOffset = 3;

        const string Root = "KenneyCity/Traffic/";

        static readonly string[] Models = { "sedan", "suv", "van", "truck", "delivery", "hatchback-sports", "garbage-truck", "ambulance" };

        class TrafficCar
        {
            public Transform transform;
            public float speed;
            public float cruiseSpeed;
            public float radius;
            public float honkTime;
        }

        [Header("Taxi")]
        [Tooltip("How quickly the traffic slows down for a red light or a car ahead")]
        public float brakeRate = 14;

        [Tooltip("How quickly the traffic speeds up again")]
        public float accelerateRate = 5;

        [Tooltip("The gap the traffic keeps to the car ahead")]
        public float followGap = 2.5f;

        [Tooltip("A player car slower than this only bumps a traffic car, it doesn't crash")]
        public float softBumpSpeed = 4.5f;

        /// <summary>
        /// Called when the player's car crashes into a traffic car ( a violation, see TaxiPolice )
        /// </summary>
        public static event System.Action<ECCCar> PlayerCrashed;

        /// <summary>
        /// The traffic of the current run, or null
        /// </summary>
        public static ECCTraffic Instance { get; private set; }

        /// <summary>
        /// How many traffic cars are on the road
        /// </summary>
        public int CarCount
        {
            get { return cars.Count; }
        }

        /// <summary>
        /// One traffic car ( may be null for a moment after it was destroyed )
        /// </summary>
        public Transform GetCar(int index)
        {
            return index >= 0 && index < cars.Count ? cars[index].transform : null;
        }

        ECCGameController gameController;
        readonly List<TrafficCar> cars = new List<TrafficCar>();
        readonly Dictionary<string, GameObject> templates = new Dictionary<string, GameObject>();
        readonly Dictionary<string, Bounds> bounds = new Dictionary<string, Bounds>();

        Material material;
        Transform templateRoot;
        float spawnTimer = 2;

        void Start()
        {
            gameController = GetComponent<ECCGameController>();

            Instance = this;

            templateRoot = new GameObject("TrafficTemplates").transform;
            templateRoot.gameObject.SetActive(false);
        }

        bool IsPlaying
        {
            get { return gameController && gameController.gameStarted && gameController.isGameOver == false && gameController.playerObject && gameController.playerObject.health > 0; }
        }

        void Update()
        {
            if (gameController == null) return;

            Vector3 player = gameController.playerObject ? gameController.playerObject.transform.position : Vector3.zero;

            // Drive, and remove the far ones
            for (int index = cars.Count - 1; index >= 0; index--)
            {
                TrafficCar car = cars[index];

                if (car.transform == null)
                {
                    cars.RemoveAt(index);
                    continue;
                }

                // Stop at red lights and behind the car ahead, then speed up again
                float wanted = MustWait(car, index) ? 0 : car.cruiseSpeed;

                car.speed = Mathf.MoveTowards(car.speed, wanted, (wanted < car.speed ? brakeRate : accelerateRate) * Time.deltaTime);

                car.transform.position += car.transform.forward * car.speed * Time.deltaTime;

                if (gameController.playerObject && ECCRuntimeVisuals.FlatDistance(car.transform.position, player) > removeDistance)
                {
                    Destroy(car.transform.gameObject);
                    cars.RemoveAt(index);
                }
            }

            if (IsPlaying == false) return;

            CheckCrashes();

            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0)
            {
                bool highway = ECCHighwayWorld.IsActive;

                spawnTimer = (highway ? highwaySpawnInterval : spawnInterval) * Random.Range(0.7f, 1.3f);

                if (cars.Count < (highway ? highwayMaxCars : maxCars)) Spawn(gameController.playerObject.transform);
            }
        }

        // ----- Lights and queues -----

        /// <summary>
        /// Should this traffic car stop: a red or yellow light just ahead, or a car ( traffic, taxi, police ) close
        /// ahead in its lane
        /// </summary>
        bool MustWait(TrafficCar car, int self)
        {
            Vector3 position = car.transform.position;
            Vector3 forward = car.transform.forward;

            // The distance it needs to stop, plus a little
            float stopDistance = car.speed * car.speed / (2 * brakeRate) + car.radius + 1;

            float lineDistance;

            if (ECCHighwayWorld.IsActive == false && TaxiTrafficLights.Instance && TaxiTrafficLights.Instance.MustStopAhead(position + forward * car.radius, forward, Mathf.Max(3, stopDistance), out lineDistance)) return true;

            Vector3 right = TaxiCity.Right(forward);

            for (int index = 0; index < cars.Count; index++)
            {
                if (index == self || cars[index].transform == null) continue;

                if (IsAhead(position, forward, right, cars[index].transform.position, car.radius + cars[index].radius + followGap + stopDistance * 0.5f)) return true;
            }

            for (int index = 0; index < ECCCar.ActiveCars.Count; index++)
            {
                ECCCar other = ECCCar.ActiveCars[index];

                if (other == null || other.health <= 0) continue;

                if (IsAhead(position, forward, right, other.transform.position, car.radius + CarRadius(other) + followGap + stopDistance * 0.5f))
                {
                    // Honk at a taxi that blocks the street
                    if (other.IsPlayer && car.honkTime <= Time.time)
                    {
                        car.honkTime = Time.time + 6;

                        TaxiAudio.Honk(car.transform.position, 0.35f);
                    }

                    return true;
                }
            }

            return false;
        }

        static bool IsAhead(Vector3 position, Vector3 forward, Vector3 right, Vector3 other, float gap)
        {
            Vector3 offset = other - position;
            offset.y = 0;

            float ahead = Vector3.Dot(offset, forward);

            return ahead > 0 && ahead < gap && Mathf.Abs(Vector3.Dot(offset, right)) < 1.8f;
        }

        // ----- Crashes -----

        void CheckCrashes()
        {
            for (int index = cars.Count - 1; index >= 0; index--)
            {
                TrafficCar traffic = cars[index];

                for (int other = ECCCar.ActiveCars.Count - 1; other >= 0; other--)
                {
                    ECCCar car = ECCCar.ActiveCars[other];

                    if (car == null || car.health <= 0) continue;

                    // Cars are about as wide as half their length: a circle a bit smaller than both cars together
                    float reach = traffic.radius + CarRadius(car);

                    if (ECCRuntimeVisuals.FlatDistance(traffic.transform.position, car.transform.position) > reach) continue;

                    // A slow car only bumps it: both are pushed apart, nothing breaks
                    if (car.CurrentSpeed < softBumpSpeed)
                    {
                        Bump(traffic, car, reach);
                        continue;
                    }

                    Crash(traffic, car);
                    cars.RemoveAt(index);

                    break;
                }
            }
        }

        static float CarRadius(ECCCar car)
        {
            BoxCollider box = car.GetComponent<BoxCollider>();

            return box ? Mathf.Max(box.size.x, box.size.z) * 0.4f : 1.2f;
        }

        void Bump(TrafficCar traffic, ECCCar car, float reach)
        {
            Vector3 away = car.transform.position - traffic.transform.position;
            away.y = 0;

            if (away.sqrMagnitude < 0.0001f) away = -car.transform.forward;

            float overlap = reach - away.magnitude;

            if (overlap > 0) car.transform.position += away.normalized * overlap;
        }

        void Crash(TrafficCar traffic, ECCCar car)
        {
            if (car.IsPlayer && PlayerCrashed != null) PlayerCrashed(car);

            Vector3 position = traffic.transform.position;

            // The traffic car explodes, with the explosion of the car that hit it
            if (car.deathEffect) Instantiate(car.deathEffect, position, traffic.transform.rotation);

            Destroy(traffic.transform.gameObject);

            if (car.IsPlayer && car.invulnerableTime <= 0) ECCRunMessages.Crash();

            // The police lose more: running them into the traffic is a way to escape
            car.ChangeHealth(-crashDamage * (car.IsPlayer ? 1 : 2));
        }

        // ----- Spawning -----

        void Spawn(Transform player)
        {
            Vector3 position;
            Vector3 direction;

            float spacing;

            if (ECCHighwayWorld.IsActive) PlaceOnHighway(player, out position, out direction);
            else if (ECCCityWorld.GetStreetSpacing(out spacing)) PlaceOnStreet(player, spacing, out position, out direction);
            else PlaceOnGround(player, out position, out direction);

            string model = Models[Random.Range(0, Models.Length)];

            GameObject template = GetTemplate(model);

            if (template == null) return;

            // A plain root that drives; the model inside it is scaled and stands on the ground
            GameObject root = new GameObject("Traffic_" + model);
            root.transform.position = new Vector3(position.x, 0, position.z);
            root.transform.rotation = Quaternion.LookRotation(direction);

            Bounds size = bounds[model];

            float scale = carLength * Mathf.Max(1, gameController.carSize) / Mathf.Max(0.01f, size.size.z);

            GameObject body = Instantiate(template, root.transform, false);
            body.transform.localScale = Vector3.one * scale;
            body.transform.localPosition = new Vector3(-size.center.x * scale, -size.min.y * scale + 0.01f, -size.center.z * scale);

            Vector2 footprint = new Vector2(size.size.x, size.size.z) * scale;

            ECCCarShadow.Create(root.transform, footprint);

            float cruise = ECCHighwayWorld.IsActive ? Random.Range(highwaySpeedRange.x, highwaySpeedRange.y) : Random.Range(speedRange.x, speedRange.y);

            cars.Add(new TrafficCar
            {
                transform = root.transform,
                speed = cruise,
                cruiseSpeed = cruise,
                radius = Mathf.Max(footprint.x, footprint.y) * 0.4f,
            });
        }

        /// <summary>
        /// On the highway: in a lane of either direction, ahead of or behind the player and out of view. Most come from
        /// ahead ( the oncoming ones, and the slower ones the player catches up with )
        /// </summary>
        void PlaceOnHighway(Transform player, out Vector3 position, out Vector3 direction)
        {
            ECCHighwayWorld highway = ECCHighwayWorld.Active;

            bool towardsPlusZ = Random.value < 0.5f;
            int lane = Random.Range(0, highway.lanesPerSide);

            direction = towardsPlusZ ? Vector3.forward : Vector3.back;

            float ahead = Random.value < 0.75f ? 1 : -1;
            float distance = Random.Range(spawnDistance.x, spawnDistance.y);

            position = new Vector3(highway.LaneCenter(lane, towardsPlusZ), 0, player.position.z + ahead * distance);
        }

        /// <summary>
        /// In the right lane of a street near the player, somewhere out of view
        /// </summary>
        void PlaceOnStreet(Transform player, float spacing, out Vector3 position, out Vector3 direction)
        {
            Vector3 center = player.position;

            bool alongX = Random.value < 0.5f;
            int line = Random.Range(-1, 2);
            float sign = Random.value < 0.5f ? 1 : -1;
            float distance = Random.Range(spawnDistance.x, spawnDistance.y);

            if (alongX)
            {
                // A street running along X, at a constant z
                float streetZ = (Mathf.Round(center.z / spacing) + line) * spacing;

                direction = new Vector3(sign, 0, 0);

                // Right-hand traffic: driving to +X, the right side is -Z
                position = new Vector3(center.x - sign * distance, 0, streetZ - sign * laneOffset);
            }
            else
            {
                float streetX = (Mathf.Round(center.x / spacing) + line) * spacing;

                direction = new Vector3(0, 0, sign);

                // Driving to +Z, the right side is +X
                position = new Vector3(streetX + sign * laneOffset, 0, center.z - sign * distance);
            }

            // Half of them come towards the player instead
            if (Random.value < 0.5f)
            {
                position += direction * distance * 2;
                direction = -direction;
            }

            position = KeepRightLane(position, direction, spacing, alongX);
        }

        /// <summary>
        /// Moves a traffic car into the right lane of its street for its direction
        /// </summary>
        Vector3 KeepRightLane(Vector3 position, Vector3 direction, float spacing, bool alongX)
        {
            if (alongX)
            {
                float streetZ = Mathf.Round(position.z / spacing) * spacing;
                position.z = streetZ - Mathf.Sign(direction.x) * laneOffset;
            }
            else
            {
                float streetX = Mathf.Round(position.x / spacing) * spacing;
                position.x = streetX + Mathf.Sign(direction.z) * laneOffset;
            }

            return position;
        }

        /// <summary>
        /// On the open ground: a straight line that crosses the area around the player
        /// </summary>
        void PlaceOnGround(Transform player, out Vector3 position, out Vector3 direction)
        {
            float angle = Random.Range(0f, 360f);

            Vector3 away = Quaternion.Euler(0, angle, 0) * Vector3.forward;

            position = player.position + away * Random.Range(spawnDistance.x, spawnDistance.y);

            // Head back across the player's area, a little to the side
            direction = Quaternion.Euler(0, Random.Range(-25f, 25f), 0) * -away;
        }

        // ----- Models -----

        GameObject GetTemplate(string model)
        {
            GameObject template;

            if (templates.TryGetValue(model, out template)) return template;

            GameObject source = Resources.Load<GameObject>(Root + model);

            if (source == null)
            {
                templates[model] = null;
                return null;
            }

            // A plain holder with the model inside ( it keeps its own import rotation and scale ), the kit's material
            template = new GameObject(model);
            template.transform.SetParent(templateRoot, false);

            GameObject inner = Instantiate(source, template.transform, false);

            // The car must face +Z ( its driving direction ): turn it if its front wheels are on the -Z side
            float front = 0, back = 0;

            foreach (Renderer wheel in inner.GetComponentsInChildren<Renderer>(true))
            {
                string name = wheel.name.ToLowerInvariant();

                MeshFilter wheelMesh = wheel.GetComponent<MeshFilter>();

                if (name.Contains("wheel") == false || wheelMesh == null || wheelMesh.sharedMesh == null) continue;

                // Mesh bounds ( a renderer's bounds are empty while the template is inactive )
                float z = (template.transform.worldToLocalMatrix * wheel.transform.localToWorldMatrix).MultiplyPoint3x4(wheelMesh.sharedMesh.bounds.center).z;

                if (name.Contains("front")) front += z;
                else if (name.Contains("back")) back += z;
            }

            if (front < back) inner.transform.localRotation = Quaternion.Euler(0, 180, 0) * inner.transform.localRotation;

            Bounds measured = new Bounds();
            bool first = true;

            foreach (Renderer renderer in template.GetComponentsInChildren<Renderer>(true))
            {
                Material[] shared = new Material[Mathf.Max(1, renderer.sharedMaterials.Length)];

                for (int index = 0; index < shared.Length; index++) shared[index] = Material;

                renderer.sharedMaterials = shared;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();

                if (filter == null || filter.sharedMesh == null) continue;

                Matrix4x4 matrix = template.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                Bounds mesh = filter.sharedMesh.bounds;

                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = matrix.MultiplyPoint3x4(new Vector3((corner & 1) == 0 ? mesh.min.x : mesh.max.x, (corner & 2) == 0 ? mesh.min.y : mesh.max.y, (corner & 4) == 0 ? mesh.min.z : mesh.max.z));

                    if (first) measured = new Bounds(point, Vector3.zero);
                    else measured.Encapsulate(point);

                    first = false;
                }
            }

            foreach (Collider collider in template.GetComponentsInChildren<Collider>(true)) Destroy(collider);

            templates[model] = template;
            bounds[model] = measured;

            return template;
        }

        Material Material
        {
            get
            {
                if (material) return material;

                material = new Material(Shader.Find("Standard"));
                material.mainTexture = Resources.Load<Texture2D>(Root + "Textures/colormap");
                material.SetFloat("_Glossiness", 0.3f);
                material.enableInstancing = true;

                return material;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (templateRoot) Destroy(templateRoot.gameObject);
        }
    }
}
