using System;
using System.Collections.Generic;
using System.Globalization;
using EndlessCarChase.Levels;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// One row of the league table
    /// </summary>
    public class ECCLeagueEntry
    {
        public string name;
        public int stars;
        public bool isPlayer;
        public int rank;
    }

    /// <summary>
    /// What a normal level win did to the league, for the win screen
    /// </summary>
    public class ECCLeagueWin
    {
        public string levelId;
        public int stars;          // stars of this win
        public int added;          // stars actually added to the week
        public bool improvement;   // the level already counted this week with fewer stars
        public int previousRank;   // the rank the player saw last ( 0: none yet this week )
        public int rank;           // the rank now
    }

    /// <summary>
    /// The weekly star league ( «لیگ هفتگی» ): the player against generated competitors, by the stars earned this week.
    ///
    /// - Only normal level wins count ( ECCLevelMode, not the daily challenge, skips or replays that don't improve ).
    ///   With bestStarsPerLevelOnly each level adds at most its best stars of the week, so replaying an easy level adds
    ///   nothing new. Each win has an id and is counted once.
    /// - The week runs Saturday 00:00 to Friday 24:00 ( the Iranian week ), on the shared clock-protected day of ECCDaily.
    ///   Local device time can be moved forward; turning it back never reopens a week or a day.
    /// - Competitors are generated from the week and an install id, so the table is the same after every restart and
    ///   nothing runs in the background: a competitor's stars = its start + its gains of the days passed this week.
    ///   In the install week they grow only from the install day on ( no retroactive gains ).
    /// - Offline: the competitors are invented nicknames, never real players, and the UI never calls them online.
    /// </summary>
    public static class ECCWeeklyLeague
    {
        const int MaxRememberedWins = 40;

        // Generic invented nicknames ( never real people )
        static readonly string[] Nicknames =
        {
            "ستاره‌چین", "مسافر شب", "راننده‌ی آرام", "چراغ سبز", "تاکسی‌ران_۲۳", "ترمز طلایی", "شهرگرد", "باد شمال",
            "گاز و ترمز", "مسیریاب", "خیابون‌گرد", "رفیق جاده", "سرعت‌سنج", "فرمون‌طلا", "شب‌رو", "کوچه‌باغ",
            "بوق‌زن_۷", "چهارراه", "مسافرکش_۴۰", "آفتاب‌گردون", "ماه‌نو", "رهگذر", "زرد قناری", "دنده‌سنگین",
            "جاده‌ی ابریشم", "سفر خوش", "ایستگاه آخر", "پلاک_۱۱", "کیلومترشمار", "خط ویژه", "میدون‌گرد", "تند و تیز",
            "آینه‌بغل", "صبح زود", "ترافیک‌شکن", "شب‌تاب", "رادیو تاکسی", "سرنشین", "مسیر میانبر", "گردش شهری",
        };

        /// <summary>
        /// Raised when the player's stars, the week or the day changed
        /// </summary>
        public static event Action Changed;

        static ECCLeagueConfig Config
        {
            get { return ECCLeagueConfig.Instance; }
        }

        static ECCLeagueSave Data
        {
            get
            {
                ECCSaveData data = ECCSave.Data;

                if (data.league == null) data.league = new ECCLeagueSave();

                return data.league;
            }
        }

        // ----- The week -----

        /// <summary>
        /// Day of the league week for a date: 0 = Saturday ... 6 = Friday
        /// </summary>
        public static int DayOfLeagueWeek(DateTime date)
        {
            return ((int)date.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
        }

        /// <summary>
        /// The id of the week a day belongs to: the date of its Saturday
        /// </summary>
        public static string WeekOf(string day)
        {
            DateTime date;

            if (ECCDaily.TryParse(day, out date) == false) return string.Empty;

            return date.AddDays(-DayOfLeagueWeek(date)).ToString(ECCDaily.DayFormat, CultureInfo.InvariantCulture);
        }

        static DateTime TodayDate
        {
            get
            {
                DateTime date;

                return ECCDaily.TryParse(ECCDaily.Today, out date) ? date : DateTime.Now.Date;
            }
        }

        /// <summary>
        /// Today's day of the league week ( 0 = Saturday )
        /// </summary>
        public static int TodayIndex
        {
            get { return DayOfLeagueWeek(TodayDate); }
        }

        /// <summary>
        /// The current week id ( after Ensure )
        /// </summary>
        public static string WeekId
        {
            get
            {
                Ensure();
                return Data.weekId;
            }
        }

        /// <summary>
        /// Time left until Friday 24:00 ( the next Saturday 00:00 ), never negative and never more than a week
        /// </summary>
        public static TimeSpan TimeLeft
        {
            get
            {
                DateTime end = TodayDate.AddDays(7 - TodayIndex);

                TimeSpan left = end - DateTime.Now;

                if (left < TimeSpan.Zero) left = TimeSpan.Zero;
                if (left > TimeSpan.FromDays(7)) left = TimeSpan.FromDays(7);

                return left;
            }
        }

        /// <summary>
        /// Starts a new week when the calendar reached one ( once; a closed app across several weeks just lands on the
        /// current week, with no catch-up ). Also notices a new day. Returns true if something changed
        /// </summary>
        public static bool Ensure()
        {
            ECCLeagueSave data = Data;

            string today = ECCDaily.Today;
            string week = WeekOf(today);
            int index = TodayIndex;

            bool changed = false;

            if (string.IsNullOrEmpty(data.salt))
            {
                data.salt = Guid.NewGuid().ToString("N");
                changed = true;
            }

            if (data.weekId != week)
            {
                // The first league week of this install starts on the install day; every later week on Saturday.
                // The clock-protected day never goes back, so a week is never reopened
                bool first = string.IsNullOrEmpty(data.weekId);

                data.weekId = week;
                data.firstWeek = first;
                data.startDay = first ? index : 0;
                data.stars = 0;
                data.levelIds.Clear();
                data.levelStars.Clear();
                data.wins.Clear();
                data.lastShownRank = 0;
                data.lastDay = index;

                changed = true;
            }
            else if (data.lastDay != index)
            {
                data.lastDay = index;
                changed = true;
            }

            if (changed)
            {
                ECCSave.Save();

                if (Changed != null) Changed();
            }

            return changed;
        }

        // ----- Competitors -----

        static int GameStarCap
        {
            get
            {
                int total = 0;

                try
                {
                    total = ECCLevelCatalog.Instance.AllLevels.Count * 3;
                }
                catch (Exception)
                {
                    total = 0;
                }

                int cap = Mathf.Max(1, Config.weeklyCap);

                return total > 0 ? Mathf.Min(cap, total) : cap;
            }
        }

        /// <summary>
        /// The competitors of the current week at today's day, generated from the week and the install id
        /// </summary>
        public static List<ECCLeagueEntry> Competitors()
        {
            Ensure();

            ECCLeagueSave data = Data;
            ECCLeagueConfig config = Config;

            return Generate(data.weekId, data.salt, data.firstWeek, data.startDay, TodayIndex, config);
        }

        /// <summary>
        /// Pure generation ( testable ): the same week, salt and day always give the same table
        /// </summary>
        public static List<ECCLeagueEntry> Generate(string weekId, string salt, bool firstWeek, int startDay, int dayIndex, ECCLeagueConfig config)
        {
            System.Random random = new System.Random(ECCDaily.Seed(weekId, "league|" + salt));

            // Distinct nicknames for the week
            List<string> names = new List<string>(Nicknames);

            for (int index = names.Count - 1; index > 0; index--)
            {
                int other = random.Next(index + 1);
                string swap = names[index];
                names[index] = names[other];
                names[other] = swap;
            }

            int count = Mathf.Max(1, config.leagueSize) - 1;
            int cap = GameStarCap;

            int aheadMin = firstWeek ? config.firstWeekLeaderMin : config.leaderAheadMin;
            int aheadMax = firstWeek ? config.firstWeekLeaderMax : config.leaderAheadMax;

            int gainMax = Mathf.Max(0, config.dailyGainMax);
            int gainMin = Mathf.Clamp(config.dailyGainMin, 0, gainMax);
            int leaderGainMin = Mathf.Clamp(config.leaderDailyGainMin, gainMin, gainMax);

            List<ECCLeagueEntry> entries = new List<ECCLeagueEntry>();

            for (int index = 0; index < count; index++)
            {
                bool leader = index < config.leaderCount;

                int start = leader ? Range(random, aheadMin, aheadMax) : Range(random, config.othersStartMin, config.othersStartMax);

                // Every competitor draws all six gains ( Sunday to Friday ) so the numbers never depend on the day asked
                int stars = start;

                for (int day = 1; day <= 6; day++)
                {
                    int gain = leader ? Range(random, leaderGainMin, gainMax) : Range(random, gainMin, gainMax);

                    if (day > startDay && day <= dayIndex) stars += gain;
                }

                string name = index < names.Count ? names[index] : names[index % names.Count] + "_" + ECCUIKit.PersianDigits(index + 1);

                entries.Add(new ECCLeagueEntry { name = name, stars = Mathf.Clamp(stars, 0, cap) });
            }

            return entries;
        }

        static int Range(System.Random random, int min, int max)
        {
            if (max < min)
            {
                int swap = max;
                max = min;
                min = swap;
            }

            return random.Next(min, max + 1);
        }

        // ----- The player -----

        public static int PlayerStars
        {
            get
            {
                Ensure();
                return Data.stars;
            }
        }

        /// <summary>
        /// The full table, sorted, ranks set ( the player's row has isPlayer )
        /// </summary>
        public static List<ECCLeagueEntry> Table()
        {
            List<ECCLeagueEntry> entries = Competitors();

            ECCLeagueEntry player = new ECCLeagueEntry { name = "شما", stars = Data.stars, isPlayer = true };

            entries.Add(player);

            bool playerWinsTies = Config.playerWinsTies;

            entries.Sort(delegate (ECCLeagueEntry a, ECCLeagueEntry b)
            {
                if (a.stars != b.stars) return b.stars.CompareTo(a.stars);
                if (a.isPlayer) return playerWinsTies ? -1 : 1;
                if (b.isPlayer) return playerWinsTies ? 1 : -1;
                return string.CompareOrdinal(a.name, b.name);
            });

            for (int index = 0; index < entries.Count; index++) entries[index].rank = index + 1;

            return entries;
        }

        /// <summary>
        /// The player's rank now ( 1 is first )
        /// </summary>
        public static int Rank
        {
            get
            {
                List<ECCLeagueEntry> competitors = Competitors();

                int stars = Data.stars;
                int rank = 1;

                foreach (ECCLeagueEntry entry in competitors)
                {
                    if (entry.stars > stars || (entry.stars == stars && Config.playerWinsTies == false)) rank++;
                }

                return rank;
            }
        }

        /// <summary>
        /// The rank was shown to the player ( menu button, league screen or win screen ): rank changes are counted from it
        /// </summary>
        public static void MarkRankShown(int rank)
        {
            ECCLeagueSave data = Data;

            if (data.lastShownRank == rank) return;

            data.lastShownRank = rank;
            ECCSave.Save();
        }

        public static int LastShownRank
        {
            get { return Data.lastShownRank; }
        }

        static int BestThisWeek(ECCLeagueSave data, string levelId)
        {
            int index = data.levelIds.IndexOf(levelId);

            return index >= 0 && index < data.levelStars.Count ? data.levelStars[index] : 0;
        }

        public static int BestThisWeek(string levelId)
        {
            Ensure();
            return BestThisWeek(Data, levelId);
        }

        /// <summary>
        /// The last win counted, waiting for the win screen ( null when there is none )
        /// </summary>
        public static ECCLeagueWin PendingWin { get; private set; }

        /// <summary>
        /// Counts a normal level win ( call it once from the level win, never for the daily challenge or a skip ).
        /// winId must be unique per run: the same id is never counted twice. Returns what was added, or null if this
        /// win was already counted
        /// </summary>
        public static ECCLeagueWin RecordWin(string levelId, int stars, string winId)
        {
            if (string.IsNullOrEmpty(levelId)) return null;

            Ensure();

            ECCLeagueSave data = Data;

            if (string.IsNullOrEmpty(winId) == false && data.wins.Contains(winId)) return null;

            stars = Mathf.Clamp(stars, 0, 3);

            int previousRank = data.lastShownRank;
            int best = BestThisWeek(data, levelId);

            int added = Config.bestStarsPerLevelOnly ? Mathf.Max(0, stars - best) : stars;

            if (stars > best)
            {
                int index = data.levelIds.IndexOf(levelId);

                if (index >= 0 && index < data.levelStars.Count) data.levelStars[index] = stars;
                else
                {
                    data.levelIds.Add(levelId);
                    data.levelStars.Add(stars);
                }
            }

            data.stars += added;

            if (string.IsNullOrEmpty(winId) == false)
            {
                data.wins.Add(winId);

                while (data.wins.Count > MaxRememberedWins) data.wins.RemoveAt(0);
            }

            ECCSave.Save();

            PendingWin = new ECCLeagueWin
            {
                levelId = levelId,
                stars = stars,
                added = added,
                improvement = added > 0 && best > 0,
                previousRank = previousRank,
                rank = Rank,
            };

            if (Changed != null) Changed();

            return PendingWin;
        }

        /// <summary>
        /// The win screen takes the pending win of its level and marks the new rank as seen
        /// </summary>
        public static ECCLeagueWin TakePendingWin(string levelId)
        {
            ECCLeagueWin win = PendingWin;

            if (win == null || win.levelId != levelId) return null;

            PendingWin = null;

            MarkRankShown(win.rank);

            return win;
        }

        // ----- Texts -----

        /// <summary>
        /// «+۳ ستاره به لیگ هفتگی», «+۱ ستاره (بهبود رکورد این هفته)» or the neutral message
        /// </summary>
        public static string StarsText(ECCLeagueWin win)
        {
            if (win.added <= 0) return "این مرحله این هفته با همین ستاره‌ها حساب شده؛ ستاره‌ی تازه‌ای به لیگ اضافه نشد";

            if (win.improvement) return "+" + ECCUIKit.PersianDigits(win.added) + " ستاره (بهبود رکورد این هفته)";

            return "+" + ECCUIKit.PersianDigits(win.added) + " ستاره به لیگ هفتگی";
        }

        /// <summary>
        /// «رتبه‌ات ۴ پله بهتر شد ↑» / «رتبه‌ات ۲ پله پایین‌تر رفت ↓» / «رتبه‌ات تغییری نکرد», with the rank itself
        /// </summary>
        public static string RankChangeText(ECCLeagueWin win)
        {
            string rank = "رتبه " + ECCUIKit.PersianDigits(win.rank);

            if (win.previousRank <= 0) return rank + " در لیگ هفتگی";

            int change = win.previousRank - win.rank;

            if (change > 0) return rank + " · رتبه‌ات " + ECCUIKit.PersianDigits(change) + " پله بهتر شد ↑";
            if (change < 0) return rank + " · رتبه‌ات " + ECCUIKit.PersianDigits(-change) + " پله پایین‌تر رفت ↓";

            return rank + " · رتبه‌ات تغییری نکرد";
        }

        /// <summary>
        /// «پایان هفته: ۲ روز و ۵ ساعت»
        /// </summary>
        public static string TimeLeftText()
        {
            TimeSpan left = TimeLeft;

            int days = (int)left.TotalDays;
            int hours = left.Hours;

            if (days > 0) return "پایان هفته: " + ECCUIKit.PersianDigits(days) + " روز و " + ECCUIKit.PersianDigits(hours) + " ساعت";

            if (hours > 0) return "پایان هفته: " + ECCUIKit.PersianDigits(hours) + " ساعت و " + ECCUIKit.PersianDigits(left.Minutes) + " دقیقه";

            return "پایان هفته: " + ECCUIKit.PersianDigits(Mathf.Max(1, left.Minutes)) + " دقیقه";
        }
    }
}
