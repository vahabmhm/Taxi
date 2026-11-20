using UnityEngine;

namespace EndlessCarChase
{
    /// <summary>
    /// The settings of the weekly star league ( «لیگ هفتگی» ). The game uses Resources/ECCLeagueConfig if it exists,
    /// otherwise the defaults below. To edit in the Inspector: Assets > Create > Endless Car Chase > League Config, saved
    /// in a Resources folder with the name ECCLeagueConfig.
    ///
    /// The league is an OFFLINE challenge: only the player's stars are real, the other entries are generated on the device.
    /// </summary>
    [CreateAssetMenu(fileName = "ECCLeagueConfig", menuName = "Endless Car Chase/League Config")]
    public class ECCLeagueConfig : ScriptableObject
    {
        const string ResourcePath = "ECCLeagueConfig";

        [Header("Unlock")]
        [Tooltip("The league opens when this level is reached ( the level before it was completed )")]
        public int leagueUnlockLevel = 8;

        [Header("Names ( share of each kind, any scale )")]
        public float boyNameShare = 35;
        public float girlNameShare = 35;
        public float nicknameShare = 30;

        [Tooltip("Chance ( 0-1 ) that a first name gets a light variation like «سارا ۷۷» or «رضا_پرو»")]
        public float nameVariationChance = 0.2f;

        [Header("Table")]
        [Tooltip("Entries in the table, the player included")]
        public int leagueSize = 30;

        [Tooltip("Competitors placed well ahead of the player at the start of every week")]
        public int leaderCount = 5;

        [Header("Start of a normal week ( Saturday )")]
        public int leaderAheadMin = 50;
        public int leaderAheadMax = 80;

        [Tooltip("Where the other competitors start")]
        public int othersStartMin = 0;
        public int othersStartMax = 40;

        [Header("The week of the first install")]
        [Tooltip("The leading group's head start in the install week. Must stay small enough for a new player to see a way up")]
        public int firstWeekLeaderMin = 50;
        public int firstWeekLeaderMax = 80;

        [Header("Daily growth ( once per calendar day )")]
        [Tooltip("Stars a competitor gains on a day, at least")]
        public int dailyGainMin = 0;

        [Tooltip("Stars a competitor gains on a day, at most. Three 3-star wins ( 9 ) a day beat it")]
        public int dailyGainMax = 8;

        [Tooltip("The leading group gains between this and dailyGainMax ( about 7 a day on average with 6-8 )")]
        public int leaderDailyGainMin = 6;

        [Header("Limits")]
        [Tooltip("No competitor ever shows more than this in a week ( also never more than the game's total stars )")]
        public int weeklyCap = 250;

        [Header("Rules")]
        [Tooltip("A level adds at most its best star count of the week; a replay only adds the improvement")]
        public bool bestStarsPerLevelOnly = true;

        [Tooltip("The player ranks above competitors with the same stars")]
        public bool playerWinsTies = true;

        [Tooltip("Shows a small note that the league is an offline challenge ( off by default )")]
        public bool showOfflineNote = false;

        static ECCLeagueConfig instance;

        public static ECCLeagueConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ECCLeagueConfig>(ResourcePath);

                    if (instance == null) instance = CreateInstance<ECCLeagueConfig>();
                }

                return instance;
            }
        }
    }
}
