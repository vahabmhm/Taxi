using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase
{
    [Serializable]
    public class ECCLevelRecord
    {
        public string id;
        public int stars;
    }

    [Serializable]
    public class ECCUpgradeRecord
    {
        public string car;
        public int speed;
        public int handling;
        public int health;
    }

    [Serializable]
    public class ECCMissionState
    {
        public int type;
        public int target;
        public int progress;
        public int reward;
        public bool claimed;
    }

    /// <summary>
    /// Everything the game saves besides the money and the cars bought in the shop ( those keep their original PlayerPrefs keys )
    /// </summary>
    [Serializable]
    public class ECCSaveData
    {
        public List<ECCLevelRecord> levels = new List<ECCLevelRecord>();
        public string selectedLevel = string.Empty;

        public List<ECCUpgradeRecord> upgrades = new List<ECCUpgradeRecord>();

        // Legacy daily missions ( before the 25 mission definitions ). Only read once, for migration
        public string missionDay = string.Empty;
        public List<ECCMissionState> missions = new List<ECCMissionState>();

        // ----- Shared daily calendar ( ECCDaily ) -----
        public string installDay = string.Empty;
        public string lastSeenDay = string.Empty;

        // ----- Coin wallet: ids of the reward transactions already paid ( ECCWallet ) -----
        public List<string> transactions = new List<string>();
        public string coinGiftDay = string.Empty;
        public int coinGiftCount = 0;

        // ----- Daily missions -----
        public string dailyMissionDay = string.Empty;
        public List<ECCDailyMissionState> dailyMissions = new List<ECCDailyMissionState>();

        // ----- Daily challenge -----
        public string challengeDay = string.Empty;   // The day the challenge below was generated for
        public int challengeTier = 0;
        public string challengePopupDay = string.Empty;
        public string challengeCompletedDay = string.Empty;
        public string challengeBaseClaimDay = string.Empty;
        public int challengeBaseReward = 0;
        public string dailyChallengeReviveUsed = string.Empty;       // The day the +30 seconds continuation was used
        public string dailyChallengeDoubleRewardUsed = string.Empty; // The day the 2X victory bonus was claimed

        // ----- Power-ups -----
        public bool powerUpsGranted = false;
        public bool repairGranted = false; // The repair kit came later; its free use is given once, separately
        public List<ECCPowerUpStock> powerUpStock = new List<ECCPowerUpStock>();
        public long freeShieldPassUntil = 0; // Unix seconds. The day 10 login reward

        // ----- Lucky wheel -----
        public string wheelDay = string.Empty;
        public int wheelResult = -1;
        public int wheelPrize = 0;

        // ----- Daily other-game recommendation -----
        public string recommendDay = string.Empty;
        public string recommendPackage = string.Empty;
        public string recommendShownDay = string.Empty;

        // ----- Ten-day login rewards -----
        public string loginLastDay = string.Empty;
        public int loginStreak = 0;
        public int loginCycle = 0;
        public string loginClaimDay = string.Empty;
        public string loginPopupDay = string.Empty;
        public int loginClaimedMask = 0; // Bit d-1 is set when day d of the current cycle was claimed

        // ----- Result screens -----
        public string levelDoubleDay = string.Empty;
        public int levelDoubleCount = 0;
        public string lossLevelId = string.Empty;
        public int lossCount = 0;
        public List<string> skippedLevels = new List<string>();

        // ----- First-run tutorial ( ECCTutorial ): the next step to show; -1 until decided -----
        public int tutorialStep = -1;

        // ----- Permanent achievements ( ECCAchievements ) -----
        public List<ECCStatRecord> achievementStats = new List<ECCStatRecord>();
        public List<string> achievementsClaimed = new List<string>();

        // ----- Chapter star chests ( ECCStarChests ): ids like "c3-1" of the chests opened -----
        public List<string> chestsOpened = new List<string>();

        // ----- Car customization ( ECCCarStyle ) -----
        public List<string> styleOwned = new List<string>();
        public List<ECCCarStyleRecord> carStyles = new List<ECCCarStyleRecord>();

        // ----- Endless mode: the longest distance driven in one run, in meters ( the record line ) -----
        public int endlessBestDistance = 0;

        // ----- Review prompt -----
        public bool reviewPromptShown = false;
        public List<string> reviewWins = new List<string>();

        // ----- Interstitial pacing: «مرحله بعد» taps after a win since the last interstitial -----
        public int levelsSinceInterstitial = 0;

        // The run id of the last win counted for the interstitial schedule ( a run reporting twice counts once )
        public string lastInterstitialWinId = string.Empty;

        // ----- Weekly star league ( ECCWeeklyLeague ) -----
        public ECCLeagueSave league = new ECCLeagueSave();
    }

    [Serializable]
    public class ECCLeagueSave
    {
        // A random id made once per install: with the week it seeds the competitors
        public string salt = string.Empty;

        // The league opens at ECCLeagueConfig.leagueUnlockLevel; recorded once
        public bool unlocked = false;
        public string unlockDay = string.Empty;
        public long unlockTime = 0;
        public string firstWeekId = string.Empty;
        public bool unlockMessageShown = false;

        // The league screen was opened once ( stops the menu button's attention pulse )
        public bool opened = false;

        // The Saturday that started the current league week ( "yyyy-MM-dd" ), empty before the first week
        public string weekId = string.Empty;

        // Day of the week ( 0 = Saturday ) the competitors started growing: the install day in the first week, else 0
        public int startDay = 0;
        public bool firstWeek = false;

        public int stars = 0;

        // The best stars of each level this week ( parallel lists, JsonUtility has no dictionaries )
        public List<string> levelIds = new List<string>();
        public List<int> levelStars = new List<int>();

        // The last win ids counted ( bounded ), so a win is never counted twice
        public List<string> wins = new List<string>();

        // The rank the player saw last ( 0: none yet this week ) and the last day the table was looked at
        public int lastShownRank = 0;
        public int lastDay = -1;
    }

    [Serializable]
    public class ECCStatRecord
    {
        public int stat;
        public int value;
    }

    [Serializable]
    public class ECCCarStyleRecord
    {
        public string car;
        public string paint = string.Empty;
        public string rims = string.Empty;
        public string neon = string.Empty;
        public string trail = string.Empty;
    }

    [Serializable]
    public class ECCDailyMissionState
    {
        public string id;
        public int progress;
        public bool claimed;
    }

    [Serializable]
    public class ECCPowerUpStock
    {
        public int type;
        public int free;
        public int helpShown; // Times its explanation popup was shown ( ECCPowerUpInventory.HelpPopupLimit )
    }

    /// <summary>
    /// Loads and saves the game progress as JSON in PlayerPrefs
    /// </summary>
    public static class ECCSave
    {
        const string SavePlayerPrefs = "ECCSaveData";

        // The same key the shop and the game controller use for the money
        public const string MoneyPlayerPrefs = "Money";

        static ECCSaveData data;

        // True when the player had progress before this version of the save ( used to tell an update from a fresh install )
        public static bool HadLegacyProgress { get; private set; }

        public static ECCSaveData Data
        {
            get
            {
                if (data == null) Load();

                return data;
            }
        }

        public static int Money
        {
            get { return Mathf.Max(0, PlayerPrefs.GetInt(MoneyPlayerPrefs, 0)); }
            set
            {
                int previous = Money;
                int newValue = Mathf.Max(0, value);

                PlayerPrefs.SetInt(MoneyPlayerPrefs, newValue);
                PlayerPrefs.Save();

                // Every balance display listens to this event
                if (newValue != previous) ECCWallet.NotifyChanged(newValue);
            }
        }

        static void Load()
        {
            string json = PlayerPrefs.GetString(SavePlayerPrefs, string.Empty);

            HadLegacyProgress = string.IsNullOrEmpty(json) == false || PlayerPrefs.HasKey(MoneyPlayerPrefs);

            if (string.IsNullOrEmpty(json) == false)
            {
                try
                {
                    data = JsonUtility.FromJson<ECCSaveData>(json);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Couldn't read the saved progress, starting fresh. " + exception.Message);
                }
            }

            if (data == null) data = new ECCSaveData();
        }

        public static void Save()
        {
            Save(true);
        }

        /// <summary>
        /// Writes the progress. Pass false to only stage it, when another write ( like the money ) flushes right after
        /// </summary>
        public static void Save(bool flush)
        {
            PlayerPrefs.SetString(SavePlayerPrefs, JsonUtility.ToJson(Data));

            if (flush) PlayerPrefs.Save();
        }

        public static int GetStars(string levelId)
        {
            foreach (ECCLevelRecord record in Data.levels)
            {
                if (record.id == levelId) return record.stars;
            }

            return 0;
        }

        /// <summary>
        /// Records the stars of a level, keeping the best result. Returns true if this is the first time the level is completed
        /// </summary>
        public static bool SetStars(string levelId, int stars)
        {
            foreach (ECCLevelRecord record in Data.levels)
            {
                if (record.id == levelId)
                {
                    bool firstCompletion = record.stars <= 0 && stars > 0;

                    record.stars = Mathf.Max(record.stars, stars);

                    Save();

                    return firstCompletion;
                }
            }

            Data.levels.Add(new ECCLevelRecord { id = levelId, stars = stars });

            Save();

            return stars > 0;
        }

        public static ECCUpgradeRecord GetUpgrades(string carName)
        {
            foreach (ECCUpgradeRecord record in Data.upgrades)
            {
                if (record.car == carName) return record;
            }

            ECCUpgradeRecord newRecord = new ECCUpgradeRecord { car = carName };

            Data.upgrades.Add(newRecord);

            return newRecord;
        }
    }
}
