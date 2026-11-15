using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    public enum ECCPowerUpType
    {
        Nitro = 0,  // Drive faster for a few seconds
        Shield = 1, // Can't be hurt for a few seconds
        EMP = 2,    // Destroys the nearby police cars
        Magnet = 3, // Pulls the nearby money in
        Repair = 4, // Repairs part of the car right away ( only from the power-up bar, never a road pickup )
    }

    /// <summary>
    /// Spawns power-up pickups around the player, and runs their effects.
    /// Added to the game controller when the game starts.
    /// The remaining time of the timed effects is shown as a ring around the power-up buttons ( ECCPowerUpBar ).
    /// </summary>
    public class ECCPowerUpManager : MonoBehaviour
    {
        [Tooltip("Seconds between power-up spawns")]
        public float spawnInterval = 12;

        [Tooltip("The most power-ups on the ground at the same time")]
        public int maxPickups = 2;

        [Tooltip("Distance from the player at which power-ups appear")]
        public Vector2 spawnDistance = new Vector2(16, 26);

        [Tooltip("How close the player must drive to a power-up to pick it up")]
        public float pickupRadius = 2.6f;

        [Header("Effects")]
        public float nitroDuration = 4;
        public float nitroSpeedMultiplier = 1.5f;
        public float shieldDuration = 6;
        public float empRadius = 14;
        public float magnetDuration = 8;
        public float magnetRadius = 18;
        public float magnetPullSpeed = 22;

        [Tooltip("Part of the car's full health given back by the repair kit")]
        [Range(0.1f, 1)]
        public float repairAmount = 0.5f;

        // The pickups found on the road ( the repair kit isn't one: the road already has repair items )
        const int RoadPickupTypes = 4;

        /// <summary>
        /// Called when the player picks up a power-up
        /// </summary>
        public static event Action<ECCPowerUpType> PowerUpUsed;

        ECCGameController gameController;
        readonly List<ECCPowerUpPickup> pickups = new List<ECCPowerUpPickup>();
        readonly Dictionary<ECCPowerUpType, float> activeTimers = new Dictionary<ECCPowerUpType, float>();

        float spawnTimer;
        float nitroBaseSpeed = -1;
        Transform shieldRing;
        Material[] shieldMaterials;
        Color[] shieldColors;

        public static string GetName(ECCPowerUpType type)
        {
            switch (type)
            {
                case ECCPowerUpType.Nitro: return "نیترو";
                case ECCPowerUpType.Shield: return "سپر";
                case ECCPowerUpType.EMP: return "شوک الکتریکی";
                case ECCPowerUpType.Repair: return "تعمیر فوری";
                default: return "آهن‌ربا";
            }
        }

        public static Color GetColor(ECCPowerUpType type)
        {
            switch (type)
            {
                case ECCPowerUpType.Nitro: return new Color(1, 0.45f, 0.15f);
                case ECCPowerUpType.Shield: return new Color(0.25f, 0.7f, 1);
                case ECCPowerUpType.EMP: return new Color(0.95f, 0.75f, 0.1f);
                case ECCPowerUpType.Repair: return new Color(0.3f, 0.82f, 0.4f);
                default: return new Color(1, 0.35f, 0.55f);
            }
        }

        /// <summary>
        /// The icon in Resources/ECCUI ( white shapes, tinted by the UI )
        /// </summary>
        public static string GetIcon(ECCPowerUpType type)
        {
            switch (type)
            {
                case ECCPowerUpType.Nitro: return "Icons/fire";
                case ECCPowerUpType.Shield: return "Icons/shield";
                case ECCPowerUpType.EMP: return "Icons/bolt";
                case ECCPowerUpType.Repair: return "Icons/heart";
                default: return "Icons/magnet";
            }
        }

        /// <summary>
        /// What the power-up does, for the confirmation popup
        /// </summary>
        public static string GetDescription(ECCPowerUpType type)
        {
            ECCPowerUpManager manager = ECCGameController.Instance ? ECCGameController.Instance.GetComponent<ECCPowerUpManager>() : null;

            float nitro = manager ? manager.nitroDuration : 4;
            float shield = manager ? manager.shieldDuration : 6;
            float magnet = manager ? manager.magnetDuration : 8;
            float repair = manager ? manager.repairAmount : 0.5f;

            switch (type)
            {
                case ECCPowerUpType.Nitro: return ECCUIKit.PersianDigits(Mathf.RoundToInt(nitro)) + " ثانیه با سرعت خیلی بیشتر از پلیس‌ها دور شو.";
                case ECCPowerUpType.Shield: return ECCUIKit.PersianDigits(Mathf.RoundToInt(shield)) + " ثانیه هیچ ضربه‌ای به ماشینت آسیب نمی‌زند.";
                case ECCPowerUpType.EMP: return "همه‌ی ماشین‌های پلیس نزدیک تو در یک لحظه از کار می‌افتند.";
                case ECCPowerUpType.Repair: return "همین حالا " + ECCUIKit.PersianDigits(Mathf.RoundToInt(repair * 100)) + " درصد سلامت ماشینت برمی‌گردد.";
                default: return ECCUIKit.PersianDigits(Mathf.RoundToInt(magnet)) + " ثانیه همه‌ی پول‌های اطرافت به سمتت کشیده می‌شوند.";
            }
        }

        void Start()
        {
            gameController = GetComponent<ECCGameController>();

            spawnTimer = spawnInterval * 0.5f;
        }

        bool IsPlaying
        {
            get { return gameController && gameController.gameStarted && gameController.isGameOver == false && gameController.startDelay <= 0 && gameController.playerObject; }
        }

        void Update()
        {
            if (IsPlaying == false)
            {
                UpdateEffects(false);
                return;
            }

            Transform player = gameController.playerObject.transform;

            // Spawn new power-ups
            spawnTimer -= Time.deltaTime;

            pickups.RemoveAll(pickup => pickup == null);

            if (spawnTimer <= 0)
            {
                spawnTimer = spawnInterval * UnityEngine.Random.Range(0.8f, 1.2f);

                if (pickups.Count < maxPickups) SpawnPickup(player);
            }

            // Pick up the power-ups the player drives into
            for (int index = pickups.Count - 1; index >= 0; index--)
            {
                if (ECCRuntimeVisuals.FlatDistance(pickups[index].transform.position, player.position) <= pickupRadius)
                {
                    ECCPowerUpType type = pickups[index].type;

                    Destroy(pickups[index].gameObject);
                    pickups.RemoveAt(index);

                    Activate(type);
                }
            }

            UpdateEffects(true);
        }

        void SpawnPickup(Transform player)
        {
            ECCPowerUpType type = (ECCPowerUpType)UnityEngine.Random.Range(0, RoadPickupTypes);

            // In front of the player, so it can be reached
            Vector3 forward = player.forward;
            forward.y = 0;

            Vector3 direction = Quaternion.Euler(0, UnityEngine.Random.Range(-70f, 70f), 0) * forward.normalized;
            Vector3 position = ECCRuntimeVisuals.SnapToGround(player.position + direction * UnityEngine.Random.Range(spawnDistance.x, spawnDistance.y), gameController.groundLayer);

            pickups.Add(ECCPowerUpPickup.Create(type, position));
        }

        /// <summary>
        /// Whether the power-up would do something right now. reason: a Persian message when it wouldn't
        /// ( still running, the car is already fully repaired )
        /// </summary>
        public bool CanActivate(ECCPowerUpType type, out string reason)
        {
            reason = string.Empty;

            ECCCar player = gameController ? gameController.playerObject : null;

            if (player == null || IsPlaying == false || player.health <= 0)
            {
                reason = "الان نمی‌شود از این قدرت استفاده کرد";
                return false;
            }

            if (IsActive(type))
            {
                reason = GetName(type) + " هنوز فعال است";
                return false;
            }

            if (type == ECCPowerUpType.Repair && player.health >= player.healthMax)
            {
                reason = "ماشینت سالم است و تعمیر لازم ندارد";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Starts a power-up effect. Can also be called from other scripts ( for example a revive uses the EMP )
        /// </summary>
        public bool Activate(ECCPowerUpType type, bool announce = true)
        {
            ECCCar player = gameController ? gameController.playerObject : null;

            // Only while the chase is running, so a bought power-up is never wasted
            if (player == null || IsPlaying == false || player.health <= 0) return false;

            switch (type)
            {
                case ECCPowerUpType.Nitro:
                    if (nitroBaseSpeed < 0) nitroBaseSpeed = player.speed;
                    player.speed = nitroBaseSpeed * nitroSpeedMultiplier;
                    activeTimers[type] = nitroDuration;
                    break;

                case ECCPowerUpType.Shield:
                    player.invulnerableTime = Mathf.Max(player.invulnerableTime, shieldDuration);
                    activeTimers[type] = shieldDuration;
                    if (shieldRing == null) CreateShieldBubble(player);
                    break;

                case ECCPowerUpType.EMP:
                    // The effect first: it finds the police in range before the blast knocks them out
                    ECCPickupEffects.Electric(player, empRadius);
                    EmpBlast(player.transform.position, empRadius);
                    break;

                case ECCPowerUpType.Magnet:
                    activeTimers[type] = magnetDuration;
                    break;

                case ECCPowerUpType.Repair:
                    // A full car gains nothing, so the kit isn't used
                    if (player.health >= player.healthMax) return false;
                    player.ChangeHealth(player.healthMax * repairAmount);
                    ECCPickupEffects.Heal(player);
                    break;
            }

            if (announce) ECCHintBanner.Show(GetIcon(type), GetColor(type), GetName(type) + "!", GetDescription(type), 1.6f, ECCHintBanner.Kind.Urgent);

            ECCHaptics.Vibrate(ECCHaptics.Kind.Medium);

            if (PowerUpUsed != null) PowerUpUsed(type);

            return true;
        }

        /// <summary>
        /// True while a timed power-up ( nitro, shield, magnet ) is running
        /// </summary>
        public bool IsActive(ECCPowerUpType type)
        {
            return activeTimers.ContainsKey(type);
        }

        /// <summary>
        /// Seconds left of a timed power-up, 0 when it's not running
        /// </summary>
        public float GetTimeLeft(ECCPowerUpType type)
        {
            float left;

            return activeTimers.TryGetValue(type, out left) ? Mathf.Max(0, left) : 0;
        }

        /// <summary>
        /// The full duration of a timed power-up, 0 for the instant ones ( EMP, repair )
        /// </summary>
        public float GetDuration(ECCPowerUpType type)
        {
            switch (type)
            {
                case ECCPowerUpType.Nitro: return nitroDuration;
                case ECCPowerUpType.Shield: return shieldDuration;
                case ECCPowerUpType.Magnet: return magnetDuration;
                default: return 0;
            }
        }

        /// <summary>
        /// A see-through bubble around the whole player car: an ellipsoid measured from the car's own meshes, so a bigger
        /// car gets a bigger bubble, plus a glowing ring where it meets the road. Uses the city's Fade material ( Standard,
        /// transparent, included in builds through Resources ), or the always included Sprites/Default
        /// </summary>
        void CreateShieldBubble(ECCCar player)
        {
            Bounds bounds = MeasureCar(player);
            Color color = GetColor(ECCPowerUpType.Shield);

            // Big enough that the car's corners stay inside; the lower half is hidden under the road, so it reads as a dome
            Vector3 size = new Vector3(bounds.extents.x * 1.7f + 0.25f, bounds.extents.y * 2 + 0.25f, bounds.extents.z * 1.5f + 0.25f) * 2;

            shieldRing = new GameObject("ShieldBubble").transform;
            shieldRing.SetParent(player.transform, false);
            shieldRing.localPosition = bounds.center;

            Color shell = new Color(color.r, color.g, color.b, 0.22f);
            Color inner = new Color(color.r * 0.8f + 0.2f, color.g * 0.8f + 0.2f, color.b * 0.8f + 0.2f, 0.12f);
            Color ring = new Color(color.r, color.g, color.b, 0.5f);

            shieldColors = new[] { shell, inner, ring };
            shieldMaterials = new Material[shieldColors.Length];

            for (int index = 0; index < shieldColors.Length; index++) shieldMaterials[index] = CreateShieldMaterial(shieldColors[index]);

            ECCRuntimeVisuals.CreatePart(PrimitiveType.Sphere, shieldRing, Vector3.zero, size, shieldMaterials[0]);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Sphere, shieldRing, Vector3.zero, size * 0.94f, shieldMaterials[1]);

            // The ring on the road around the car
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, shieldRing, new Vector3(0, bounds.min.y - bounds.center.y + 0.05f, 0), new Vector3(size.x, 0.01f, size.z), shieldMaterials[2]);
        }

        static Material CreateShieldMaterial(Color color)
        {
            Material fade = Resources.Load<Material>("KenneyCity/KenneyFade");
            Material material;

            if (fade)
            {
                material = new Material(fade);
                material.mainTexture = null;

                if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.9f);

                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", new Color(color.r, color.g, color.b) * 0.6f);
                }
            }
            else
            {
                material = new Material(Shader.Find("Sprites/Default"));
            }

            material.color = color;

            return material;
        }

        /// <summary>
        /// The car's size in its own space, from its meshes ( the contact shadow and the bubble left out ). Parts that
        /// reach far outside the car's collider ( light beams ) are clipped to it
        /// </summary>
        static Bounds MeasureCar(ECCCar player)
        {
            Transform root = player.transform;
            Bounds bounds = new Bounds(Vector3.up * 0.6f, new Vector3(1.2f, 1.2f, 2.2f));
            bool found = false;

            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            {
                Renderer part = filter.GetComponent<Renderer>();

                if (filter.sharedMesh == null || part == null || part.enabled == false) continue;

                if (filter.GetComponentInParent<ECCCarShadow>() || (filter.transform.parent && filter.transform.parent.name == "ShieldBubble")) continue;

                Bounds mesh = filter.sharedMesh.bounds;

                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = new Vector3((corner & 1) == 0 ? mesh.min.x : mesh.max.x, (corner & 2) == 0 ? mesh.min.y : mesh.max.y, (corner & 4) == 0 ? mesh.min.z : mesh.max.z);

                    point = root.InverseTransformPoint(filter.transform.TransformPoint(point));

                    if (found) bounds.Encapsulate(point);
                    else bounds = new Bounds(point, Vector3.zero);

                    found = true;
                }
            }

            BoxCollider box = player.GetComponent<BoxCollider>();

            if (box)
            {
                // Never smaller than the collider, never much wider or longer than it
                Bounds limit = new Bounds(box.center, Vector3.Scale(box.size, new Vector3(1.4f, 3, 1.3f)));

                Vector3 min = Vector3.Max(bounds.min, limit.min);
                Vector3 max = Vector3.Min(bounds.max, limit.max);

                bounds.SetMinMax(Vector3.Min(min, max), Vector3.Max(min, max));
                bounds.Encapsulate(new Bounds(box.center, box.size));
            }

            return bounds;
        }

        /// <summary>
        /// A gentle shimmer while the shield is on, blinking in its last second and a half so its end is no surprise
        /// </summary>
        void AnimateShieldBubble(float timeLeft)
        {
            shieldRing.localScale = Vector3.one * (1 + Mathf.Sin(Time.time * 5) * 0.025f);

            if (shieldMaterials == null) return;

            float alpha = timeLeft < 1.5f ? (Mathf.Sin(Time.time * 22) > 0 ? 1 : 0.35f) : 0.85f + Mathf.Sin(Time.time * 3) * 0.15f;

            for (int index = 0; index < shieldMaterials.Length; index++)
            {
                Color color = shieldColors[index];
                color.a *= alpha;

                if (shieldMaterials[index]) shieldMaterials[index].color = color;
            }
        }

        void DestroyShieldMaterials()
        {
            if (shieldMaterials == null) return;

            foreach (Material material in shieldMaterials)
            {
                if (material) Destroy(material);
            }

            shieldMaterials = null;
        }

        /// <summary>
        /// Destroys every police car around a point
        /// </summary>
        public static void EmpBlast(Vector3 center, float radius)
        {
            for (int index = ECCCar.ActiveCars.Count - 1; index >= 0; index--)
            {
                ECCCar car = ECCCar.ActiveCars[index];

                if (car == null || car.IsPlayer || car.health <= 0) continue;

                if (ECCRuntimeVisuals.FlatDistance(car.transform.position, center) <= radius)
                {
                    car.invulnerableTime = 0;
                    car.ChangeHealth(-car.health);
                }
            }
        }

        void UpdateEffects(bool running)
        {
            ECCCar player = gameController ? gameController.playerObject : null;

            List<ECCPowerUpType> ended = new List<ECCPowerUpType>();
            List<ECCPowerUpType> active = new List<ECCPowerUpType>(activeTimers.Keys);

            foreach (ECCPowerUpType type in active)
            {
                if (running) activeTimers[type] -= Time.deltaTime;

                if (activeTimers[type] <= 0 || player == null) ended.Add(type);
            }

            foreach (ECCPowerUpType type in ended)
            {
                activeTimers.Remove(type);

                if (type == ECCPowerUpType.Nitro && player && nitroBaseSpeed >= 0) player.speed = nitroBaseSpeed;
                if (type == ECCPowerUpType.Nitro) nitroBaseSpeed = -1;
                if (type == ECCPowerUpType.Shield && shieldRing)
                {
                    Destroy(shieldRing.gameObject);
                    DestroyShieldMaterials();
                }
            }

            if (shieldRing) AnimateShieldBubble(GetTimeLeft(ECCPowerUpType.Shield));

            // The magnet pulls the money towards the player
            if (running && player && activeTimers.ContainsKey(ECCPowerUpType.Magnet))
            {
                foreach (ECCItem item in ECCItem.ActiveItems)
                {
                    if (item == null || item.CashValue <= 0) continue;

                    if (ECCRuntimeVisuals.FlatDistance(item.transform.position, player.transform.position) <= magnetRadius)
                    {
                        item.transform.position = Vector3.MoveTowards(item.transform.position, player.transform.position, magnetPullSpeed * Time.deltaTime);
                    }
                }
            }
        }
    }
}
