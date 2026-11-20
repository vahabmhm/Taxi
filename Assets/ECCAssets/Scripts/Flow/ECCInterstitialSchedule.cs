using EndlessCarChase.Levels;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// When interstitials are due ( AGENTS.md task 5, level-based schedule; replaces the old "every 4th Next Level tap" ).
    /// All numbers are in ECCEconomyConfig ( Interstitial section ):
    /// - up to interstitialStartAfterLevel ( 10 ): none at all
    /// - after it, up to lateTierStartLevel ( 50 ): one every earlyInterval ( 4 ) completed levels
    /// - after level 50: one every lateInterval ( 3 ) completed levels
    /// The tier comes from the player's highest COMPLETED normal level ( won or skipped ), read from the real campaign
    /// progress, so replaying early levels later follows the current tier and players who update past level 10 / 50 get the
    /// right tier at once. Only normal level wins count ( ECCLevelMode, once per run id; not losses, the daily challenge or
    /// skips ). When the counter reaches the interval the interstitial is DUE and is tried at the next transition out of a
    /// win screen ( ECCPostVictoryFlow ). The counter is reset only after an interstitial was really shown; a failed or
    /// unavailable one is skipped silently ( no NoAds panel ) and stays due.
    /// There was no time-based interstitial cooldown in this project, so nothing else was kept or removed.
    /// </summary>
    public static class ECCInterstitialSchedule
    {
        /// <summary>
        /// 1-based number of the highest completed normal level ( 0 when none )
        /// </summary>
        public static int HighestCompletedLevel
        {
            get
            {
                var levels = ECCLevelCatalog.Instance.AllLevels;

                for (int index = levels.Count - 1; index >= 0; index--)
                {
                    if (ECCLevelProgress.IsPassed(levels[index])) return index + 1;
                }

                return 0;
            }
        }

        /// <summary>
        /// Completed levels between two interstitials for a player progress; 0 means no interstitials
        /// </summary>
        public static int IntervalFor(int highestCompleted)
        {
            ECCEconomyConfig config = ECCEconomyConfig.Instance;

            if (highestCompleted <= config.interstitialStartAfterLevel) return 0;

            if (highestCompleted <= config.lateTierStartLevel) return Mathf.Max(1, config.interstitialEarlyInterval);

            return Mathf.Max(1, config.interstitialLateInterval);
        }

        public static int CurrentInterval
        {
            get { return IntervalFor(HighestCompletedLevel); }
        }

        /// <summary>
        /// True when an interstitial should be tried at the next transition out of a win screen
        /// </summary>
        public static bool IsDue
        {
            get
            {
                int interval = CurrentInterval;

                return interval > 0 && ECCSave.Data.levelsSinceInterstitial >= interval;
            }
        }

        /// <summary>
        /// A normal level was won. runId makes repeated reports of the same run count once.
        /// Call after the win's stars were saved, so the tier includes this level
        /// </summary>
        public static void RecordWin(string runId)
        {
            ECCSaveData data = ECCSave.Data;

            if (string.IsNullOrEmpty(runId) == false && data.lastInterstitialWinId == runId) return;

            data.lastInterstitialWinId = runId ?? string.Empty;

            // Wins inside the free tier don't count towards the first interstitial
            if (CurrentInterval > 0) data.levelsSinceInterstitial++;

            ECCSave.Save();
        }

        /// <summary>
        /// An interstitial was really displayed: start counting again
        /// </summary>
        public static void MarkShown()
        {
            ECCSave.Data.levelsSinceInterstitial = 0;
            ECCSave.Save();
        }
    }
}
