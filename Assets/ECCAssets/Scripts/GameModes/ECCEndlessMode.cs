using UnityEngine;

namespace EndlessCarChase.Modes
{
    /// <summary>
    /// The endless mode: survive as long as possible. The wanted level rises over time, bringing more and faster
    /// police cars, and a higher score multiplier as a reward.
    /// Add this component to the GameController object to tweak its values, otherwise it's added with the defaults.
    /// </summary>
    public class ECCEndlessMode : ECCGameModeBase
    {
        [Header("Wanted Level")]
        [Tooltip("How many seconds of survival raise the wanted level by one")]
        public float levelUpInterval = 30;

        [Tooltip("The highest wanted level")]
        public int maxWantedLevel = 6;

        [Tooltip("The time between police spawns is multiplied by this value for every wanted level. Lower is harder")]
        [Range(0.3f, 1)]
        public float copSpawnRateFactor = 0.8f;

        [Tooltip("Extra speed added to newly spawned police cars for every wanted level")]
        public float copSpeedBonusPerLevel = 0.75f;

        [Tooltip("Multiply the score per second by the wanted level")]
        public bool multiplyScoreByWantedLevel = true;

        [Header("Police Variants")]
        [Tooltip("Chance of an interceptor added per wanted level, starting at level 2")]
        public float interceptorChancePerLevel = 0.1f;

        [Tooltip("Chance of a rammer added per wanted level, starting at level 3")]
        public float rammerChancePerLevel = 0.1f;

        [Tooltip("Chance of a heavy car added per wanted level, starting at level 4")]
        public float heavyChancePerLevel = 0.12f;

        [Header("Hazards")]
        [Tooltip("The wanted level from which spike strips appear")]
        public int spikeStripsFromLevel = 3;

        [Tooltip("The wanted level from which roadblocks appear")]
        public int roadblocksFromLevel = 4;

        [Tooltip("The wanted level at which the police helicopter joins the chase ( searchlight and spike strips )")]
        public int helicopterFromLevel = 6;

        [Header("HUD")]
        [Tooltip("Position of the wanted level HUD, measured from the top center of the screen")]
        public Vector2 hudPosition = new Vector2(0, -110);

        public int WantedLevel { get; private set; }

        /// <summary>
        /// Progress towards the next wanted level, from 0 to 1
        /// </summary>
        public float LevelProgress
        {
            get { return WantedLevel >= maxWantedLevel ? 1 : Mathf.Clamp01(levelTimer / levelUpInterval); }
        }

        public override GameModeType ModeType
        {
            get { return GameModeType.Endless; }
        }

        float levelTimer = 0;

        ECCSpawnTuning spawnTuning;
        ECCHazardSpawner hazards;

        ECCWantedHUD hud;

        public override void Initialize(ECCGameController controller)
        {
            base.Initialize(controller);

            WantedLevel = 1;
            levelTimer = 0;
        }

        /// <summary>
        /// The endless mode keeps the original high score key, so existing records are kept
        /// </summary>
        public override string GetHighScoreKey(string sceneKey)
        {
            return sceneKey;
        }

        public override void OnGameStarted()
        {
            // The endless chase drives through the endless city
            ECCCityWorld.Enable(gameController);

            // The same camera distance as chapter 1 in every mode
            ECCCameraFraming.Apply();

            // The endless chase follows the clock: at night ( 19:00 to 6:00 ) the city is dark, with headlights and sirens
            if (ECCNightMode.IsNightTime) ECCNightMode.Enable(gameController);

            ECCSpawnAroundObject spawner = gameController.GetComponent<ECCSpawnAroundObject>();
            if (spawner) spawnTuning = new ECCSpawnTuning(spawner);

            hazards = gameController.GetComponent<ECCHazardSpawner>();

            ECCSpawnAroundObject.ObjectSpawned += OnObjectSpawned;

            hud = ECCWantedHUD.Create(gameController, this);

            ApplyWantedLevel(false);
        }

        void OnDestroy()
        {
            ECCSpawnAroundObject.ObjectSpawned -= OnObjectSpawned;
        }

        /// <summary>
        /// Higher wanted levels bring tougher police variants
        /// </summary>
        void OnObjectSpawned(Transform spawned)
        {
            if (enabled == false) return;

            ECCCar car = spawned.GetComponent<ECCCar>();

            if (car == null) return;

            // Tough enough for the player's car; the wanted level brings tougher variants instead of chapters
            ECCCopVariant.Toughen(car, 0);

            ECCCopVariant.RollAndApply(car,
                Mathf.Max(0, WantedLevel - 1) * interceptorChancePerLevel,
                Mathf.Max(0, WantedLevel - 2) * rammerChancePerLevel,
                Mathf.Max(0, WantedLevel - 3) * heavyChancePerLevel);
        }

        public override void OnGameOver()
        {
            if (hud) hud.Hide();
        }

        public override bool ShowsOwnResultScreen
        {
            get { return true; }
        }

        /// <summary>
        /// The endless mode result screen ( replaces the old game over canvas ): coins earned, record, wanted level
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
            if (IsPlaying == false || WantedLevel >= maxWantedLevel) return;

            levelTimer += Time.deltaTime;

            if (levelTimer >= levelUpInterval)
            {
                levelTimer -= levelUpInterval;

                WantedLevel++;

                ApplyWantedLevel(true);
            }
        }

        void ApplyWantedLevel(bool announce)
        {
            int extraLevels = WantedLevel - 1;

            // Police spawn faster
            if (spawnTuning != null) spawnTuning.SetSpawnRateMultiplier(ECCSpawnTuning.GroupKind.Police, Mathf.Pow(copSpawnRateFactor, extraLevels));

            if (hazards)
            {
                hazards.spikeStrips = WantedLevel >= spikeStripsFromLevel;
                hazards.roadblocks = WantedLevel >= roadblocksFromLevel;
            }

            // Police drive faster
            ECCCar.aiSpeedBonus = extraLevels * copSpeedBonusPerLevel;

            // The player gets more score
            gameController.SetScoreMultiplier(multiplyScoreByWantedLevel ? WantedLevel : 1);

            if (hud) hud.SetWantedLevel(WantedLevel, gameController.scoreMultiplier, announce);

            // The highest level of the chase: the helicopter
            if (WantedLevel >= helicopterFromLevel) ECCPoliceHelicopter.Spawn(gameController);
        }
    }
}
