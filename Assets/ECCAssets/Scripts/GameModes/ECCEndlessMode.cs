using UnityEngine;

namespace EndlessCarChase.Modes
{
    /// <summary>
    /// The endless mode of the taxi game: an endless shift in the city. The shift clock runs down; every passenger
    /// brought to their destination adds time to it ( more for a good ride ) and pays the fare. The longer the shift,
    /// the more rival taxis are on the streets and the more demanding the passengers. The police only come after a
    /// violation ( TaxiPolice ). The shift ends when the clock runs out or the taxi is wrecked; the record is the
    /// money earned in one shift.
    /// Add this component to the GameController object to tweak its values, otherwise it's added with the defaults.
    /// </summary>
    public class ECCEndlessMode : ECCGameModeBase
    {
        [Header("Shift")]
        [Tooltip("Seconds on the shift clock at the start")]
        public float startTime = 75;

        [Tooltip("The most seconds the shift clock can hold")]
        public float maxTime = 150;

        [Tooltip("Seconds added for a delivered passenger, plus the bonus per rating star")]
        public float timePerDelivery = 8;
        public float timePerStar = 2;

        [Tooltip("Extra seconds for a VIP")]
        public float vipTimeBonus = 12;

        [Header("Rivals")]
        [Tooltip("Rival taxis at the start, and the most of them")]
        public int startRivals = 1;
        public int maxRivals = 4;

        [Tooltip("Seconds of the shift until one more rival taxi joins")]
        public float rivalInterval = 60;

        [Tooltip("Seconds between two VIP passengers")]
        public float vipInterval = 70;

        [Header("HUD")]
        [Tooltip("Position of the shift clock, measured from the top center of the screen")]
        public Vector2 hudPosition = new Vector2(0, -110);

        /// <summary>
        /// The wanted stars of the taxi ( kept under this name for the missions and the old HUD )
        /// </summary>
        public int WantedLevel
        {
            get { return TaxiPolice.Instance ? TaxiPolice.Instance.MaxStarsThisRun : 0; }
        }

        /// <summary>
        /// The shift clock, from 0 ( empty ) to 1 ( full )
        /// </summary>
        public float LevelProgress
        {
            get { return Mathf.Clamp01(TimeLeft / Mathf.Max(1, maxTime)); }
        }

        public float TimeLeft { get; private set; }

        public float Elapsed { get; private set; }

        public override GameModeType ModeType
        {
            get { return GameModeType.Endless; }
        }

        TaxiSession session;
        TaxiShiftHUD hud;
        int rivalsJoined;
        bool lowTimeWarned;
        bool ended;

        public override void Initialize(ECCGameController controller)
        {
            base.Initialize(controller);

            TimeLeft = startTime;
            Elapsed = 0;
        }

        /// <summary>
        /// The endless mode keeps the original high score key, so existing records are kept
        /// </summary>
        public override string GetHighScoreKey(string sceneKey)
        {
            return sceneKey;
        }

        /// <summary>
        /// The passengers of the shift: at first mostly normal ones, then more of the demanding kinds
        /// </summary>
        public static TaxiRules ShiftRules(float elapsed, int rivals, float vipInterval)
        {
            float harder = Mathf.Clamp01(elapsed / 240);

            return new TaxiRules
            {
                waitingPassengers = 6,
                rivals = rivals,
                kindWeights = new float[] { 10, Mathf.Lerp(1, 4, harder), 0, Mathf.Lerp(1, 3, harder), Mathf.Lerp(0.5f, 2, harder), Mathf.Lerp(0.5f, 3, harder) },
                vipInterval = vipInterval,
                police = true,
                fareMultiplier = 1,
            };
        }

        public override void OnGameStarted()
        {
            // The taxi drives through the endless city
            ECCCityWorld.Enable(gameController);

            ECCCameraFraming.Apply();

            // The endless shift follows the clock: at night ( 19:00 to 6:00 ) the city is dark, with headlights
            if (ECCNightMode.IsNightTime) ECCNightMode.Enable(gameController);

            // No money for just driving around: the fares are the income
            gameController.CancelInvoke("ScorePerSecond");

            string carName = gameController.playerObject ? gameController.playerObject.name.Replace("(Clone)", string.Empty) : string.Empty;

            rivalsJoined = startRivals;

            session = TaxiSession.Begin(gameController, ShiftRules(0, startRivals, vipInterval), carName);

            TaxiSession.Delivered += OnDelivered;

            hud = TaxiShiftHUD.Create(gameController, this);

            ECCHintBanner.Show("Icons/clock", ECCCarTheme.CandyBlue, "شیفت شروع شد!", "مسافرها رو سوار کن؛ هر مسافر وقت شیفت رو بیشتر می‌کنه");
        }

        void OnDestroy()
        {
            TaxiSession.Delivered -= OnDelivered;
        }

        void OnDelivered(TaxiCab cab, TaxiPassenger passenger, int fare, int rating, int dropQuality)
        {
            if (cab.IsPlayer == false || ended) return;

            float bonus = timePerDelivery + rating * timePerStar + (passenger.kind == TaxiPassengerKind.Vip ? vipTimeBonus : 0);

            TimeLeft = Mathf.Min(maxTime, TimeLeft + bonus);

            if (session) ECCMissions.Report(ECCMissionType.DeliverInOneShift, session.PlayerDeliveries);

            lowTimeWarned = false;

            if (hud) hud.Punch("+" + ECCUIKit.PersianDigits(Mathf.RoundToInt(bonus)) + " ثانیه");
        }

        public override void OnGameOver()
        {
            ended = true;

            if (hud) hud.Hide();
            if (TaxiHUD.Instance) TaxiHUD.Instance.Hide();
        }

        public override bool ShowsOwnResultScreen
        {
            get { return true; }
        }

        /// <summary>
        /// The endless mode result screen: money earned, record, passengers and rating
        /// </summary>
        public override void ShowResultScreen(bool victory)
        {
            int score = gameController.score;

            bool newRecord = score > gameController.highScore;

            if (newRecord)
            {
                gameController.highScore = score;
                PlayerPrefs.SetInt(gameController.HighScoreKey, score);
                PlayerPrefs.Save();
            }

            ECCEndlessResultUI.Show(gameController, score, gameController.highScore, newRecord, WantedLevel);

            // Freeze the game behind the result screen
            Time.timeScale = 0;
        }

        void Update()
        {
            if (IsPlaying == false || ended || session == null) return;

            Elapsed += Time.deltaTime;
            TimeLeft -= Time.deltaTime;

            // One more rival now and then; the passengers get more demanding
            int rivals = Mathf.Min(maxRivals, startRivals + Mathf.FloorToInt(Elapsed / rivalInterval));

            if (rivals != rivalsJoined)
            {
                rivalsJoined = rivals;

                ECCHintBanner.Show("Icons/target", ECCCarTheme.CandyRed, "یه تاکسی رقیب دیگه اومد!", "حالا " + ECCUIKit.PersianDigits(rivals) + " تاکسی رقیب توی شهرن", 2, ECCHintBanner.Kind.Minor);
            }

            TaxiRules rules = ShiftRules(Elapsed, rivals, vipInterval);

            session.rules.kindWeights = rules.kindWeights;
            session.SetRivalCount(rivals);

            if (lowTimeWarned == false && TimeLeft <= 15)
            {
                lowTimeWarned = true;

                ECCHintBanner.Show("Icons/clock", ECCCarTheme.CandyOrange, "شیفت داره تموم می‌شه!", "زود یه مسافر برسون تا وقت بیشتری بگیری", 2.2f, ECCHintBanner.Kind.Urgent);
            }

            if (TimeLeft <= 0)
            {
                TimeLeft = 0;
                ended = true;

                // Passengers still on board are let out
                if (session.Player) session.Player.car.throttle = 0;

                ECCToast.Show("پایان شیفت!", ECCRuntimeUI.StarColor, ECCUIKit.PersianDigits(session.PlayerDeliveries) + " مسافر رسوندی", 2);

                gameController.Fail();
            }
        }
    }
}
