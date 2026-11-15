using UnityEngine;
using UnityEngine.UI;
using EndlessCarChase.Levels;

namespace EndlessCarChase
{
    /// <summary>
    /// Star chests: every chapter has three reward chests that open when enough of its stars are collected ( 10, 20 and
    /// all 30 ). Each pays coins once, through the shared wallet with a stable transaction id.
    /// The chest bar sits on the level map, under the chapter name.
    /// </summary>
    public static class ECCStarChests
    {
        public static readonly int[] Thresholds = { 10, 20, 30 };

        public const int ChestCount = 3;

        public static string ChestId(int chapter, int chest)
        {
            return "c" + (chapter + 1) + "-" + (chest + 1);
        }

        /// <summary>
        /// The coins in a chest: more for later chapters and bigger chests
        /// </summary>
        public static int Reward(int chapter, int chest)
        {
            int[] baseReward = { 150, 300, 600 };
            int[] perChapter = { 15, 30, 60 };

            int index = Mathf.Clamp(chest, 0, ChestCount - 1);

            return baseReward[index] + perChapter[index] * Mathf.Max(0, chapter);
        }

        /// <summary>
        /// Stars needed for a chest ( never more than the chapter has )
        /// </summary>
        public static int Needed(int chapter, int chest)
        {
            int maxStars = MaxStars(chapter);

            return Mathf.Min(Thresholds[Mathf.Clamp(chest, 0, ChestCount - 1)], maxStars);
        }

        static int MaxStars(int chapter)
        {
            ECCLevelCatalog catalog = ECCLevelCatalog.Instance;

            if (chapter < 0 || chapter >= catalog.chapters.Length) return 30;

            return catalog.chapters[chapter].levels.Length * 3;
        }

        public static bool IsOpened(int chapter, int chest)
        {
            string id = ChestId(chapter, chest);

            return ECCSave.Data.chestsOpened.Contains(id) || ECCWallet.IsPaid("chest:" + id);
        }

        public static bool CanOpen(int chapter, int chest)
        {
            return IsOpened(chapter, chest) == false && ECCLevelCatalog.Instance.GetChapterStars(chapter) >= Needed(chapter, chest);
        }

        /// <summary>
        /// Chests ready to open in all chapters ( for the menu's badge )
        /// </summary>
        public static int ReadyCount
        {
            get
            {
                int count = 0;
                int chapters = ECCLevelCatalog.Instance.chapters.Length;

                for (int chapter = 0; chapter < chapters; chapter++)
                {
                    // A chapter without stars has no chest to open; skip the rest of the checks
                    if (ECCLevelCatalog.Instance.GetChapterStars(chapter) < Thresholds[0]) continue;

                    for (int chest = 0; chest < ChestCount; chest++)
                    {
                        if (CanOpen(chapter, chest)) count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// Opens a chest: pays its coins once. Returns the coins paid ( 0 if it can't be opened )
        /// </summary>
        public static int Open(int chapter, int chest)
        {
            if (CanOpen(chapter, chest) == false) return 0;

            string id = ChestId(chapter, chest);
            int reward = Reward(chapter, chest);

            ECCSave.Data.chestsOpened.Add(id);
            ECCSave.Save(false);

            bool paid = ECCWallet.TryReward("chest:" + id, reward);

            ECCAchievements.Report(ECCStat.ChestsOpened, 1);

            return paid ? reward : 0;
        }
    }

    /// <summary>
    /// The three chests of the shown chapter on the level map: locked ( gray, with the stars needed ), ready ( gold and
    /// pulsing ) or opened ( a green check )
    /// </summary>
    public class ECCStarChestBar : MonoBehaviour
    {
        const float ChestSize = 70;

        int chapter = -1;
        bool chapterOpen;

        readonly Image[] faces = new Image[ECCStarChests.ChestCount];
        readonly Text[] labels = new Text[ECCStarChests.ChestCount];
        readonly Image[] marks = new Image[ECCStarChests.ChestCount];
        readonly Button[] buttons = new Button[ECCStarChests.ChestCount];

        public static ECCStarChestBar Create(Transform parent, Vector2 position, float width)
        {
            RectTransform root = ECCRuntimeUI.CreateRect("StarChests", parent, new Vector2(0.5f, 1), position, new Vector2(width, ChestSize + 10));

            ECCStarChestBar bar = root.gameObject.AddComponent<ECCStarChestBar>();
            bar.Build(root, width);

            return bar;
        }

        void Build(RectTransform root, float width)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);

            Text title = ECCRuntimeUI.CreateText("Title", root, "صندوق‌های ستاره", 22, new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(190, 34), ECCCarTheme.CardInk);
            title.alignment = TextAnchor.MiddleRight;
            title.rectTransform.pivot = new Vector2(1, 0.5f);
            ECCCarTheme.InkText(title, ECCCarTheme.CardInk);

            for (int chest = 0; chest < ECCStarChests.ChestCount; chest++)
            {
                // Right to left: the first chest next to the title
                float x = width * 0.5f - 250 - chest * (ChestSize + 70);

                Image face = ECCRuntimeUI.CreateImage("Chest" + (chest + 1), root, center, new Vector2(x, 4), new Vector2(ChestSize, ChestSize), ECCCarTheme.CandyGray);
                ECCCarTheme.Round(face);
                face.gameObject.AddComponent<ECCKeepColors>();

                Shadow lip = face.gameObject.AddComponent<Shadow>();
                lip.effectColor = new Color(0, 0, 0, 0.25f);
                lip.effectDistance = new Vector2(0, -5);

                ECCCarTheme.CreateIcon("Icon", face.transform, "Icons/gift", center, new Vector2(0, 4), ChestSize * 0.6f, Color.white).rectTransform.pivot = center;

                Image mark = ECCCarTheme.CreateIcon("Opened", face.transform, "Icons/check", new Vector2(1, 0), new Vector2(-4, 4), 30, ECCCarTheme.CandyGreen);
                mark.rectTransform.pivot = center;

                Text label = ECCUIKit.CreateNumberText("Needed", root, string.Empty, 17, center, new Vector2(x, -ChestSize * 0.5f - 6), new Vector2(120, 24), ECCCarTheme.CardSubInk);
                ECCCarTheme.InkText(label, ECCCarTheme.CardSubInk);

                Button button = face.gameObject.AddComponent<Button>();
                button.targetGraphic = face;
                button.transition = Selectable.Transition.None;

                int index = chest;
                button.onClick.AddListener(delegate { OnChest(index); });

                face.gameObject.AddComponent<ECCPressScale>();

                faces[chest] = face;
                labels[chest] = label;
                marks[chest] = mark;
                buttons[chest] = button;
            }
        }

        public void Show(int shownChapter, bool open)
        {
            chapter = shownChapter;
            chapterOpen = open;

            Refresh();
        }

        void Refresh()
        {
            for (int chest = 0; chest < ECCStarChests.ChestCount; chest++)
            {
                bool opened = ECCStarChests.IsOpened(chapter, chest);
                bool ready = chapterOpen && ECCStarChests.CanOpen(chapter, chest);

                faces[chest].color = opened ? ECCCarTheme.Hex(0xC9CED8) : (ready ? ECCCarTheme.Yellow : ECCCarTheme.CandyGray);
                marks[chest].gameObject.SetActive(opened);

                ECCPulse.Set(faces[chest], ready, 0.08f);

                string text = opened ? "باز شد" : ECCUIKit.PersianDigits(ECCStarChests.Needed(chapter, chest)) + " ستاره";
                ECCRuntimeUI.SetText(labels[chest], text);
            }
        }

        void OnChest(int chest)
        {
            if (chapter < 0) return;

            if (ECCStarChests.IsOpened(chapter, chest))
            {
                ECCToast.Show("این صندوق رو قبلاً باز کردی", ECCRuntimeUI.AccentColor);
                return;
            }

            if (chapterOpen == false || ECCStarChests.CanOpen(chapter, chest) == false)
            {
                int stars = ECCLevelCatalog.Instance.GetChapterStars(chapter);
                int needed = ECCStarChests.Needed(chapter, chest);

                ECCToast.Show(ECCUIKit.PersianDigits(Mathf.Max(0, needed - stars)) + " ستاره‌ی دیگه از این فصل لازم داری", ECCRuntimeUI.AccentColor, "جایزه: " + ECCWallet.Format(ECCStarChests.Reward(chapter, chest)) + " سکه");
                return;
            }

            buttons[chest].interactable = false;

            int paid = ECCStarChests.Open(chapter, chest);

            buttons[chest].interactable = true;

            if (paid > 0)
            {
                ECCToast.Show("صندوق جایزه باز شد!", ECCRuntimeUI.GoodColor, "+" + ECCWallet.Format(paid) + " سکه");
                ECCRewardFx.ShowCoins(paid);
                ECCRewardFx.Confetti();
                ECCPunchScale.Play(faces[chest]);
            }

            ECCMenuHub.RefreshIndicators();

            Refresh();
        }
    }
}
