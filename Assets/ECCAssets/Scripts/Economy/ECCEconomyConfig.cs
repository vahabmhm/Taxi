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
    /// How the defaults were chosen ( money = coins in this game ), rebalanced so videos are sometimes needed:
    /// - A normal 60-90 second run banks about 100-250 coins ( 1 coin per second x wanted multiplier, near-miss bonuses, cash ).
    /// - A first level win pays its level reward plus the banked run: chapter 1 about 150-450, generated chapters
    ///   levelRewardBase + chapter x levelRewardPerChapter + slot x levelRewardPerSlot ( about 250-600, bosses double ).
    ///   Replays pay a quarter of the level reward.
    /// - Daily free income without any video: missions ~500, daily challenge 250, wheel ~100, login 50-300.
    /// - Sinks: cars 800 rising 30% each ( ~45,000 for all eleven ), upgrades 200-2000 ( 4,700 per stat per car ),
    ///   power-ups 100-250 per use, wreck revive 400, level skip 1200.
    /// - Two power-ups in a level cost about what that level pays, so a player who also saves for the next car or
    ///   upgrade runs short every few levels. Then the free paths are videos: the 200 coin gift, a power-up for a
    ///   video, the 2X level reward, the video revive. Playing without power-ups still progresses, only slower.
    /// </summary>
    [CreateAssetMenu(fileName = "ECCEconomyConfig", menuName = "Endless Car Chase/Economy Config")]
    public class ECCEconomyConfig : ScriptableObject
    {
        const string ResourcePath = "ECCEconomyConfig";

        [Header("Coin gift popup ( rewarded video )")]
        [Tooltip("Coins given for watching one rewarded video from the coin balance button")]
        public int rewardedCoinGift = 200;

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
        public int levelRewardBase = 200;
        public int levelRewardPerChapter = 12;
        public int levelRewardPerSlot = 8;

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

        [Header("Interstitial ( AdsManagerNew )")]
        [Tooltip("An interstitial is shown on every Nth «مرحله بعد» tap after a normal level win ( if one is ready )")]
        public int interstitialEveryLevels = 4;

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
