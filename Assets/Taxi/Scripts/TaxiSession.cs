using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The rules of one taxi run, set by the game mode
    /// </summary>
    [Serializable]
    public class TaxiRules
    {
        [Tooltip("How many passengers wait around the player at the same time")]
        public int waitingPassengers = 6;

        [Tooltip("How many rival taxis drive in the city")]
        public int rivals = 2;

        [Tooltip("Chance weights of the kinds of passengers: normal, hurry, VIP, tourist, family, grumpy")]
        public float[] kindWeights = { 10, 2, 0, 2, 1, 1.5f };

        [Tooltip("A VIP passenger appears every this many seconds ( 0: never by itself )")]
        public float vipInterval = 0;

        [Tooltip("Breaking the traffic rules brings the police")]
        public bool police = true;

        [Tooltip("All the fares are multiplied by this")]
        public float fareMultiplier = 1;

        [Tooltip("Passengers whose ride is cut short ( a wrecked taxi ) end the level")]
        public bool failOnLostPassenger = false;

        public TaxiRules Clone()
        {
            TaxiRules copy = (TaxiRules)MemberwiseClone();
            copy.kindWeights = (float[])kindWeights.Clone();
            return copy;
        }
    }

    /// <summary>
    /// One taxi run: the passengers waiting around the player, the rival taxis, the traffic lights and the police.
    /// Adds the player's fares to the run's money ( the game controller's score, banked into the wallet at the end ),
    /// keeps the counts for the HUD and the game modes, and tells them what happened through its events
    /// </summary>
    public class TaxiSession : MonoBehaviour
    {
        public static TaxiSession Instance { get; private set; }

        public TaxiRules rules = new TaxiRules();

        public TaxiDriver Player { get; private set; }

        public readonly List<TaxiRival> Rivals = new List<TaxiRival>();

        // ----- The player's run -----

        public int PlayerDeliveries { get; private set; }
        public int PlayerEarned { get; private set; }
        public int PerfectStops { get; private set; }
        public int LostPassengers { get; private set; }
        public int RatingSum { get; private set; }

        // Deliveries in a row with 4 or 5 stars
        public int Streak { get; private set; }

        // Taken by a rival from the player ( the VIP, or one the player was going for )
        public int Stolen { get; private set; }

        public float AverageRating
        {
            get { return PlayerDeliveries > 0 ? RatingSum / (float)PlayerDeliveries : 0; }
        }

        public int StreakMultiplier
        {
            get { return Streak >= 6 ? 3 : (Streak >= 3 ? 2 : 1); }
        }

        public float FareMultiplier
        {
            get { return rules.fareMultiplier; }
        }

        // ----- Events -----

        public static event Action<TaxiCab, TaxiPassenger> PickedUp;

        // The cab, the passenger, the fare, the rating ( 1-5 ), the drop-off stop quality ( 0-2 )
        public static event Action<TaxiCab, TaxiPassenger, int, int, int> Delivered;

        public static event Action<TaxiPassenger> Lost;

        public static event Action<TaxiPassenger> VipAppeared;

        ECCGameController gameController;
        ECCCar rivalPrefab;
        float vipTimer;
        float spawnTimer;
        float rivalTimer;

        static readonly string[] RivalNames = { "تاکسی سبز", "تاکسی آبی", "تاکسی قرمز", "تاکسی بنفش", "تاکسی نارنجی" };

        public static TaxiSession Begin(ECCGameController controller, TaxiRules rules, string carName)
        {
            if (Instance) return Instance;

            TaxiSession session = controller.gameObject.AddComponent<TaxiSession>();
            session.gameController = controller;
            session.rules = rules != null ? rules.Clone() : new TaxiRules();

            Instance = session;

            // The police of the chase game don't come by themselves any more
            TaxiPolice.TakeChasePolice(controller);

            TaxiTrafficLights.Enable(controller);

            if (controller.playerObject) session.Player = TaxiDriver.Attach(controller.playerObject, carName);

            if (session.rules.police) TaxiPolice.Enable(controller);

            session.rivalPrefab = FindRivalPrefab(controller);
            session.vipTimer = session.rules.vipInterval;

            TaxiHUD.Create(controller, session);

            // A few passengers close by right away, so the first one is easy to find
            for (int index = 0; index < 2; index++) session.SpawnWaiting(18, 38, PickKind(session.rules.kindWeights, false));

            return session;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// The Kenney taxi from the garage, or else any car of the garage, for the rivals
        /// </summary>
        static ECCCar FindRivalPrefab(ECCGameController controller)
        {
            ECCShop shop = controller.shopMenu;

            ECCCar fallback = null;

            if (shop && shop.items != null)
            {
                foreach (Types.ShopItem item in shop.items)
                {
                    ECCCar car = item != null && item.itemIcon ? item.itemIcon.GetComponent<ECCCar>() : null;

                    if (car == null) continue;

                    if (car.name.IndexOf("Taxi", StringComparison.OrdinalIgnoreCase) >= 0) return car;

                    if (fallback == null && car.name.IndexOf("Sedan", StringComparison.OrdinalIgnoreCase) >= 0) fallback = car;
                }

                if (fallback == null && shop.items.Length > 0 && shop.items[0].itemIcon) fallback = shop.items[0].itemIcon.GetComponent<ECCCar>();
            }

            return fallback;
        }

        bool IsPlaying
        {
            get { return gameController && gameController.gameStarted && gameController.isGameOver == false && gameController.playerObject; }
        }

        void Update()
        {
            if (IsPlaying == false) return;

            Vector3 center = gameController.playerObject.transform.position;

            RemoveFarPassengers(center);

            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0 && WaitingCount < rules.waitingPassengers)
            {
                spawnTimer = 0.8f;

                SpawnWaiting(30, 95, PickKind(rules.kindWeights, Player && Player.seats >= 3));
            }

            if (rules.vipInterval > 0)
            {
                vipTimer -= Time.deltaTime;

                if (vipTimer <= 0)
                {
                    vipTimer = rules.vipInterval;
                    SpawnVip();
                }
            }

            rivalTimer -= Time.deltaTime;

            if (rivalTimer <= 0)
            {
                rivalTimer = 1.5f;
                KeepRivals(center);
            }
        }

        // ----- Passengers -----

        public int WaitingCount
        {
            get
            {
                int count = 0;

                foreach (TaxiPassenger passenger in TaxiPassenger.All)
                {
                    if (passenger && passenger.IsWaiting) count++;
                }

                return count;
            }
        }

        static TaxiPassengerKind PickKind(float[] weights, bool familyFits)
        {
            float total = 0;

            for (int index = 0; index < weights.Length; index++)
            {
                if (index == (int)TaxiPassengerKind.Family && familyFits == false) continue;

                total += Mathf.Max(0, weights[index]);
            }

            float roll = UnityEngine.Random.value * total;

            for (int index = 0; index < weights.Length; index++)
            {
                if (index == (int)TaxiPassengerKind.Family && familyFits == false) continue;

                roll -= Mathf.Max(0, weights[index]);

                if (roll <= 0) return (TaxiPassengerKind)index;
            }

            return TaxiPassengerKind.Normal;
        }

        /// <summary>
        /// A new waiting passenger on a sidewalk around the player ( never on top of another one )
        /// </summary>
        public TaxiPassenger SpawnWaiting(float minDistance, float maxDistance, TaxiPassengerKind kind)
        {
            if (gameController == null || gameController.playerObject == null || TaxiCity.IsActive == false) return null;

            Vector3 center = gameController.playerObject.transform.position;

            for (int attempt = 0; attempt < 8; attempt++)
            {
                TaxiSpot spot = TaxiCity.RandomSpot(center, minDistance, maxDistance);

                if (IsFree(spot.zone) == false) continue;

                Vector2 ride = TaxiPassenger.RideDistance(kind);
                TaxiSpot destination = TaxiCity.RandomSpot(spot.zone, ride.x * 0.7f, ride.y * 0.7f);

                return TaxiPassenger.Create(kind, spot, destination);
            }

            return null;
        }

        static bool IsFree(Vector3 zone)
        {
            foreach (TaxiPassenger passenger in TaxiPassenger.All)
            {
                if (passenger && ECCRuntimeVisuals.FlatDistance(passenger.pickup.zone, zone) < 12) return false;
            }

            return true;
        }

        /// <summary>
        /// A VIP appears near the player, and every rival goes for them
        /// </summary>
        public TaxiPassenger SpawnVip()
        {
            TaxiPassenger vip = SpawnWaiting(40, 80, TaxiPassengerKind.Vip);

            if (vip && VipAppeared != null) VipAppeared(vip);

            return vip;
        }

        void RemoveFarPassengers(Vector3 center)
        {
            for (int index = TaxiPassenger.All.Count - 1; index >= 0; index--)
            {
                TaxiPassenger passenger = TaxiPassenger.All[index];

                if (passenger && passenger.IsWaiting && passenger.kind != TaxiPassengerKind.Vip && ECCRuntimeVisuals.FlatDistance(passenger.transform.position, center) > 160) Destroy(passenger.gameObject);
            }
        }

        /// <summary>
        /// The passenger a taxi should go for: a VIP first; then the nearest one that fits, preferring the ones no
        /// rival is already going for. Nobody farther than 140 meters
        /// </summary>
        public TaxiPassenger BestPassengerFor(TaxiCab cab)
        {
            if (cab == null || cab.car == null) return null;

            Vector3 position = cab.car.transform.position;

            TaxiPassenger best = null;
            float bestScore = float.MaxValue;

            foreach (TaxiPassenger passenger in TaxiPassenger.All)
            {
                if (passenger == null || passenger.IsWaiting == false || passenger.Seats > cab.FreeSeats) continue;

                float distance = TaxiCity.StreetDistance(position, passenger.pickup.zone);

                if (distance > 140 && passenger.kind != TaxiPassengerKind.Vip) continue;

                float score = distance;

                if (passenger.kind == TaxiPassengerKind.Vip) score -= 1000;

                if (passenger.reservedBy && passenger.reservedBy != cab) score += cab.IsPlayer ? 0 : 60;

                // Behind the car costs a turn around
                Vector3 offset = passenger.pickup.zone - position;
                if (Vector3.Dot(offset, cab.car.transform.forward) < 0) score += 25;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = passenger;
                }
            }

            return best;
        }

        // ----- Rivals -----

        public void SetRivalCount(int count)
        {
            rules.rivals = Mathf.Clamp(count, 0, RivalNames.Length);
        }

        void KeepRivals(Vector3 center)
        {
            Rivals.RemoveAll(rival => rival == null || rival.car == null || rival.car.health <= 0);

            // Rivals that are far away come back around the player
            foreach (TaxiRival rival in Rivals)
            {
                if (rival.riding.Count == 0 && ECCRuntimeVisuals.FlatDistance(rival.transform.position, center) > 190)
                {
                    PlaceRival(rival.car, center);
                }
            }

            if (rivalPrefab == null || Rivals.Count >= rules.rivals) return;

            int slot = 0;

            while (slot < RivalNames.Length && Rivals.Exists(rival => rival.displayName == RivalNames[slot])) slot++;

            if (slot >= RivalNames.Length) return;

            Vector3 position;
            Vector3 direction;

            OutOfViewStreet(center, out position, out direction);

            TaxiRival created = TaxiRival.Create(rivalPrefab, position, direction, RivalNames[slot], TaxiColors.Rivals[slot % TaxiColors.Rivals.Length]);

            // Keep the score of a rival that was wrecked
            int kept;

            if (rivalScores.TryGetValue(created.displayName, out kept)) created.delivered = kept;

            Rivals.Add(created);
        }

        readonly Dictionary<string, int> rivalScores = new Dictionary<string, int>();

        void PlaceRival(ECCCar car, Vector3 center)
        {
            Vector3 position;
            Vector3 direction;

            OutOfViewStreet(center, out position, out direction);

            car.transform.position = new Vector3(position.x, car.transform.position.y, position.z);
            car.transform.rotation = Quaternion.LookRotation(direction);
        }

        /// <summary>
        /// A lane out of the camera's view, on a street near the player
        /// </summary>
        void OutOfViewStreet(Vector3 center, out Vector3 position, out Vector3 direction)
        {
            Vector3 away = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0) * Vector3.forward;

            Vector3 point = ECCRuntimeVisuals.OutOfView(center, away, 45, gameController.groundLayer);

            TaxiSpot spot = TaxiCity.SpotNear(point, true);

            position = spot.zone;
            direction = TaxiCity.Snap(center - spot.zone);

            // Drive along the street ( not across it ), towards the player's side
            if (Mathf.Abs(Vector3.Dot(direction, spot.streetDirection)) < 0.5f) direction = spot.streetDirection * (Vector3.Dot(center - spot.zone, spot.streetDirection) >= 0 ? 1 : -1);

            // The street's center line, then the right-hand lane for the direction
            float spacing = TaxiCity.Spacing;
            Vector3 line = spot.zone;

            if (spot.streetDirection.z != 0) line.x = Mathf.Round(line.x / spacing) * spacing;
            else line.z = Mathf.Round(line.z / spacing) * spacing;

            position = line + TaxiCity.Right(direction) * TaxiCity.LaneOffset;
        }

        /// <summary>
        /// The taxi with the most deliveries ( the player wins a tie )
        /// </summary>
        public TaxiCab Leader
        {
            get
            {
                TaxiCab leader = Player;
                int best = Player ? Player.delivered : -1;

                foreach (TaxiRival rival in Rivals)
                {
                    if (rival && rival.delivered > best)
                    {
                        best = rival.delivered;
                        leader = rival;
                    }
                }

                return leader;
            }
        }

        public int BestRivalDeliveries
        {
            get
            {
                int best = 0;

                foreach (TaxiRival rival in Rivals)
                {
                    if (rival) best = Mathf.Max(best, rival.delivered);
                }

                foreach (int kept in rivalScores.Values) best = Mathf.Max(best, kept);

                return best;
            }
        }

        // ----- Called by the taxis -----

        public void NotifyPickedUp(TaxiCab cab, TaxiPassenger passenger)
        {
            if (cab.IsPlayer)
            {
                if (passenger.pickupQuality == 2)
                {
                    PerfectStops++;
                    ECCMissions.Report(ECCMissionType.PerfectStops, 1);
                }

                TaxiAudio.Play(TaxiAudio.Pickup);
            }
            else if (Player)
            {
                // A rival took the passenger the player was going for
                Vector3 guide;
                Color color;
                bool destination;

                if (passenger.kind == TaxiPassengerKind.Vip || (Player.GetGuide(out guide, out color, out destination) && destination == false && ECCRuntimeVisuals.FlatDistance(guide, passenger.pickup.zone) < 1)) Stolen++;
            }

            if (PickedUp != null) PickedUp(cab, passenger);
        }

        public void NotifyDelivered(TaxiCab cab, TaxiPassenger passenger, int fare, int rating, int dropQuality)
        {
            if (cab.IsPlayer)
            {
                Streak = rating >= 4 ? Streak + 1 : 0;

                fare *= StreakMultiplier;

                PlayerDeliveries++;
                PlayerEarned += fare;
                RatingSum += rating;

                if (dropQuality == 2) PerfectStops++;

                gameController.ChangeScore(fare);

                ECCMissions.Report(ECCMissionType.DeliverPassengers, 1);
                ECCMissions.Report(ECCMissionType.EarnFares, fare);

                if (rating >= 5) ECCMissions.Report(ECCMissionType.FiveStarRides, 1);
                if (passenger.kind == TaxiPassengerKind.Vip) ECCMissions.Report(ECCMissionType.DeliverVips, 1);
                if (dropQuality == 2) ECCMissions.Report(ECCMissionType.PerfectStops, 1);

                TaxiAudio.Play(rating >= 3 ? TaxiAudio.Cash : TaxiAudio.Bad);
            }
            else
            {
                rivalScores[cab.displayName] = cab.delivered;
            }

            if (Delivered != null) Delivered(cab, passenger, fare, rating, dropQuality);
        }

        public void NotifyLost(TaxiPassenger passenger)
        {
            LostPassengers++;

            if (Lost != null) Lost(passenger);
        }
    }
}
