using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// A vertical scroll list for popups: rows are placed by hand ( top to bottom ) inside the returned content
    /// </summary>
    public static class ECCScrollList
    {
        public static RectTransform Create(RectTransform panel, Vector2 position, Vector2 size)
        {
            Vector2 topCenter = new Vector2(0.5f, 1);

            RectTransform scrollArea = ECCRuntimeUI.CreateRect("Scroll", panel, topCenter, position, size);

            ScrollRect scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40;

            RectTransform viewport = ECCRuntimeUI.CreateRect("Viewport", scrollArea, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(24, 0);
            viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();

            Image dragArea = viewport.gameObject.AddComponent<Image>();
            dragArea.color = new Color(0, 0, 0, 0.001f);

            RectTransform content = ECCRuntimeUI.CreateRect("Content", viewport, topCenter, Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = topCenter;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.verticalScrollbar = ECCOtherGamesUI.CreateScrollbar(scrollArea);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

            return content;
        }

        /// <summary>
        /// Sets the scrollable height of a list's content
        /// </summary>
        public static void SetHeight(RectTransform content, float height)
        {
            content.sizeDelta = new Vector2(0, height);
        }

        /// <summary>
        /// A light gray rounded row tile with its own ink colors ( the card styling leaves it as it is )
        /// </summary>
        public static Image CreateTile(string name, RectTransform content, float y, Vector2 size)
        {
            Image tile = ECCRuntimeUI.CreateImage(name, content, new Vector2(0.5f, 1), new Vector2(0, y), size, ECCCarTheme.Hex(0xF1F4F9));
            tile.rectTransform.pivot = new Vector2(0.5f, 1);
            ECCCarTheme.Round(tile);
            tile.gameObject.AddComponent<ECCKeepColors>();

            Shadow lip = tile.gameObject.AddComponent<Shadow>();
            lip.effectColor = ECCCarTheme.Hex(0xD3D9E4);
            lip.effectDistance = new Vector2(0, -5);

            return tile;
        }
    }

    /// <summary>
    /// The «دستاوردها» screen, on the shared popup card: a gold ribbon and trophy, the overall progress, and a scroll
    /// list of tiles ( icon, name, goal, progress bar, coin reward and claim button: «در حال انجام» / «دریافت جایزه» /
    /// «دریافت شده» ). Ready ones come first
    /// </summary>
    public class ECCAchievementsUI : MonoBehaviour
    {
        const float Width = 660;
        const float ListHeight = 560;
        const float RowHeight = 122;
        const float RowGap = 14;

        static readonly Color Gold = ECCCarTheme.Hex(0xE0A100);

        static ECCAchievementsUI open;

        RectTransform content;
        float rowWidth;

        public static void Open()
        {
            if (open) return;

            int claimed = 0;
            foreach (ECCAchievement achievement in ECCAchievements.All) if (ECCAchievements.IsClaimed(achievement)) claimed++;

            ECCResultCard card = ECCPopupKit.Create("CanvasAchievements", 70, "دستاوردها", Gold, "Icons/trophy", "هدف‌های بزرگ و همیشگی؛ هر کدوم یه جایزه‌ی سکه‌ای داره", true, delegate { if (open) open.Close(); }, Width);

            card.Progress(ECCUIKit.PersianDigits(claimed) + " از " + ECCUIKit.PersianDigits(ECCAchievements.All.Length) + " دستاورد گرفته شده", claimed / (float)ECCAchievements.All.Length, Gold);

            RectTransform listRow = card.CustomRow(ListHeight);

            open = card.Panel.gameObject.AddComponent<ECCAchievementsUI>();
            open.rowWidth = card.ContentWidth - 34;
            open.content = ECCScrollList.Create(listRow, Vector2.zero, new Vector2(card.ContentWidth, ListHeight));

            ECCAchievements.Changed += open.Refresh;

            open.Refresh();

            card.Finish(false);
        }

        void OnDestroy()
        {
            ECCAchievements.Changed -= Refresh;

            if (open == this) open = null;
        }

        void Close()
        {
            if (open == this) open = null;

            ECCUIKit.ClosePopup(this);
        }

        void Refresh()
        {
            // Closed already ( the event can come in the same frame the screen is destroyed )
            if (this == null || content == null)
            {
                ECCAchievements.Changed -= Refresh;
                return;
            }

            for (int index = content.childCount - 1; index >= 0; index--) Destroy(content.GetChild(index).gameObject);

            List<ECCAchievement> sorted = new List<ECCAchievement>(ECCAchievements.All);

            // Ready to claim first, then the ones in progress ( closest first ), the claimed ones last
            sorted.Sort((a, b) =>
            {
                int rank = Rank(a).CompareTo(Rank(b));

                if (rank != 0) return rank;

                return Fraction(b).CompareTo(Fraction(a));
            });

            for (int index = 0; index < sorted.Count; index++) CreateRow(sorted[index], -8 - index * (RowHeight + RowGap));

            ECCScrollList.SetHeight(content, 24 + sorted.Count * (RowHeight + RowGap));
        }

        static int Rank(ECCAchievement achievement)
        {
            if (ECCAchievements.CanClaim(achievement)) return 0;

            return ECCAchievements.IsClaimed(achievement) ? 2 : 1;
        }

        static float Fraction(ECCAchievement achievement)
        {
            return Mathf.Clamp01(ECCAchievements.GetValue(achievement.stat) / (float)Mathf.Max(1, achievement.target));
        }

        void CreateRow(ECCAchievement achievement, float y)
        {
            Vector2 right = new Vector2(1, 0.5f);
            Vector2 left = new Vector2(0, 0.5f);

            bool claimable = ECCAchievements.CanClaim(achievement);
            bool claimed = ECCAchievements.IsClaimed(achievement);

            Color accent = claimed ? ECCCarTheme.CandyGray : (claimable ? ECCCarTheme.CandyGreen : Gold);

            Image tile = ECCPopupKit.Tile("Achievement_" + achievement.id, content, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(rowWidth, RowHeight), accent);
            Transform row = tile.transform;

            Sprite sprite = achievement.icon == "dollar" ? ECCCoinBalanceButton.DollarSprite : null;
            ECCPopupKit.IconDisc(row, right, new Vector2(-64, 4), 76, claimed ? ECCCarTheme.CandyGray : accent, achievement.icon, sprite);

            ECCPopupKit.TileText(row, "Title", achievement.title, 24, right, new Vector2(-112, 32), new Vector2(250, 34), ECCCarTheme.CardInk, true);
            ECCPopupKit.TileText(row, "Description", achievement.Text, 18, right, new Vector2(-112, 2), new Vector2(270, 28), ECCCarTheme.CardSubInk);

            int value = Mathf.Min(ECCAchievements.GetValue(achievement.stat), achievement.target);

            ECCPopupKit.Bar(row, right, new Vector2(-112, -32), new Vector2(190, 14), value / (float)Mathf.Max(1, achievement.target), accent);

            Text progress = ECCUIKit.CreateNumberText("Progress", row, ECCWallet.Format(value) + " / " + ECCWallet.Format(achievement.target), 16, right, new Vector2(-310, -32), new Vector2(110, 24), ECCCarTheme.CardSubInk);
            progress.alignment = TextAnchor.MiddleRight;
            progress.rectTransform.pivot = new Vector2(1, 0.5f);
            ECCCarTheme.InkText(progress, ECCCarTheme.CardSubInk);

            ECCPopupKit.CoinPill(row, achievement.reward, left, new Vector2(16, 26), 128);

            string label = claimed ? "دریافت شده" : (claimable ? "دریافت جایزه" : "در حال انجام");

            Button claim = ECCPopupKit.ClaimButton(row, "Claim", label, claimable, claimed, left, new Vector2(16, -24), new Vector2(128, 50));

            if (claimable)
            {
                claim.onClick.AddListener(delegate
                {
                    claim.interactable = false;

                    int paid = ECCAchievements.Claim(achievement);

                    if (paid > 0) ECCRewardFx.ShowCoins(paid);

                    ECCMenuHub.RefreshIndicators();
                });
            }
        }
    }
}
