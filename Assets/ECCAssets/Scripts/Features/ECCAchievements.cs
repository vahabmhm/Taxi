using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// What an achievement counts. The first values are the daily mission counters ( ECCMissionType ), reported through
    /// ECCMissions.Report; the others are reported directly
    /// </summary>
    public enum ECCStat
    {
        DestroyCops = 0,
        CollectCash = 1,
        NearMisses = 2,
        SurviveSeconds = 3,
        ReachWantedLevel = 4,
        CompleteLevels = 5,
        UsePowerUps = 6,
        PlayRuns = 7,
        EarnInOneRun = 8,
        // 9 was ReachCombo ( removed )
        ThreeStarLevels = 10,
        DestroyCopsInOneRun = 11,

        TotalStars = 100,      // Stars of all the levels ( read from the level progress, not counted )
        BossesDefeated = 101,
        HeistsWon = 102,
        EndlessDistance = 103, // Longest endless run, in meters
        ChestsOpened = 104,
    }

    public class ECCAchievement
    {
        public string id;
        public ECCStat stat;
        public int target;
        public int reward;
        public string title;
        public string description; // {0} is the target
        public string icon;

        public string Text
        {
            get { return string.Format(description, ECCUIKit.PersianDigits(target)); }
        }
    }

    /// <summary>
    /// Permanent achievements, next to the daily missions: long goals like «۱۰۰۰ ماشین پلیس رو نابود کن», each with a
    /// one-time coin reward claimed from the «دستاوردها» screen. Progress never resets. Rewards are paid through the shared
    /// wallet with a stable transaction id, so a reward can't be paid twice.
    /// </summary>
    public static class ECCAchievements
    {
        public static event Action Changed;

        static ECCAchievement A(string id, ECCStat stat, int target, int reward, string icon, string title, string description)
        {
            return new ECCAchievement { id = id, stat = stat, target = target, reward = reward, icon = icon, title = title, description = description };
        }

        public static readonly ECCAchievement[] All =
        {
            A("cops_100", ECCStat.DestroyCops, 100, 150, "Icons/fire", "دردسر پلیس", "{0} ماشین پلیس رو نابود کن"),
            A("cops_500", ECCStat.DestroyCops, 500, 400, "Icons/fire", "کابوس پلیس", "{0} ماشین پلیس رو نابود کن"),
            A("cops_1000", ECCStat.DestroyCops, 1000, 900, "Icons/fire", "افسانه‌ی خیابان", "{0} ماشین پلیس رو نابود کن"),
            A("cops_run_20", ECCStat.DestroyCopsInOneRun, 20, 300, "Icons/fire", "طوفان آهن", "در یک دور {0} ماشین پلیس رو نابود کن"),
            A("cash_500", ECCStat.CollectCash, 500, 150, "dollar", "جیب‌پرکن", "در مجموع {0} دلار از جاده جمع کن"),
            A("cash_3000", ECCStat.CollectCash, 3000, 500, "dollar", "دلارباز", "در مجموع {0} دلار از جاده جمع کن"),
            A("nearmiss_50", ECCStat.NearMisses, 50, 150, "Icons/bolt", "از بیخ گوش", "{0} بار از بیخ گوش ماشین‌ها رد شو"),
            A("nearmiss_300", ECCStat.NearMisses, 300, 450, "Icons/bolt", "اعصاب فولادی", "{0} بار از بیخ گوش ماشین‌ها رد شو"),
            A("runs_25", ECCStat.PlayRuns, 25, 100, "Icons/play", "راننده‌ی تازه‌کار", "{0} بار تعقیب رو شروع کن"),
            A("runs_100", ECCStat.PlayRuns, 100, 300, "Icons/play", "راننده‌ی کهنه‌کار", "{0} بار تعقیب رو شروع کن"),
            A("levels_10", ECCStat.CompleteLevels, 10, 150, "Icons/levels", "مرحله‌به‌مرحله", "{0} مرحله رو ببر"),
            A("levels_50", ECCStat.CompleteLevels, 50, 400, "Icons/levels", "فاتح شهرها", "{0} مرحله رو ببر"),
            A("stars_30", ECCStat.TotalStars, 30, 200, "Icons/star", "ستاره‌جمع‌کن", "در مجموع {0} ستاره بگیر"),
            A("stars_150", ECCStat.TotalStars, 150, 600, "Icons/star", "آسمان پرستاره", "در مجموع {0} ستاره بگیر"),
            A("boss_3", ECCStat.BossesDefeated, 3, 300, "Icons/crown", "شکارچی باس", "{0} اتوبوس زرهی پلیس رو نابود کن"),
            A("heist_5", ECCStat.HeistsWon, 5, 300, "Icons/gift", "دزد حرفه‌ای", "{0} مرحله‌ی دزدی و فرار رو ببر"),
            A("wanted_6", ECCStat.ReachWantedLevel, 6, 400, "Icons/target", "تحت تعقیب هوایی", "در بی‌نهایت به سطح تعقیب {0} برس تا هلیکوپتر بیاد"),
            A("survive_180", ECCStat.SurviveSeconds, 180, 350, "Icons/clock", "جون‌سخت", "در یک دور {0} ثانیه دوام بیار"),
            A("distance_3000", ECCStat.EndlessDistance, 3000, 400, "Icons/play", "جاده‌ی بی‌پایان", "در بی‌نهایت یک‌جا {0} متر برو"),
            A("powerups_50", ECCStat.UsePowerUps, 50, 250, "Icons/shield", "استاد قدرت‌ها", "{0} بار از قدرت ویژه استفاده کن"),
            A("chests_3", ECCStat.ChestsOpened, 3, 250, "Icons/gift", "صندوق‌باز", "{0} صندوق جایزه‌ی فصل باز کن"),
        };

        /// <summary>
        /// Counters that keep the best single value instead of adding up
        /// </summary>
        public static bool KeepsBest(ECCStat stat)
        {
            return stat == ECCStat.SurviveSeconds || stat == ECCStat.ReachWantedLevel || stat == ECCStat.EarnInOneRun
                || stat == ECCStat.DestroyCopsInOneRun || stat == ECCStat.EndlessDistance;
        }

        static ECCStatRecord GetRecord(ECCStat stat, bool create)
        {
            foreach (ECCStatRecord record in ECCSave.Data.achievementStats)
            {
                if (record.stat == (int)stat) return record;
            }

            if (create == false) return null;

            ECCStatRecord newRecord = new ECCStatRecord { stat = (int)stat };
            ECCSave.Data.achievementStats.Add(newRecord);

            return newRecord;
        }

        public static int GetValue(ECCStat stat)
        {
            if (stat == ECCStat.TotalStars) return TotalStars;

            ECCStatRecord record = GetRecord(stat, false);

            return record != null ? record.value : 0;
        }

        static int TotalStars
        {
            get
            {
                int stars = 0;

                foreach (ECCLevelRecord level in ECCSave.Data.levels) stars += Mathf.Clamp(level.stars, 0, 3);

                return stars;
            }
        }

        /// <summary>
        /// Adds to a counter ( or raises a best value ). Saves only when something changed
        /// </summary>
        public static void Report(ECCStat stat, int amount)
        {
            if (amount <= 0 || stat == ECCStat.TotalStars) return;

            ECCStatRecord record = GetRecord(stat, true);

            int value = KeepsBest(stat) ? Mathf.Max(record.value, amount) : (int)Mathf.Min((long)record.value + amount, int.MaxValue);

            if (value == record.value) return;

            int before = ClaimableCount;

            record.value = value;

            ECCSave.Save();

            int after = ClaimableCount;

            // A newly completed achievement: a short message during the chase
            if (after > before) ECCToast.Show("دستاورد جدید!", ECCRuntimeUI.StarColor, "جایزه‌اش رو از «دستاوردها» بگیر", 2);

            if (Changed != null) Changed();
        }

        public static void Report(ECCMissionType type, int amount)
        {
            Report((ECCStat)(int)type, amount);
        }

        public static bool IsClaimed(ECCAchievement achievement)
        {
            return ECCSave.Data.achievementsClaimed.Contains(achievement.id) || ECCWallet.IsPaid(TransactionId(achievement));
        }

        public static bool IsComplete(ECCAchievement achievement)
        {
            return GetValue(achievement.stat) >= achievement.target;
        }

        public static bool CanClaim(ECCAchievement achievement)
        {
            return IsComplete(achievement) && IsClaimed(achievement) == false;
        }

        static string TransactionId(ECCAchievement achievement)
        {
            return "achievement:" + achievement.id;
        }

        public static int ClaimableCount
        {
            get
            {
                int count = 0;

                foreach (ECCAchievement achievement in All)
                {
                    if (CanClaim(achievement)) count++;
                }

                return count;
            }
        }

        /// <summary>
        /// Pays a completed achievement's reward once. Returns the coins paid ( 0 if it can't be claimed )
        /// </summary>
        public static int Claim(ECCAchievement achievement)
        {
            if (CanClaim(achievement) == false) return 0;

            ECCSave.Data.achievementsClaimed.Add(achievement.id);
            ECCSave.Save(false);

            bool paid = ECCWallet.TryReward(TransactionId(achievement), achievement.reward);

            if (Changed != null) Changed();

            return paid ? achievement.reward : 0;
        }
    }
}
