using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// A rival taxi: an AI car that drives the streets ( along the grid, turning at the crossroads ) to the waiting
    /// passenger it wants, stops in their ring, takes them, drives them to their destination and lets them out.
    /// - it goes for the VIP whenever there is one, otherwise for the nearest passenger nobody else is going for
    /// - it stops at red lights and behind cars ( a reckless one now and then runs a red light )
    /// - the EMP power-up kills its engine for a few seconds instead of destroying it
    /// </summary>
    public class TaxiRival : TaxiCab
    {
        public const float EmpStunSeconds = 4;

        [Tooltip("How quickly the rival brakes and speeds up ( throttle per second )")]
        public float brakeRate = 2.2f;
        public float accelerateRate = 0.9f;

        [Tooltip("Chance that this rival ignores red lights")]
        public float recklessChance = 0.2f;

        internal bool reckless;
        internal float stunTime;

        TaxiPassenger targetPassenger;
        float retargetTime;
        float stuckTime;
        Vector3 lastPosition;
        float lastProgressTime;

        public override bool IsPlayer
        {
            get { return false; }
        }

        public TaxiPassenger TargetPassenger
        {
            get { return targetPassenger; }
        }

        public static TaxiRival Create(ECCCar prefab, Vector3 position, Vector3 direction, string name, Color color)
        {
            ECCCar car = Instantiate(prefab, position, Quaternion.LookRotation(direction));
            car.name = "RivalTaxi_" + name;

            TaxiRival rival = car.gameObject.AddComponent<TaxiRival>();
            rival.car = car;
            rival.displayName = name;
            rival.color = color;
            rival.seats = 1;
            rival.reckless = Random.value < rival.recklessChance;

            car.avoidObstacles = true;
            car.throttle = 1;
            car.aiTarget = rival.SteerTarget;

            TaxiMarkers.Tint(car.gameObject, color);
            TaxiMarkers.AddRoofSign(car, color);

            return rival;
        }

        public void Stun(float seconds)
        {
            stunTime = Mathf.Max(stunTime, seconds);

            if (car) car.throttle = 0;
        }

        /// <summary>
        /// The point the car steers to ( ECCCar.aiTarget )
        /// </summary>
        Vector3 SteerTarget()
        {
            Vector3 goal;

            if (car == null) return transform.position;

            if (GetGoal(out goal) == false)
            {
                // Nothing to do: cruise along the street
                return car.transform.position + TaxiCity.Snap(car.transform.forward) * 20;
            }

            return TaxiCity.NextWaypoint(car.transform.position, goal);
        }

        /// <summary>
        /// Where the rival is going: the destination of its passenger, or the ring of the passenger it wants
        /// </summary>
        bool GetGoal(out Vector3 goal)
        {
            goal = Vector3.zero;

            if (riding.Count > 0 && riding[0])
            {
                goal = riding[0].dropoff.zone;
                return true;
            }

            if (targetPassenger && targetPassenger.IsWaiting)
            {
                goal = targetPassenger.pickup.zone;
                return true;
            }

            return false;
        }

        protected override bool CanTake(TaxiPassenger passenger)
        {
            // The player's accepted ride is theirs
            return passenger == targetPassenger && passenger.accepted == false;
        }

        protected override void OnPassengerBoarded(TaxiPassenger passenger)
        {
            targetPassenger = null;
        }

        protected override void Update()
        {
            ECCGameController controller = ECCGameController.Instance;

            if (car == null || car.health <= 0 || controller == null || controller.gameStarted == false) return;

            // Rivals turn exactly towards their waypoint
            car.chaseAngle = 3;

            if (controller.isGameOver)
            {
                car.throttle = 0;
                return;
            }

            ChooseTarget();

            Drive();

            CheckStuck();

            base.Update();
        }

        void ChooseTarget()
        {
            if (riding.Count > 0 || boarding)
            {
                if (targetPassenger) targetPassenger.reservedBy = null;
                targetPassenger = null;

                return;
            }

            retargetTime -= Time.deltaTime;

            bool lost = targetPassenger == null || targetPassenger.IsWaiting == false || targetPassenger.accepted;

            if (lost == false && retargetTime > 0) return;

            retargetTime = 2;

            TaxiPassenger best = TaxiSession.Instance ? TaxiSession.Instance.BestPassengerFor(this) : null;

            if (best != targetPassenger)
            {
                if (targetPassenger && targetPassenger.reservedBy == this) targetPassenger.reservedBy = null;

                targetPassenger = best;

                if (best && best.reservedBy == null) best.reservedBy = this;
            }
        }

        void Drive()
        {
            float wanted = 1;

            if (stunTime > 0)
            {
                stunTime -= Time.deltaTime;
                wanted = 0;
            }
            else if (boarding)
            {
                wanted = 0;
            }
            else
            {
                Vector3 position = car.transform.position;
                Vector3 goal;

                if (GetGoal(out goal))
                {
                    float distance = ECCRuntimeVisuals.FlatDistance(position, goal);

                    // Brake into the ring: slower and slower, standing still in the middle
                    if (distance < 16) wanted = Mathf.Clamp01((distance - 0.6f) / 16) * 0.9f;

                    if (distance < 1.2f) wanted = 0;

                    // Just passed it: keep rolling to turn around ( a car can't turn standing still )
                    Vector3 toGoal = goal - position;
                    toGoal.y = 0;

                    if (distance < 16 && distance > 2 && Vector3.Dot(toGoal, car.transform.forward) < 0) wanted = Mathf.Max(wanted, 0.45f);

                    // Slow down for the turns
                    Vector3 waypoint = TaxiCity.NextWaypoint(position, goal);

                    if (waypoint != goal && ECCRuntimeVisuals.FlatDistance(position, waypoint) < 13) wanted = Mathf.Min(wanted, 0.55f);
                }
                else
                {
                    wanted = 0.6f;
                }

                float lineDistance;

                if (reckless == false && TaxiTrafficLights.Instance && TaxiTrafficLights.Instance.MustStopAhead(position, car.transform.forward, 4 + car.CurrentSpeed * 0.5f, out lineDistance)) wanted = 0;

                if (TrafficAhead(car)) wanted = 0;
            }

            car.throttle = Mathf.MoveTowards(car.throttle, wanted, (wanted < car.throttle ? brakeRate : accelerateRate) * Time.deltaTime);
        }

        /// <summary>
        /// A traffic car close ahead in the lane of an AI car
        /// </summary>
        public static bool TrafficAhead(ECCCar car)
        {
            ECCTraffic traffic = ECCTraffic.Instance;

            if (traffic == null || car == null) return false;

            Vector3 position = car.transform.position;

            Vector3 forward = car.transform.forward;
            Vector3 right = TaxiCity.Right(forward);

            float gap = 5 + car.CurrentSpeed * 0.35f;

            for (int index = 0; index < traffic.CarCount; index++)
            {
                Transform other = traffic.GetCar(index);

                if (other == null) continue;

                Vector3 offset = other.position - position;
                offset.y = 0;

                float ahead = Vector3.Dot(offset, forward);

                if (ahead > 0 && ahead < gap && Mathf.Abs(Vector3.Dot(offset, right)) < 2) return true;
            }

            return false;
        }

        /// <summary>
        /// A rival that is stuck ( against a wall, in a jam ) for a long time while out of view is put back on the street
        /// </summary>
        void CheckStuck()
        {
            if (car.throttle < 0.3f || boarding)
            {
                lastProgressTime = Time.time;
                lastPosition = car.transform.position;
                return;
            }

            if (ECCRuntimeVisuals.FlatDistance(lastPosition, car.transform.position) > 3)
            {
                lastPosition = car.transform.position;
                lastProgressTime = Time.time;
                return;
            }

            stuckTime = Time.time - lastProgressTime;

            if (stuckTime < 5) return;

            Camera view = Camera.main;

            if (view && ECCRuntimeVisuals.IsInView(view, car.transform.position)) return;

            TaxiSpot spot = TaxiCity.SpotNear(car.transform.position, true);

            car.transform.position = spot.zone + Vector3.up * car.transform.position.y;
            car.transform.rotation = Quaternion.LookRotation(spot.streetDirection);

            lastProgressTime = Time.time;
        }
    }
}
