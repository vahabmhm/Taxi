using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// What every taxi does, the player's and the rivals': a taxi that stops ( brakes to almost standing still ) in
    /// the ring next to a waiting passenger takes them on board, and one that stops in the ring of a passenger's
    /// destination lets them out and gets the fare. The closer to the middle of the ring it stops, the better
    /// </summary>
    public abstract class TaxiCab : MonoBehaviour
    {
        // The ring the car's center must be in
        public const float ZoneRadius = 3.4f;

        // Slower than this counts as stopped
        public const float StopSpeed = 1.4f;

        // How long the car must stand in the ring
        public const float PickupHold = 0.45f;
        public const float DropHold = 0.35f;

        // Within these distances from the middle a stop is perfect / good
        public const float PerfectDistance = 1.1f;
        public const float GoodDistance = 2.3f;

        // Seconds between two bumps that count for the passengers
        const float BumpCooldown = 1;

        public ECCCar car;
        public string displayName = "تاکسی";
        public Color color = TaxiColors.PlayerYellow;
        public int seats = 1;

        public readonly List<TaxiPassenger> riding = new List<TaxiPassenger>();

        public int delivered;
        public int earned;

        protected TaxiPassenger boarding;
        protected float stoppedTime;
        float lastBump = -10;

        public static readonly List<TaxiCab> All = new List<TaxiCab>();

        public abstract bool IsPlayer { get; }

        public int UsedSeats
        {
            get
            {
                int used = boarding ? boarding.Seats : 0;

                foreach (TaxiPassenger passenger in riding)
                {
                    if (passenger) used += passenger.Seats;
                }

                return used;
            }
        }

        public int FreeSeats
        {
            get { return seats - UsedSeats; }
        }

        public bool IsBoarding
        {
            get { return boarding != null; }
        }

        public bool IsStopped
        {
            get { return car && car.CurrentSpeed < StopSpeed; }
        }

        public float StoppedTime
        {
            get { return stoppedTime; }
        }

        /// <summary>
        /// How far from the middle of a ring the car may stop
        /// </summary>
        public virtual float ZoneReach
        {
            get { return ZoneRadius; }
        }

        public static int StopQuality(float distance)
        {
            return distance <= PerfectDistance ? 2 : (distance <= GoodDistance ? 1 : 0);
        }

        protected virtual void OnEnable()
        {
            All.Add(this);
        }

        protected virtual void OnDisable()
        {
            All.Remove(this);
        }

        protected virtual void Awake()
        {
            if (car == null) car = GetComponent<ECCCar>();
        }

        protected virtual void Update()
        {
            if (car == null || car.health <= 0) return;

            stoppedTime = IsStopped ? stoppedTime + Time.deltaTime : 0;

            riding.RemoveAll(passenger => passenger == null);

            // Wait until the passenger is in
            if (boarding) return;

            if (stoppedTime >= DropHold && TryDropOff()) return;

            if (stoppedTime >= PickupHold) TryPickUp();
        }

        bool TryDropOff()
        {
            Vector3 position = car.transform.position;

            foreach (TaxiPassenger passenger in riding)
            {
                // A follow ride has no destination until the followed car stops
                if (passenger.dropoffHidden) continue;

                float distance = ECCRuntimeVisuals.FlatDistance(position, passenger.dropoff.zone);

                if (distance > ZoneReach) continue;

                Deliver(passenger, StopQuality(distance));

                return true;
            }

            return false;
        }

        void TryPickUp()
        {
            TaxiPassenger passenger = WaitingPassengerInReach();

            if (passenger == null || passenger.Seats > FreeSeats || CanTake(passenger) == false) return;

            float distance = ECCRuntimeVisuals.FlatDistance(car.transform.position, passenger.pickup.zone);

            if (passenger.Board(this, StopQuality(distance)))
            {
                boarding = passenger;

                OnBoardingStarted(passenger);
            }
        }

        /// <summary>
        /// The waiting passenger whose ring the car is in, or null
        /// </summary>
        public TaxiPassenger WaitingPassengerInReach()
        {
            if (car == null) return null;

            Vector3 position = car.transform.position;

            TaxiPassenger best = null;
            float bestDistance = ZoneReach;

            foreach (TaxiPassenger passenger in TaxiPassenger.All)
            {
                if (passenger == null || passenger.IsWaiting == false) continue;

                float distance = ECCRuntimeVisuals.FlatDistance(position, passenger.pickup.zone);

                if (distance <= bestDistance)
                {
                    best = passenger;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>
        /// Rules of a mode can forbid some passengers ( a rival only takes the one it's going for )
        /// </summary>
        protected virtual bool CanTake(TaxiPassenger passenger)
        {
            return true;
        }

        protected virtual void OnBoardingStarted(TaxiPassenger passenger) { }

        protected virtual void OnPassengerBoarded(TaxiPassenger passenger) { }

        protected virtual void OnPassengerDelivered(TaxiPassenger passenger, int fare, int rating) { }

        /// <summary>
        /// The passenger is in the car ( called by the passenger )
        /// </summary>
        public void OnBoarded(TaxiPassenger passenger)
        {
            if (boarding == passenger) boarding = null;

            if (riding.Contains(passenger) == false) riding.Add(passenger);

            OnPassengerBoarded(passenger);

            if (TaxiSession.Instance) TaxiSession.Instance.NotifyPickedUp(this, passenger);
        }

        void Deliver(TaxiPassenger passenger, int quality)
        {
            riding.Remove(passenger);

            bool late = passenger.IsLate;

            // A wish still open is settled now: the hurry and the gentle ride were kept, the stop was skipped
            bool wishOpen = passenger.wish != null && passenger.wish.IsActive;

            if (passenger.wish != null) passenger.wish.Arrive();

            if (wishOpen && IsPlayer) TaxiWishes.Announce(passenger.wish);

            int rating = Mathf.Clamp(TaxiPassenger.Rate(passenger.kind, passenger.bumps, late, passenger.pickupQuality, quality) + passenger.WishStars, 1, 5);
            int fare = TaxiPassenger.Fare(passenger.kind, TaxiCity.StreetDistance(passenger.pickup.zone, passenger.dropoff.zone), rating, passenger.pickupQuality, quality, late);

            if (TaxiSession.Instance) fare = Mathf.Max(1, Mathf.RoundToInt(fare * TaxiSession.Instance.FareFor(this, passenger)));

            passenger.GetOut(rating >= 3);

            delivered++;
            earned += fare;

            OnPassengerDelivered(passenger, fare, rating);

            if (TaxiSession.Instance) TaxiSession.Instance.NotifyDelivered(this, passenger, fare, rating, quality);
        }

        /// <summary>
        /// The car was hit or crashed: the passengers on board feel it
        /// </summary>
        public void Bump()
        {
            if (Time.time - lastBump < BumpCooldown || riding.Count == 0) return;

            lastBump = Time.time;

            foreach (TaxiPassenger passenger in riding)
            {
                if (passenger) passenger.bumps++;
            }

            OnBumped();
        }

        protected virtual void OnBumped() { }

        /// <summary>
        /// Everybody out ( the car was wrecked )
        /// </summary>
        public void EjectAll()
        {
            if (boarding) boarding.cab = null;
            boarding = null;

            foreach (TaxiPassenger passenger in riding)
            {
                if (passenger == null) continue;

                passenger.dropoff = passenger.pickup;
                passenger.dropoff.stand = TaxiCity.SpotNear(transform.position, true).stand;
                passenger.GetOut(false);

                if (TaxiSession.Instance) TaxiSession.Instance.NotifyLost(passenger);
            }

            riding.Clear();
        }

        protected virtual void OnDestroy()
        {
            EjectAll();
        }
    }
}
