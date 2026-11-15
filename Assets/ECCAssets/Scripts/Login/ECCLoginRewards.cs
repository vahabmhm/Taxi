using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// Ten-day consecutive login rewards. The streak advances once per calendar day ( ECCDaily ) when the game is opened:
    /// - opened the next day: next day of the cycle ( after day 10 a new cycle starts at day 1 )
    /// - a full day missed: back to day 1 ( a new cycle ); coins already paid and an active pass are kept
    /// One claim per day, only when the player taps it. Days 1-9 pay increasing coins ( ECCEconomyConfig.loginRewards ),
    /// day 10 starts a timed free shield pass ( ECCPowerUpInventory ). Claims use transaction ids, so they're never paid twice.
    /// The "calendar shown today" state is separate from the "claimed today" state.
    /// </summary>
    public static class ECCLoginRewards
    {
        public const int CycleLength = 10;

        /// <summary>
        /// Called when the streak or a claim changes
        /// </summary>
        public static event System.Action Changed;

        /// <summary>
        /// Records today's login. Safe to call any number of times a day
        /// </summary>
        public static void RegisterLogin()
        {
            string today = ECCDaily.Today;
            ECCSaveData data = ECCSave.Data;

            if (data.loginLastDay == today) return;

            int gap = string.IsNullOrEmpty(data.loginLastDay) ? int.MaxValue : ECCDaily.DaysBetween(data.loginLastDay, today);

            if (gap <= 0) return;

            if (gap == 1 && data.loginStreak >= 1 && data.loginStreak < CycleLength)
            {
                data.loginStreak++;
            }
            else
            {
                // First login, a missed day, or the day after day 10: a new cycle from day 1
                data.loginStreak = 1;
                data.loginCycle++;
                data.loginClaimedMask = 0;
            }

            data.loginLastDay = today;

            ECCSave.Save();

            if (Changed != null) Changed();
        }

        /// <summary>
        /// Today's day in the cycle, 1-10
        /// </summary>
        public static int CurrentDay
        {
            get
            {
                RegisterLogin();

                return Mathf.Clamp(ECCSave.Data.loginStreak, 1, CycleLength);
            }
        }

        public static bool IsClaimedToday
        {
            get { return ECCDaily.IsToday(ECCSave.Data.loginClaimDay); }
        }

        public static bool CanClaimToday
        {
            get
            {
                RegisterLogin();

                return IsClaimedToday == false;
            }
        }

        public static bool IsDayClaimed(int day)
        {
            return day >= 1 && day <= CycleLength && (ECCSave.Data.loginClaimedMask & (1 << (day - 1))) != 0;
        }

        public static bool IsSpecialDay(int day)
        {
            return day == CycleLength;
        }

        public static int GetCoinReward(int day)
        {
            return IsSpecialDay(day) ? 0 : ECCEconomyConfig.Instance.GetLoginReward(day);
        }

        /// <summary>
        /// Claims today's reward once. Returns the coins paid ( 0 on day 10, which starts the shield pass instead ),
        /// or -1 if nothing could be claimed
        /// </summary>
        public static int Claim()
        {
            if (CanClaimToday == false) return -1;

            ECCSaveData data = ECCSave.Data;
            string today = ECCDaily.Today;
            int day = CurrentDay;

            string transaction = "login-c" + data.loginCycle + "-d" + day + "-" + today;

            int paid = 0;

            if (IsSpecialDay(day))
            {
                // The pass and its transaction id are written in the same save
                if (ECCWallet.IsPaid(transaction) == false)
                {
                    ECCPowerUpInventory.GrantShieldPass(ECCEconomyConfig.Instance.loginDay10PassHours, false);
                    ECCWallet.TryRecord(transaction);
                }
            }
            else if (ECCWallet.TryReward(transaction, GetCoinReward(day)))
            {
                paid = GetCoinReward(day);
            }

            data.loginClaimDay = today;
            data.loginClaimedMask |= 1 << (day - 1);
            ECCSave.Save();

            if (Changed != null) Changed();

            ECCMenuHub.RefreshIndicators();

            return paid;
        }

        // ----- The automatic calendar popup -----

        /// <summary>
        /// The calendar opens by itself once a day while today's reward is waiting
        /// </summary>
        public static bool ShouldAutoShowPopup
        {
            get { return CanClaimToday && ECCDaily.IsToday(ECCSave.Data.loginPopupDay) == false; }
        }

        public static void MarkPopupShown()
        {
            ECCSave.Data.loginPopupDay = ECCDaily.Today;
            ECCSave.Save();
        }
    }
}
