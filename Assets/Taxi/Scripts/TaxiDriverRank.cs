using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The driver's rank, kept across all runs ( like the rating of a ride-hailing driver ): every passenger the player
    /// delivers, in any mode, counts as a ride and their rating ( 1 to 5 stars ) joins the latest ratings. The average of
    /// the latest RatingWindow ratings is the driver's rating. A rank needs enough rides and a high enough rating; once
    /// reached it is kept ( a few bad rides lower the rating, never the rank ).
    /// Every rank gives a little more for each fare and opens something new:
    /// 1 «دنبال اون ماشین برو!» passengers in the shift, 2 passengers wait longer, 3 VIPs come more often,
    /// 4 a golden roof sign
    /// </summary>
    public static class TaxiDriverRank
    {
        public struct Rank
        {
            public string title;
            public int rides;          // Rides needed
            public float rating;       // Rating needed ( average of the latest ratings )
            public float fareBonus;    // 0.05 = 5% more for every fare
            public string perk;        // What the rank opens, in Persian
        }

        public const int RatingWindow = 50;

        public static readonly Rank[] Ranks =
        {
            new Rank { title = "راننده‌ی تازه‌کار", rides = 0, rating = 0, fareBonus = 0, perk = "اول راه؛ مسافرها رو سالم برسون" },
            new Rank { title = "راننده‌ی شهر", rides = 15, rating = 3.0f, fareBonus = 0.03f, perk = "مسافر «دنبال اون ماشین برو!» توی شیفت میاد" },
            new Rank { title = "راننده‌ی حرفه‌ای", rides = 50, rating = 3.8f, fareBonus = 0.06f, perk = "مسافرها ۸ ثانیه بیشتر منتظرت می‌مونن" },
            new Rank { title = "راننده‌ی کارکشته", rides = 120, rating = 4.2f, fareBonus = 0.1f, perk = "مسافرهای VIP زودتر توی شیفت پیدا می‌شن" },
            new Rank { title = "سلطان خیابان", rides = 250, rating = 4.5f, fareBonus = 0.15f, perk = "علامت طلایی روی سقف تاکسی" },
        };

        public const int FollowRank = 1;
        public const int PatienceRank = 2;
        public const int VipRank = 3;
        public const int GoldenSignRank = 4;

        public const float PatienceBonus = 8;
        public const float VipIntervalScale = 0.8f;

        /// <summary>
        /// A new rank was reached ( its index )
        /// </summary>
        public static event Action<int> RankUp;

        public static event Action Changed;

        static ECCSaveData Data
        {
            get
            {
                ECCSaveData data = ECCSave.Data;

                if (data.driverRatings == null) data.driverRatings = new List<int>();

                return data;
            }
        }

        public static int Rides
        {
            get { return Mathf.Max(0, Data.driverRides); }
        }

        public static int Current
        {
            get { return Mathf.Clamp(Data.driverRank, 0, Ranks.Length - 1); }
        }

        public static Rank CurrentRank
        {
            get { return Ranks[Current]; }
        }

        public static bool IsMax
        {
            get { return Current >= Ranks.Length - 1; }
        }

        /// <summary>
        /// The average of the latest ratings ( 0 before the first ride )
        /// </summary>
        public static float Rating
        {
            get { return Average(Data.driverRatings); }
        }

        public static float Average(List<int> ratings)
        {
            if (ratings == null || ratings.Count == 0) return 0;

            float sum = 0;

            foreach (int rating in ratings) sum += rating;

            return sum / ratings.Count;
        }

        /// <summary>
        /// The highest rank the rides and the rating reach
        /// </summary>
        public static int RankFor(int rides, float rating)
        {
            int rank = 0;

            for (int index = 1; index < Ranks.Length; index++)
            {
                if (rides >= Ranks[index].rides && rating >= Ranks[index].rating - 0.0001f) rank = index;
            }

            return rank;
        }

        public static float FareMultiplier
        {
            get { return 1 + CurrentRank.fareBonus; }
        }

        public static bool FollowUnlocked
        {
            get { return Current >= FollowRank; }
        }

        public static float ExtraPatience
        {
            get { return Current >= PatienceRank ? PatienceBonus : 0; }
        }

        public static float VipIntervalMultiplier
        {
            get { return Current >= VipRank ? VipIntervalScale : 1; }
        }

        public static bool GoldenSign
        {
            get { return Current >= GoldenSignRank; }
        }

        /// <summary>
        /// A ride of the player ended with this rating. Returns the new rank when this ride reached one, otherwise -1
        /// </summary>
        public static int RecordRide(int rating)
        {
            ECCSaveData data = Data;

            data.driverRides = Mathf.Max(0, data.driverRides) + 1;
            data.driverRatings.Add(Mathf.Clamp(rating, 1, 5));

            while (data.driverRatings.Count > RatingWindow) data.driverRatings.RemoveAt(0);

            int reached = RankFor(data.driverRides, Average(data.driverRatings));
            int risen = -1;

            // The rank is never lost
            if (reached > data.driverRank)
            {
                data.driverRank = reached;
                risen = reached;
            }

            ECCSave.Save();

            if (risen >= 0 && RankUp != null) RankUp(risen);
            if (Changed != null) Changed();

            return risen;
        }

        /// <summary>
        /// What is still missing for the next rank, in Persian ( empty at the top )
        /// </summary>
        public static string NextGoal()
        {
            if (IsMax) return string.Empty;

            Rank next = Ranks[Current + 1];

            List<string> parts = new List<string>();

            int rides = next.rides - Rides;

            if (rides > 0) parts.Add(ECCUIKit.PersianDigits(rides) + " سفر دیگه");

            if (Rating < next.rating) parts.Add("امتیاز " + FormatRating(next.rating) + " یا بیشتر");

            if (parts.Count == 0) return "یه سفر دیگه تا «" + next.title + "»";

            return string.Join(" و ", parts.ToArray()) + " تا «" + next.title + "»";
        }

        /// <summary>
        /// 0 to 1 towards the next rank ( the lower of the rides and the rating progress )
        /// </summary>
        public static float Progress
        {
            get
            {
                if (IsMax) return 1;

                Rank from = Ranks[Current];
                Rank next = Ranks[Current + 1];

                float rides = Mathf.Clamp01((Rides - from.rides) / (float)Mathf.Max(1, next.rides - from.rides));
                float rating = Rating >= next.rating ? 1 : Mathf.Clamp01(Rating / Mathf.Max(0.1f, next.rating));

                return Mathf.Min(rides, rating);
            }
        }

        /// <summary>
        /// A rating like ۴٫۳
        /// </summary>
        public static string FormatRating(float rating)
        {
            int tenths = Mathf.RoundToInt(rating * 10);

            return ECCUIKit.PersianDigits(tenths / 10) + "٫" + ECCUIKit.PersianDigits(tenths % 10);
        }
    }
}
