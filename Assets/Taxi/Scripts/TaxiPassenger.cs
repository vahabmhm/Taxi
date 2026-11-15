using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    public enum TaxiPassengerKind
    {
        Normal = 0,
        Hurry = 1,    // «عجول»: a short ride timer, a big tip when it's kept
        Vip = 2,      // «VIP»: pays a lot, every rival taxi wants them
        Tourist = 3,  // «توریست»: a far destination, pays more
        Family = 4,   // «خانواده»: three people, need a car with three free seats
        Grumpy = 5,   // «بدخلق»: every bump costs more of the tip
    }

    public enum TaxiPassengerState { Waiting, Boarding, Riding, Leaving }

    /// <summary>
    /// A person waiting at the side of the street for a taxi ( the crowd game's stick figure and its Mixamo
    /// animations: Idle, Walk, Running, Win, Fail ). A taxi that stops in the ring next to them takes them; they run to
    /// the car and get in, ride to their destination, get out there and cheer ( or stomp off when the ride was bad )
    /// </summary>
    public class TaxiPassenger : MonoBehaviour
    {
        public TaxiPassengerKind kind;
        public TaxiPassengerState state = TaxiPassengerState.Waiting;

        public TaxiSpot pickup;
        public TaxiSpot dropoff;

        [Tooltip("Seconds the passenger still waits before walking away")]
        public float patience = 50;
        internal float patienceMax;

        // Hurry passengers: seconds the ride may take ( 0 means no limit )
        internal float rideLimit;
        internal float rideTime;

        // Bumps and crashes during the ride
        internal int bumps;

        // The pickup and drop-off stops: 2 perfect, 1 good, 0 ok
        internal int pickupQuality;

        internal TaxiCab cab;

        // The cab that is on its way to this passenger ( a rival's plan, so two rivals don't chase the same person )
        internal TaxiCab reservedBy;

        public static readonly List<TaxiPassenger> All = new List<TaxiPassenger>();

        const string ModelPath = "TaxiPeople/PlayerModel";
        const string ControllerPath = "TaxiPeople/Passenger";

        // Height of a person in the game ( the cars are drawn bigger, see ECCGameController.carSize )
        public const float PersonHeight = 2.3f;

        const float WalkSpeed = 2.6f;
        const float RunSpeed = 7f;

        readonly List<Animator> animators = new List<Animator>();
        readonly List<Transform> people = new List<Transform>();

        Transform zone;
        Transform icon;
        Transform beacon;
        Renderer zoneRing;
        float leaveTimer;
        Vector3 walkTarget;
        bool walking;
        bool happyEnding;

        public int Seats
        {
            get { return kind == TaxiPassengerKind.Family ? 3 : 1; }
        }

        public bool IsWaiting
        {
            get { return state == TaxiPassengerState.Waiting; }
        }

        public Color KindColor
        {
            get { return GetColor(kind); }
        }

        public static Color GetColor(TaxiPassengerKind kind)
        {
            switch (kind)
            {
                case TaxiPassengerKind.Hurry: return TaxiColors.Hurry;
                case TaxiPassengerKind.Vip: return TaxiColors.Vip;
                case TaxiPassengerKind.Tourist: return TaxiColors.Tourist;
                case TaxiPassengerKind.Family: return TaxiColors.Family;
                case TaxiPassengerKind.Grumpy: return TaxiColors.Grumpy;
                default: return TaxiColors.Normal;
            }
        }

        public static string GetName(TaxiPassengerKind kind)
        {
            switch (kind)
            {
                case TaxiPassengerKind.Hurry: return "مسافر عجول";
                case TaxiPassengerKind.Vip: return "مسافر VIP";
                case TaxiPassengerKind.Tourist: return "توریست";
                case TaxiPassengerKind.Family: return "خانواده";
                case TaxiPassengerKind.Grumpy: return "مسافر بدخلق";
                default: return "مسافر";
            }
        }

        /// <summary>
        /// How much more a kind of passenger pays than a normal one
        /// </summary>
        public static float FareMultiplier(TaxiPassengerKind kind)
        {
            switch (kind)
            {
                case TaxiPassengerKind.Hurry: return 1.4f;
                case TaxiPassengerKind.Vip: return 4;
                case TaxiPassengerKind.Tourist: return 1.5f;
                case TaxiPassengerKind.Family: return 2.2f;
                case TaxiPassengerKind.Grumpy: return 1.2f;
                default: return 1;
            }
        }

        /// <summary>
        /// Street distance from pickup to destination for a kind of passenger
        /// </summary>
        public static Vector2 RideDistance(TaxiPassengerKind kind)
        {
            switch (kind)
            {
                case TaxiPassengerKind.Tourist: return new Vector2(260, 420);
                case TaxiPassengerKind.Vip: return new Vector2(200, 340);
                case TaxiPassengerKind.Hurry: return new Vector2(120, 220);
                default: return new Vector2(110, 260);
            }
        }

        // ----- Creating -----

        public static TaxiPassenger Create(TaxiPassengerKind kind, TaxiSpot spot, TaxiSpot destination)
        {
            GameObject root = new GameObject("Passenger_" + kind);
            root.transform.position = spot.stand;

            TaxiPassenger passenger = root.AddComponent<TaxiPassenger>();
            passenger.kind = kind;
            passenger.pickup = spot;
            passenger.dropoff = destination;
            passenger.patience = kind == TaxiPassengerKind.Hurry ? Random.Range(24f, 32f) : (kind == TaxiPassengerKind.Vip ? 45 : Random.Range(45f, 70f));
            passenger.patienceMax = passenger.patience;

            if (kind == TaxiPassengerKind.Hurry) passenger.rideLimit = TaxiCity.StreetDistance(spot.zone, destination.zone) / 8.5f + 8;

            int count = kind == TaxiPassengerKind.Family ? 3 : 1;

            for (int index = 0; index < count; index++)
            {
                Vector3 offset = spot.streetDirection * (index - (count - 1) * 0.5f) * 1.1f;

                passenger.AddPerson(offset, index == 0 ? kind : TaxiPassengerKind.Normal);
            }

            passenger.FaceStreet();

            passenger.zone = TaxiMarkers.CreateZone(spot.zone, TaxiCab.ZoneRadius, passenger.KindColor);
            passenger.zone.SetParent(root.transform, true);
            passenger.zoneRing = passenger.zone.GetComponentInChildren<Renderer>();

            passenger.icon = TaxiMarkers.CreateIcon(root.transform, PersonHeight + 1.6f, passenger.KindColor);

            // The VIP gets a crown: a second, bigger diamond
            if (kind == TaxiPassengerKind.Vip) passenger.icon.localScale = Vector3.one * 1.5f;

            return passenger;
        }

        void AddPerson(Vector3 offset, TaxiPassengerKind look)
        {
            GameObject source = Resources.Load<GameObject>(ModelPath);

            Transform person;

            if (source)
            {
                person = Instantiate(source, transform).transform;
            }
            else
            {
                // Without the model a capsule stands in
                person = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
                person.SetParent(transform, false);

                Collider capsuleCollider = person.GetComponent<Collider>();
                if (capsuleCollider) Destroy(capsuleCollider);
            }

            person.localPosition = offset;
            person.localRotation = Quaternion.identity;

            FitHeight(person);
            Paint(person, look);

            foreach (Collider part in person.GetComponentsInChildren<Collider>()) Destroy(part);

            Animator animator = person.GetComponentInChildren<Animator>();

            if (animator == null && source) animator = person.gameObject.AddComponent<Animator>();

            if (animator)
            {
                RuntimeAnimatorController controller = Resources.Load<RuntimeAnimatorController>(ControllerPath);

                if (controller) animator.runtimeAnimatorController = controller;

                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                // Not all in step
                animator.Update(Random.Range(0f, 1f));

                animators.Add(animator);
            }

            people.Add(person);
        }

        static void FitHeight(Transform person)
        {
            Bounds bounds;

            if (MeasureBounds(person, out bounds) == false || bounds.size.y < 0.01f) return;

            person.localScale *= PersonHeight / bounds.size.y;

            // Stand on the ground
            if (MeasureBounds(person, out bounds)) person.position += Vector3.up * (person.parent.position.y - bounds.min.y);
        }

        static bool MeasureBounds(Transform person, out Bounds bounds)
        {
            bounds = new Bounds();
            bool first = true;

            foreach (Renderer part in person.GetComponentsInChildren<Renderer>())
            {
                if (first) bounds = part.bounds;
                else bounds.Encapsulate(part.bounds);

                first = false;
            }

            return first == false;
        }

        static readonly Color[] Clothes =
        {
            new Color(0.95f, 0.3f, 0.3f), new Color(0.3f, 0.6f, 1), new Color(0.35f, 0.85f, 0.45f), new Color(1, 0.6f, 0.2f),
            new Color(0.75f, 0.4f, 0.95f), new Color(0.95f, 0.95f, 0.95f), new Color(0.2f, 0.2f, 0.25f), new Color(1, 0.45f, 0.7f),
        };

        static void Paint(Transform person, TaxiPassengerKind look)
        {
            Color color = look == TaxiPassengerKind.Normal ? Clothes[Random.Range(0, Clothes.Length)] : GetColor(look);

            Material material = TaxiMarkers.Material(color, 0.05f);

            foreach (Renderer part in person.GetComponentsInChildren<Renderer>())
            {
                Material[] shared = new Material[Mathf.Max(1, part.sharedMaterials.Length)];

                for (int index = 0; index < shared.Length; index++) shared[index] = material;

                part.sharedMaterials = shared;
                part.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        void FaceStreet()
        {
            if (pickup.toStreet.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(pickup.toStreet);
        }

        void OnEnable()
        {
            All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        // ----- Animations -----

        void Play(string animation)
        {
            foreach (Animator animator in animators)
            {
                if (animator && animator.runtimeAnimatorController) animator.CrossFade(animation, 0.15f);
            }
        }

        /// <summary>
        /// Waving at a taxi that honked: a short cheer
        /// </summary>
        public void Wave()
        {
            if (state != TaxiPassengerState.Waiting) return;

            Play("Win");
            CancelInvoke("BackToIdle");
            Invoke("BackToIdle", 1.4f);

            patience = Mathf.Min(patienceMax, patience + 8);
        }

        void BackToIdle()
        {
            if (state == TaxiPassengerState.Waiting) Play("Idle");
        }

        // ----- The ride -----

        /// <summary>
        /// A taxi stopped in the ring: the passenger runs to it. False if someone else already took them
        /// </summary>
        public bool Board(TaxiCab taxi, int quality)
        {
            if (state != TaxiPassengerState.Waiting || taxi == null) return false;

            state = TaxiPassengerState.Boarding;
            cab = taxi;
            pickupQuality = quality;
            reservedBy = null;

            if (zone) zone.gameObject.SetActive(false);
            if (icon) icon.gameObject.SetActive(false);

            Play("Running");

            return true;
        }

        /// <summary>
        /// The passenger got out at the destination ( or anywhere when the ride was cut short ): they walk to the
        /// sidewalk and cheer, or stomp off
        /// </summary>
        public void GetOut(bool happy)
        {
            state = TaxiPassengerState.Leaving;
            happyEnding = happy;

            if (beacon) Destroy(beacon.gameObject);

            Vector3 door = cab ? cab.transform.position + cab.transform.right * 1.5f : transform.position;

            transform.position = new Vector3(door.x, 0, door.z);
            ShowPeople(true);

            walkTarget = dropoff.stand;
            walking = true;

            Play("Walk");

            cab = null;
            leaveTimer = 6;
        }

        /// <summary>
        /// The passenger gave up waiting: they walk away along the sidewalk
        /// </summary>
        public void GiveUp()
        {
            if (state != TaxiPassengerState.Waiting) return;

            state = TaxiPassengerState.Leaving;
            happyEnding = false;

            if (zone) zone.gameObject.SetActive(false);
            if (icon) icon.gameObject.SetActive(false);

            Play("Fail");

            walkTarget = pickup.stand + pickup.streetDirection * (Random.value < 0.5f ? 12 : -12);
            walking = false;

            Invoke("StartWalkingAway", 1.4f);

            leaveTimer = 7;
        }

        void StartWalkingAway()
        {
            walking = true;
            Play("Walk");
        }

        /// <summary>
        /// Shows the destination beacon ( only for the player's passengers )
        /// </summary>
        public void ShowDestination()
        {
            if (beacon == null) beacon = TaxiMarkers.CreateBeacon(dropoff.zone, TaxiCab.ZoneRadius, KindColor);
        }

        void ShowPeople(bool visible)
        {
            foreach (Transform person in people)
            {
                if (person) person.gameObject.SetActive(visible);
            }
        }

        void Update()
        {
            switch (state)
            {
                case TaxiPassengerState.Waiting:
                    UpdateWaiting();
                    break;

                case TaxiPassengerState.Boarding:
                    UpdateBoarding();
                    break;

                case TaxiPassengerState.Riding:
                    if (cab) transform.position = cab.transform.position;
                    rideTime += Time.deltaTime;
                    break;

                case TaxiPassengerState.Leaving:
                    UpdateLeaving();
                    break;
            }
        }

        void UpdateWaiting()
        {
            patience -= Time.deltaTime;

            if (icon)
            {
                icon.localRotation = Quaternion.Euler(45, Time.time * 90, 45);
                icon.localPosition = Vector3.up * (PersonHeight + 1.6f + Mathf.Sin(Time.time * 3 + transform.position.x) * 0.25f);
            }

            if (zone)
            {
                // The ring pulses, faster when the passenger is about to leave
                float hurry = 1 - Mathf.Clamp01(patience / Mathf.Max(1, patienceMax));
                float pulse = 1 + Mathf.Sin(Time.time * Mathf.Lerp(3, 10, hurry)) * 0.06f;

                zone.localScale = new Vector3(pulse, 1, pulse);

                if (zoneRing) zoneRing.sharedMaterial = TaxiMarkers.Material(hurry > 0.75f ? Color.Lerp(KindColor, TaxiColors.Hurry, Mathf.PingPong(Time.time * 3, 1)) : KindColor, 1.1f);
            }

            if (patience <= 0) GiveUp();
        }

        void UpdateBoarding()
        {
            if (cab == null)
            {
                // The taxi was destroyed on the way: back to waiting
                state = TaxiPassengerState.Waiting;

                if (zone) zone.gameObject.SetActive(true);
                if (icon) icon.gameObject.SetActive(true);

                Play("Idle");
                return;
            }

            Vector3 door = cab.transform.position + cab.transform.right * (cab.transform.InverseTransformPoint(transform.position).x >= 0 ? 1.3f : -1.3f);
            door.y = 0;

            Vector3 offset = door - transform.position;
            offset.y = 0;

            if (offset.magnitude < 0.6f || offset.magnitude > 25)
            {
                state = TaxiPassengerState.Riding;
                rideTime = 0;
                ShowPeople(false);

                cab.OnBoarded(this);
                return;
            }

            transform.position += offset.normalized * Mathf.Min(offset.magnitude, RunSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(offset);
        }

        void UpdateLeaving()
        {
            leaveTimer -= Time.deltaTime;

            if (walking)
            {
                Vector3 offset = walkTarget - transform.position;
                offset.y = 0;

                if (offset.magnitude < 0.3f)
                {
                    walking = false;

                    // At the sidewalk: cheer or be angry, facing the street
                    Play(happyEnding ? "Win" : "Fail");
                    transform.rotation = Quaternion.LookRotation(dropoff.toStreet.sqrMagnitude > 0.01f ? dropoff.toStreet : transform.forward);
                    leaveTimer = Mathf.Min(leaveTimer, 2.6f);
                }
                else
                {
                    transform.position += offset.normalized * Mathf.Min(offset.magnitude, WalkSpeed * Time.deltaTime);
                    transform.rotation = Quaternion.LookRotation(offset);
                }
            }

            // Shrink away at the end
            if (leaveTimer < 0.5f) transform.localScale = Vector3.one * Mathf.Clamp01(leaveTimer * 2);

            if (leaveTimer <= 0) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (beacon) Destroy(beacon.gameObject);
        }

        // ----- Fares -----

        /// <summary>
        /// The ride's rating from 1 to 5 stars: bumps cost stars ( more with a grumpy passenger ), a hurry passenger
        /// who arrived late takes two, perfect stops add half a star each
        /// </summary>
        public static int Rate(TaxiPassengerKind kind, int bumps, bool late, int pickupQuality, int dropQuality)
        {
            float stars = 5;

            stars -= bumps * (kind == TaxiPassengerKind.Grumpy ? 1.5f : 1);

            if (late) stars -= 2;

            if (pickupQuality == 0) stars -= 0.5f;
            if (dropQuality == 0) stars -= 0.5f;

            return Mathf.Clamp(Mathf.RoundToInt(stars), 1, 5);
        }

        /// <summary>
        /// The money a ride pays: a base plus the street distance, times the kind of passenger, plus a tip for a good
        /// rating and for perfect stops
        /// </summary>
        public static int Fare(TaxiPassengerKind kind, float streetDistance, int rating, int pickupQuality, int dropQuality, bool late)
        {
            float fare = (8 + streetDistance * 0.1f) * FareMultiplier(kind);

            float tip = 0;

            if (rating >= 4) tip += fare * (rating - 3) * 0.15f;
            if (pickupQuality == 2) tip += fare * 0.1f;
            if (dropQuality == 2) tip += fare * 0.1f;

            // The hurry passenger's tip is for being on time
            if (kind == TaxiPassengerKind.Hurry && late == false) tip += fare * 0.4f;

            if (rating <= 1) fare *= 0.6f;

            return Mathf.Max(1, Mathf.RoundToInt(fare + tip));
        }

        public bool IsLate
        {
            get { return rideLimit > 0 && rideTime > rideLimit; }
        }

        public float RideTimeLeft
        {
            get { return rideLimit > 0 ? Mathf.Max(0, rideLimit - rideTime) : float.PositiveInfinity; }
        }
    }
}
