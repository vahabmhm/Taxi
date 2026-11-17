using UnityEngine;

namespace EndlessCarChase
{
    public enum TaxiCityEventType
    {
        None = 0,
        RushHour = 1,   // «ساعت شلوغی»: much more traffic, every fare ×1.5
        Concert = 2,    // «پایان کنسرت»: a crowd of passengers on one street, the rivals rush there too
        Crackdown = 3,  // «گشت ویژه‌ی پلیس»: fines ×2 and the police get hot faster
        TouristTour = 4, // «تور گردشگری»: many tourists ( long rides, ×1.5 fare ) and a VIP or two
    }

    /// <summary>
    /// Events of the city during the endless shift: after FirstDelay seconds, and then every IntervalMin to IntervalMax
    /// seconds, one event ( never the same one twice in a row ) changes the city for Duration seconds. It's announced
    /// on the banner and its time left shows under the shift clock
    /// </summary>
    public class TaxiCityEvents : MonoBehaviour
    {
        public static TaxiCityEvents Instance { get; private set; }

        [Tooltip("Seconds of the shift before the first event, and between two events")]
        public float firstDelay = 45;
        public float intervalMin = 55;
        public float intervalMax = 80;

        [Tooltip("Seconds an event lasts")]
        public float duration = 25;

        [Tooltip("Rush hour: traffic cars and how much faster they come; the fares")]
        public float rushTrafficMultiplier = 1.6f;
        public float rushFareMultiplier = 1.5f;

        [Tooltip("Police crackdown: fines and heat")]
        public float crackdownFineMultiplier = 2;
        public float crackdownHeatMultiplier = 1.5f;

        [Tooltip("Concert: passengers in the crowd and how far from the taxi")]
        public int concertCrowd = 5;
        public Vector2 concertDistance = new Vector2(70, 110);

        public TaxiCityEventType Active { get; private set; }
        public float TimeLeft { get; private set; }
        public TaxiCityEventType Last { get; private set; }

        public static event System.Action<TaxiCityEventType> Started;
        public static event System.Action<TaxiCityEventType> Ended;

        ECCGameController gameController;
        TaxiSession session;
        float timer;
        Transform concertBeacon;

        int savedMaxCars = -1;
        float savedSpawnInterval;

        static readonly TaxiCityEventType[] Types = { TaxiCityEventType.RushHour, TaxiCityEventType.Concert, TaxiCityEventType.Crackdown, TaxiCityEventType.TouristTour };

        public static TaxiCityEvents Create(ECCGameController controller, TaxiSession session)
        {
            TaxiCityEvents events = controller.gameObject.AddComponent<TaxiCityEvents>();
            events.gameController = controller;
            events.session = session;
            events.timer = events.firstDelay;

            Instance = events;

            return events;
        }

        void OnDestroy()
        {
            Finish(false);

            if (Instance == this) Instance = null;
        }

        // ----- What an event changes -----

        public float FareMultiplier
        {
            get { return Active == TaxiCityEventType.RushHour ? rushFareMultiplier : 1; }
        }

        public float FineMultiplier
        {
            get { return Active == TaxiCityEventType.Crackdown ? crackdownFineMultiplier : 1; }
        }

        public float HeatMultiplier
        {
            get { return Active == TaxiCityEventType.Crackdown ? crackdownHeatMultiplier : 1; }
        }

        /// <summary>
        /// The tourist tour brings tourists and a few VIPs ( the shift's weights are set again every frame )
        /// </summary>
        public static void ModifyWeights(TaxiCityEventType type, float[] weights)
        {
            if (weights == null || type != TaxiCityEventType.TouristTour) return;

            if (weights.Length > (int)TaxiPassengerKind.Tourist) weights[(int)TaxiPassengerKind.Tourist] = weights[(int)TaxiPassengerKind.Tourist] * 5 + 4;
            if (weights.Length > (int)TaxiPassengerKind.Vip) weights[(int)TaxiPassengerKind.Vip] += 0.6f;
        }

        public void ModifyWeights(float[] weights)
        {
            ModifyWeights(Active, weights);
        }

        /// <summary>
        /// The next event: any but the last one ( roll from 0 to 1 )
        /// </summary>
        public static TaxiCityEventType PickNext(TaxiCityEventType last, float roll)
        {
            int count = 0;

            foreach (TaxiCityEventType type in Types)
            {
                if (type != last) count++;
            }

            int pick = Mathf.Clamp(Mathf.FloorToInt(roll * count), 0, count - 1);

            foreach (TaxiCityEventType type in Types)
            {
                if (type == last) continue;

                if (pick == 0) return type;

                pick--;
            }

            return TaxiCityEventType.RushHour;
        }

        public static string GetName(TaxiCityEventType type)
        {
            switch (type)
            {
                case TaxiCityEventType.RushHour: return "ساعت شلوغی";
                case TaxiCityEventType.Concert: return "پایان کنسرت";
                case TaxiCityEventType.Crackdown: return "گشت ویژه‌ی پلیس";
                case TaxiCityEventType.TouristTour: return "تور گردشگری";
                default: return string.Empty;
            }
        }

        public static string GetDescription(TaxiCityEventType type)
        {
            switch (type)
            {
                case TaxiCityEventType.RushHour: return "خیابون‌ها شلوغ شده؛ همه‌ی کرایه‌ها ۱٫۵ برابره";
                case TaxiCityEventType.Concert: return "یه عالمه مسافر یه‌جا منتظرن؛ رقیب‌ها هم دارن می‌رن!";
                case TaxiCityEventType.Crackdown: return "مراقب باش! جریمه‌ها دو برابره";
                case TaxiCityEventType.TouristTour: return "توریست‌ها اومدن؛ سفرهای دور و کرایه‌ی بیشتر";
                default: return string.Empty;
            }
        }

        public static Color GetColor(TaxiCityEventType type)
        {
            switch (type)
            {
                case TaxiCityEventType.RushHour: return ECCCarTheme.CandyOrange;
                case TaxiCityEventType.Concert: return TaxiColors.Concert;
                case TaxiCityEventType.Crackdown: return ECCCarTheme.CandyRed;
                case TaxiCityEventType.TouristTour: return TaxiColors.Tourist;
                default: return Color.white;
            }
        }

        static string Icon(TaxiCityEventType type)
        {
            switch (type)
            {
                case TaxiCityEventType.RushHour: return "Icons/clock";
                case TaxiCityEventType.Concert: return "Icons/star";
                case TaxiCityEventType.Crackdown: return "Icons/lock";
                default: return "Icons/crown";
            }
        }

        // ----- Running -----

        bool IsPlaying
        {
            get { return gameController && gameController.gameStarted && gameController.isGameOver == false && gameController.playerObject; }
        }

        void Update()
        {
            if (gameController && gameController.isGameOver)
            {
                Finish(false);
                return;
            }

            if (IsPlaying == false) return;

            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Moves the event clock on: starts and ends the events
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (Active != TaxiCityEventType.None)
            {
                TimeLeft -= deltaTime;

                if (TimeLeft <= 0) Finish(true);

                return;
            }

            timer -= deltaTime;

            if (timer <= 0) Begin(PickNext(Last, Random.value));
        }

        public void Begin(TaxiCityEventType type)
        {
            if (type == TaxiCityEventType.None) return;

            Finish(false);

            Active = type;
            Last = type;
            TimeLeft = duration;

            switch (type)
            {
                case TaxiCityEventType.RushHour:
                    ECCTraffic traffic = ECCTraffic.Instance;

                    if (traffic)
                    {
                        savedMaxCars = traffic.maxCars;
                        savedSpawnInterval = traffic.spawnInterval;

                        traffic.maxCars = Mathf.RoundToInt(traffic.maxCars * rushTrafficMultiplier);
                        traffic.spawnInterval = traffic.spawnInterval / rushTrafficMultiplier;
                    }
                    break;

                case TaxiCityEventType.Concert:
                    StartConcert();
                    break;
            }

            ECCHintBanner.Show(Icon(type), GetColor(type), GetName(type) + "!", GetDescription(type), 2.8f, ECCHintBanner.Kind.Urgent);

            if (Started != null) Started(type);
        }

        void StartConcert()
        {
            if (session == null || gameController == null || gameController.playerObject == null || TaxiCity.IsActive == false) return;

            TaxiSpot center = TaxiCity.RandomSpot(gameController.playerObject.transform.position, concertDistance.x, concertDistance.y);

            int made = session.SpawnCrowd(center, concertCrowd, duration + 15);

            if (made > 0) concertBeacon = TaxiMarkers.CreateBeacon(center.zone, TaxiCab.ZoneRadius * 2.5f, TaxiColors.Concert);
        }

        /// <summary>
        /// Ends the event: the traffic goes back to normal
        /// </summary>
        void Finish(bool announce)
        {
            if (savedMaxCars >= 0 && ECCTraffic.Instance)
            {
                ECCTraffic.Instance.maxCars = savedMaxCars;
                ECCTraffic.Instance.spawnInterval = savedSpawnInterval;
            }

            savedMaxCars = -1;

            if (concertBeacon) Destroy(concertBeacon.gameObject);
            concertBeacon = null;

            if (Active == TaxiCityEventType.None) return;

            TaxiCityEventType ended = Active;

            Active = TaxiCityEventType.None;
            TimeLeft = 0;
            timer = Random.Range(intervalMin, intervalMax);

            if (announce) ECCHintBanner.Show(Icon(ended), Color.white, GetName(ended) + " تموم شد", "شهر به حالت عادی برگشت", 1.6f, ECCHintBanner.Kind.Minor);

            if (Ended != null) Ended(ended);
        }
    }
}
