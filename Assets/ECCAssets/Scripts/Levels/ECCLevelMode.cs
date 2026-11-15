using System.Collections;
using UnityEngine;
using EndlessCarChase.Levels;

namespace EndlessCarChase.Modes
{
    /// <summary>
    /// The levels mode: each level has a goal ( survive, destroy police, collect money, escape, destroy the boss, heist,
    /// reach the destination without damage, timed checkpoints ),
    /// and rewards up to 3 stars based on the health left when the goal is reached.
    /// </summary>
    public class ECCLevelMode : ECCGameModeBase
    {
        public override GameModeType ModeType
        {
            get { return GameModeType.Levels; }
        }

        public override bool ShowsOwnResultScreen
        {
            get { return true; }
        }

        public ECCLevelData Level { get; protected set; }

        // Progress of the current run
        public float Elapsed { get; private set; }
        public int CopsDestroyed { get; private set; }
        public int CashCollected { get; private set; }

        // Heist: the bag of money was picked up ( the escape point appears then )
        public bool HasLoot { get; private set; }
        float lootDistance = 1;

        // Checkpoints passed in a row
        public int CheckpointsPassed { get; private set; }

        /// <summary>
        /// Meters between two checkpoints, the seconds on the clock at the start and the seconds each checkpoint adds
        /// </summary>
        public const float CheckpointDistance = 95;
        public const float CheckpointSeconds = 15;

        internal ECCEscapePoint escapePoint;
        internal ECCCar boss;
        bool bossDefeated = false;

        ECCSpawnTuning spawnTuning;
        protected ECCLevelHUD hud;
        protected bool isRunning = false;

        // Extra seconds added to the time limit by a continuation ( rewarded +30 seconds, level continue )
        protected float bonusTime = 0;

        public override void Initialize(ECCGameController controller)
        {
            base.Initialize(controller);

            Level = ResolveLevel();
        }

        /// <summary>
        /// The level this mode plays. The normal levels mode plays the level selected in the level map
        /// </summary>
        protected virtual ECCLevelData ResolveLevel()
        {
            return ECCLevelCatalog.Instance.SelectedLevel;
        }

        /// <summary>
        /// The title shown in the HUD and the start message
        /// </summary>
        public virtual string HudTitle
        {
            get { return Level != null ? Level.DisplayName + ": " + Level.title : string.Empty; }
        }

        public override string GetHighScoreKey(string sceneKey)
        {
            return sceneKey + "Level" + (Level != null ? Level.id : string.Empty);
        }

        public override void OnGameStarted()
        {
            // The level may have been changed in the level select after the mode was attached
            Level = ResolveLevel();

            if (Level == null) return;

            ECCSpawnAroundObject spawner = gameController.GetComponent<ECCSpawnAroundObject>();

            if (spawner)
            {
                spawnTuning = new ECCSpawnTuning(spawner);
                spawnTuning.SetSpawnRateMultiplier(ECCSpawnTuning.GroupKind.Police, Level.copSpawnMultiplier);
            }

            ECCThemes.Apply(Level.theme, gameController, spawnTuning);

            ECCHazardSpawner hazards = gameController.GetComponent<ECCHazardSpawner>();

            if (hazards)
            {
                hazards.spikeStrips = Level.spikeStrips;
                hazards.roadblocks = Level.roadblocks;
            }

            ECCSpawnAroundObject.ObjectSpawned += OnObjectSpawned;

            ECCCar.aiSpeedBonus = Level.copSpeedBonus;

            ECCCar.CarDestroyed += OnCarDestroyed;
            ECCItem.Collected += OnItemCollected;

            switch (Level.objective)
            {
                case ECCObjectiveType.Escape:
                case ECCObjectiveType.NoDamage:
                    CreateEscapePoint();
                    break;

                case ECCObjectiveType.Heist:
                    CreateLoot();
                    break;

                case ECCObjectiveType.Checkpoints:
                    CreateNextCheckpoint();
                    break;

                case ECCObjectiveType.DestroyBoss:
                    StartCoroutine(SpawnBoss(3));
                    break;
            }

            if (Level.objective == ECCObjectiveType.NoDamage) ECCCar.PlayerDamaged += OnPlayerDamaged;

            // Night levels: headlights, siren lights, a dark sky ( the rainy road is already at night )
            if (Level.night && Level.theme != ECCLevelTheme.Rain) ECCNightMode.Enable(gameController);

            // The police helicopter joins a little after the start
            if (Level.helicopter) StartCoroutine(SpawnHelicopter(8));

            hud = ECCLevelHUD.Create(gameController, this);

            ShowObjectiveHint();

            isRunning = true;
        }

        void OnDestroy()
        {
            ECCCar.CarDestroyed -= OnCarDestroyed;
            ECCItem.Collected -= OnItemCollected;
            ECCSpawnAroundObject.ObjectSpawned -= OnObjectSpawned;
            ECCCar.PlayerDamaged -= OnPlayerDamaged;
        }

        IEnumerator SpawnHelicopter(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (isRunning && IsPlaying) ECCPoliceHelicopter.Spawn(gameController);
        }

        /// <summary>
        /// The lowest health, in percent of the car's full health, allowed on a «کمترین خسارت» level
        /// </summary>
        public const float MinHealthPercent = 60;

        /// <summary>
        /// «با کمترین خسارت تا انتها»: the run ends when the car's health falls below MinHealthPercent
        /// </summary>
        void OnPlayerDamaged(ECCCar car, float amount)
        {
            if (isRunning == false || Level == null || Level.objective != ECCObjectiveType.NoDamage) return;

            if (car.healthMax <= 0 || car.health / car.healthMax * 100 >= MinHealthPercent) return;

            isRunning = false;

            ECCToast.Show("خسارت زیاد شد!", ECCRuntimeUI.BadColor, "سلامتی ماشین نباید زیر " + ECCUIKit.PersianDigits((int)MinHealthPercent) + "٪ بیاید", 2.2f);

            gameController.Fail();
        }

        void OnObjectSpawned(Transform spawned)
        {
            if (enabled == false || Level == null) return;

            ECCCar car = spawned.GetComponent<ECCCar>();

            if (car == null) return;

            ECCCopVariant.Toughen(car, Level.chapterIndex);
            ECCCopVariant.RollAndApply(car, Level.interceptorChance, Level.rammerChance, Level.heavyChance);
        }

        /// <summary>
        /// The boss is the big armored police bus ( the garage's prison bus, driven by the police ) that arrives from
        /// ahead after a short delay. Without it, the van, the biggest police car, is made bigger instead
        /// </summary>
        IEnumerator SpawnBoss(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (isRunning == false || gameController.playerObject == null || spawnTuning == null) yield break;

            System.Collections.Generic.List<ECCCar> policePrefabs = spawnTuning.GetPolicePrefabs();

            if (policePrefabs.Count == 0) yield break;

            ECCCar van = policePrefabs.Find(car => car.name.Contains("Van"));
            if (van == null) van = policePrefabs[0];

            ECCCar bus = FindBossBus();
            ECCCar prefab = bus ? bus : van;

            Transform player = gameController.playerObject.transform;

            Vector3 forward = player.forward;
            forward.y = 0;

            // Ahead of the player, but out of the camera's view so it drives in
            Vector3 position = ECCRuntimeVisuals.OutOfView(player.position, forward, 35, gameController.groundLayer);

            boss = Instantiate(prefab, position, Quaternion.LookRotation(-forward));
            boss.name = bus ? "BossBus" : "BossTruck";

            // The bus is a player car in the garage: it takes the police van's stats, so the boss is as tough as before
            if (bus)
            {
                boss.speed = van.speed;
                boss.rotateSpeed = van.rotateSpeed;
                boss.health = van.health;
                boss.damage = van.damage;
                boss.chaseAngleRange = van.chaseAngleRange;
                boss.speedVariation = van.speedVariation;
            }

            ECCCopVariant.Apply(boss, ECCCopType.Boss);
            ECCCopVariant.Toughen(boss, Level.chapterIndex);
            ECCCopVariant.ToughenBoss(boss);

            ECCHintBanner.Show("Icons/crown", ECCCarTheme.CandyRed, BossName + " رسید!", "اون رو به پلیس‌ها، صخره‌ها و میخ‌ها بکوبون تا نابود بشه");
        }

        /// <summary>
        /// The prison bus among the garage's cars, or null
        /// </summary>
        ECCCar FindBossBus()
        {
            ECCShop shop = gameController.shopMenu;

            if (shop == null || shop.items == null) return null;

            foreach (Types.ShopItem item in shop.items)
            {
                if (item != null && item.itemIcon && item.itemIcon.name.Contains("Prison_Bus"))
                {
                    ECCCar car = item.itemIcon.GetComponent<ECCCar>();

                    if (car) return car;
                }
            }

            return null;
        }

        void CreateEscapePoint()
        {
            escapePoint = ECCEscapePoint.Create(PointAhead(Level.target, 60));
        }

        /// <summary>
        /// A spot on the road some distance ahead of the player, within a spread of angles, so the first part of the drive
        /// is straightforward. On the highway it's straight up ( or down ) the road
        /// </summary>
        Vector3 PointAhead(float distance, float spread)
        {
            Transform player = gameController.playerObject.transform;

            Vector3 direction = Quaternion.Euler(0, Random.Range(-spread, spread), 0) * player.forward;
            direction.y = 0;

            if (ECCHighwayWorld.IsActive) direction = new Vector3(0, 0, player.forward.z < 0 ? -1 : 1);

            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;

            return ECCRuntimeVisuals.SnapToGround(player.position + direction.normalized * distance, gameController.groundLayer);
        }

        /// <summary>
        /// Heist: the bag of money is a part of the way; the escape point appears once it's picked up
        /// </summary>
        void CreateLoot()
        {
            lootDistance = Mathf.Clamp(Level.target * 0.4f, 90, 220);

            escapePoint = ECCEscapePoint.CreateLoot(PointAhead(lootDistance, 50));
        }

        void PickUpLoot()
        {
            HasLoot = true;

            if (escapePoint) Destroy(escapePoint.gameObject);

            escapePoint = ECCEscapePoint.Create(PointAhead(Level.target, 70));
            closestToEscape = float.MaxValue;

            // The alarm went off: more police come
            if (spawnTuning != null) spawnTuning.SetSpawnRateMultiplier(ECCSpawnTuning.GroupKind.Police, Level.copSpawnMultiplier * 0.75f);

            ECCHaptics.Vibrate(ECCHaptics.Kind.Medium);

            ECCHintBanner.Show("Icons/gift", ECCCarTheme.CandyOrange, "کیف پول مال تو شد!", "آژیر خطر به صدا دراومد؛ حالا خودت رو به نقطه‌ی فرار برسون");
        }

        void CreateNextCheckpoint()
        {
            if (escapePoint) Destroy(escapePoint.gameObject);

            escapePoint = ECCEscapePoint.CreateCheckpoint(PointAhead(CheckpointDistance, 55));
            closestToEscape = float.MaxValue;
        }

        void PassCheckpoint()
        {
            CheckpointsPassed++;

            ECCHaptics.Vibrate(ECCHaptics.Kind.Medium);

            if (CheckpointsPassed >= Level.target)
            {
                if (escapePoint) Destroy(escapePoint.gameObject);
                return;
            }

            bonusTime += CheckpointSeconds;
            lowTimeWarned = false;

            ECCHintBanner.Show("Icons/clock", ECCRuntimeUI.GoodColor, "چک‌پوینت " + ECCUIKit.PersianDigits(CheckpointsPassed) + " از " + ECCUIKit.PersianDigits(Level.target), "+" + ECCUIKit.PersianDigits((int)CheckpointSeconds) + " ثانیه وقت اضافه", 1.6f, ECCHintBanner.Kind.Urgent);

            CreateNextCheckpoint();
        }

        void Update()
        {
            if (isRunning == false || IsPlaying == false) return;

            Elapsed += Time.deltaTime;

            Transform playerTransform = gameController.playerObject ? gameController.playerObject.transform : null;

            if (Level.objective == ECCObjectiveType.Heist && HasLoot == false && escapePoint && escapePoint.IsReachedBy(playerTransform)) PickUpLoot();

            if (Level.objective == ECCObjectiveType.Checkpoints && escapePoint && escapePoint.IsReachedBy(playerTransform)) PassCheckpoint();

            if (IsGoalReached())
            {
                isRunning = false;

                // At the destination the car brakes to a full stop, and the win screen comes two seconds later
                if (Level.objective == ECCObjectiveType.Escape || Level.objective == ECCObjectiveType.Heist || Level.objective == ECCObjectiveType.NoDamage)
                {
                    StartCoroutine(BrakeToStop(gameController.playerObject, ArrivalBrakeTime));

                    ECCToast.Show("به مقصد رسیدی!", ECCRuntimeUI.GoodColor, string.Empty, ArrivalBrakeTime + ArrivalWinDelay);

                    gameController.Victory(ArrivalBrakeTime + ArrivalWinDelay);
                }
                else
                {
                    gameController.Victory();
                }

                return;
            }

            if (TimeLeft <= 0)
            {
                isRunning = false;

                OnTimeUp();

                return;
            }

            UpdateHints();
        }

        // ----- Hints: the banner at the top that tells the player what to do -----

        // How much farther than the closest it got the player may drive before being warned, the seconds between two
        // warnings, and the time left that brings the hurry-up hint
        const float AwayWarningDistance = 30;
        const float AwayWarningCooldown = 10;
        const float LowTimeWarning = 15;

        float closestToEscape = float.MaxValue;
        float awayWarningTime = 0;
        bool lowTimeWarned = false;
        bool bossHalfWarned = false;

        /// <summary>
        /// The big vehicle of boss levels, as the player sees it
        /// </summary>
        public string BossName
        {
            get { return "اتوبوس زرهی پلیس"; }
        }

        /// <summary>
        /// At the start: what this level asks for, in a few friendly words
        /// </summary>
        void ShowObjectiveHint()
        {
            string target = ECCUIKit.PersianDigits(Level.target);

            switch (Level.objective)
            {
                case ECCObjectiveType.CollectCash:
                    ECCHintBanner.Show("dollar", ECCCarTheme.CandyGreen, "تا می‌تونی دلار جمع کن!", "برای بردن این مرحله باید " + target + " دلار جمع کنی");
                    break;

                case ECCObjectiveType.DestroyCops:
                    ECCHintBanner.Show("Icons/fire", ECCCarTheme.CandyRed, "پلیس‌ها رو نابود کن!", target + " ماشین پلیس رو به هم، به موانع یا به میخ‌ها بکوبون");
                    break;

                case ECCObjectiveType.Escape:
                    ECCHintBanner.Show("Icons/target", ECCCarTheme.CandyBlue, "به مقصد برس!", "دنبال فلش سبز برو و خودت رو به نقطه‌ی فرار برسون (" + target + " متر)");
                    break;

                case ECCObjectiveType.DestroyBoss:
                    ECCHintBanner.Show("Icons/crown", ECCCarTheme.CandyRed, "هدف: نابودی " + BossName, "چند ثانیه‌ی دیگه از راه می‌رسه؛ آماده باش");
                    break;

                case ECCObjectiveType.Heist:
                    ECCHintBanner.Show("Icons/gift", ECCCarTheme.CandyOrange, "دزدی و فرار!", "دنبال فلش برو، کیف پول رو بردار و بعد به نقطه‌ی فرار برسون");
                    break;

                case ECCObjectiveType.NoDamage:
                    ECCHintBanner.Show("Icons/shield", ECCCarTheme.CandyBlue, "با کمترین خسارت تا انتها!", target + " متر تا مقصد برو؛ سلامتی ماشین نباید زیر " + ECCUIKit.PersianDigits((int)MinHealthPercent) + "٪ بیاید");
                    break;

                case ECCObjectiveType.Checkpoints:
                    ECCHintBanner.Show("Icons/bolt", ECCCarTheme.CandyBlue, "چک‌پوینت‌ها!", "از " + target + " چک‌پوینت آبی رد شو؛ هر کدوم " + ECCUIKit.PersianDigits((int)CheckpointSeconds) + " ثانیه وقت اضافه می‌ده");
                    break;

                default:
                    ECCHintBanner.Show("Icons/heart", ECCCarTheme.CandyGreen, "زنده بمون!", target + " ثانیه از دست پلیس‌ها فرار کن و ماشینت رو سالم نگه دار");
                    break;
            }
        }

        /// <summary>
        /// During the run: a warning when driving away from the destination, a hurry-up when time runs short, and a push
        /// when the boss is half destroyed
        /// </summary>
        void UpdateHints()
        {
            awayWarningTime -= Time.deltaTime;

            if (ECCObjectives.UsesDestination(Level.objective) && escapePoint)
            {
                float distance = DistanceToEscape;

                closestToEscape = Mathf.Min(closestToEscape, distance);

                if (distance > closestToEscape + AwayWarningDistance && awayWarningTime <= 0)
                {
                    ECCHintBanner.Show("Icons/target", ECCCarTheme.CandyRed, "داری از مقصد دور می‌شی!", "دور بزن و دنبال فلش سبز برو تا به نقطه‌ی فرار برسی");

                    awayWarningTime = AwayWarningCooldown;

                    // Warned again only if it keeps going the wrong way
                    closestToEscape = distance;
                }
            }

            if (lowTimeWarned == false && TimeLeft <= LowTimeWarning && TimeLimitTotal > LowTimeWarning * 2 && Progress < 0.95f)
            {
                lowTimeWarned = true;

                ECCHintBanner.Show("Icons/clock", ECCCarTheme.CandyOrange, "فقط " + ECCUIKit.PersianDigits((int)LowTimeWarning) + " ثانیه مونده!", HurryText());
            }

            if (bossHalfWarned == false && boss && boss.healthMax > 0 && boss.health > 0 && boss.health <= boss.healthMax * 0.5f)
            {
                bossHalfWarned = true;

                ECCHintBanner.Show("Icons/crown", ECCCarTheme.CandyGreen, "نصف جونش رفته!", "ادامه بده، " + BossName + " داره کم میاره");
            }
        }

        string HurryText()
        {
            switch (Level.objective)
            {
                case ECCObjectiveType.DestroyCops: return "هنوز " + ECCUIKit.PersianDigits(Mathf.Max(0, Level.target - CopsDestroyed)) + " پلیس دیگه مونده؛ سریع‌تر بکوبونشون";
                case ECCObjectiveType.CollectCash: return "هنوز " + ECCUIKit.PersianDigits(Mathf.Max(0, Level.target - CashCollected)) + " دلار دیگه لازم داری";
                case ECCObjectiveType.Escape: return "با تمام سرعت به سمت مقصد برو";
                case ECCObjectiveType.DestroyBoss: return "زودتر " + BossName + " رو نابود کن";
                case ECCObjectiveType.Heist: return HasLoot ? "با کیف پول به سمت نقطه‌ی فرار برو" : "سریع‌تر کیف پول رو بردار";
                case ECCObjectiveType.NoDamage: return "با احتیاط ولی سریع به مقصد برو";
                case ECCObjectiveType.Checkpoints: return "خودت رو به چک‌پوینت بعدی برسون تا وقت اضافه بگیری";
                default: return "یه کم دیگه دووم بیار";
            }
        }

        // Seconds the car takes to brake at the destination, and the wait after it stops before the win screen
        const float ArrivalBrakeTime = 0.6f;
        const float ArrivalWinDelay = 2;

        /// <summary>
        /// Slows the car down to a full stop ( the steering is already off once the run is won )
        /// </summary>
        IEnumerator BrakeToStop(ECCCar car, float duration)
        {
            if (car == null) yield break;

            float startSpeed = car.speed;

            for (float time = 0; time < duration && car; time += Time.deltaTime)
            {
                car.speed = Mathf.Lerp(startSpeed, 0, Mathf.SmoothStep(0, 1, time / duration));

                yield return null;
            }

            if (car) car.speed = 0;
        }

        /// <summary>
        /// The time limit ran out while the player is still alive
        /// </summary>
        protected virtual void OnTimeUp()
        {
            int seconds = ECCEconomyConfig.Instance.levelContinueSeconds;

            // Once per run: freeze the run and offer extra time for a rewarded video
            if (timeContinueUsed == false && seconds > 0)
            {
                timeContinueUsed = true;

                Time.timeScale = 0;

                ECCContinueOfferUI.Show(new ECCContinueOfferUI.Options
                {
                    title = "وقت تمام شد!",
                    reason = ProgressText,
                    videoLabel = "تماشای ویدئو و " + ECCUIKit.PersianDigits(seconds) + " ثانیه وقت بیشتر",
                    videoTag = "+" + ECCUIKit.PersianDigits(seconds) + " ثانیه",
                    placement = "level_time",
                    onVideoRewarded = delegate { ContinueWithExtraTime(seconds); },
                    onDeclined = delegate
                    {
                        Time.timeScale = 1;
                        gameController.Fail();
                    },
                });

                return;
            }

            ECCToast.Show("وقت تمام شد!", ECCRuntimeUI.BadColor);

            gameController.Fail();
        }

        // The rewarded extra time was offered in this run
        bool timeContinueUsed = false;

        /// <summary>
        /// Resumes the same run, with the same board state, after a time-up pause, adding seconds to the time limit
        /// </summary>
        public void ContinueWithExtraTime(float seconds)
        {
            bonusTime += seconds;

            // The hurry-up hint can come again for the extra time
            lowTimeWarned = false;

            isRunning = true;

            Time.timeScale = 1;
        }

        bool IsGoalReached()
        {
            switch (Level.objective)
            {
                case ECCObjectiveType.DestroyCops: return CopsDestroyed >= Level.target;
                case ECCObjectiveType.CollectCash: return CashCollected >= Level.target;
                case ECCObjectiveType.Escape:
                case ECCObjectiveType.NoDamage:
                    return escapePoint && gameController.playerObject && escapePoint.IsReachedBy(gameController.playerObject.transform);
                case ECCObjectiveType.Heist: return HasLoot && escapePoint && gameController.playerObject && escapePoint.IsReachedBy(gameController.playerObject.transform);
                case ECCObjectiveType.Checkpoints: return CheckpointsPassed >= Level.target;
                case ECCObjectiveType.DestroyBoss: return bossDefeated;
                default: return Elapsed >= Level.target;
            }
        }

        /// <summary>
        /// Seconds left before failing, or infinity when the level has no time limit
        /// </summary>
        public float TimeLeft
        {
            get
            {
                if (Level == null || Level.timeLimit <= 0 || Level.objective == ECCObjectiveType.Survive) return float.PositiveInfinity;

                return Mathf.Max(0, Level.timeLimit + bonusTime - Elapsed);
            }
        }

        /// <summary>
        /// The whole time limit, with the extra time of continuations
        /// </summary>
        public float TimeLimitTotal
        {
            get { return Level != null ? Level.timeLimit + bonusTime : 0; }
        }

        /// <summary>
        /// Progress towards the goal, from 0 to 1
        /// </summary>
        public float Progress
        {
            get
            {
                if (Level == null) return 0;

                switch (Level.objective)
                {
                    case ECCObjectiveType.DestroyCops: return Mathf.Clamp01((float)CopsDestroyed / Level.target);
                    case ECCObjectiveType.CollectCash: return Mathf.Clamp01((float)CashCollected / Level.target);
                    case ECCObjectiveType.Escape:
                    case ECCObjectiveType.NoDamage:
                        return Mathf.Clamp01(1 - DistanceToEscape / Level.target);
                    case ECCObjectiveType.Heist:
                        return HasLoot ? 0.5f + 0.5f * Mathf.Clamp01(1 - DistanceToEscape / Level.target) : 0.5f * Mathf.Clamp01(1 - DistanceToEscape / lootDistance);
                    case ECCObjectiveType.Checkpoints: return Mathf.Clamp01((float)CheckpointsPassed / Mathf.Max(1, Level.target));
                    case ECCObjectiveType.DestroyBoss: return bossDefeated ? 1 : (boss ? Mathf.Clamp01(1 - boss.health / Mathf.Max(1, boss.healthMax)) : 0);
                    default: return Mathf.Clamp01(Elapsed / Level.target);
                }
            }
        }

        public float DistanceToEscape
        {
            get
            {
                if (escapePoint == null || gameController.playerObject == null) return 0;

                return ECCRuntimeVisuals.FlatDistance(gameController.playerObject.transform.position, escapePoint.transform.position);
            }
        }

        /// <summary>
        /// The goal progress as text, like "Police destroyed: 2 / 6"
        /// </summary>
        public string ProgressText
        {
            get
            {
                if (Level == null) return string.Empty;

                switch (Level.objective)
                {
                    case ECCObjectiveType.DestroyCops: return "پلیس نابودشده: " + CopsDestroyed + " / " + Level.target;
                    case ECCObjectiveType.CollectCash: return "پول: " + CashCollected + " / " + Level.target;
                    case ECCObjectiveType.Escape: return "فاصله تا نقطه‌ی فرار: " + Mathf.RoundToInt(DistanceToEscape) + " متر";
                    case ECCObjectiveType.Heist: return (HasLoot ? "فاصله تا نقطه‌ی فرار: " : "فاصله تا کیف پول: ") + Mathf.RoundToInt(DistanceToEscape) + " متر";
                    case ECCObjectiveType.NoDamage: return "با کمترین خسارت تا مقصد: " + Mathf.RoundToInt(DistanceToEscape) + " متر";
                    case ECCObjectiveType.Checkpoints: return "چک‌پوینت: " + CheckpointsPassed + " / " + Level.target;
                    case ECCObjectiveType.DestroyBoss: return boss ? "سلامت " + BossName + ": " + Mathf.CeilToInt(Mathf.Max(0, boss.health) / Mathf.Max(1, boss.healthMax) * 100) + "%" : "منتظر " + BossName + " باش...";
                    default: return "زنده بمان: " + Mathf.FloorToInt(Elapsed) + " / " + Level.target;
                }
            }
        }

        void OnCarDestroyed(ECCCar car)
        {
            if (isRunning == false || car.IsPlayer) return;

            if (car == boss) bossDefeated = true;
            else CopsDestroyed++;
        }

        void OnItemCollected(ECCItem item)
        {
            if (isRunning) CashCollected += item.CashValue;
        }

        public override void OnGameOver()
        {
            isRunning = false;

            ECCHintBanner.Clear();

            if (hud) hud.Hide();
        }

        /// <summary>
        /// Stars for a completed level: 1 for finishing, more for finishing with more health left
        /// </summary>
        int CalculateStars()
        {
            ECCCar player = gameController.playerObject;

            float healthLeft = player && player.healthMax > 0 ? player.health / player.healthMax : 0;

            if (healthLeft >= Level.threeStarHealth) return 3;
            if (healthLeft >= Level.twoStarHealth) return 2;

            return 1;
        }

        public override void ShowResultScreen(bool victory)
        {
            isRunning = false;

            ECCHintBanner.Clear();

            if (hud) hud.Hide();

            int stars = 0;
            int reward = 0;
            bool firstCompletion = false;

            if (Level != null) ECCLevelProgress.RecordResult(Level, victory);

            if (victory && Level != null)
            {
                stars = CalculateStars();

                firstCompletion = ECCSave.SetStars(Level.id, stars);

                // The full reward the first time, a quarter for replays
                reward = firstCompletion ? Level.reward : Level.reward / 4;

                ECCWallet.Add(reward);

                ECCMissions.Report(ECCMissionType.CompleteLevels, 1);

                if (stars >= 3) ECCMissions.Report(ECCMissionType.ThreeStarLevels, 1);

                // Distinct normal level wins count towards the one-time review prompt
                ECCReviewPrompt.RecordNormalWin(Level.id);

                // Permanent achievements for the special goals
                if (Level.objective == ECCObjectiveType.DestroyBoss) ECCAchievements.Report(ECCStat.BossesDefeated, 1);
                if (Level.objective == ECCObjectiveType.Heist) ECCAchievements.Report(ECCStat.HeistsWon, 1);

                // A chapter chest that just became ready
                for (int chest = 0; chest < ECCStarChests.ChestCount && this is ECCDailyChallengeMode == false; chest++)
                {
                    if (ECCStarChests.CanOpen(Level.chapterIndex, chest))
                    {
                        ECCToast.Show("صندوق جایزه‌ی فصل آماده‌ست!", ECCRuntimeUI.StarColor, "از نقشه‌ی مراحل بازش کن", 2.2f);
                        break;
                    }
                }
            }

            ECCLevelResultUI.Show(gameController, Level, victory, stars, reward, gameController.score);

            // Freeze the game behind the result screen
            Time.timeScale = 0;
        }
    }
}
