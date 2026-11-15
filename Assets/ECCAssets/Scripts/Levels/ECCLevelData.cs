using System;
using UnityEngine;

namespace EndlessCarChase.Levels
{
    /// <summary>
    /// What the player must do to win a level
    /// </summary>
    public enum ECCObjectiveType
    {
        Survive = 0,      // Stay alive for [target] seconds
        DestroyCops = 1,  // Make [target] police cars crash
        CollectCash = 2,  // Pick up [target] money
        Escape = 3,       // Reach the escape point [target] meters away
        DestroyBoss = 4,  // Destroy the armored police truck
        Heist = 5,        // «دزدی و فرار»: pick up the bag of money, then reach the escape point [target] meters away
        NoDamage = 6,     // «با کمترین خسارت تا انتها»: reach the destination [target] meters away with the car's health above ECCLevelMode.MinHealthPercent
        Checkpoints = 7,  // «چک‌پوینت‌ها»: pass [target] checkpoints in a row; each one adds time to the clock
    }

    /// <summary>
    /// The look of a level: which obstacles spawn and the color of the ground and light
    /// </summary>
    public enum ECCLevelTheme
    {
        Default = 0,
        City = 1,
        Desert = 2,
        Forest = 3,
        Rain = 4,         // The city streets at night in heavy rain, wet and slippery
        Highway = 5,      // An endless straight highway with barriers and fast traffic
        Ruins = 6,        // The city in ruins: burnt buildings, wrecks on the streets, smoke and dust
    }

    [Serializable]
    public class ECCLevelData
    {
        [Tooltip("The level name shown in the level select and the HUD")]
        public string title = "مرحله";

        public ECCObjectiveType objective = ECCObjectiveType.Survive;

        [Tooltip("Seconds to survive / police to destroy / money to collect / meters to the escape point")]
        public int target = 30;

        [Tooltip("Seconds to reach the goal. 0 means no time limit ( survive levels never need one )")]
        public float timeLimit = 0;

        [Tooltip("The time between police spawns is multiplied by this value. Lower is harder")]
        public float copSpawnMultiplier = 1;

        [Tooltip("Extra speed for every police car")]
        public float copSpeedBonus = 0;

        [Tooltip("Money given the first time the level is completed. Replays give a quarter")]
        public int reward = 100;

        [Header("Stars")]
        [Tooltip("Health left ( 0-1 ) needed for 2 stars")]
        [Range(0, 1)]
        public float twoStarHealth = 0.4f;

        [Tooltip("Health left ( 0-1 ) needed for 3 stars")]
        [Range(0, 1)]
        public float threeStarHealth = 0.75f;

        [Header("Police Variants")]
        [Tooltip("Chance ( 0-1 ) that a police car is a fast interceptor")]
        [Range(0, 1)]
        public float interceptorChance = 0;

        [Tooltip("Chance ( 0-1 ) that a police car is a rammer that charges at the player")]
        [Range(0, 1)]
        public float rammerChance = 0;

        [Tooltip("Chance ( 0-1 ) that a police car is a heavy armored car")]
        [Range(0, 1)]
        public float heavyChance = 0;

        [Header("Hazards")]
        [Tooltip("Spike strips appear on the road ahead")]
        public bool spikeStrips = false;

        [Tooltip("Police roadblocks appear on the road ahead")]
        public bool roadblocks = false;

        [Tooltip("The police helicopter chases the player ( searchlight, spike strips dropped ahead )")]
        public bool helicopter = false;

        [Header("Look")]
        [Tooltip("The level is played at night: headlights, siren lights and a dark sky")]
        public bool night = false;

        // Set by the catalog, not saved in the inspector
        [NonSerialized] public string id;
        [NonSerialized] public int chapterIndex;
        [NonSerialized] public int levelIndex;
        [NonSerialized] public ECCLevelTheme theme;

        /// <summary>
        /// A short description of the goal, in Persian
        /// </summary>
        public string ObjectiveDescription
        {
            get
            {
                string description;

                switch (objective)
                {
                    case ECCObjectiveType.DestroyCops: description = target + " ماشین پلیس را نابود کن"; break;
                    case ECCObjectiveType.CollectCash: description = target + " دلار جمع کن"; break;
                    case ECCObjectiveType.Escape: description = "به نقطه‌ی فرار برس (" + target + " متر)"; break;
                    case ECCObjectiveType.DestroyBoss: description = "اتوبوس زرهی پلیس را نابود کن"; break;
                    case ECCObjectiveType.Heist: description = "کیف پول را بردار و به نقطه‌ی فرار برسان"; break;
                    case ECCObjectiveType.NoDamage: description = target + " متر تا مقصد برو؛ سلامتی ماشین زیر " + Modes.ECCLevelMode.MinHealthPercent + "٪ نیاید"; break;
                    case ECCObjectiveType.Checkpoints: description = "از " + target + " چک‌پوینت پشت سر هم رد شو"; break;
                    default: description = target + " ثانیه زنده بمان"; break;
                }

                if (timeLimit > 0 && objective == ECCObjectiveType.Checkpoints) description += " - هر چک‌پوینت وقت اضافه می‌دهد";
                else if (timeLimit > 0 && objective != ECCObjectiveType.Survive) description += " - زمان: " + Mathf.RoundToInt(timeLimit) + " ثانیه";

                if (night) description += " (شب)";

                return description;
            }
        }

        /// <summary>
        /// A short id like 1-3 ( chapter-level ), used in object names
        /// </summary>
        public string Number
        {
            get { return (chapterIndex + 1) + "-" + (levelIndex + 1); }
        }

        /// <summary>
        /// The level's number inside its chapter, in Persian digits ( the level select tiles )
        /// </summary>
        public string NumberInChapter
        {
            get { return ECCUIKit.PersianDigits(levelIndex + 1); }
        }

        /// <summary>
        /// How a level is named to the player: «مرحله‌ی ۴ از فصل ۱»
        /// </summary>
        public string DisplayName
        {
            get { return "مرحله‌ی " + NumberInChapter + " از فصل " + ECCUIKit.PersianDigits(chapterIndex + 1); }
        }
    }

    public static class ECCObjectives
    {
        /// <summary>
        /// Goals that use a destination on the map ( the green guide arrow points at it )
        /// </summary>
        public static bool UsesDestination(ECCObjectiveType objective)
        {
            return objective == ECCObjectiveType.Escape || objective == ECCObjectiveType.Heist || objective == ECCObjectiveType.NoDamage || objective == ECCObjectiveType.Checkpoints;
        }
    }

    [Serializable]
    public class ECCChapterData
    {
        public string title = "فصل";

        public ECCLevelTheme theme = ECCLevelTheme.City;

        public ECCLevelData[] levels = new ECCLevelData[0];
    }
}
