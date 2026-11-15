using System;
using System.Globalization;

namespace EndlessCarChase
{
    /// <summary>
    /// The one calendar-day policy shared by every daily feature ( missions, challenge, wheel, login rewards, recommendation ).
    /// Days are local calendar days as "yyyy-MM-dd".
    ///
    /// Clock-rollback protection: the latest day ever seen is saved, and the current day never goes back before it.
    /// Turning the clock back therefore can't repeat a day that was already used. Turning the clock forward does skip ahead
    /// ( a local-only save can't prevent that; there is no trusted server time in this project ).
    /// </summary>
    public static class ECCDaily
    {
        public const string DayFormat = "yyyy-MM-dd";

        // Set for an update of an existing install, so the update day doesn't count as the first ever launch
        const string LegacyInstallDay = "2000-01-01";

        /// <summary>
        /// True for a player who had the game before the daily features came ( an update, not a fresh install )
        /// </summary>
        public static bool IsLegacyInstall
        {
            get
            {
                string today = Today;

                return ECCSave.Data.installDay == LegacyInstallDay && today.Length > 0;
            }
        }

        /// <summary>
        /// Today's day key, protected against moving the clock backwards
        /// </summary>
        public static string Today
        {
            get
            {
                string now = DateTime.Now.ToString(DayFormat, CultureInfo.InvariantCulture);

                ECCSaveData data = ECCSave.Data;

                bool changed = false;

                if (string.IsNullOrEmpty(data.installDay))
                {
                    data.installDay = ECCSave.HadLegacyProgress ? LegacyInstallDay : now;
                    changed = true;
                }

                if (string.IsNullOrEmpty(data.lastSeenDay) || string.CompareOrdinal(now, data.lastSeenDay) > 0)
                {
                    data.lastSeenDay = now;
                    changed = true;
                }

                if (changed) ECCSave.Save();

                return data.lastSeenDay;
            }
        }

        /// <summary>
        /// True on the calendar day the game was installed ( the first ever launch day )
        /// </summary>
        public static bool IsInstallDay
        {
            get
            {
                string today = Today;

                return ECCSave.Data.installDay == today;
            }
        }

        /// <summary>
        /// True if the given day key is today
        /// </summary>
        public static bool IsToday(string day)
        {
            return string.IsNullOrEmpty(day) == false && day == Today;
        }

        /// <summary>
        /// Whole days from one day key to another ( positive when 'to' is later ). Returns int.MaxValue for invalid keys
        /// </summary>
        public static int DaysBetween(string from, string to)
        {
            DateTime fromDate, toDate;

            if (TryParse(from, out fromDate) == false || TryParse(to, out toDate) == false) return int.MaxValue;

            return (int)Math.Round((toDate - fromDate).TotalDays);
        }

        public static bool TryParse(string day, out DateTime date)
        {
            return DateTime.TryParseExact(day, DayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        /// <summary>
        /// A stable number for a day, used as a random seed so the same day always gives the same daily content
        /// </summary>
        public static int Seed(string day, string salt)
        {
            unchecked
            {
                int seed = 17;

                foreach (char character in day + "|" + salt) seed = seed * 31 + character;

                return seed & 0x7fffffff;
            }
        }

        /// <summary>
        /// The current time in unix seconds, for timed benefits
        /// </summary>
        public static long UnixNow
        {
            get { return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds; }
        }
    }
}
