using System.Collections;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The police helicopter: the highest level of the chase. It hovers over the player with a searchlight that follows the
    /// car ( a beam and a bright circle on the road ), and every few seconds flies ahead and drops a spike strip on the
    /// road in front of the player. It can't be destroyed; it leaves when the run ends.
    /// Built from simple shapes at runtime ( no model in the project ). Use ECCPoliceHelicopter.Spawn(gameController).
    /// </summary>
    public class ECCPoliceHelicopter : MonoBehaviour
    {
        [Tooltip("Height above the road")]
        public float height = 13;

        [Tooltip("Seconds between two spike strips ( random in this range )")]
        public Vector2 dropInterval = new Vector2(9, 13);

        [Tooltip("How far ahead of the player the spike strips land ( in view, so they can be dodged )")]
        public float dropDistance = 24;

        [Tooltip("Seconds the helicopter flies ahead before dropping")]
        public float dropRunTime = 1.6f;

        public static ECCPoliceHelicopter Active { get; private set; }

        ECCGameController gameController;

        Transform body;
        Transform mainRotor;
        Transform tailRotor;
        Transform beam;
        Transform spotCircle;
        Transform shadow;
        Light searchLight;
        Renderer redLamp;
        Renderer blueLamp;

        Vector3 velocity;
        float dropTimer;
        float dropRun;
        bool leaving;

        // Taxi: the police gave up the chase, the helicopter flies away
        bool dismissed;

        public void Dismiss()
        {
            dismissed = true;
        }
        float leaveTime;

        static readonly Color BodyColor = new Color(0.1f, 0.13f, 0.25f);
        static readonly Color StripeColor = new Color(0.95f, 0.95f, 0.97f);
        static readonly Color SpotColor = new Color(1, 0.97f, 0.82f, 0.55f);

        /// <summary>
        /// Brings the helicopter into the chase ( once; calling again keeps the one that is there )
        /// </summary>
        public static ECCPoliceHelicopter Spawn(ECCGameController gameController)
        {
            if (Active) return Active;

            if (gameController == null || gameController.playerObject == null) return null;

            ECCPoliceHelicopter helicopter = new GameObject("PoliceHelicopter").AddComponent<ECCPoliceHelicopter>();
            helicopter.gameController = gameController;
            helicopter.Build();

            // It flies in from behind the player, high up
            Transform player = gameController.playerObject.transform;
            helicopter.transform.position = player.position - FlatForward(player) * 45 + Vector3.up * (helicopter.height + 8);

            Active = helicopter;

            ECCHintBanner.Show("Icons/target", ECCCarTheme.CandyRed, "هلیکوپتر پلیس رسید!", "نورافکنش دنبالته و جلوی راهت میخ می‌ندازه؛ میخ‌ها رو دور بزن");

            return helicopter;
        }

        static Vector3 FlatForward(Transform target)
        {
            Vector3 forward = target.forward;
            forward.y = 0;

            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        void Build()
        {
            Material bodyMaterial = ECCRuntimeVisuals.CreateMaterial(BodyColor, 0.15f);
            Material stripeMaterial = ECCRuntimeVisuals.CreateMaterial(StripeColor, 0.2f);
            Material glassMaterial = ECCRuntimeVisuals.CreateMaterial(new Color(0.35f, 0.6f, 0.85f), 0.5f);
            Material darkMaterial = ECCRuntimeVisuals.CreateMaterial(new Color(0.12f, 0.12f, 0.12f), 0);
            Material redMaterial = ECCRuntimeVisuals.CreateMaterial(new Color(1, 0.15f, 0.15f), 1.5f);
            Material blueMaterial = ECCRuntimeVisuals.CreateMaterial(new Color(0.2f, 0.4f, 1), 1.5f);

            body = new GameObject("Body").transform;
            body.SetParent(transform, false);

            // The cabin, the windshield and the tail
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Sphere, body, new Vector3(0, 0, 0.6f), new Vector3(3.2f, 2.8f, 5.6f), bodyMaterial);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Sphere, body, new Vector3(0, 0.35f, 2.2f), new Vector3(2.6f, 2, 2.6f), glassMaterial);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, body, new Vector3(0, -0.2f, 0.5f), new Vector3(3.25f, 0.35f, 4), stripeMaterial);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, body, new Vector3(0, 0.4f, -3.8f), new Vector3(0.6f, 0.6f, 5), bodyMaterial);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, body, new Vector3(0, 1.2f, -6.1f), new Vector3(0.25f, 1.8f, 1), bodyMaterial);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, body, new Vector3(0, 0.45f, -6.1f), new Vector3(2, 0.15f, 0.7f), stripeMaterial);

            // Landing skids
            for (int side = -1; side <= 1; side += 2)
            {
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, body, new Vector3(side * 1.3f, -1.9f, 0.6f), new Vector3(0.18f, 0.18f, 4.6f), darkMaterial);
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, body, new Vector3(side * 1.15f, -1.45f, 1.6f), new Vector3(0.12f, 0.9f, 0.12f), darkMaterial);
                ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, body, new Vector3(side * 1.15f, -1.45f, -0.6f), new Vector3(0.12f, 0.9f, 0.12f), darkMaterial);
            }

            // The rotors spin in Update
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, body, new Vector3(0, 1.55f, 0.6f), new Vector3(0.5f, 0.25f, 0.5f), darkMaterial);

            mainRotor = new GameObject("MainRotor").transform;
            mainRotor.SetParent(body, false);
            mainRotor.localPosition = new Vector3(0, 1.8f, 0.6f);

            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, mainRotor, Vector3.zero, new Vector3(11, 0.06f, 0.45f), darkMaterial);
            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, mainRotor, Vector3.zero, new Vector3(0.45f, 0.06f, 11), darkMaterial);

            tailRotor = new GameObject("TailRotor").transform;
            tailRotor.SetParent(body, false);
            tailRotor.localPosition = new Vector3(0.25f, 1.2f, -6.2f);

            ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, tailRotor, Vector3.zero, new Vector3(0.05f, 2.2f, 0.25f), darkMaterial);

            // Police lamps on the roof
            redLamp = ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, body, new Vector3(0.45f, 1.4f, 1.4f), new Vector3(0.5f, 0.25f, 0.3f), redMaterial).GetComponent<Renderer>();
            blueLamp = ECCRuntimeVisuals.CreatePart(PrimitiveType.Cube, body, new Vector3(-0.45f, 1.4f, 1.4f), new Vector3(0.5f, 0.25f, 0.3f), blueMaterial).GetComponent<Renderer>();

            // The searchlight under the nose: a real spot light, a see-through beam and a bright circle on the road
            Transform lamp = ECCRuntimeVisuals.CreatePart(PrimitiveType.Cylinder, body, new Vector3(0, -1.3f, 2.4f), new Vector3(0.6f, 0.2f, 0.6f), stripeMaterial);
            lamp.name = "Searchlight";

            searchLight = new GameObject("SearchLight").AddComponent<Light>();
            searchLight.transform.SetParent(transform, false);
            searchLight.type = LightType.Spot;
            searchLight.spotAngle = 30;
            searchLight.range = height * 3;
            searchLight.intensity = ECCNightMode.IsNight ? 6 : 2.5f;
            searchLight.color = new Color(1, 0.97f, 0.85f);
            searchLight.shadows = LightShadows.None;

            beam = ECCGlow.CreateCone("Beam", transform, ECCGlow.ConeMaterial(new Color(1, 0.97f, 0.8f, ECCNightMode.IsNight ? 0.22f : 0.12f)));

            spotCircle = ECCGlow.CreateFlat("SpotCircle", null, Vector3.zero, new Vector2(9, 9), ECCGlow.RadialMaterial(SpotColor));
            shadow = ECCGlow.CreateFlat("Shadow", null, Vector3.zero, new Vector2(9, 6), ECCGlow.RadialMaterial(new Color(0, 0, 0, 0.45f)));

            dropTimer = 4;
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;

            if (spotCircle) Destroy(spotCircle.gameObject);
            if (shadow) Destroy(shadow.gameObject);
        }

        bool IsChasing
        {
            get { return leaving == false && dismissed == false && gameController && gameController.isGameOver == false && gameController.playerObject && gameController.playerObject.health > 0; }
        }

        void Update()
        {
            if (mainRotor) mainRotor.Rotate(Vector3.up, 1400 * Time.deltaTime, Space.Self);
            if (tailRotor) tailRotor.Rotate(Vector3.right, 2000 * Time.deltaTime, Space.Self);

            bool redOn = Mathf.Repeat(Time.time * 2.5f, 1) < 0.5f;
            if (redLamp) redLamp.enabled = redOn;
            if (blueLamp) blueLamp.enabled = redOn == false;

            if (IsChasing == false)
            {
                FlyAway();
                return;
            }

            Transform player = gameController.playerObject.transform;
            Vector3 forward = FlatForward(player);
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            // Normally a little behind and beside the car; during a drop run, ahead of it
            Vector3 target;

            if (dropRun > 0) target = player.position + forward * (dropDistance + 2) + Vector3.up * height;
            else target = player.position - forward * 5 + right * 4 + Vector3.up * height;

            Move(target, dropRun > 0 ? 3.5f : 1.6f);

            AimSearchlight(player.position);

            UpdateDrops(player);
        }

        void Move(Vector3 target, float sharpness)
        {
            Vector3 previous = transform.position;

            transform.position = Vector3.Lerp(transform.position, target, 1 - Mathf.Exp(-sharpness * Time.deltaTime));

            velocity = Time.deltaTime > 0 ? (transform.position - previous) / Time.deltaTime : Vector3.zero;

            // It faces where it flies ( or the player's way when hovering ), tilting into the movement
            Vector3 flat = velocity;
            flat.y = 0;

            if (flat.sqrMagnitude > 1)
            {
                Quaternion facing = Quaternion.LookRotation(flat.normalized);

                transform.rotation = Quaternion.Slerp(transform.rotation, facing, 1 - Mathf.Exp(-2 * Time.deltaTime));
            }

            if (body) body.localRotation = Quaternion.Euler(Mathf.Clamp(flat.magnitude * 0.9f, 0, 18), 0, 0);
        }

        void AimSearchlight(Vector3 groundTarget)
        {
            Vector3 lampPosition = transform.position + Vector3.down * 1.3f;

            // The circle wanders a little around the car, like an operator keeping it in the light
            Vector3 wander = new Vector3(Mathf.Sin(Time.time * 1.3f), 0, Mathf.Cos(Time.time * 0.9f)) * 1.2f;
            Vector3 spot = groundTarget + wander + Vector3.up * 0.06f;

            if (searchLight)
            {
                searchLight.transform.position = lampPosition;
                searchLight.transform.rotation = Quaternion.LookRotation(spot - lampPosition);
            }

            if (beam)
            {
                Vector3 toSpot = spot - lampPosition;
                float length = toSpot.magnitude;

                beam.position = lampPosition;
                beam.rotation = Quaternion.FromToRotation(Vector3.down, toSpot.normalized);
                beam.localScale = new Vector3(2.6f, length, 2.6f);
            }

            if (spotCircle) spotCircle.position = spot;

            if (shadow)
            {
                Vector3 below = transform.position;
                below.y = groundTarget.y + 0.04f;

                shadow.position = below;
                shadow.rotation = Quaternion.Euler(90, transform.eulerAngles.y, 0);
            }
        }

        void UpdateDrops(Transform player)
        {
            if (dropRun > 0)
            {
                dropRun -= Time.deltaTime;

                if (dropRun <= 0) StartCoroutine(DropSpikes(player));

                return;
            }

            dropTimer -= Time.deltaTime;

            if (dropTimer <= 0)
            {
                dropTimer = Random.Range(dropInterval.x, dropInterval.y);
                dropRun = dropRunTime;
            }
        }

        /// <summary>
        /// A spike strip falls from the helicopter onto the road ahead of the player. It only hurts once it's on the road
        /// </summary>
        IEnumerator DropSpikes(Transform player)
        {
            if (player == null || gameController == null) yield break;

            Vector3 forward = FlatForward(player);
            Vector3 landing = player.position + forward * dropDistance;

            ECCSpikeStrip strip = ECCSpikeStrip.Create(landing, Quaternion.LookRotation(forward), gameController.groundLayer);

            if (strip == null) yield break;

            Vector3 ground = strip.transform.position;
            Vector3 start = new Vector3(ground.x, transform.position.y - 2, ground.z);

            strip.enabled = false;

            const float fallTime = 0.55f;

            for (float time = 0; time < fallTime && strip; time += Time.deltaTime)
            {
                float t = time / fallTime;

                strip.transform.position = Vector3.Lerp(start, ground, t * t);

                yield return null;
            }

            if (strip == null) yield break;

            strip.transform.position = ground;
            strip.enabled = true;

            ECCRunMessages.HelicopterSpikes();
        }

        void FlyAway()
        {
            if (leaving == false)
            {
                leaving = true;
                leaveTime = 0;
            }

            leaveTime += Time.deltaTime;

            transform.position += (transform.forward * 12 + Vector3.up * 8) * Time.deltaTime;

            if (searchLight) searchLight.enabled = false;
            if (beam) beam.gameObject.SetActive(false);
            if (spotCircle) spotCircle.gameObject.SetActive(false);
            if (shadow) shadow.gameObject.SetActive(false);

            if (leaveTime > 6) Destroy(gameObject);
        }
    }
}
