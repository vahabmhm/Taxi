using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// «سفر اشتراکی»: two passengers on board whose destinations lie the same way. Driving both together must be much
    /// shorter than driving each one alone ( from the taxi, the better of the two orders against the two separate
    /// trips ); then both pay a bonus. A follow ride is never shared
    /// </summary>
    public static class TaxiSharedRide
    {
        // Together must be at most this part of the two separate trips
        public const float MaxRatio = 0.75f;

        // Every passenger of a shared ride pays this much more
        public const float FareBonus = 1.25f;

        /// <summary>
        /// Are two destinations the same way from here?
        /// </summary>
        public static bool AreCompatible(Vector3 from, Vector3 dropA, Vector3 dropB)
        {
            float toA = TaxiCity.StreetDistance(from, dropA);
            float toB = TaxiCity.StreetDistance(from, dropB);
            float between = TaxiCity.StreetDistance(dropA, dropB);

            float separate = toA + toB;

            if (separate < 1) return false;

            float together = Mathf.Min(toA, toB) + between;

            return together <= separate * MaxRatio;
        }

        static bool CanShare(TaxiPassenger passenger)
        {
            return passenger && passenger.kind != TaxiPassengerKind.Follow && passenger.dropoffHidden == false;
        }

        /// <summary>
        /// Would this waiting passenger make a shared ride with someone already in the taxi?
        /// </summary>
        public static bool FitsRiding(TaxiCab cab, TaxiPassenger waiting)
        {
            if (cab == null || cab.car == null || CanShare(waiting) == false || waiting.Seats > cab.FreeSeats) return false;

            Vector3 from = waiting.pickup.zone;

            foreach (TaxiPassenger riding in cab.riding)
            {
                if (CanShare(riding) && AreCompatible(from, riding.dropoff.zone, waiting.dropoff.zone)) return true;
            }

            return false;
        }

        /// <summary>
        /// A passenger just got in: marks them and a fellow passenger going the same way as shared. True when it made a
        /// new shared ride
        /// </summary>
        public static bool OnBoarded(TaxiCab cab, TaxiPassenger passenger)
        {
            if (cab == null || cab.car == null || CanShare(passenger) == false) return false;

            Vector3 from = cab.car.transform.position;
            bool made = false;

            foreach (TaxiPassenger riding in cab.riding)
            {
                if (riding == passenger || CanShare(riding) == false) continue;

                if (AreCompatible(from, riding.dropoff.zone, passenger.dropoff.zone))
                {
                    riding.shared = true;
                    passenger.shared = true;
                    made = true;
                }
            }

            return made;
        }
    }
}
