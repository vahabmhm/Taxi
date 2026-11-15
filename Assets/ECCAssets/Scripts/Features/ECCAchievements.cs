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

        // Taxi ( the same numbers as the mission types )
        DeliverPassengers = 12,
        PerfectStops = 13,
        DeliverVips = 14,
        EarnFares = 15,
        DeliverInOneShift = 16,
        FiveStarRides = 17,
        EscapePolice = 18,

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
            A("passengers_50", ECCStat.DeliverPassengers, 50, 150, "Icons/target", "مسافرکش", "{0} مسافر رو به مقصد برسون"),
            A("passengers_300", ECCStat.DeliverPassengers, 300, 400, "Icons/target", "راننده‌ی محبوب", "{0} مسافر رو به مقصد برسون"),
            A("passengers_1000", ECCStat.DeliverPassengers, 1000, 900, "Icons/target", "افسانه‌ی خیابان", "{0} مسافر رو به مقصد برسون"),
            A("perfect_50", ECCStat.PerfectStops, 50, 200, "Icons/check", "ترمز طلایی", "{0} بار درست وسط دایره ترمز کن"),
            A("perfect_300", ECCStat.PerfectStops, 300, 500, "Icons/check", "استاد ترمز", "{0} بار درست وسط دایره ترمز کن"),
            A("vip_10", ECCStat.DeliverVips, 10, 350, "Icons/crown", "راننده‌ی VIP", "{0} مسافر VIP رو برسون"),
            A("five_star_50", ECCStat.FiveStarRides, 50, 300, "Icons/star", "پنج ستاره", "{0} سفر پنج‌ستاره داشته باش"),
            A("shift_15", ECCStat.DeliverInOneShift, 15, 400, "Icons/clock", "شیفت طلایی", "در یک شیفت بی‌نهایت {0} مسافر برسون"),
            A("fares_5000", ECCStat.EarnFares, 5000, 450, "dollar", "کرایه‌جمع‌کن", "در مجموع {0} سکه کرایه جمع کن"),
            A("escape_10", ECCStat.EscapePolice, 10, 300, "Icons/shield", "فراری", "{0} بار از دست پلیس در برو"),
            A("runs_25", ECCStat.PlayRuns, 25, 100, "Icons/play", "راننده‌ی تازه‌کار", "{0} بار رانندگی رو شروع کن"),
            A("runs_100", ECCStat.PlayRuns, 100, 300, "Icons/play", "راننده‌ی کهنه‌کار", "{0} بار رانندگی رو شروع کن"),
            A("levels_10", ECCStat.CompleteLevels, 10, 150, "Icons/levels", "مرحله‌به‌مرحله", "{0} مرحله رو ببر"),
            A("levels_50", ECCStat.CompleteLevels, 50, 400, "Icons/levels", "فاتح شهر", "{0} مرحله رو ببر"),
            A("stars_30", ECCStat.TotalStars, 30, 200, "Icons/star", "ستاره‌جمع‌کن", "در مجموع {0} ستاره بگیر"),
            A("stars_150", ECCStat.TotalStars, 150, 600, "Icons/star", "آسمان پرستاره", "در مجموع {0} ستاره بگیر"),
            A("powerups_50", ECCStat.UsePowerUps, 50, 250, "Icons/shield", "استاد قدرت‌ها", "{0} بار از قدرت ویژه استفاده کن"),
            A("chests_3", ECCStat.ChestsOpened, 3, 250, "Icons/gift", "صندوق‌باز", "{0} صندوق جایزه‌ی فصل باز کن"),
        };

        /// <summary>
        /// Counters that keep the best single value instead of adding up
        /// </summary>
        public static bool KeepsBest(ECCStat stat)
        {
            return stat == ECCStat.SurviveSeconds || stat == ECCStat.ReachWantedLevel || stat == ECCStat.EarnInOneRun
                || stat == ECCStat.DestroyCopsInOneRun || stat == ECCStat.EndlessDistance || stat == ECCStat.DeliverInOneShift;
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
