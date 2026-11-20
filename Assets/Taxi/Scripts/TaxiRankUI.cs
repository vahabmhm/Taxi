using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The «رتبه‌ی راننده» popup ( opened from the rank line under the menu title ), on the shared popup card: the
    /// driver's rating and rides, the way to the next rank, and a tile for every rank with what it needs and what it
    /// gives ( «رتبه‌ی فعلی» / «باز شده» / «قفل» )
    /// </summary>
    public class TaxiRankUI : MonoBehaviour
    {
        const float Width = 660;
        const float ListHeight = 520;
        const float RowHeight = 112;
        const float RowGap = 12;

        static TaxiRankUI open;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        /// <summary>
        /// The rank line of the main menu, like «رتبه‌ی تو: راننده‌ی شهر · امتیاز ۴٫۳ از ۵»
        /// </summary>
        public static string MenuLine()
        {
            string line = "رتبه‌ی تو: " + TaxiDriverRank.CurrentRank.title;

            if (TaxiDriverRank.Rides == 0) return line + " · هنوز سفری نداری";

            return line + " · امتیاز " + TaxiDriverRank.FormatRating(TaxiDriverRank.Rating) + " از ۵";
        }

        public static void Open()
        {
            if (open) return;

            Color gold = ECCCarTheme.Hex(0xE0A100);

            string subtitle = TaxiDriverRank.Rides == 0 ? "هر مسافری که برسونی بهت امتیاز می‌ده" : ECCUIKit.PersianDigits(TaxiDriverRank.Rides) + " سفر · امتیاز " + TaxiDriverRank.FormatRating(TaxiDriverRank.Rating) + " از ۵ ( " + ECCUIKit.PersianDigits(TaxiDriverRank.RatingWindow) + " سفر آخر )";

            ECCResultCard card = ECCPopupKit.Create("CanvasDriverRank", 70, "رتبه‌ی راننده", gold, "Icons/crown", subtitle, false, delegate { if (open) open.Close(); }, Width);

            if (TaxiDriverRank.IsMax) card.Progress("به بالاترین رتبه رسیدی!", 1, gold);
            else card.Progress(TaxiDriverRank.NextGoal(), TaxiDriverRank.Progress, gold);

            RectTransform listRow = card.CustomRow(ListHeight);

            open = card.Panel.gameObject.AddComponent<TaxiRankUI>();

            RectTransform content = ECCScrollList.Create(listRow, Vector2.zero, new Vector2(card.ContentWidth, ListHeight));

            float rowWidth = card.ContentWidth - 34;

            for (int index = 0; index < TaxiDriverRank.Ranks.Length; index++) CreateRow(content, index, rowWidth, -8 - index * (RowHeight + RowGap), gold);

            ECCScrollList.SetHeight(content, 24 + TaxiDriverRank.Ranks.Length * (RowHeight + RowGap));

            card.Finish(false);
        }

        static void CreateRow(RectTransform content, int index, float width, float y, Color gold)
        {
            Vector2 right = new Vector2(1, 0.5f);
            Vector2 left = new Vector2(0, 0.5f);

            TaxiDriverRank.Rank rank = TaxiDriverRank.Ranks[index];

            bool current = index == TaxiDriverRank.Current;
            bool reached = index <= TaxiDriverRank.Current;

            Color accent = current ? ECCCarTheme.CandyGreen : (reached ? gold : ECCCarTheme.CandyGray);

            Image tile = ECCPopupKit.Tile("Rank" + index, content, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(width, RowHeight), accent);
            Transform row = tile.transform;

            ECCPopupKit.IconDisc(row, right, new Vector2(-62, 2), 72, accent, reached ? "Icons/crown" : "Icons/lock");

            ECCPopupKit.TileText(row, "Title", rank.title, 24, right, new Vector2(-110, 30), new Vector2(300, 34), ECCCarTheme.CardInk, true);

            string needs = index == 0 ? "از اول بازی" : ECCUIKit.PersianDigits(rank.rides) + " سفر · امتیاز " + TaxiDriverRank.FormatRating(rank.rating) + " یا بیشتر";

            if (rank.fareBonus > 0) needs += " · کرایه +" + ECCUIKit.PersianDigits(Mathf.RoundToInt(rank.fareBonus * 100)) + "٪";

            ECCPopupKit.TileText(row, "Needs", needs, 17, right, new Vector2(-110, 0), new Vector2(330, 26), ECCCarTheme.CardSubInk);
            ECCPopupKit.TileText(row, "Perk", rank.perk, 17, right, new Vector2(-110, -28), new Vector2(330, 26), reached ? ECCCarTheme.CandyGreenShade : ECCCarTheme.CardSubInk);

            ECCPopupKit.Chip(row, "State", current ? "رتبه‌ی فعلی" : (reached ? "باز شده" : "قفل"), accent, left, new Vector2(16, 0), new Vector2(112, 34), 17);
        }

        void OnDestroy()
        {
            if (open == this) open = null;
        }

        void Close()
        {
            if (open == this) open = null;

            ECCUIKit.ClosePopup(this);
        }
    }
}
