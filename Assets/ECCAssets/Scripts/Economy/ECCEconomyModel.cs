using System.Collections.Generic;
using EndlessCarChase.Levels;
using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// A small estimate of the coin economy, built from the game's real values ( ECCEconomyConfig, the level catalog,
    /// the taxi fare formula, the mission catalog, the wheel and login tables ). Used by the logic tests and to fill the
    /// economy table in CODEX_PROGRESS.md. There are no real player analytics yet, so every result is an ESTIMATE
    /// that depends on the assumptions written in the profiles below; tune ECCEconomyConfig after release.
    /// Not used at runtime.
    /// </summary>
    public static class ECCEconomyModel
    {
        /// <summary>
        /// What a kind of player does in one active day
        /// </summary>
        public class Profile
        {
            public string name;

            // The chapter the player is in ( 0 based ), for level rewards and the gift amount
            public int chapter;

            public int newLevelWins;
            public int replayWins;
            public int levelLosses;

            // Minutes of the endless shift mode
            public float shiftMinutes;

            // Missions claimed of today's list ( 0-1 )
            public float missionShare = 1;

            public bool challenge = true;
            public bool wheel = true;
            public bool login = true;

            // Power-ups bought with coins per level played ( free uses aside )
            public float powerUpsPerLevel = 1;

            // Wreck revives bought with coins per day
            public float coinRevives;

            // Fines paid per level ( caught by the police )
            public float finesPerLevel = 25;

            // Coin gift videos watched
            public int giftVideos;

            // 2X level reward videos watched ( limited per day by the config )
            public int doubleVideos;
        }

        public class Day
        {
            public Profile profile;

            public float levels, fares, shift, missions, challenge, wheel, login, gifts, doubles;
            public float powerUps, revives, fines;

            public float Income
            {
                get { return levels + fares + shift + missions + challenge + wheel + login + gifts + doubles; }
            }

            public float AdIncome
            {
                get { return gifts + doubles; }
            }

            /// <summary>
            /// Coins spent on things used up in play ( power-ups, revives, fines )
            /// </summary>
            public float Consumables
            {
                get { return powerUps + revives + fines; }
            }

            /// <summary>
            /// What is left for cars, upgrades and styles
            /// </summary>
            public float Savings
            {
                get { return Income - Consumables; }
            }
        }

        // ----- Assumptions ( estimates, see CODEX_PROGRESS.md ) -----

        // Average ride rating and stop quality of a regular player ( 0 bad, 1 good, 2 perfect )
        public const int TypicalRating = 4;
        public const int TypicalStop = 1;

        // The good-ride streak multiplier an average player keeps ( x2 after 3 good rides, x3 after 6 )
        public const float TypicalStreak = 1.25f;

        // Passengers delivered in an average level and per minute of the endless shift
        public const float PassengersPerLevel = 5.5f;
        public const float PassengersPerShiftMinute = 1.4f;

        // Minutes one level takes, retries included
        public const float MinutesPerLevel = 3;

        /// <summary>
        /// The average fare of one ride for a mix of passenger kinds ( weights in TaxiPassengerKind order )
        /// </summary>
        public static float AverageFare(float[] kindWeights, float streak)
        {
            float total = 0, weights = 0;

            for (int kind = 0; kind < kindWeights.Length; kind++)
            {
                float weight = kindWeights[kind];

                if (weight <= 0) continue;

                Vector2 range = TaxiPassenger.RideDistance((TaxiPassengerKind)kind);

                float fare = TaxiPassenger.Fare((TaxiPassengerKind)kind, (range.x + range.y) / 2, TypicalRating, TypicalStop, TypicalStop, false);

                total += fare * weight;
                weights += weight;
            }

            return weights > 0 ? total / weights * streak : 0;
        }

        public static float AverageFare()
        {
            return AverageFare(new TaxiRules().kindWeights, TypicalStreak);
        }

        /// <summary>
        /// The expected coins of one wheel spin
        /// </summary>
        public static float WheelExpected(ECCEconomyConfig config)
        {
            float total = 0, weights = 0;

            foreach (ECCWheelPrize prize in config.wheelPrizes)
            {
                if (prize.weight <= 0) continue;

                total += prize.coins * prize.weight;
                weights += prize.weight;
            }

            return weights > 0 ? total / weights : 0;
        }

        /// <summary>
        /// The average coins of a login day over a full 10 day streak ( day 10 is the shield pass, no coins )
        /// </summary>
        public static float LoginAverage(ECCEconomyConfig config)
        {
            float total = 0;

            for (int day = 1; day <= 9; day++) total += config.GetLoginReward(day);

            return total / 10;
        }

        /// <summary>
        /// The coins of an average day's mission list ( 4.5 missions: about 2 easy, 1.5 medium and 1 hard )
        /// </summary>
        public static float MissionsExpected()
        {
            Dictionary<ECCMissionDifficulty, float> sum = new Dictionary<ECCMissionDifficulty, float>();
            Dictionary<ECCMissionDifficulty, int> count = new Dictionary<ECCMissionDifficulty, int>();

            foreach (ECCMissionDefinition mission in ECCMissionCatalog.Instance.missions)
            {
                float value;
                int number;

                sum.TryGetValue(mission.difficulty, out value);
                count.TryGetValue(mission.difficulty, out number);

                sum[mission.difficulty] = value + mission.reward;
                count[mission.difficulty] = number + 1;
            }

            return Average(sum, count, ECCMissionDifficulty.Easy) * 2 + Average(sum, count, ECCMissionDifficulty.Medium) * 1.5f + Average(sum, count, ECCMissionDifficulty.Hard);
        }

        static float Average(Dictionary<ECCMissionDifficulty, float> sum, Dictionary<ECCMissionDifficulty, int> count, ECCMissionDifficulty difficulty)
        {
            int number;

            return count.TryGetValue(difficulty, out number) && number > 0 ? sum[difficulty] / number : 0;
        }

        /// <summary>
        /// The average first-win reward of a chapter's levels
        /// </summary>
        public static float LevelReward(int chapter)
        {
            ECCEconomyConfig config = ECCEconomyConfig.Instance;

            float total = 0;

            for (int slot = 0; slot < ECCLevelCatalog.LevelsPerChapter; slot++) total += config.GetLevelReward(chapter, slot, slot == ECCLevelCatalog.LevelsPerChapter - 1);

            return total / ECCLevelCatalog.LevelsPerChapter;
        }

        public static float AveragePowerUpPrice(ECCEconomyConfig config)
        {
            float total = 0;

            foreach (ECCPowerUpType type in ECCPowerUpInventory.Types) total += config.GetPowerUpPrice(type);

            return total / Mathf.Max(1, ECCPowerUpInventory.Types.Length);
        }

        public static Day Simulate(Profile profile)
        {
            ECCEconomyConfig config = ECCEconomyConfig.Instance;

            Day day = new Day { profile = profile };

            float fare = AverageFare();
            float reward = LevelReward(profile.chapter);
            int levelsPlayed = profile.newLevelWins + profile.replayWins + profile.levelLosses;

            day.levels = profile.newLevelWins * reward + profile.replayWins * (reward / 4);

            // Lost levels still bank the fares earned before the end, about half a level's worth
            day.fares = (profile.newLevelWins + profile.replayWins + profile.levelLosses * 0.5f) * PassengersPerLevel * fare;
            day.shift = profile.shiftMinutes * PassengersPerShiftMinute * fare;

            day.missions = MissionsExpected() * profile.missionShare;
            day.challenge = profile.challenge ? config.challengeReward : 0;
            day.wheel = profile.wheel ? WheelExpected(config) : 0;
            day.login = profile.login ? LoginAverage(config) : 0;

            day.gifts = profile.giftVideos * config.GetRewardedCoinGift(profile.chapter);

            int doubles = config.levelDoubleDailyLimit > 0 ? Mathf.Min(profile.doubleVideos, config.levelDoubleDailyLimit) : profile.doubleVideos;
            day.doubles = Mathf.Min(doubles, profile.newLevelWins + profile.replayWins) * reward;

            day.powerUps = levelsPlayed * profile.powerUpsPerLevel * AveragePowerUpPrice(config);
            day.revives = profile.coinRevives * config.reviveCoinPrice;
            day.fines = (levelsPlayed + profile.shiftMinutes / MinutesPerLevel) * profile.finesPerLevel;

            return day;
        }

        /// <summary>
        /// The four players of the economy check, at a chapter ( 0 based )
        /// </summary>
        public static Profile[] Profiles(int chapter)
        {
            return new[]
            {
                new Profile { name = "casual, no videos", chapter = chapter, newLevelWins = 2, replayWins = 0, levelLosses = 1, shiftMinutes = 0, missionShare = 0.4f, challenge = false, powerUpsPerLevel = 0.5f },
                new Profile { name = "regular, no videos", chapter = chapter, newLevelWins = 4, replayWins = 1, levelLosses = 2, shiftMinutes = 5, powerUpsPerLevel = 1, coinRevives = 0.5f },
                new Profile { name = "regular, 1 video", chapter = chapter, newLevelWins = 4, replayWins = 1, levelLosses = 2, shiftMinutes = 5, powerUpsPerLevel = 1, coinRevives = 0.5f, giftVideos = 1 },
                new Profile { name = "active, 2 videos", chapter = chapter, newLevelWins = 7, replayWins = 2, levelLosses = 3, shiftMinutes = 10, powerUpsPerLevel = 1.3f, coinRevives = 1, giftVideos = 1, doubleVideos = 1 },
            };
        }

        /// <summary>
        /// Coins per minute of replaying the easiest level again and again ( the farming check )
        /// </summary>
        public static float ReplayFarmingPerMinute()
        {
            ECCEconomyConfig config = ECCEconomyConfig.Instance;

            float replay = config.GetLevelReward(0, 0, false) / 4f + 3 * AverageFare();

            // The first level is short: about two minutes with three passengers
            return replay / 2;
        }

        /// <summary>
        /// Coins per minute of progressing through new levels of a chapter, without power-ups
        /// </summary>
        public static float ProgressPerMinute(int chapter)
        {
            return (LevelReward(chapter) + PassengersPerLevel * AverageFare()) / MinutesPerLevel;
        }

        public static float ShiftPerMinute()
        {
            return PassengersPerShiftMinute * AverageFare();
        }

        /// <summary>
        /// The economy table as text ( for the logs and CODEX_PROGRESS.md )
        /// </summary>
        public static string Report()
        {
            ECCEconomyConfig config = ECCEconomyConfig.Instance;

            System.Text.StringBuilder text = new System.Text.StringBuilder();

            text.AppendLine("average fare " + Mathf.RoundToInt(AverageFare()) + ", wheel " + Mathf.RoundToInt(WheelExpected(config)) + ", login " + Mathf.RoundToInt(LoginAverage(config)) + ", missions " + Mathf.RoundToInt(MissionsExpected()) + ", power-up " + Mathf.RoundToInt(AveragePowerUpPrice(config)));
            text.AppendLine("per minute: progress ch1 " + Mathf.RoundToInt(ProgressPerMinute(0)) + ", ch15 " + Mathf.RoundToInt(ProgressPerMinute(14)) + ", shift " + Mathf.RoundToInt(ShiftPerMinute()) + ", replay farming " + Mathf.RoundToInt(ReplayFarmingPerMinute()));

            foreach (int chapter in new[] { 0, 9, 24 })
            {
                foreach (Profile profile in Profiles(chapter))
                {
                    Day day = Simulate(profile);

                    text.AppendLine("ch" + (chapter + 1) + " | " + profile.name + " | income " + Mathf.RoundToInt(day.Income) + " (videos " + Mathf.RoundToInt(day.AdIncome) + ") | consumables " + Mathf.RoundToInt(day.Consumables) + " | left " + Mathf.RoundToInt(day.Savings) + " | gift " + config.GetRewardedCoinGift(chapter));
                }
            }

            return text.ToString();
        }
    }
}
