using System.Collections.Generic;
using UnityEngine;

namespace EndlessCarChase.Levels
{
    /// <summary>
    /// The list of chapters and levels of the levels mode.
    /// The game uses the asset at Resources/ECCLevelCatalog if one exists, otherwise the default levels written below.
    /// To edit the levels in the Inspector: Assets > Create > Endless Car Chase > Level Catalog, save it as
    /// Resources/ECCLevelCatalog, then use "Fill With Default Levels" from the component's context menu as a starting point.
    /// </summary>
    [CreateAssetMenu(fileName = "ECCLevelCatalog", menuName = "Endless Car Chase/Level Catalog")]
    public class ECCLevelCatalog : ScriptableObject
    {
        const string ResourcePath = "ECCLevelCatalog";

        public ECCChapterData[] chapters = new ECCChapterData[0];

        static ECCLevelCatalog instance;

        List<ECCLevelData> allLevels;

        public static ECCLevelCatalog Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ECCLevelCatalog>(ResourcePath);

                    if (instance == null || instance.chapters == null || instance.chapters.Length == 0)
                    {
                        instance = CreateInstance<ECCLevelCatalog>();
                        instance.chapters = CreateDefaultChapters();
                    }

                    instance.Index();
                }

                return instance;
            }
        }

        /// <summary>
        /// All the levels of all the chapters, in play order
        /// </summary>
        public List<ECCLevelData> AllLevels
        {
            get
            {
                if (allLevels == null) Index();

                return allLevels;
            }
        }

        void Index()
        {
            allLevels = new List<ECCLevelData>();

            for (int chapter = 0; chapter < chapters.Length; chapter++)
            {
                for (int level = 0; level < chapters[chapter].levels.Length; level++)
                {
                    ECCLevelData data = chapters[chapter].levels[level];

                    data.id = "c" + (chapter + 1) + "-l" + (level + 1);
                    data.chapterIndex = chapter;
                    data.levelIndex = level;
                    data.theme = chapters[chapter].theme;

                    allLevels.Add(data);
                }
            }
        }

        public ECCLevelData GetLevel(string id)
        {
            foreach (ECCLevelData level in AllLevels)
            {
                if (level.id == id) return level;
            }

            return null;
        }

        /// <summary>
        /// The level after this one, or null if it's the last level
        /// </summary>
        public ECCLevelData GetNextLevel(ECCLevelData level)
        {
            int index = AllLevels.IndexOf(level);

            return index >= 0 && index + 1 < AllLevels.Count ? AllLevels[index + 1] : null;
        }

        /// <summary>
        /// A level is unlocked when the level before it was completed
        /// </summary>
        public bool IsUnlocked(ECCLevelData level)
        {
            int index = AllLevels.IndexOf(level);

            return index <= 0 || ECCLevelProgress.IsPassed(AllLevels[index - 1]);
        }

        /// <summary>
        /// The level the player selected, or the first level that isn't completed yet
        /// </summary>
        public ECCLevelData SelectedLevel
        {
            get
            {
                ECCLevelData selected = GetLevel(ECCSave.Data.selectedLevel);

                if (selected != null && IsUnlocked(selected)) return selected;

                foreach (ECCLevelData level in AllLevels)
                {
                    if (ECCLevelProgress.IsPassed(level) == false) return level;
                }

                return AllLevels.Count > 0 ? AllLevels[AllLevels.Count - 1] : null;
            }
            set
            {
                ECCSave.Data.selectedLevel = value != null ? value.id : string.Empty;
                ECCSave.Save();
            }
        }

        public int GetChapterStars(int chapter)
        {
            int stars = 0;

            foreach (ECCLevelData level in chapters[chapter].levels) stars += ECCSave.GetStars(level.id);

            return stars;
        }

        [ContextMenu("Fill With Default Levels")]
        void FillWithDefaultLevels()
        {
            chapters = CreateDefaultChapters();
        }

        static ECCLevelData Level(string title, ECCObjectiveType objective, int target, float timeLimit, float copSpawnMultiplier, int reward)
        {
            return new ECCLevelData
            {
                title = title,
                objective = objective,
                target = target,
                timeLimit = timeLimit,
                copSpawnMultiplier = copSpawnMultiplier,
                reward = reward,
            };
        }

        /// <summary>
        /// Adds police variants and hazards to a level
        /// </summary>
        static ECCLevelData With(ECCLevelData level, float interceptor, float rammer, float heavy, bool spikeStrips, bool roadblocks, float copSpeedBonus = 0)
        {
            level.interceptorChance = interceptor;
            level.rammerChance = rammer;
            level.heavyChance = heavy;
            level.spikeStrips = spikeStrips;
            level.roadblocks = roadblocks;
            level.copSpeedBonus = copSpeedBonus;

            return level;
        }

        /// <summary>
        /// Plays the level at night ( headlights and siren lights )
        /// </summary>
        static ECCLevelData Night(ECCLevelData level)
        {
            level.night = true;

            return level;
        }

        /// <summary>
        /// Brings the police helicopter into the level
        /// </summary>
        static ECCLevelData Helicopter(ECCLevelData level)
        {
            level.helicopter = true;

            return level;
        }

        /// <summary>
        /// The built-in levels
        /// </summary>
        public static ECCChapterData[] CreateDefaultChapters()
        {
            ECCChapterData city = new ECCChapterData
            {
                title = ChapterTitle(0),
                theme = ECCLevelTheme.City,
                levels = new ECCLevelData[]
                {
                    Level("اولین فرار", ECCObjectiveType.Survive, 30, 0, 1.4f, 100),
                    Level("پول خیابانی", ECCObjectiveType.CollectCash, 30, 90, 1.3f, 120),
                    Level("تصادف عمدی", ECCObjectiveType.DestroyCops, 8, 120, 1.1f, 140),
                    Level("تا گاراژ", ECCObjectiveType.Escape, 250, 0, 1.2f, 160),
                    Level("نیم‌دقیقه‌ی داغ", ECCObjectiveType.Survive, 45, 0, 0.9f, 180),
                    Level("کیف پول پر", ECCObjectiveType.CollectCash, 60, 100, 1, 200),
                    Level("شکارچی پلیس", ECCObjectiveType.DestroyCops, 14, 160, 0.9f, 220),
                    Level("دزدی از بانک مرکزی", ECCObjectiveType.Heist, 300, 0, 1, 250),
                    Night(Level("یک دقیقه جهنم", ECCObjectiveType.Survive, 60, 0, 0.75f, 280)),
                    Level("باس: ون زرهی", ECCObjectiveType.DestroyBoss, 1, 0, 0.9f, 400),
                },
            };

            // Desert: fast interceptors and spike strips
            ECCChapterData desert = new ECCChapterData
            {
                title = ChapterTitle(1),
                theme = ECCLevelTheme.Desert,
                levels = new ECCLevelData[]
                {
                    With(Level("گرمای جاده", ECCObjectiveType.Survive, 40, 0, 1, 200), 0.2f, 0, 0, false, false),
                    With(Level("رهگیرها", ECCObjectiveType.DestroyCops, 12, 160, 1, 220), 0.35f, 0, 0, false, false),
                    With(Level("میخ‌های جاده", ECCObjectiveType.Escape, 350, 0, 1, 240), 0.15f, 0, 0, true, false),
                    With(Level("طلای بیابان", ECCObjectiveType.CollectCash, 75, 110, 1, 260), 0.25f, 0, 0, true, false),
                    With(Level("طوفان شن", ECCObjectiveType.Survive, 60, 0, 0.85f, 280), 0.3f, 0, 0, true, false),
                    With(Level("تله‌ی میخ", ECCObjectiveType.DestroyCops, 18, 200, 0.9f, 300), 0.2f, 0, 0, true, false),
                    With(Level("مسابقه با زمان", ECCObjectiveType.Checkpoints, 5, 28, 1, 320), 0.3f, 0, 0, false, false),
                    Night(With(Level("بی‌امان", ECCObjectiveType.Survive, 75, 0, 0.75f, 350), 0.4f, 0, 0, true, false, 1)),
                    With(Level("شکار در شن", ECCObjectiveType.CollectCash, 105, 140, 0.9f, 380), 0.35f, 0, 0, true, false),
                    With(Level("باس: اتوبوس زرهی بیابان", ECCObjectiveType.DestroyBoss, 1, 0, 0.9f, 550), 0.2f, 0, 0, true, false),
                },
            };

            // Highway: fast traffic, rammers, heavy cars and roadblocks
            ECCChapterData highway = new ECCChapterData
            {
                title = ChapterTitle(2),
                theme = ECCLevelTheme.Highway,
                levels = new ECCLevelData[]
                {
                    With(Level("لاین به لاین", ECCObjectiveType.Survive, 45, 0, 1, 300), 0, 0.2f, 0, false, false),
                    With(Level("قوچ‌ها", ECCObjectiveType.DestroyCops, 14, 170, 1, 320), 0, 0.3f, 0, false, false),
                    With(Level("کمترین خسارت", ECCObjectiveType.NoDamage, 300, 0, 1.4f, 340), 0, 0.1f, 0, false, false),
                    With(Level("سکه‌های اتوبان", ECCObjectiveType.CollectCash, 90, 120, 1, 360), 0, 0.25f, 0.1f, false, false),
                    With(Level("زره‌پوش‌ها", ECCObjectiveType.Survive, 70, 0, 0.85f, 380), 0, 0.2f, 0.25f, false, false),
                    With(Level("سدشکن", ECCObjectiveType.DestroyCops, 22, 220, 0.9f, 400), 0, 0.15f, 0.2f, false, true),
                    With(Level("غارت کامیون پول", ECCObjectiveType.Heist, 450, 0, 1, 420), 0, 0.3f, 0, false, true),
                    Helicopter(Night(With(Level("همه علیه تو", ECCObjectiveType.Survive, 90, 0, 0.7f, 450), 0.2f, 0.2f, 0.2f, true, true, 1.5f))),
                    With(Level("آخرین ذخیره", ECCObjectiveType.CollectCash, 135, 160, 0.85f, 480), 0.2f, 0.2f, 0.2f, true, true),
                    With(Level("باس نهایی", ECCObjectiveType.DestroyBoss, 1, 0, 0.85f, 800), 0, 0.2f, 0.2f, false, true),
                },
            };

            List<ECCChapterData> all = new List<ECCChapterData> { city, desert, highway };

            // The rest are generated: the same rotation of places, harder every round
            for (int chapter = all.Count; chapter < ChapterCount; chapter++) all.Add(GenerateChapter(chapter));

            return all.ToArray();
        }

        // ----- Generated chapters -----

        /// <summary>
        /// Chapters in the campaign: the first three are written above, the rest are generated
        /// </summary>
        public const int ChapterCount = 30;

        public const int LevelsPerChapter = 10;

        /// <summary>
        /// The places chapters rotate through, in order. A new place ( highway, rainy road, ... ) is added here
        /// </summary>
        public static readonly ECCLevelTheme[] ThemeRotation = { ECCLevelTheme.City, ECCLevelTheme.Desert, ECCLevelTheme.Highway, ECCLevelTheme.Forest, ECCLevelTheme.Rain, ECCLevelTheme.Ruins };

        static readonly string[] RoundNames = { "شروع فرار", "رهگیرها", "قوچ‌ها", "زره‌پوش‌ها", "شب داغ", "طوفان", "تعقیب سنگین", "بی‌امان", "افسانه", "فرار نهایی" };

        public static ECCLevelTheme ChapterTheme(int chapter)
        {
            return ThemeRotation[chapter % ThemeRotation.Length];
        }

        public static string PlaceName(ECCLevelTheme theme)
        {
            switch (theme)
            {
                case ECCLevelTheme.City: return "شهر";
                case ECCLevelTheme.Desert: return "بیابان";
                case ECCLevelTheme.Forest: return "جنگل";
                case ECCLevelTheme.Rain: return "جاده‌ی بارانی";
                case ECCLevelTheme.Highway: return "اتوبان";
                case ECCLevelTheme.Ruins: return "ویرانه";
                default: return "جاده";
            }
        }

        /// <summary>
        /// Like «شهر · رهگیرها»: the place and the round ( a round is one pass through all the places )
        /// </summary>
        public static string ChapterTitle(int chapter)
        {
            int round = chapter / ThemeRotation.Length;

            return PlaceName(ChapterTheme(chapter)) + " · " + RoundNames[Mathf.Min(round, RoundNames.Length - 1)];
        }

        /// <summary>
        /// Dollars picked up per second on a collect-money level ( the road's dollars are worth 5 and 10 )
        /// </summary>
        const float CashPerSecond = 0.78f;

        /// <summary>
        /// A chapter of ten levels for a chapter number ( 0 based ), always the same for the same number.
        /// Difficulty grows with the chapter: more and faster police, tougher police variants, longer goals.
        /// The place shapes it: the desert has fast interceptors and spike strips, the forest rammers, heavy cars and
        /// roadblocks, the rainy road a slippery night with fewer spike strips and more time, the highway fast traffic,
        /// long escapes and roadblocks, the ruins heavy armored police, wrecks and spike strips, the city a mix. Level 5 is the place's own kind of level, level 10 is always the boss
        /// </summary>
        public static ECCChapterData GenerateChapter(int chapter)
        {
            ECCLevelTheme theme = ChapterTheme(chapter);

            System.Random random = new System.Random(7919 * (chapter + 1));

            // 0 at chapter 4, 1 at the last chapter
            float progress = Mathf.Clamp01((chapter - 3) / (float)(ChapterCount - 4));

            // The order of the other goals changes from chapter to chapter
            ECCObjectiveType[] pattern = { ECCObjectiveType.Survive, ECCObjectiveType.CollectCash, ECCObjectiveType.DestroyCops, ECCObjectiveType.Escape, ECCObjectiveType.Heist, ECCObjectiveType.Checkpoints, ECCObjectiveType.NoDamage };

            ECCLevelData[] levels = new ECCLevelData[LevelsPerChapter];

            // No title twice in a chapter
            HashSet<string> titles = new HashSet<string>();

            for (int slot = 0; slot < LevelsPerChapter; slot++)
            {
                ECCObjectiveType objective;

                if (slot == LevelsPerChapter - 1) objective = ECCObjectiveType.DestroyBoss;
                else if (slot == 4) objective = SignatureObjective(theme);
                else objective = pattern[(slot + chapter) % pattern.Length];

                levels[slot] = GenerateLevel(theme, objective, chapter, slot, progress, random, titles);
            }

            return new ECCChapterData { title = ChapterTitle(chapter), theme = theme, levels = levels };
        }

        static ECCObjectiveType SignatureObjective(ECCLevelTheme theme)
        {
            switch (theme)
            {
                case ECCLevelTheme.Desert: return ECCObjectiveType.Escape;
                case ECCLevelTheme.Forest: return ECCObjectiveType.DestroyCops;
                case ECCLevelTheme.Rain: return ECCObjectiveType.Survive;
                case ECCLevelTheme.Highway: return ECCObjectiveType.Escape;
                case ECCLevelTheme.Ruins: return ECCObjectiveType.DestroyCops;
                default: return ECCObjectiveType.CollectCash;
            }
        }

        static ECCLevelData GenerateLevel(ECCLevelTheme theme, ECCObjectiveType objective, int chapter, int slot, float progress, System.Random random, HashSet<string> titles)
        {
            // Within a chapter the later levels are a little harder
            float step = slot / (float)(LevelsPerChapter - 1);
            float difficulty = Mathf.Clamp01(progress * 0.9f + step * 0.1f);

            ECCLevelData level = new ECCLevelData { objective = objective };

            // On the wet road the cars slide, so the timed goals get a little more time
            float timeFactor = theme == ECCLevelTheme.Rain ? 1.15f : 1;

            switch (objective)
            {
                case ECCObjectiveType.Survive:
                    level.target = Mathf.RoundToInt(Mathf.Lerp(45, 120, difficulty) / 5) * 5;
                    break;

                case ECCObjectiveType.DestroyCops:
                    // Police cars also wreck each other, so the counts are high
                    level.target = Mathf.RoundToInt(Mathf.Lerp(14, 36, difficulty));
                    level.timeLimit = Mathf.Round((90 + level.target * 5) * timeFactor);
                    break;

                case ECCObjectiveType.CollectCash:
                    // The road's dollars are worth 5 and 10, so about 0.3 of the old amounts ( same time limits as before )
                    level.target = Mathf.RoundToInt(Mathf.Lerp(90, 270, difficulty) / 5) * 5;
                    level.timeLimit = Mathf.Round((level.target / CashPerSecond + 15) * timeFactor);
                    break;

                case ECCObjectiveType.Escape:
                    level.target = Mathf.RoundToInt(Mathf.Lerp(400, 1100, difficulty) / 10) * 10;

                    // About half the escapes are against the clock
                    level.timeLimit = random.NextDouble() < 0.5 ? Mathf.Round((level.target / 6.5f + 10) * timeFactor) : 0;
                    break;

                case ECCObjectiveType.Heist:
                    // The bag is about 40% of the way, then the escape point the full distance on
                    level.target = Mathf.RoundToInt(Mathf.Lerp(350, 900, difficulty) / 10) * 10;
                    level.timeLimit = random.NextDouble() < 0.4 ? Mathf.Round((level.target * 1.4f / 6.5f + 15) * timeFactor) : 0;
                    break;

                case ECCObjectiveType.NoDamage:
                    level.target = Mathf.RoundToInt(Mathf.Lerp(250, 650, difficulty) / 10) * 10;
                    break;

                case ECCObjectiveType.Checkpoints:
                    level.target = Mathf.RoundToInt(Mathf.Lerp(4, 9, difficulty));
                    level.timeLimit = Mathf.Round((Modes.ECCLevelMode.CheckpointDistance / 6.5f + 13) * timeFactor);
                    break;

                default:
                    level.target = 1;
                    break;
            }

            level.copSpawnMultiplier = Mathf.Max(0.6f, 1 - progress * 0.35f - step * 0.08f);
            level.copSpeedBonus = Mathf.Round(Mathf.Lerp(0, 2.5f, progress) * 10) / 10;

            // Police variants: more of them every round, more of the place's own kind
            float variants = Mathf.Lerp(0.25f, 0.7f, difficulty);

            float interceptor = 1, rammer = 1, heavy = progress > 0.15f ? 1 : 0;

            if (theme == ECCLevelTheme.Desert) interceptor = 2;
            else if (theme == ECCLevelTheme.Ruins) { heavy = progress > 0.05f ? 1.8f : 0.8f; rammer = 1.3f; }
            else if (theme == ECCLevelTheme.Forest) { rammer = 1.6f; heavy *= 1.6f; }

            float total = interceptor + rammer + heavy;

            level.interceptorChance = Round2(variants * interceptor / total);
            level.rammerChance = Round2(variants * rammer / total);
            level.heavyChance = Round2(variants * heavy / total);

            // Hazards
            double hazardChance = (0.3 + progress * 0.5) * 0.6;

            // Spike strips are hard to see in the dark rain, so the rainy road has fewer of them
            double spikeChance = theme == ECCLevelTheme.Rain ? hazardChance * 0.5 : hazardChance;

            level.spikeStrips = theme == ECCLevelTheme.Desert ? random.NextDouble() < 0.8 : (theme == ECCLevelTheme.Ruins ? random.NextDouble() < 0.6 : random.NextDouble() < spikeChance);
            // Police roadblocks are the highway's own trap
            level.roadblocks = theme == ECCLevelTheme.Forest || theme == ECCLevelTheme.Highway ? random.NextDouble() < 0.7 : random.NextDouble() < hazardChance;

            // With little damage is hard enough: fewer police, no traps
            if (objective == ECCObjectiveType.NoDamage)
            {
                level.copSpawnMultiplier = Mathf.Min(1.5f, level.copSpawnMultiplier * 1.4f);
                level.spikeStrips = false;
                level.roadblocks = false;
            }

            // The eighth level of a chapter is at night ( the rainy road always is ). From chapter 9 on, the ninth level
            // brings the police helicopter
            level.night = slot == 7 && theme != ECCLevelTheme.Rain;
            level.helicopter = slot == 8 && progress >= 0.2f && objective != ECCObjectiveType.NoDamage;

            // The boss arrives with fewer police around it
            if (objective == ECCObjectiveType.DestroyBoss)
            {
                level.copSpawnMultiplier = Mathf.Max(0.75f, level.copSpawnMultiplier + 0.1f);
                level.spikeStrips = false;
            }

            level.reward = ECCEconomyConfig.Instance.GetLevelReward(chapter, slot, objective == ECCObjectiveType.DestroyBoss);

            level.title = LevelTitle(theme, objective, random, titles);

            return level;
        }

        static float Round2(float value)
        {
            return Mathf.Round(value * 100) / 100;
        }

        static string LevelTitle(ECCLevelTheme theme, ECCObjectiveType objective, System.Random random, HashSet<string> titles)
        {
            string place = PlaceName(theme);

            string[] names;

            switch (objective)
            {
                case ECCObjectiveType.DestroyCops:
                    names = new[] { "شکار پلیس", "تصادف پشت تصادف", "دام برای پلیس", "ضربه‌ی کاری", "پلیس‌ها در " + place, "خراب‌کاری" };
                    break;

                case ECCObjectiveType.CollectCash:
                    names = new[] { "پول‌های " + place, "جیب پر", "گنج " + place, "دلارهای پراکنده", "غارت سریع", "صندوق باز" };
                    break;

                case ECCObjectiveType.Escape:
                    names = new[] { "فرار از " + place, "راه خروج", "تا مخفیگاه", "مسیر امن", "فرار بزرگ", "آخرین خروجی" };
                    break;

                case ECCObjectiveType.Heist:
                    names = new[] { "دزدی و فرار", "سرقت از بانک " + place, "کیف پول طلایی", "غارت شبانه", "دستبرد بزرگ", "گاوصندوق " + place };
                    break;

                case ECCObjectiveType.NoDamage:
                    names = new[] { "کمترین خسارت", "رانندگی تمیز", "خسارت کم", "ماشین سالم", "با احتیاط تا " + place, "بدنه‌ی سالم" };
                    break;

                case ECCObjectiveType.Checkpoints:
                    names = new[] { "مسابقه با زمان", "چک‌پوینت‌های " + place, "ثانیه‌شمار", "تند و پشت سر هم", "رالی " + place, "عقربه‌ها" };
                    break;

                case ECCObjectiveType.DestroyBoss:
                    names = new[] { "باس: اتوبوس زرهی", "باس: غول آهنی " + place, "باس: فرمانده‌ی پلیس", "باس: دژ متحرک" };
                    break;

                default:
                    names = new[] { "زنده بمان", "نفس‌گیر", "تعقیب بی‌پایان", "مقاومت در " + place, "زیر آتش", "دقیقه‌های داغ" };
                    break;
            }

            // A name not used yet in this chapter ( the lists are longer than the goals of one kind in a chapter )
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

