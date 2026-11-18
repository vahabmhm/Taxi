using UnityEngine;

namespace EndlessCarChase
{
    public enum TaxiWishType
    {
        None = 0,
        Hurry = 1,   // «تندتر برو، دیرم شده!»: reach the destination in time
        Gentle = 2,  // «آروم‌تر برو، حالم خوب نیست»: drive slowly, without a bump, for a while
        Stop = 3,    // «یه لحظه جلوی اون مغازه وایسا»: stop in a ring on the way
    }

    public enum TaxiWishState { Active, Done, Failed }

    /// <summary>
    /// A wish of a passenger during the ride, without Unity. Kept: a star more for the ride; not kept: a star less
    /// </summary>
    public class TaxiWish
    {
        public const float HurrySpeed = 9;      // Street meters a second the hurry wish expects
        public const float HurryExtra = 8;      // Seconds on top of that
        public const float GentleSeconds = 15;
        public const float GentleThrottle = 0.65f; // Faster than this part of the car's speed is too fast
        public const float GentleTooFast = 2.5f;   // Seconds too fast in all that fail the wish
        public const float StopSeconds = 40;    // To find the stop ring
        public const float StopHold = 1.5f;     // Seconds standing in it

        public TaxiWishType type;
        public TaxiWishState state = TaxiWishState.Active;
        public float timeLeft;
        public float tooFast;
        public float held;

        // The stop ring of a Stop wish
        public TaxiSpot stop;

        public bool IsActive
        {
            get { return state == TaxiWishState.Active; }
        }

        /// <summary>
        /// Stars the wish adds to the ride's rating
        /// </summary>
        public int Stars
        {
            get { return state == TaxiWishState.Done ? 1 : (state == TaxiWishState.Failed ? -1 : 0); }
        }

        public static TaxiWish Create(TaxiWishType type, float streetDistanceLeft)
        {
            TaxiWish wish = new TaxiWish { type = type };

            switch (type)
            {
                case TaxiWishType.Hurry: wish.timeLeft = streetDistanceLeft / HurrySpeed + HurryExtra; break;
                case TaxiWishType.Gentle: wish.timeLeft = GentleSeconds; break;
                default: wish.timeLeft = StopSeconds; break;
            }

            return wish;
        }

        /// <summary>
        /// The wish a kind of passenger may have ( roll from 0 to 1 ). Follow and hurry passengers have their own task
        /// </summary>
        public static TaxiWishType PickFor(TaxiPassengerKind kind, float roll)
        {
            switch (kind)
            {
                case TaxiPassengerKind.Follow:
                case TaxiPassengerKind.Hurry: return TaxiWishType.None;
                case TaxiPassengerKind.Grumpy: return TaxiWishType.Gentle;
                case TaxiPassengerKind.Tourist: return TaxiWishType.Stop;
                case TaxiPassengerKind.Family: return roll < 0.5f ? TaxiWishType.Gentle : TaxiWishType.Stop;
                case TaxiPassengerKind.Vip: return roll < 0.5f ? TaxiWishType.Hurry : TaxiWishType.Stop;
                default: return roll < 0.34f ? TaxiWishType.Hurry : (roll < 0.67f ? TaxiWishType.Gentle : TaxiWishType.Stop);
            }
        }

        /// <summary>
        /// One frame of the ride: the part of the car's speed it drives with, whether it was bumped, whether it stands
        /// still in the stop ring
        /// </summary>
        public TaxiWishState Step(float deltaTime, float throttle, bool bumped, bool standingInStop)
        {
            if (state != TaxiWishState.Active) return state;

            switch (type)
            {
                case TaxiWishType.Hurry:
                    timeLeft -= deltaTime;
                    if (timeLeft <= 0) state = TaxiWishState.Failed;
                    break;

                case TaxiWishType.Gentle:
                    if (bumped) { state = TaxiWishState.Failed; break; }

                    if (throttle > GentleThrottle) tooFast += deltaTime;
                    if (tooFast > GentleTooFast) { state = TaxiWishState.Failed; break; }

                    timeLeft -= deltaTime;
                    if (timeLeft <= 0) state = TaxiWishState.Done;
                    break;

                case TaxiWishType.Stop:
                    held = standingInStop ? held + deltaTime : 0;
                    if (held >= StopHold) { state = TaxiWishState.Done; break; }

                    timeLeft -= deltaTime;
                    if (timeLeft <= 0) state = TaxiWishState.Failed;
                    break;
            }

            return state;
        }

        /// <summary>
        /// The taxi reached the destination with the wish still open: in time for the hurry, slow enough for the
        /// gentle ride, but the shop was skipped
        /// </summary>
        public void Arrive()
        {
            if (state != TaxiWishState.Active) return;

            state = type == TaxiWishType.Stop ? TaxiWishState.Failed : TaxiWishState.Done;
        }

        public static string Quote(TaxiWishType type, TaxiPassengerKind kind)
        {
            switch (type)
            {
                case TaxiWishType.Hurry: return "تندتر برو، دیرم شده!";
                case TaxiWishType.Gentle: return "آروم‌تر برو، حالم خوب نیست";
                case TaxiWishType.Stop: return kind == TaxiPassengerKind.Tourist ? "یه لحظه وایسا عکس بگیرم!" : "یه لحظه جلوی اون مغازه وایسا";
                default: return string.Empty;
            }
        }

        public static string Explain(TaxiWishType type)
        {
            switch (type)
            {
                case TaxiWishType.Hurry: return "زود برسونش تا یه ستاره‌ی اضافه بگیری";
                case TaxiWishType.Gentle: return "۱۵ ثانیه آروم و بدون ضربه برون؛ ترمز رو کم‌کم بگیر";
                case TaxiWishType.Stop: return "توی دایره‌ی سبز سر راه وایسا";
                default: return string.Empty;
            }
        }
    }

    /// <summary>
    /// The passengers' wishes during the shift's rides: a while after a passenger got in ( and with a long enough ride
    /// left ) they may ask for something. One wish at a time; the HUD shows it with its time left, the guide arrow goes
    /// to the stop ring of a Stop wish. The ride's rating gets a star more or less ( TaxiCab.Deliver )
    /// </summary>
    public class TaxiWishes : MonoBehaviour
    {
        public static readonly Color StopColor = new Color(0.3f, 1, 0.45f);

        [Tooltip("Chance that a passenger asks for something during the ride")]
        public float chance = 0.55f;

        [Tooltip("Seconds of the ride before the wish, at the least and the most")]
        public Vector2 delay = new Vector2(6, 12);

        [Tooltip("Street meters of the ride that must be left for a wish")]
        public float minDistanceLeft = 120;

        public static TaxiWishes Instance { get; private set; }

        TaxiPassenger current;
        Transform stopRing;
        Transform stopBeacon;
        int lastBumps;

        public TaxiPassenger Current
        {
            get { return current && current.wish != null && current.wish.IsActive ? current : null; }
        }

        public static TaxiWishes Create(GameObject holder)
        {
            TaxiWishes wishes = holder.AddComponent<TaxiWishes>();
            Instance = wishes;

            return wishes;
        }

        void OnDestroy()
        {
            ClearRing();

            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// A passenger got into the player's taxi: maybe they will ask for something later
        /// </summary>
        public void OnBoarded(TaxiPassenger passenger)
        {
            if (passenger == null || passenger.kind == TaxiPassengerKind.Follow || passenger.kind == TaxiPassengerKind.Hurry) return;

            passenger.wishTimer = Random.value < chance ? Random.Range(delay.x, delay.y) : -1;
        }

        /// <summary>
        /// The stop ring the guide arrow should point at, if any
        /// </summary>
        public bool GetStop(out Vector3 position)
        {
            TaxiPassenger passenger = Current;

            position = passenger ? passenger.wish.stop.zone : Vector3.zero;

            return passenger && passenger.wish.type == TaxiWishType.Stop;
        }

        void Update()
        {
            ECCGameController controller = ECCGameController.Instance;
            TaxiDriver driver = TaxiDriver.Instance;

            if (controller == null || controller.gameStarted == false || controller.isGameOver || controller.isPaused || driver == null || driver.car == null) return;

            if (current)
            {
                UpdateCurrent(driver);
                return;
            }

            ClearRing();

            foreach (TaxiPassenger passenger in driver.riding)
            {
                if (passenger == null || passenger.wishTimer < 0 || passenger.wish != null) continue;

                passenger.wishTimer -= Time.deltaTime;

                if (passenger.wishTimer > 0) continue;

                passenger.wishTimer = -1;

                Ask(driver, passenger);

                if (current) return;
            }
        }

        void Ask(TaxiDriver driver, TaxiPassenger passenger)
        {
            Vector3 position = driver.car.transform.position;

            float left = TaxiCity.StreetDistance(position, passenger.dropoff.zone);

            if (left < minDistanceLeft || passenger.dropoffHidden) return;

            TaxiWishType type = TaxiWish.PickFor(passenger.kind, Random.value);

            if (type == TaxiWishType.None) return;

            TaxiWish wish = TaxiWish.Create(type, left);

            if (type == TaxiWishType.Stop)
            {
                // A curb a bit ahead, on the right hand side
                Vector3 direction = TaxiCity.Snap(driver.car.transform.forward);
                Vector3 right = TaxiCity.Right(direction);

                bool side = direction.z != 0 ? right.x > 0 : right.z > 0;

                wish.stop = TaxiCity.SpotNear(position + direction * 55, side);

                // Never on top of the destination
                if (ECCRuntimeVisuals.FlatDistance(wish.stop.zone, passenger.dropoff.zone) < 25) return;

                stopRing = TaxiMarkers.CreateZone(wish.stop.zone, TaxiCab.ZoneRadius, StopColor);
                stopBeacon = TaxiMarkers.CreateBeacon(wish.stop.zone, TaxiCab.ZoneRadius, StopColor);
            }

            passenger.wish = wish;
            current = passenger;
            lastBumps = passenger.bumps;

            ECCHintBanner.Show("Icons/heart", passenger.KindColor, "«" + TaxiWish.Quote(type, passenger.kind) + "»", TaxiWish.Explain(type), 2.8f, ECCHintBanner.Kind.Urgent);
        }

        void UpdateCurrent(TaxiDriver driver)
        {
            TaxiWish wish = current.wish;

            // The ride ended ( delivered: TaxiCab.Deliver already settled the wish; or the taxi was wrecked )
            if (wish == null || current.state != TaxiPassengerState.Riding || current.cab != driver)
            {
                Settle(false);
                return;
            }

            bool bumped = current.bumps > lastBumps;
            lastBumps = current.bumps;

            bool standing = wish.type == TaxiWishType.Stop && driver.IsStopped && ECCRuntimeVisuals.FlatDistance(driver.car.transform.position, wish.stop.zone) <= driver.ZoneReach;

            if (wish.Step(Time.deltaTime, driver.car.throttle, bumped, standing) != TaxiWishState.Active) Settle(true);
        }

        /// <summary>
        /// The wish is over: tell the player how it went
        /// </summary>
        void Settle(bool announce)
        {
            TaxiPassenger passenger = current;

            current = null;

            ClearRing();

            if (announce == false || passenger == null || passenger.wish == null) return;

            Announce(passenger.wish);
        }

        public static void Announce(TaxiWish wish)
        {
            if (wish == null) return;

            if (wish.state == TaxiWishState.Done)
            {
                ECCToast.Show("مسافر راضی شد!", ECCRuntimeUI.GoodColor, "یه ستاره‌ی اضافه برای این سفر", 1.6f);
                TaxiAudio.Play(TaxiAudio.Cash);
            }
            else if (wish.state == TaxiWishState.Failed)
            {
                ECCToast.Show("مسافر ناراحت شد", ECCRuntimeUI.BadColor, "یه ستاره از این سفر کم شد", 1.6f);
            }
        }

        void ClearRing()
        {
            if (stopRing) Destroy(stopRing.gameObject);
            if (stopBeacon) Destroy(stopBeacon.gameObject);

            stopRing = null;
            stopBeacon = null;
        }
    }
}
