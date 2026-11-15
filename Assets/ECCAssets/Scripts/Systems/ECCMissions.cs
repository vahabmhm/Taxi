using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    public enum ECCMissionType
    {
        DestroyCops = 0,        // Total police cars wrecked today
        CollectCash = 1,        // Total money picked up from the road today
        NearMisses = 2,         // Total close passes today
        SurviveSeconds = 3,     // Best single run, in seconds
        ReachWantedLevel = 4,   // Best wanted level in the endless mode
        CompleteLevels = 5,     // Normal levels won today ( replays count )
        UsePowerUps = 6,        // Power-ups used today ( picked up or bought )
        PlayRuns = 7,           // Chases started today
        EarnInOneRun = 8,       // Best money of a single run
        // 9 was ReachCombo ( removed with the combo multiplier, don't reuse the value )
        ThreeStarLevels = 10,   // Normal levels won with 3 stars today
        DestroyCopsInOneRun = 11, // Best police cars wrecked in a single run
    }

    public enum ECCMissionDifficulty
    {
        Easy = 0,
        Medium = 1,
        Hard = 2,
    }

    [Serializable]
    public class ECCMissionDefinition
    {
        [Tooltip("Stable id, saved with the progress. Never change it after release")]
        public string id;

        [Tooltip("Persian description. {0} is replaced with the target")]
        public string description;

        public ECCMissionDifficulty difficulty;
        public ECCMissionType type;
        public int target = 1;
        public int reward = 50;

        public string Text
        {
            get { return string.Format(description, target); }
        }

        /// <summary>
        /// Missions about a single run keep the best value instead of adding up
        /// </summary>
        public bool KeepsBest
        {
            get
            {
                return type == ECCMissionType.SurviveSeconds || type == ECCMissionType.ReachWantedLevel || type == ECCMissionType.EarnInOneRun
                    || type == ECCMissionType.DestroyCopsInOneRun;
            }
        }
    }

    /// <summary>
    /// The 25 mission definitions ( 10 easy, 9 medium, 6 hard ).
    /// The game uses Resources/ECCMissionCatalog if it exists, otherwise the defaults below.
    /// To edit: Assets > Create > Endless Car Chase > Mission Catalog, save it in a Resources folder, then "Fill With Default Missions"
    /// </summary>
    [CreateAssetMenu(fileName = "ECCMissionCatalog", menuName = "Endless Car Chase/Mission Catalog")]
    public class ECCMissionCatalog : ScriptableObject
    {
        const string ResourcePath = "ECCMissionCatalog";

        public ECCMissionDefinition[] missions = new ECCMissionDefinition[0];

        static ECCMissionCatalog instance;

        public static ECCMissionCatalog Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ECCMissionCatalog>(ResourcePath);

                    if (instance == null || instance.missions == null || instance.missions.Length == 0)
                    {
                        instance = CreateInstance<ECCMissionCatalog>();
                        instance.missions = CreateDefaultMissions();
                    }
                }

                return instance;
            }
        }

        public ECCMissionDefinition Get(string id)
        {
            foreach (ECCMissionDefinition mission in missions)
            {
                if (mission.id == id) return mission;
            }

            return null;
        }

        [ContextMenu("Fill With Default Missions")]
        void FillWithDefaults()
        {
            missions = CreateDefaultMissions();
        }

        static ECCMissionDefinition Mission(string id, ECCMissionDifficulty difficulty, ECCMissionType type, int target, int reward, string description)
        {
            return new ECCMissionDefinition { id = id, difficulty = difficulty, type = type, target = target, reward = reward, description = description };
        }

        public static ECCMissionDefinition[] CreateDefaultMissions()
        {
            ECCMissionDifficulty easy = ECCMissionDifficulty.Easy;
            ECCMissionDifficulty medium = ECCMissionDifficulty.Medium;
            ECCMissionDifficulty hard = ECCMissionDifficulty.Hard;

            return new[]
            {
                // Easy: one or two runs
                Mission("easy_destroy_5", easy, ECCMissionType.DestroyCops, 5, 60, "{0} ماشین پلیس را نابود کن"),
                Mission("easy_cash_100", easy, ECCMissionType.CollectCash, 30, 60, "{0} دلار از جاده جمع کن"),
                Mission("easy_nearmiss_5", easy, ECCMissionType.NearMisses, 5, 60, "{0} بار از بیخ گوش پلیس یا ماشین‌ها رد شو"),
                Mission("easy_survive_45", easy, ECCMissionType.SurviveSeconds, 45, 60, "در یک دور {0} ثانیه دوام بیاور"),
                Mission("easy_runs_3", easy, ECCMissionType.PlayRuns, 3, 50, "{0} بار تعقیب را شروع کن"),
                Mission("easy_powerups_2", easy, ECCMissionType.UsePowerUps, 2, 60, "{0} قدرت ویژه استفاده کن"),
                Mission("easy_levels_1", easy, ECCMissionType.CompleteLevels, 1, 70, "{0} مرحله را در مود مرحله‌ای تمام کن"),
                Mission("easy_wanted_2", easy, ECCMissionType.ReachWantedLevel, 2, 60, "در مود بی‌نهایت به سطح تعقیب {0} برس"),
                Mission("easy_earn_100", easy, ECCMissionType.EarnInOneRun, 100, 70, "در یک دور {0} سکه به دست بیاور"),
                Mission("easy_destroy_run_3", easy, ECCMissionType.DestroyCopsInOneRun, 3, 70, "در یک دور {0} ماشین پلیس را نابود کن"),

                // Medium: a few focused runs
                Mission("medium_destroy_15", medium, ECCMissionType.DestroyCops, 15, 120, "{0} ماشین پلیس را نابود کن"),
                Mission("medium_cash_300", medium, ECCMissionType.CollectCash, 90, 120, "{0} دلار از جاده جمع کن"),
                Mission("medium_nearmiss_15", medium, ECCMissionType.NearMisses, 15, 120, "{0} بار از بیخ گوش پلیس یا ماشین‌ها رد شو"),
                Mission("medium_survive_90", medium, ECCMissionType.SurviveSeconds, 90, 130, "در یک دور {0} ثانیه دوام بیاور"),
                Mission("medium_levels_2", medium, ECCMissionType.CompleteLevels, 2, 140, "{0} مرحله را در مود مرحله‌ای تمام کن"),
                Mission("medium_wanted_3", medium, ECCMissionType.ReachWantedLevel, 3, 130, "در مود بی‌نهایت به سطح تعقیب {0} برس"),
                Mission("medium_powerups_5", medium, ECCMissionType.UsePowerUps, 5, 120, "{0} قدرت ویژه استفاده کن"),
                Mission("medium_destroy_run_8", medium, ECCMissionType.DestroyCopsInOneRun, 8, 140, "در یک دور {0} ماشین پلیس را نابود کن"),
                Mission("medium_three_stars_1", medium, ECCMissionType.ThreeStarLevels, 1, 150, "{0} مرحله را با ۳ ستاره تمام کن"),

                // Hard: a skilled run or a long session
                Mission("hard_destroy_35", hard, ECCMissionType.DestroyCops, 35, 220, "{0} ماشین پلیس را نابود کن"),
                Mission("hard_cash_700", hard, ECCMissionType.CollectCash, 210, 220, "{0} دلار از جاده جمع کن"),
                Mission("hard_survive_150", hard, ECCMissionType.SurviveSeconds, 150, 250, "در یک دور {0} ثانیه دوام بیاور"),
                Mission("hard_wanted_5", hard, ECCMissionType.ReachWantedLevel, 5, 250, "در مود بی‌نهایت به سطح تعقیب {0} برس"),
                Mission("hard_earn_400", hard, ECCMissionType.EarnInOneRun, 400, 240, "در یک دور {0} سکه به دست بیاور"),
                Mission("hard_nearmiss_35", hard, ECCMissionType.NearMisses, 35, 250, "{0} بار از بیخ گوش پلیس یا ماشین‌ها رد شو"),
            };
        }
    }

    /// <summary>
    /// Today's missions: 4 or 5 different missions picked from the catalog each day ( easy, medium and hard mixed ),
    /// stable for the whole day. Progress comes from real game events ( ECCMissionTracker, level results ),
    /// rewards are claimed in the missions screen through idempotent coin transactions.
    /// </summary>
    public static class ECCMissions
    {
        // Gameplay limits used to skip missions that can't be done
        public const int MaxWantedLevel = 5;

        /// <summary>
        /// Called when progress or claimed state changes ( menu badge, open screen )
        /// </summary>
        public static event Action Changed;

        /// <summary>
        /// Today's missions. New missions are picked when the day changes
        /// </summary>
        public static List<ECCDailyMissionState> Today
        {
            get
            {
                string today = ECCDaily.Today;

                ECCSaveData data = ECCSave.Data;

                MigrateLegacyMissions(data);

                if (data.dailyMissionDay != today || data.dailyMissions.Count == 0)
                {
                    data.dailyMissionDay = today;
                    data.dailyMissions = Generate(today);

                    ECCSave.Save();
                }

                // Drop entries whose definition was removed from the catalog
                data.dailyMissions.RemoveAll(state => ECCMissionCatalog.Instance.Get(state.id) == null);

                return data.dailyMissions;
            }
        }

        /// <summary>
        /// The missions of the version with 3 daily missions are replaced. A finished, unclaimed one from today is paid once
        /// </summary>
        static void MigrateLegacyMissions(ECCSaveData data)
        {
            if (data.missions == null || data.missions.Count == 0) return;

            string legacyToday = DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            for (int index = 0; index < data.missions.Count; index++)
            {
                ECCMissionState legacy = data.missions[index];

                if (data.missionDay == legacyToday && legacy.claimed == false && legacy.progress >= legacy.target && legacy.reward > 0)
                {
                    ECCWallet.TryReward("legacy-mission-" + data.missionDay + "-" + index, legacy.reward);
                }
            }

            data.missions.Clear();
            ECCSave.Save();
        }

        /// <summary>
        /// Picks 4 or 5 missions for a day: 2 easy, 1-2 medium and 1 hard. The same day always gives the same missions
        /// </summary>
        public static List<ECCDailyMissionState> Generate(string day)
        {
            System.Random random = new System.Random(ECCDaily.Seed(day, "missions"));

            int count = random.Next(2) == 0 ? 4 : 5;

            int[] perDifficulty = count == 4 ? new[] { 2, 1, 1 } : new[] { 2, 2, 1 };

            List<ECCDailyMissionState> picked = new List<ECCDailyMissionState>();

            for (int difficulty = 0; difficulty < 3; difficulty++)
            {
                List<ECCMissionDefinition> pool = GetAvailable((ECCMissionDifficulty)difficulty);

                // Two missions of the same kind on one day would be boring
                pool.RemoveAll(mission => picked.Exists(state => ECCMissionCatalog.Instance.Get(state.id).type == mission.type));

                for (int index = 0; index < perDifficulty[difficulty] && pool.Count > 0; index++)
                {
                    ECCMissionDefinition mission = pool[random.Next(pool.Count)];

                    pool.RemoveAll(other => other.type == mission.type);

                    picked.Add(new ECCDailyMissionState { id = mission.id });
                }
            }

            // Fill up from any difficulty if a pool ran dry ( a small custom catalog )
            if (picked.Count < 4)
            {
                foreach (ECCMissionDefinition mission in ECCMissionCatalog.Instance.missions)
                {
                    if (picked.Count >= count) break;

                    if (IsAvailable(mission) && picked.Exists(state => state.id == mission.id) == false) picked.Add(new ECCDailyMissionState { id = mission.id });
                }
            }

            return picked;
        }

        static List<ECCMissionDefinition> GetAvailable(ECCMissionDifficulty difficulty)
        {
            List<ECCMissionDefinition> available = new List<ECCMissionDefinition>();

            foreach (ECCMissionDefinition mission in ECCMissionCatalog.Instance.missions)
            {
                if (mission.difficulty == difficulty && IsAvailable(mission)) available.Add(mission);
            }

            return available;
        }

        /// <summary>
        /// Only missions the player can actually do with the current game rules
        /// </summary>
        public static bool IsAvailable(ECCMissionDefinition mission)
        {
            if (mission == null || mission.target <= 0) return false;

            switch (mission.type)
            {
                case ECCMissionType.ReachWantedLevel: return mission.target <= MaxWantedLevel;
                default: return true;
            }
        }

        public static ECCMissionDefinition GetDefinition(ECCDailyMissionState state)
        {
            return state != null ? ECCMissionCatalog.Instance.Get(state.id) : null;
        }

        /// <summary>
        /// Adds progress to today's missions of a type. Single-run missions keep the best value instead
        /// </summary>
        public static void Report(ECCMissionType type, int amount)
        {
            if (amount <= 0) return;

            // The permanent achievements count the same things, without daily resets
            ECCAchievements.Report(type, amount);

            bool changed = false;

            foreach (ECCDailyMissionState state in Today)
            {
                ECCMissionDefinition mission = GetDefinition(state);

                if (mission == null || mission.type != type || state.progress >= mission.target) continue;

                int progress = mission.KeepsBest ? Mathf.Max(state.progress, amount) : state.progress + amount;

                if (progress == state.progress) continue;

                state.progress = Mathf.Min(progress, mission.target);
                changed = true;
            }

            if (changed)
            {
                ECCSave.Save();

                if (Changed != null) Changed();
            }
        }

        public static bool IsComplete(ECCDailyMissionState state)
        {
            ECCMissionDefinition mission = GetDefinition(state);

            return mission != null && state.progress >= mission.target;
        }

        public static bool CanClaim(ECCDailyMissionState state)
        {
            return state.claimed == false && IsComplete(state);
        }

        /// <summary>
        /// Completed missions whose reward wasn't taken yet ( the menu badge )
        /// </summary>
        public static int ClaimableCount
        {
            get
            {
                int count = 0;

                foreach (ECCDailyMissionState state in Today)
                {
                    if (CanClaim(state)) count++;
                }

                return count;
            }
        }

        public static bool HasRewardToClaim
        {
            get { return ClaimableCount > 0; }
        }

        /// <summary>
        /// Pays the reward of a completed mission once. Returns the coins paid, or 0
        /// </summary>
        public static int Claim(ECCDailyMissionState state)
        {
            if (CanClaim(state) == false) return 0;

            ECCMissionDefinition mission = GetDefinition(state);

            bool paid = ECCWallet.TryReward("mission-" + ECCSave.Data.dailyMissionDay + "-" + state.id, mission.reward);

            state.claimed = true;
            ECCSave.Save();

            if (Changed != null) Changed();

            return paid ? mission.reward : 0;
        }

        public static string GetDescription(ECCDailyMissionState state)
        {
            ECCMissionDefinition mission = GetDefinition(state);

            return mission != null ? mission.Text : string.Empty;
        }
    }
}
