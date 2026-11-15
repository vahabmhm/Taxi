using UnityEngine;
using EndlessCarChase.Levels;

namespace EndlessCarChase
{
    /// <summary>
    /// The daily challenge: one generated, timed chase per calendar day, the same for the whole day ( seeded by the day ).
    /// It's built from the real level system ( objectives, themes, police variants ), scaled to the player's campaign
    /// progress so it's always possible, and it never changes the campaign progress.
    ///
    /// Daily state ( all keyed to ECCDaily ):
    /// - challengePopupDay: the automatic popup was shown that day ( never on the install day )
    /// - challengeCompletedDay / challengeBaseClaimDay: won and paid that day ( base reward paid once )
    /// - dailyChallengeReviveUsed: the rewarded +30 seconds was used that day ( once per day )
    /// - dailyChallengeDoubleRewardUsed: the rewarded 2X was claimed that day ( once per day, independent of the revive )
    /// </summary>
    public static class ECCDailyChallenge
    {
        /// <summary>
        /// Set when the player accepts the challenge; the next scene load runs the challenge mode. Session only, never saved
        /// </summary>
        public static bool PlayRequested { get; set; }

        static string cachedDay;
        static ECCLevelData cachedLevel;

        /// <summary>
        /// Today's challenge
        /// </summary>
        public static ECCLevelData Today
        {
            get
            {
                string today = ECCDaily.Today;

                if (cachedLevel == null || cachedDay != today)
                {
                    ECCSaveData data = ECCSave.Data;

                    // The difficulty is fixed for the whole day, even if the player progresses or restarts
                    if (data.challengeDay != today)
                    {
                        data.challengeDay = today;
                        data.challengeTier = ProgressTier;
                        ECCSave.Save();
                    }

                    cachedDay = today;
                    cachedLevel = Generate(today, data.challengeTier);
                }

                return cachedLevel;
            }
        }

        /// <summary>
        /// 0, 1 or 2 depending on how far the player got in the campaign
        /// </summary>
        public static int ProgressTier
        {
            get
            {
                int completed = 0;

                foreach (ECCLevelData level in ECCLevelCatalog.Instance.AllLevels)
                {
                    if (ECCSave.GetStars(level.id) > 0) completed++;
                }

                return completed >= 20 ? 2 : (completed >= 8 ? 1 : 0);
            }
        }

        /// <summary>
        /// Builds a timed challenge for a day. Only objectives that are always possible from the start are used
        /// ( destroy police, collect money, escape ), with the content of chapters the player already reached
        /// </summary>
        public static ECCLevelData Generate(string day, int tier)
        {
            System.Random random = new System.Random(ECCDaily.Seed(day, "challenge"));

            ECCObjectiveType[] objectives = { ECCObjectiveType.DestroyCops, ECCObjectiveType.CollectCash, ECCObjectiveType.Escape };

            ECCLevelData level = new ECCLevelData();
            level.title = "چالش روز";
            level.objective = objectives[random.Next(objectives.Length)];
            level.reward = ECCEconomyConfig.Instance.challengeReward;

            tier = Mathf.Clamp(tier, 0, 2);

            switch (level.objective)
            {
                case ECCObjectiveType.DestroyCops:
                    level.target = 10 + tier * 4 + random.Next(3);
                    level.timeLimit = 130;
                    break;

                case ECCObjectiveType.CollectCash:
                    level.target = 35 + tier * 25 + random.Next(3) * 5;
                    level.timeLimit = 100;
                    break;

                default:
                    level.target = 280 + tier * 100 + random.Next(3) * 20;
                    level.timeLimit = 70 + tier * 10;
                    break;
            }

            level.copSpawnMultiplier = 1.1f - tier * 0.1f;
            level.copSpeedBonus = tier * 0.5f;
            level.interceptorChance = tier >= 1 ? 0.15f : 0;
            level.rammerChance = tier >= 2 ? 0.15f : 0;
            level.spikeStrips = tier >= 1 && random.Next(2) == 0;

            // A theme from a chapter the player already reached
            int chapters = Mathf.Clamp(tier + 1, 1, 3);
            level.theme = (ECCLevelTheme)(1 + random.Next(chapters));

            level.id = "challenge-" + day;
            level.chapterIndex = 0;
            level.levelIndex = 0;

            return level;
        }

        // ----- Popup schedule -----

        /// <summary>
        /// True if the daily popup should open automatically now: never on the install day, once per day
        /// </summary>
        public static bool ShouldAutoShowPopup
        {
            get
            {
                if (ECCDaily.IsInstallDay) return false;

                return ECCDaily.IsToday(ECCSave.Data.challengePopupDay) == false;
            }
        }

        public static void MarkPopupShown()
        {
            ECCSave.Data.challengePopupDay = ECCDaily.Today;
            ECCSave.Save();
        }

        // ----- Completion and rewards -----

        public static bool IsCompletedToday
        {
            get { return ECCDaily.IsToday(ECCSave.Data.challengeCompletedDay); }
        }

        public static bool IsReviveUsedToday
        {
            get { return ECCDaily.IsToday(ECCSave.Data.dailyChallengeReviveUsed); }
        }

        public static bool IsDoubleUsedToday
        {
            get { return ECCDaily.IsToday(ECCSave.Data.dailyChallengeDoubleRewardUsed); }
        }

        /// <summary>
        /// Today's base reward ( fixed when the challenge was won, so the 2X always matches it )
        /// </summary>
        public static int BaseReward
        {
            get
            {
                ECCSaveData data = ECCSave.Data;

                return IsCompletedToday && data.challengeBaseReward > 0 ? data.challengeBaseReward : Today.reward;
            }
        }

        /// <summary>
        /// Records today's victory and pays the base reward once. Returns the coins paid now ( 0 if already paid today )
        /// </summary>
        public static int CompleteToday()
        {
            string today = ECCDaily.Today;
            ECCSaveData data = ECCSave.Data;

            if (IsCompletedToday == false)
            {
                data.challengeCompletedDay = today;
                data.challengeBaseReward = Today.reward;
                ECCSave.Save();
            }

            if (ECCWallet.TryReward("challenge-base-" + today, data.challengeBaseReward))
            {
                data.challengeBaseClaimDay = today;
                ECCSave.Save();

                return data.challengeBaseReward;
            }

            return 0;
        }

        /// <summary>
        /// Pays the 2X bonus ( equal to the base reward ) after a verified rewarded video. Once per day.
        /// Returns the coins paid, or 0 if not allowed
        /// </summary>
        public static int ClaimDouble()
        {
            if (IsCompletedToday == false || IsDoubleUsedToday) return 0;

            string today = ECCDaily.Today;
            int bonus = BaseReward;

            bool paid = ECCWallet.TryReward("challenge-double-" + today, bonus);

            // The flag is recorded right after the payment, in the same frame
            ECCSave.Data.dailyChallengeDoubleRewardUsed = today;
            ECCSave.Save();

            ECCMenuHub.RefreshIndicators();

            return paid ? bonus : 0;
        }

        /// <summary>
        /// Records the use of today's +30 seconds second chance, after the effect was applied. Returns false if already used
        /// </summary>
        public static bool ConsumeRevive()
        {
            if (IsReviveUsedToday) return false;

            ECCSave.Data.dailyChallengeReviveUsed = ECCDaily.Today;
            ECCSave.Save();

            return true;
        }
    }
}
