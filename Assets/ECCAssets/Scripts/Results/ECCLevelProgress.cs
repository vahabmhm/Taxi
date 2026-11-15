using UnityEngine;
using EndlessCarChase.Levels;

namespace EndlessCarChase
{
    /// <summary>
    /// Losses in a row on the same level, and level skipping.
    /// After losing the same level twice in a row ( configurable ) the loss screen offers to skip it for a high coin price,
    /// or for a rewarded video when the coins aren't enough. A skipped level unlocks the next one but gives no stars,
    /// no reward and doesn't count as a win ( missions, review prompt ); it can still be won later for its full reward.
    /// The loss counter resets on a win, a skip, or a result on another level.
    /// </summary>
    public static class ECCLevelProgress
    {
        public static bool IsSkipped(ECCLevelData level)
        {
            return level != null && ECCSave.Data.skippedLevels.Contains(level.id);
        }

        /// <summary>
        /// Completed or skipped: the next level is unlocked
        /// </summary>
        public static bool IsPassed(ECCLevelData level)
        {
            return level != null && (ECCSave.GetStars(level.id) > 0 || IsSkipped(level));
        }

        public static void RecordResult(ECCLevelData level, bool victory)
        {
            ECCSaveData data = ECCSave.Data;

            if (victory)
            {
                data.lossLevelId = string.Empty;
                data.lossCount = 0;
            }
            else if (data.lossLevelId == level.id)
            {
                data.lossCount++;
            }
            else
            {
                data.lossLevelId = level.id;
                data.lossCount = 1;
            }

            ECCSave.Save();
        }

        public static int LossesInARow(ECCLevelData level)
        {
            ECCSaveData data = ECCSave.Data;

            return level != null && data.lossLevelId == level.id ? data.lossCount : 0;
        }

        /// <summary>
        /// True when the loss screen should offer to skip this level
        /// </summary>
        public static bool CanOfferSkip(ECCLevelData level)
        {
            if (level == null || IsPassed(level)) return false;

            // The last level has nothing to unlock
            if (ECCLevelCatalog.Instance.GetNextLevel(level) == null) return false;

            return LossesInARow(level) >= Mathf.Max(1, ECCEconomyConfig.Instance.lossesBeforeSkip);
        }

        public static int SkipPrice
        {
            get { return Mathf.Max(0, ECCEconomyConfig.Instance.skipLevelPrice); }
        }

        /// <summary>
        /// Marks the level as skipped and selects the next one. Payment is handled by the caller. Returns the next level
        /// </summary>
        public static ECCLevelData Skip(ECCLevelData level)
        {
            if (level == null) return null;

            ECCSaveData data = ECCSave.Data;

            if (data.skippedLevels.Contains(level.id) == false) data.skippedLevels.Add(level.id);

            data.lossLevelId = string.Empty;
            data.lossCount = 0;

            ECCSave.Save();

            ECCLevelData next = ECCLevelCatalog.Instance.GetNextLevel(level);

            if (next != null) ECCLevelCatalog.Instance.SelectedLevel = next;

            return next;
        }
    }
}
