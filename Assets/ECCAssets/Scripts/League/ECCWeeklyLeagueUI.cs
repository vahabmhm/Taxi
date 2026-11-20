using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessCarChase
{
    /// <summary>
    /// The «لیگ هفتگی» screen, on the shared popup card: a purple ribbon with a crown, the time left until Friday 24:00,
    /// a short explanation, the player's stars and rank, and a scroll list of the week's table ( rank, avatar, nickname,
    /// stars ). Gold / silver / bronze for the top three, the player's row («شما») highlighted and scrolled into view.
    /// Opening it never shows another popup or an ad.
    /// </summary>
    public class ECCWeeklyLeagueUI : MonoBehaviour
    {
        const float Width = 660;
        const float ListHeight = 560;
        const float RowHeight = 86;
        const float RowGap = 10;

        public static readonly Color LeagueColor = ECCCarTheme.Hex(0x6C4CF1);

        static readonly Color Gold = ECCCarTheme.Hex(0xF4B400);
        static readonly Color Silver = ECCCarTheme.Hex(0xA9B4C4);
        static readonly Color Bronze = ECCCarTheme.Hex(0xCD7F45);
        static readonly Color PlayerColor = ECCCarTheme.CandyGreen;

        static ECCWeeklyLeagueUI open;

        RectTransform content;
        ScrollRect scroll;
        Text summary;
        float rowWidth;

        public static bool IsOpen
        {
            get { return open != null; }
        }

        public static void Open()
        {
            if (open) return;

            // Never shown while locked
            if (ECCWeeklyLeague.IsUnlocked == false)
            {
                OpenLocked();
                return;
            }

            ECCWeeklyLeague.Ensure();
            ECCWeeklyLeague.MarkOpened();

            ECCResultCard card = ECCPopupKit.Create("CanvasWeeklyLeague", 70, "لیگ هفتگی", LeagueColor, "Icons/crown", ECCWeeklyLeague.TimeLeftText(), false, delegate { if (open) open.Close(); }, Width);

            card.Line("هر ستاره‌ای که این هفته بگیری به امتیازت اضافه می‌شه. جمعه شب ساعت ۲۴ امتیازها صفر می‌شن.", 19, 56, ECCCarTheme.CardSubInk).horizontalOverflow = HorizontalWrapMode.Wrap;

            open = card.Panel.gameObject.AddComponent<ECCWeeklyLeagueUI>();

            open.summary = card.Line(string.Empty, 24, 38, LeagueColor);

            RectTransform listRow = card.CustomRow(ListHeight);

            open.rowWidth = card.ContentWidth - 34;
            open.content = ECCScrollList.Create(listRow, Vector2.zero, new Vector2(card.ContentWidth, ListHeight));
            open.scroll = open.content.GetComponentInParent<ScrollRect>();

            if (open.scroll == null && open.content.parent != null && open.content.parent.parent != null) open.scroll = open.content.parent.parent.GetComponent<ScrollRect>();

            if (ECCLeagueConfig.Instance.showOfflineNote) card.Line("این لیگ یک چالش آفلاین است.", 16, 26, ECCCarTheme.CardSubInk);

            ECCWeeklyLeague.Changed += open.Refresh;

            open.Refresh();

            card.Finish(false);
        }

        /// <summary>
        /// The small locked-league info popup: when the league opens, and a single «باشه»
        /// </summary>
        public static void OpenLocked()
        {
            if (ECCModalTracker.AnyOpen) return;

            ECCResultCard card = ECCPopupKit.Create("CanvasLeagueLocked", 70, "لیگ هفتگی", LeagueColor, "Icons/lock", null, false, null, 560);

            card.Line(ECCWeeklyLeague.LockedText, 22, 70, ECCCarTheme.CardInk).horizontalOverflow = HorizontalWrapMode.Wrap;

            card.Space(6);

            Button ok = card.WideButton("ButtonOk", "باشه", LeagueColor, null, 84, 30);
            ok.onClick.AddListener(delegate { ECCUIKit.ClosePopup(ok); });

            // The corner X closes it too
            foreach (Button button in card.Panel.GetComponentsInChildren<Button>(true))
            {
                if (button != ok) button.onClick.AddListener(delegate { ECCUIKit.ClosePopup(ok); });
            }

            card.Finish(false);
        }

        void OnDestroy()
        {
            ECCWeeklyLeague.Changed -= Refresh;

            if (open == this) open = null;
        }

        void Close()
        {
            if (open == this) open = null;

            ECCUIKit.ClosePopup(this);
        }

        void Refresh()
        {
            if (this == null || content == null)
            {
                ECCWeeklyLeague.Changed -= Refresh;
                return;
            }

            for (int index = content.childCount - 1; index >= 0; index--) Destroy(content.GetChild(index).gameObject);

            List<ECCLeagueEntry> table = ECCWeeklyLeague.Table();

            int playerIndex = 0;

            for (int index = 0; index < table.Count; index++)
            {
                if (table[index].isPlayer) playerIndex = index;

                CreateRow(table[index], -8 - index * (RowHeight + RowGap));
            }

            ECCScrollList.SetHeight(content, 24 + table.Count * (RowHeight + RowGap));

            ECCLeagueEntry player = table[playerIndex];

            ECCRuntimeUI.SetText(summary, "ستاره‌های این هفته‌ی تو: " + ECCUIKit.PersianDigits(player.stars) + " · رتبه " + ECCUIKit.PersianDigits(player.rank));

            ECCWeeklyLeague.MarkRankShown(player.rank);

            // Scroll so the player's row is in view ( 1 is the top )
            if (scroll && table.Count > 1)
            {
                float visibleRows = ListHeight / (RowHeight + RowGap);
                float top = Mathf.Max(0, playerIndex - visibleRows * 0.5f + 0.5f);
                float scrollable = Mathf.Max(1, table.Count - visibleRows);

                scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01(top / scrollable);
            }
        }

        static Color RankColor(int rank)
        {
            switch (rank)
            {
                case 1: return Gold;
                case 2: return Silver;
                case 3: return Bronze;
                default: return LeagueColor;
            }
        }

        void CreateRow(ECCLeagueEntry entry, float y)
        {
            Vector2 right = new Vector2(1, 0.5f);
            Vector2 left = new Vector2(0, 0.5f);

            Color accent = entry.isPlayer ? PlayerColor : (entry.rank <= 3 ? RankColor(entry.rank) : new Color(0, 0, 0, 0));

            Image tile = ECCPopupKit.Tile(entry.isPlayer ? "LeaguePlayer" : "LeagueRow_" + entry.rank, content, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(rowWidth, RowHeight), accent);
            Transform row = tile.transform;

            if (entry.isPlayer)
            {
                Image face = row.Find("Face") ? row.Find("Face").GetComponent<Image>() : null;
                if (face) face.color = ECCCarTheme.Hex(0xE6F8EC);
            }

            // Rank number in a disc on the right ( RTL: the reading starts there )
            Image rankDisc = ECCUIKit.CreateCircle("RankDisc", row, right, new Vector2(-50, 0), 58, entry.rank <= 3 ? RankColor(entry.rank) : ECCCarTheme.Hex(0xE3E8F0));
            rankDisc.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rankDisc.raycastTarget = false;

            Text rank = ECCUIKit.CreateNumberText("Rank", rankDisc.transform, ECCUIKit.PersianDigits(entry.rank), 26, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(58, 50), entry.rank <= 3 ? Color.white : ECCCarTheme.CardInk);
            ECCCarTheme.InkText(rank, entry.rank <= 3 ? Color.white : ECCCarTheme.CardInk);

            // Avatar placeholder: a crown for the top three, a person disc for the others, the player's own color
            string icon = entry.rank <= 3 ? "Icons/crown" : (entry.isPlayer ? "Icons/star" : "Icons/target");
            ECCPopupKit.IconDisc(row, right, new Vector2(-122, 2), 56, entry.isPlayer ? PlayerColor : (entry.rank <= 3 ? RankColor(entry.rank) : LeagueColor), icon);

            ECCPopupKit.TileText(row, "Name", entry.isPlayer ? "شما" : entry.name, 24, right, new Vector2(-162, 0), new Vector2(rowWidth - 330, 40), entry.isPlayer ? ECCCarTheme.CandyGreenShade : ECCCarTheme.CardInk, entry.isPlayer);

            // Weekly stars with a star icon, on the left
            Image star = ECCCarTheme.CreateIcon("Star", row, "Icons/star", left, new Vector2(150, 2), 36, ECCRuntimeUI.StarColor);
            star.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Text stars = ECCUIKit.CreateNumberText("Stars", row, ECCUIKit.PersianDigits(entry.stars), 26, left, new Vector2(80, 0), new Vector2(100, 44), ECCCarTheme.CardInk);
            ECCCarTheme.InkText(stars, ECCCarTheme.CardInk);
        }
    }
}
