using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The player's taxi. The car drives on by itself ( as in the chase ) and the player steers it; holding the brake
    /// ( the big button, or Space / S / Down on a keyboard ) slows it down to a stop, letting go speeds it up again.
    /// Stopping in a passenger's ring takes them on board; their destination gets a beacon and the guide arrow
    /// points at it. Bumps and crashes during a ride lower the passengers' rating
    /// </summary>
    public class TaxiDriver : TaxiCab
    {
        [Tooltip("How quickly the brake stops the car ( throttle per second )")]
        public float brakeRate = 1.7f;

        [Tooltip("How quickly the car speeds up again after the brake is let go ( throttle per second )")]
        public float accelerateRate = 0.75f;

        [Tooltip("Seconds between two honks")]
        public float honkCooldown = 1.2f;

        [Tooltip("Waiting passengers this close hear the horn and wave")]
        public float honkRadius = 32;

        // Set by the HUD's brake button
        internal bool brakeButton;

        public static TaxiDriver Instance { get; private set; }

        float nextHonk;
        ECCPowerUpManager powerUps;

        public override bool IsPlayer
        {
            get { return true; }
        }

        public bool IsBraking
        {
            get
            {
                if (brakeButton) return true;

                return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            }
        }

        /// <summary>
        /// With the magnet power-up the rings reach farther
        /// </summary>
        public override float ZoneReach
        {
            get
            {
                if (powerUps == null && ECCGameController.Instance) powerUps = ECCGameController.Instance.GetComponent<ECCPowerUpManager>();

                return powerUps && powerUps.IsActive(ECCPowerUpType.Magnet) ? ZoneRadius * 1.8f : ZoneRadius;
            }
        }

        /// <summary>
        /// Seats for the kind of car: vans, buses and big cars take a family, sports cars only one passenger
        /// </summary>
        public static int SeatsFor(string carName)
        {
            string name = carName.ToLowerInvariant();

            if (name.Contains("van") || name.Contains("bus") || name.Contains("suv") || name.Contains("monster")) return 3;
            if (name.Contains("race") || name.Contains("sport") || name.Contains("coupe")) return 1;

            return 2;
        }

        public static TaxiDriver Attach(ECCCar player, string carName)
        {
            TaxiDriver driver = player.gameObject.AddComponent<TaxiDriver>();
            driver.car = player;
            driver.seats = SeatsFor(carName);
            driver.displayName = "تو";

            // Better handling also brakes better
            driver.brakeRate *= 1 + ECCUpgrades.GetLevel(carName, ECCUpgradeType.Handling) * 0.08f;

            TaxiMarkers.AddRoofSign(player, TaxiColors.PlayerYellow);

            return driver;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Instance = this;

            ECCCar.PlayerDamaged += OnPlayerDamaged;
            ECCCar.CarsBumped += OnCarsBumped;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (Instance == this) Instance = null;

            ECCCar.PlayerDamaged -= OnPlayerDamaged;
            ECCCar.CarsBumped -= OnCarsBumped;
        }

        void OnPlayerDamaged(ECCCar damaged, float amount)
        {
            if (damaged == car) Bump();
        }

        void OnCarsBumped(ECCCar striker, ECCCar victim)
        {
            if (striker == car || victim == car) Bump();
        }

        protected override void Update()
        {
            ECCGameController controller = ECCGameController.Instance;

            bool driving = controller && controller.gameStarted && controller.isGameOver == false && controller.isPaused == false;

            if (car && driving)
            {
                float target = IsBraking ? 0 : 1;

                car.throttle = Mathf.MoveTowards(car.throttle, target, (target < car.throttle ? brakeRate : accelerateRate) * Time.deltaTime);
            }

            if (Input.GetKeyDown(KeyCode.H)) Honk();

            base.Update();
        }

        public bool CanHonk
        {
            get { return Time.time >= nextHonk; }
        }

        /// <summary>
        /// The horn: the passengers waiting nearby wave and wait a little longer
        /// </summary>
        public void Honk()
        {
            if (car == null || CanHonk == false) return;

            nextHonk = Time.time + honkCooldown;

            TaxiAudio.Honk(car.transform.position, 0.9f);

            foreach (TaxiPassenger passenger in TaxiPassenger.All)
            {
                if (passenger && passenger.IsWaiting && ECCRuntimeVisuals.FlatDistance(passenger.transform.position, car.transform.position) <= honkRadius) passenger.Wave();
            }
        }

        protected override void OnBoardingStarted(TaxiPassenger passenger)
        {
            ECCHaptics.Vibrate(ECCHaptics.Kind.Light);
        }

        protected override void OnPassengerBoarded(TaxiPassenger passenger)
        {
            passenger.ShowDestination();
        }

        protected override void OnBumped()
        {
            ECCHaptics.Vibrate(ECCHaptics.Kind.Light);
        }

        // ----- Guide -----

        /// <summary>
        /// Where the guide arrow points: the nearest destination of a passenger on board, otherwise the passenger the
        /// player should go for ( the VIP first, then the nearest one that fits in the car )
        /// </summary>
        public bool GetGuide(out Vector3 target, out Color guideColor, out bool isDestination)
        {
            target = Vector3.zero;
            guideColor = Color.white;
            isDestination = false;

            if (car == null) return false;

            Vector3 position = car.transform.position;
            float best = float.MaxValue;

            foreach (TaxiPassenger passenger in riding)
            {
                if (passenger == null) continue;

                float distance = ECCRuntimeVisuals.FlatDistance(position, passenger.dropoff.zone);

                if (distance < best)
                {
                    best = distance;
                    target = passenger.dropoff.zone;
                    guideColor = passenger.KindColor;
                    isDestination = true;
                }
            }

            if (isDestination || FreeSeats <= 0) return isDestination;

            TaxiPassenger next = TaxiSession.Instance ? TaxiSession.Instance.BestPassengerFor(this) : null;

            if (next == null) return false;

            target = next.pickup.zone;
            guideColor = next.KindColor;

            return true;
        }
    }
}
