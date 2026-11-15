using System.Collections.Generic;
using UnityEngine;
using EndlessCarChase.Levels;

namespace EndlessCarChase
{
    /// <summary>
    /// The levels of the taxi game: ECCLevelCatalog.ChapterCount chapters of ECCLevelCatalog.LevelsPerChapter levels,
    /// always the same for the same chapter. Every chapter has the same order of goals:
    /// 1 passengers, 2 perfect stops, 3 race against the rivals, 4 fares, 5 a clean record, 6 passengers against the
    /// clock, 7 a smooth ride, 8 VIPs at night, 9 a big race, 10 the VIP final.
    /// The chapters rotate through the city by day, the city at night and the rainy city; every chapter needs more
    /// passengers in less time, against more rivals, with more demanding passengers
    /// </summary>
    public static class TaxiLevels
    {
        static readonly string[] Places = { "مرکز شهر", "شهر در شب", "شهر بارانی" };

        static readonly string[] Ranks = { "شاگرد راننده", "تازه‌کار", "راننده‌ی شهر", "حرفه‌ای", "کارکشته", "استاد", "سلطان تاکسی", "افسانه‌ی خیابان", "قهرمان شهر", "بی‌رقیب" };

        static readonly ECCObjectiveType[] Pattern =
        {
            ECCObjectiveType.Passengers,
            ECCObjectiveType.PerfectStops,
            ECCObjectiveType.RaceRivals,
            ECCObjectiveType.Earn,
            ECCObjectiveType.CleanRecord,
            ECCObjectiveType.Passengers,
            ECCObjectiveType.SmoothRide,
            ECCObjectiveType.Vip,
            ECCObjectiveType.RaceRivals,
            ECCObjectiveType.Vip,
        };

        public static ECCLevelTheme ChapterTheme(int chapter)
        {
            return chapter % Places.Length == 2 ? ECCLevelTheme.Rain : ECCLevelTheme.City;
        }

        public static bool IsNightChapter(int chapter)
        {
            return chapter % Places.Length == 1;
        }

        public static string ChapterTitle(int chapter)
        {
            int round = chapter / Places.Length;

            return Places[chapter % Places.Length] + " · " + Ranks[Mathf.Min(round, Ranks.Length - 1)];
        }

        public static ECCChapterData[] CreateChapters()
        {
            ECCChapterData[] chapters = new ECCChapterData[ECCLevelCatalog.ChapterCount];

            for (int chapter = 0; chapter < chapters.Length; chapter++) chapters[chapter] = CreateChapter(chapter);

            return chapters;
        }

        public static ECCChapterData CreateChapter(int chapter)
        {
            System.Random random = new System.Random(4271 * (chapter + 1));

            int count = ECCLevelCatalog.LevelsPerChapter;

            ECCLevelData[] levels = new ECCLevelData[count];
            HashSet<string> titles = new HashSet<string>();

            for (int slot = 0; slot < count; slot++)
            {
                levels[slot] = CreateLevel(chapter, slot, Pattern[slot % Pattern.Length], random, titles);
            }

            return new ECCChapterData { title = ChapterTitle(chapter), theme = ChapterTheme(chapter), levels = levels };
        }

        /// <summary>
        /// 0 at the first chapter, 1 at the last
        /// </summary>
        public static float Difficulty(int chapter, int slot)
        {
            float chapters = Mathf.Max(1, ECCLevelCatalog.ChapterCount - 1);

            return Mathf.Clamp01(chapter / chapters * 0.92f + slot / (float)ECCLevelCatalog.LevelsPerChapter * 0.08f);
        }

        static ECCLevelData CreateLevel(int chapter, int slot, ECCObjectiveType objective, System.Random random, HashSet<string> titles)
        {
            float d = Difficulty(chapter, slot);

            ECCLevelData level = new ECCLevelData
            {
                objective = objective,
                rivals = Mathf.RoundToInt(Mathf.Lerp(1, 3, d)),
                passengerMix = Mathf.Clamp01(d * 0.85f + slot * 0.015f),
                night = IsNightChapter(chapter) || slot == 7,
                copSpawnMultiplier = 1,
            };

            // The very first levels: learn to stop without rivals
            if (chapter == 0 && slot < 2) level.rivals = 0;

            // Seconds a passenger takes, on average, from the first sight to the destination ( more on the wet road )
            float perPassenger = Mathf.Lerp(42, 32, d) * (ChapterTheme(chapter) == ECCLevelTheme.Rain ? 1.15f : 1);

            switch (objective)
            {
                case ECCObjectiveType.Passengers:
                    level.target = 3 + Mathf.RoundToInt(d * 7);
                    level.timeLimit = chapter == 0 && slot == 0 ? 0 : Mathf.Round(30 + level.target * perPassenger * (slot >= 5 ? 0.9f : 1.1f));
                    break;

                case ECCObjectiveType.PerfectStops:
                    level.target = 2 + Mathf.RoundToInt(d * 6);
                    level.timeLimit = Mathf.Round(60 + level.target * perPassenger * 0.75f);
                    break;

                case ECCObjectiveType.RaceRivals:
                    level.target = 3 + Mathf.RoundToInt(d * 5);
                    level.rivals = Mathf.Max(1, level.rivals + (slot >= 8 ? 1 : 0));
                    break;

                case ECCObjectiveType.Earn:
                    level.target = Mathf.RoundToInt(Mathf.Lerp(120, 650, d) / 10) * 10;
                    level.timeLimit = Mathf.Round(50 + level.target * Mathf.Lerp(1.1f, 0.75f, d));
                    break;

                case ECCObjectiveType.CleanRecord:
                    level.target = 3 + Mathf.RoundToInt(d * 5);
                    level.rivals = Mathf.Min(level.rivals, 1);
                    break;

                case ECCObjectiveType.SmoothRide:
                    level.target = 3 + Mathf.RoundToInt(d * 4);
                    level.passengerMix = Mathf.Max(level.passengerMix, 0.5f);
                    break;

                case ECCObjectiveType.Vip:
                    level.target = 1 + Mathf.RoundToInt(d * 2) + (slot >= 9 ? 1 : 0);
                    level.rivals = Mathf.Max(2, level.rivals + (slot >= 9 ? 1 : 0));
                    level.vipInterval = Mathf.Lerp(30, 22, d);
                    break;
            }

            level.rivals = Mathf.Clamp(level.rivals, 0, 5);

            level.reward = ECCEconomyConfig.Instance.GetLevelReward(chapter, slot, slot == ECCLevelCatalog.LevelsPerChapter - 1);

            level.title = Title(objective, random, titles);

            return level;
        }

        static string Title(ECCObjectiveType objective, System.Random random, HashSet<string> titles)
        {
            string[] names;

            switch (objective)
            {
                case ECCObjectiveType.PerfectStops: names = new[] { "ترمز دقیق", "وسط دایره", "توقف حرفه‌ای", "ترمز طلایی" }; break;
                case ECCObjectiveType.RaceRivals: names = new[] { "رقابت با تاکسی‌ها", "کی زودتر؟", "جنگ مسافر", "مسابقه‌ی خیابون", "اولین تاکسی" }; break;
                case ECCObjectiveType.Earn: names = new[] { "کرایه‌ی روز", "جیب پر", "روز پرکار", "انعام خوب" }; break;
                case ECCObjectiveType.CleanRecord: names = new[] { "رانندگی قانونی", "بدون جریمه", "پلیس راضی", "چراغ سبز" }; break;
                case ECCObjectiveType.SmoothRide: names = new[] { "سفر آرام", "مسافر حساس", "بدون تکون", "رانندگی نرم" }; break;
                case ECCObjectiveType.Vip: names = new[] { "مسافر VIP", "مهمون ویژه", "ستاره‌ی شهر", "فرش قرمز", "مسافر طلایی" }; break;
                default: names = new[] { "اولین مسافرها", "صبح شلوغ", "سرویس شهر", "ساعت اوج", "مسافرکشی", "خط ویژه" }; break;
            }

            int first = random.Next(names.Length);

            for (int index = 0; index < names.Length; index++)
            {
                string name = names[(first + index) % names.Length];

                if (titles.Add(name)) return name;
            }

            return names[first];
        }
    }
}
