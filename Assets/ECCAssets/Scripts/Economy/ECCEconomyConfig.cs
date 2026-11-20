using System;
using UnityEngine;

namespace EndlessCarChase
{
    [Serializable]
    public class ECCWheelPrize
    {
        public int coins = 50;

        [Tooltip("Relative chance of this prize. 0 disables it")]
        public float weight = 1;
    }

    /// <summary>
    /// Every coin amount and price of the new systems, in one place.
    /// The game uses Resources/ECCEconomyConfig if it exists, otherwise the defaults written below.
    /// To edit in the Inspector: Assets > Create > Endless Car Chase > Economy Config, save it in a Resources folder
    /// with the name ECCEconomyConfig.
    ///
    /// How the defaults were chosen for the taxi game ( money = coins ). ECCEconomyModel computes the full table from
    /// these values and CODEX_PROGRESS.md records it; all of it is an estimate until real player data exists.
    /// - Fares are the main income: about 50 coins a ride for a regular driver ( base + street distance, kind of
    ///   passenger, tips, the good-ride streak ), about 280 a level.
    /// - A first level win adds levelRewardBase + chapter x perChapter + slot x perSlot ( about 150-470, the chapter's
    ///   last level double ). Replays pay a quarter of it, so replaying an easy level earns less per minute than
    ///   progressing.
    /// - Daily free income without any video: missions ~550, daily challenge 250, wheel ~100, login ~140.
    /// - A regular player ( 5 levels and a short shift a day, one power-up per level ) earns ~3,900 a day, spends
    ///   ~1,500 on power-ups, revives and fines, and saves the rest for cars ( 800 rising 30% ), upgrades ( 4,700 per
    ///   stat per car ) and styles.
    /// - The gift video ( 300, +10 per chapter reached, at most 550 ) is about 8-11% of that day: worth one or two
    ///   videos a day, never the main income. Nothing needs a video to progress.
    /// </summary>
    [CreateAssetMenu(fileName = "ECCEconomyConfig", menuName = "Endless Car Chase/Economy Config")]
    public class ECCEconomyConfig : ScriptableObject
    {
        const string ResourcePath = "ECCEconomyConfig";

        [Header("Coin gift popup ( rewarded video )")]
        [Tooltip("Coins given for watching one rewarded video from the coin balance button, in chapter 1")]
        public int rewardedCoinGift = 300;

        [Tooltip("Extra gift coins for each chapter the player has reached after the first, so the offer stays worth " +
                 "about the same share of the daily spending as the level rewards grow")]
        public int rewardedCoinGiftPerChapter = 10;

        [Tooltip("The most coins one gift video can give")]
        public int rewardedCoinGiftMax = 550;

        [Tooltip("Most coin gift videos per day. 0 means no limit")]
        public int rewardedCoinGiftDailyLimit = 0;

        [Header("Daily challenge")]
        public int challengeReward = 250;

        [Tooltip("Seconds added by the rewarded second chance. Fixed by design at 30")]
        public int challengeReviveSeconds = 30;

        [Header("Power-up prices ( coins )")]
        public int nitroPrice = 120;
        public int magnetPrice = 100;
        public int shieldPrice = 180;
        public int empPrice = 250;

        [Tooltip("The repair kit power-up ( heals part of the car )")]
        public int repairPrice = 150;

        [Tooltip("Free uses of each power-up given once to every player")]
        public int initialFreeUses = 1;

        [Header("Lucky wheel")]
        public ECCWheelPrize[] wheelPrizes =
        {
            new ECCWheelPrize { coins = 25, weight = 20 },
            new ECCWheelPrize { coins = 100, weight = 15 },
            new ECCWheelPrize { coins = 50, weight = 22 },
            new ECCWheelPrize { coins = 300, weight = 5 },
            new ECCWheelPrize { coins = 75, weight = 18 },
            new ECCWheelPrize { coins = 150, weight = 10 },
            new ECCWheelPrize { coins = 500, weight = 3 },
            new ECCWheelPrize { coins = 200, weight = 7 },
        };

        [Header("Ten-day login rewards")]
        [Tooltip("Coins of days 1 to 9. Must increase")]
        public int[] loginRewards = { 50, 75, 100, 125, 150, 175, 200, 250, 300 };

        [Tooltip("Hours the day 10 free shield pass lasts")]
        public float loginDay10PassHours = 24;

        [Header("Level rewards ( generated chapters )")]
        [Tooltip("Coins of a generated level's first win: base + chapter x perChapter + level slot x perSlot. Bosses pay double")]
        public int levelRewardBase = 150;
        public int levelRewardPerChapter = 10;
        public int levelRewardPerSlot = 6;

        [Header("Wreck revive")]
        [Tooltip("Coins to continue after the car is wrecked ( the video is the free way )")]
        public int reviveCoinPrice = 400;

        [Header("Car upgrades")]
        [Tooltip("Price of each upgrade level, from level 1 to the last")]
        public int[] upgradeCosts = { 200, 400, 800, 1300, 2000 };

        [Header("Result screens")]
        [Tooltip("Most 2X level reward videos per day. 0 means no limit")]
        public int levelDoubleDailyLimit = 3;

        [Tooltip("Seconds added when continuing a timed level after the time ran out")]
        public int levelContinueSeconds = 20;

        [Tooltip("Coins to skip a level after losing it twice in a row")]
        public int skipLevelPrice = 1200;

        [Tooltip("Losses in a row on the same level before skipping is offered")]
        public int lossesBeforeSkip = 2;

        [Header("Garage")]
        [Tooltip("Price of the first locked car that has no price set in the scene ( 0 ). Each next one costs carPriceGrowth times more")]
        public int carBasePrice = 800;

        public float carPriceGrowth = 1.3f;

        [Header("Review prompt")]
        [Tooltip("Different normal levels to win before the review prompt can appear")]
        public int reviewAfterWins = 3;

        [Header("Car toughness")]
        [Tooltip("A police car has at least this many times the player's ram damage as health, so it takes about two rams " +
                 "whatever car the player drives ( a fast ram still wrecks it at once ). Never lowers a car's own health")]
        public float policeHealthPerPlayerDamage = 1.5f;

        [Tooltip("Extra police health for each chapter after the first ( 0.15 = +15% in chapter 2, +30% in chapter 3 )")]
        public float policeHealthPerChapter = 0.15f;

        [Tooltip("The most extra health the chapters can add ( 1 = at most double )")]
        public float policeHealthChapterCap = 1;

        [Tooltip("The player's car health is multiplied by this")]
        public float playerHealthMultiplier = 2.5f;

        [Tooltip("The boss takes at least this many average rams of the player's car")]
        public float bossHitsOfPlayerDamage = 8;

        [Header("Interstitial schedule ( ECCInterstitialSchedule )")]
        [Tooltip("No interstitials at all until the player completed more than this many levels")]
        public int interstitialStartAfterLevel = 10;

        [Tooltip("After interstitialStartAfterLevel: one interstitial every this many completed levels")]
        public int interstitialEarlyInterval = 4;

        [Tooltip("From this completed level on, the late interval is used")]
        public int lateTierStartLevel = 50;

        [Tooltip("After lateTierStartLevel: one interstitial every this many completed levels")]
        public int interstitialLateInterval = 3;

        static ECCEconomyConfig instance;

        public static ECCEconomyConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ECCEconomyConfig>(ResourcePath);

                    if (instance == null) instance = CreateInstance<ECCEconomyConfig>();
                }

                return instance;
            }
        }

        /// <summary>
        /// The coin gift video's amount for a player who reached this chapter ( 0 based ), rounded to 10
        /// </summary>
        public int GetRewardedCoinGift(int chapter)
        {
            int amount = rewardedCoinGift + Mathf.Max(0, chapter) * Mathf.Max(0, rewardedCoinGiftPerChapter);

            amount = Mathf.RoundToInt(amount / 10f) * 10;

            return Mathf.Max(1, rewardedCoinGiftMax > 0 ? Mathf.Min(amount, Mathf.Max(rewardedCoinGift, rewardedCoinGiftMax)) : amount);
        }

        public int GetPowerUpPrice(ECCPowerUpType type)
        {
            switch (type)
            {
                case ECCPowerUpType.Nitro: return nitroPrice;
                case ECCPowerUpType.Shield: return shieldPrice;
                case ECCPowerUpType.EMP: return empPrice;
                case ECCPowerUpType.Repair: return repairPrice;
                default: return magnetPrice;
            }
        }

        /// <summary>
        /// The price of the nth locked car without a price ( 0 first ), rounded to 50
        /// </summary>
        public int GetDefaultCarPrice(int order)
        {
            float price = carBasePrice * Mathf.Pow(Mathf.Max(1, carPriceGrowth), order);

            return Mathf.Max(50, Mathf.RoundToInt(price / 50) * 50);
        }

        /// <summary>
        /// The first-win coins of a generated level ( chapter and slot 0 based )
        /// </summary>
        public int GetLevelReward(int chapter, int slot, bool boss)
        {
            int reward = Mathf.Max(0, levelRewardBase + chapter * levelRewardPerChapter + slot * levelRewardPerSlot);

            return boss ? reward * 2 : reward;
        }

        /// <summary>
        /// The price of an upgrade's next level ( level is the current one, 0 based )
        /// </summary>
        public int GetUpgradeCost(int level)
        {
            if (upgradeCosts == null || upgradeCosts.Length == 0) return 0;

            return Mathf.Max(0, upgradeCosts[Mathf.Clamp(level, 0, upgradeCosts.Length - 1)]);
        }

        /// <summary>
        /// The coins of a login day ( 1-9 ). Day 10 is the special reward
        /// </summary>
        public int GetLoginReward(int day)
        {
            if (loginRewards == null || loginRewards.Length == 0) return 50;

            return loginRewards[Mathf.Clamp(day - 1, 0, loginRewards.Length - 1)];
        }
    }
}
