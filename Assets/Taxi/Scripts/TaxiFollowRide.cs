using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The rules of following a car, without Unity: too close for too long and the driver sees the taxi ( a star off,
    /// and the car speeds away for a while ); too far for too long and the car is lost
    /// </summary>
    public class TaxiFollowTracker
    {
        public enum Result { None, Spotted, Lost }

        // Closer than this for SpotTime seconds: seen
        public float spotDistance = 10;
        public float spotTime = 1.6f;

        // Seconds after being seen before it can happen again
        public float spotCooldown = 4;

        // Farther than this for LoseTime seconds: lost
        public float loseDistance = 70;
        public float loseTime = 5;

        // Farther than this: the HUD warns
        public float warnDistance = 48;

        public float CloseTime { get; private set; }
        public float FarTime { get; private set; }
        public int TimesSpotted { get; private set; }
        public bool IsLost { get; private set; }

        float cooldown;

        public bool IsTooClose(float distance)
        {
            return distance < spotDistance;
        }

        public bool IsTooFar(float distance)
        {
            return distance > warnDistance;
        }

        /// <summary>
        /// Seconds left before the car is lost, while too far
        /// </summary>
        public float LoseTimeLeft
        {
            get { return Mathf.Max(0, loseTime - FarTime); }
        }

        public Result Step(float deltaTime, float distance)
        {
            if (IsLost) return Result.None;

            cooldown = Mathf.Max(0, cooldown - deltaTime);

            CloseTime = distance < spotDistance && cooldown <= 0 ? CloseTime + deltaTime : 0;
            FarTime = distance > loseDistance ? FarTime + deltaTime : 0;

            if (FarTime >= loseTime)
            {
                IsLost = true;
                return Result.Lost;
            }

            if (CloseTime >= spotTime)
            {
                CloseTime = 0;
                cooldown = spotCooldown;
                TimesSpotted++;

                return Result.Spotted;
            }

            return Result.None;
        }
    }

    /// <summary>
    /// «دنبال اون ماشین برو!»: a follow passenger got in. A dark car starts ahead of the taxi and drives a few legs
    /// through the city ( stopping at the lights ); the guide arrow points at it. Stay behind it: too close and its
    /// driver sees the taxi ( a star off, it speeds away ), too far for too long and it's lost ( the passenger gets
    /// out close by and pays half ). When it parks, the destination ring appears behind it
    /// </summary>
    public class TaxiFollowRide : MonoBehaviour
    {
        enum Phase { Leading, Parked, Leaving }

        public static readonly List<TaxiFollowRide> All = new List<TaxiFollowRide>();

        [Tooltip("How many places the car drives to before it parks")]
        public int legs = 3;

        [Tooltip("Throttle of the car while followed, and after it saw the taxi")]
        public float cruiseThrottle = 0.74f;
        public float fleeThrottle = 1;
        public float fleeSeconds = 4;

        public TaxiFollowTracker tracker = new TaxiFollowTracker();

        public ECCCar car;
        public TaxiPassenger passenger;
        public TaxiCab taxi;

        Phase phase = Phase.Leading;
        TaxiSpot goal;
        int legsLeft;
        float fleeTime;
        float leaveTime;
        float stuckTime;
        Vector3 lastPosition;

        public bool IsLeading
        {
            get { return phase == Phase.Leading; }
        }

        public float Distance
        {
            get { return car && taxi ? ECCRuntimeVisuals.FlatDistance(car.transform.position, taxi.transform.position) : 0; }
        }

        public Vector3 Position
        {
            get { return car ? car.transform.position : (passenger ? passenger.dropoff.zone : Vector3.zero); }
        }

        /// <summary>
        /// The follow ride of a passenger on board, or null
        /// </summary>
        public static TaxiFollowRide For(TaxiPassenger passenger)
        {
            foreach (TaxiFollowRide ride in All)
            {
                if (ride && ride.passenger == passenger) return ride;
            }

            return null;
        }

        public static TaxiFollowRide Begin(TaxiSession session, TaxiCab taxi, TaxiPassenger passenger)
        {
            ECCCar prefab = session ? session.RivalPrefab : null;

            if (prefab == null || taxi == null || taxi.car == null || TaxiCity.IsActive == false)
            {
                // Nothing to follow: an ordinary ride
                passenger.dropoffHidden = false;
                passenger.ShowDestination();
                return null;
            }

            // Ahead of the taxi in its lane
            Vector3 direction = TaxiCity.Snap(taxi.car.transform.forward);
            Vector3 point = taxi.car.transform.position + direction * 26;

            float spacing = TaxiCity.Spacing;
            Vector3 line = point;

            if (direction.z != 0) line.x = Mathf.Round(line.x / spacing) * spacing;
            else line.z = Mathf.Round(line.z / spacing) * spacing;

            Vector3 position = line + TaxiCity.Right(direction) * TaxiCity.LaneOffset;
            position.y = taxi.car.transform.position.y;

            ECCCar car = Instantiate(prefab, position, Quaternion.LookRotation(direction));
            car.name = "FollowedCar";

            TaxiFollowRide ride = car.gameObject.AddComponent<TaxiFollowRide>();
            ride.car = car;
            ride.taxi = taxi;
            ride.passenger = passenger;
            ride.legsLeft = ride.legs;
            ride.lastPosition = position;
            ride.NextLeg();

            car.avoidObstacles = true;
            car.throttle = ride.cruiseThrottle;
            car.aiTarget = ride.SteerTarget;

            TaxiMarkers.Tint(car.gameObject, TaxiColors.Suspect);

            ECCHintBanner.Show("Icons/target", TaxiColors.Follow, "دنبال اون ماشین برو!", "پشتش بمون؛ زیادی نزدیک بشی می‌بینتت، دور بشی گمش می‌کنی", 3, ECCHintBanner.Kind.Urgent);

            return ride;
        }

        void OnEnable()
        {
            All.Add(this);
        }

        void OnDisable()
        {
            All.Remove(this);
        }

        void NextLeg()
        {
            Vector3 from = car ? car.transform.position : transform.position;

            goal = TaxiCity.RandomSpot(from, 110, 170);
        }

        Vector3 SteerTarget()
        {
            if (car == null) return transform.position;

            if (phase == Phase.Parked) return car.transform.position + car.transform.forward * 5;

            if (phase == Phase.Leaving) return car.transform.position + TaxiCity.Snap(car.transform.forward) * 20;

            return TaxiCity.NextWaypoint(car.transform.position, goal.zone);
        }

        void Update()
        {
            ECCGameController controller = ECCGameController.Instance;

            if (car == null) return;

            car.chaseAngle = 3;

            bool ridingNow = passenger && passenger.state == TaxiPassengerState.Riding && passenger.cab == taxi && taxi;

            if (controller == null || controller.isGameOver || car.health <= 0)
            {
                car.throttle = 0;

                if (car.health > 0) return;

                // Wrecked while followed: it stops here, and the wreck is cleared once the ride is over
                if (phase == Phase.Leading) Park();
                else if (ridingNow == false)
                {
                    leaveTime += Time.deltaTime;

                    if (leaveTime > 8) Destroy(car.gameObject);
                }

                return;
            }

            if (phase == Phase.Leading && ridingNow == false)
            {
                // The ride ended some other way ( the taxi was wrecked )
                Leave();
            }

            switch (phase)
            {
                case Phase.Leading:
                    Lead();
                    break;

                case Phase.Parked:
                    car.throttle = Mathf.MoveTowards(car.throttle, 0, 2.5f * Time.deltaTime);

                    // Once the passenger is out, the car drives off
                    if (ridingNow == false) Leave();
                    break;

                case Phase.Leaving:
                    car.throttle = Mathf.MoveTowards(car.throttle, 0.8f, Time.deltaTime);

                    leaveTime += Time.deltaTime;

                    Camera view = Camera.main;

                    if (leaveTime > 15 || (leaveTime > 3 && (view == null || ECCRuntimeVisuals.IsInView(view, car.transform.position) == false))) Destroy(car.gameObject);
                    break;
            }
        }

        void Lead()
        {
            Vector3 position = car.transform.position;

            switch (tracker.Step(Time.deltaTime, Distance))
            {
                case TaxiFollowTracker.Result.Spotted:
                    fleeTime = fleeSeconds;

                    if (passenger) passenger.bumps++;

                    ECCHintBanner.Show("Icons/target", ECCRuntimeUI.BadColor, "دیدتت!", "زیادی نزدیک شدی؛ داره در می‌ره، فاصله بگیر ولی گمش نکن", 2.2f, ECCHintBanner.Kind.Urgent);
                    ECCHaptics.Vibrate(ECCHaptics.Kind.Medium);
                    break;

                case TaxiFollowTracker.Result.Lost:
                    Lost();
                    return;
            }

            if (ECCRuntimeVisuals.FlatDistance(position, goal.zone) < 5)
            {
                legsLeft--;

                if (legsLeft <= 0)
                {
                    Park();
                    return;
                }

                NextLeg();
            }

            float wanted = fleeTime > 0 ? fleeThrottle : cruiseThrottle;

            fleeTime -= Time.deltaTime;

            float distance = ECCRuntimeVisuals.FlatDistance(position, goal.zone);

            // Slow down into the last stop and for the turns
            if (legsLeft == 1 && distance < 16) wanted = Mathf.Min(wanted, Mathf.Clamp01(distance / 16) * 0.8f + 0.1f);

            Vector3 waypoint = TaxiCity.NextWaypoint(position, goal.zone);

            if (waypoint != goal.zone && ECCRuntimeVisuals.FlatDistance(position, waypoint) < 13) wanted = Mathf.Min(wanted, 0.55f);

            float lineDistance;

            if (TaxiTrafficLights.Instance && TaxiTrafficLights.Instance.MustStopAhead(position, car.transform.forward, 4 + car.CurrentSpeed * 0.5f, out lineDistance)) wanted = 0;

            if (TaxiRival.TrafficAhead(car)) wanted = 0;

            car.throttle = Mathf.MoveTowards(car.throttle, wanted, (wanted < car.throttle ? 2.2f : 0.9f) * Time.deltaTime);

            CheckStuck(wanted);
        }

        /// <summary>
        /// Stuck against something out of view for a while: put back on the street
        /// </summary>
        void CheckStuck(float wanted)
        {
            if (wanted < 0.3f || ECCRuntimeVisuals.FlatDistance(lastPosition, car.transform.position) > 3)
            {
                stuckTime = 0;
                lastPosition = car.transform.position;
                return;
            }

            stuckTime += Time.deltaTime;

            Camera view = Camera.main;

            if (stuckTime < 5 || (view && ECCRuntimeVisuals.IsInView(view, car.transform.position))) return;

            TaxiSpot spot = TaxiCity.SpotNear(car.transform.position, true);

            car.transform.position = spot.zone + Vector3.up * car.transform.position.y;
            car.transform.rotation = Quaternion.LookRotation(spot.streetDirection);

            stuckTime = 0;
        }

        /// <summary>
        /// The car stops: the passenger's destination is the curb right behind it
        /// </summary>
        void Park()
        {
            phase = Phase.Parked;

            if (passenger == null) return;

            Vector3 position = car ? car.transform.position : goal.zone;
            Vector3 forward = car ? TaxiCity.Snap(car.transform.forward) : goal.streetDirection;

            bool side = SideOf(goal);

            TaxiSpot behind = TaxiCity.SpotNear(position - forward * 11, side);

            if (ECCRuntimeVisuals.FlatDistance(behind.zone, position) < 6) behind = TaxiCity.SpotNear(position + forward * 11, side);

            passenger.dropoff = behind;
            passenger.dropoffHidden = false;
            passenger.ShowDestination();

            ECCHintBanner.Show("Icons/check", ECCRuntimeUI.GoodColor, "ماشین وایساد!", "پشتش توی دایره نگه دار تا مسافر پیاده بشه", 2.6f, ECCHintBanner.Kind.Urgent);
        }

        /// <summary>
        /// Which sidewalk a spot is on ( SpotNear's rightSide: the positive side of the street line )
        /// </summary>
        static bool SideOf(TaxiSpot spot)
        {
            float spacing = TaxiCity.Spacing;

            if (spot.streetDirection.z != 0) return spot.zone.x > Mathf.Round(spot.zone.x / spacing) * spacing;

            return spot.zone.z > Mathf.Round(spot.zone.z / spacing) * spacing;
        }

        /// <summary>
        /// The car got away: the passenger gets out at the next curb, for half the fare
        /// </summary>
        void Lost()
        {
            if (passenger)
            {
                passenger.followLost = true;
                passenger.bumps += 2;

                Vector3 from = taxi ? taxi.transform.position + taxi.transform.forward * 18 : Position;

                passenger.dropoff = TaxiCity.SpotNear(from, Random.value < 0.5f);
                passenger.dropoffHidden = false;
                passenger.ShowDestination();
            }

            ECCHintBanner.Show("Icons/lock", ECCRuntimeUI.BadColor, "گمش کردی!", "مسافر همین نزدیکی پیاده می‌شه و نصف کرایه رو می‌ده", 2.6f, ECCHintBanner.Kind.Urgent);

            Leave();
        }

        void Leave()
        {
            if (phase == Phase.Leaving) return;

            phase = Phase.Leaving;
            leaveTime = 0;
        }
    }
}
